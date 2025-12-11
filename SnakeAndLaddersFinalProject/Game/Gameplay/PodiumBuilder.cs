using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Properties.Langs;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public sealed class PodiumBuilder
    {

        private const int FIRST_PODIUM_POSITION = 1;
        private const int MAX_PODIUM_PLAYERS = 3;
        private const int INVALID_USER_ID = 0;
        private const int DEFAULT_PODIUM_REWARD = 0;
        private const int TOKENS_COUNT_NOT_AVAILABLE = -1;

        private const string LOG_PREFIX = "PodiumBuilder.BuildPodium: ";
        private const string LOG_STATE_NULL = LOG_PREFIX + "stateResponse is null.";
        private const string LOG_GAME_NOT_FINISHED = LOG_PREFIX + "game is not finished.";
        private const string LOG_TOKENS_EMPTY = LOG_PREFIX + "no tokens available in state.";
        private const string LOG_NO_MEMBERS = LOG_PREFIX + "no members from corner players.";
        private const string LOG_NO_PODIUM_PLAYERS = LOG_PREFIX + "no podium players were generated.";

        private readonly string _unknownWinnerName = Lang.PodiumUnknownWinnerNameText;
        private readonly ILog _logger;

        public PodiumBuilder(ILog logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PodiumViewModel BuildPodium(
            GetGameStateResponseDto stateResponse,
            CornerPlayersViewModel cornerPlayers)
        {
            if (stateResponse == null)
            {
                _logger.Warn(LOG_STATE_NULL);
                return BuildEmptyPodium();
            }

            LogStateInfo(stateResponse);

            if (!stateResponse.IsFinished)
            {
                _logger.Info(LOG_GAME_NOT_FINISHED);
                return BuildEmptyPodium();
            }

            if (stateResponse.Tokens == null || stateResponse.Tokens.Length == 0)
            {
                _logger.Warn(LOG_TOKENS_EMPTY);
                return BuildEmptyPodium();
            }

            List<LobbyMemberViewModel> allMembers = BuildMembersFromCornerPlayers(cornerPlayers);

            if (allMembers.Count == 0)
            {
                _logger.Warn(LOG_NO_MEMBERS);
                return BuildEmptyPodium();
            }

            List<PodiumPlayerViewModel> podiumPlayers =
                BuildPodiumPlayers(stateResponse, allMembers);

            if (podiumPlayers.Count == 0)
            {
                _logger.Warn(LOG_NO_PODIUM_PLAYERS);
                return BuildEmptyPodium();
            }

            int winnerUserId = ResolveWinnerUserId(stateResponse, podiumPlayers);
            string winnerName = ResolveWinnerName(winnerUserId, podiumPlayers, allMembers);

            return BuildPodiumViewModel(winnerUserId, winnerName, podiumPlayers);
        }

        private static PodiumViewModel BuildPodiumViewModel(
            int winnerUserId,
            string winnerName,
            List<PodiumPlayerViewModel> podiumPlayers)
        {
            var podium = new PodiumViewModel();
            podium.Initialize(
                winnerUserId,
                winnerName,
                podiumPlayers.AsReadOnly());

            return podium;
        }

        private PodiumViewModel BuildEmptyPodium()
        {
            return BuildPodiumViewModel(
                INVALID_USER_ID,
                _unknownWinnerName,
                new List<PodiumPlayerViewModel>());
        }

        private static List<LobbyMemberViewModel> BuildMembersFromCornerPlayers(
            CornerPlayersViewModel cornerPlayers)
        {
            var members = new List<LobbyMemberViewModel>();

            if (cornerPlayers == null)
            {
                return members;
            }

            if (cornerPlayers.TopLeftPlayer != null)
            {
                members.Add(cornerPlayers.TopLeftPlayer);
            }

            if (cornerPlayers.TopRightPlayer != null)
            {
                members.Add(cornerPlayers.TopRightPlayer);
            }

            if (cornerPlayers.BottomLeftPlayer != null)
            {
                members.Add(cornerPlayers.BottomLeftPlayer);
            }

            if (cornerPlayers.BottomRightPlayer != null)
            {
                members.Add(cornerPlayers.BottomRightPlayer);
            }

            return members
                .GroupBy(member => member.UserId)
                .Select(group => group.First())
                .ToList();
        }

        private static List<PodiumPlayerViewModel> BuildPodiumPlayers(
            GetGameStateResponseDto stateResponse,
            List<LobbyMemberViewModel> allMembers)
        {
            var orderedTokens = stateResponse.Tokens
                .OrderByDescending(token => token.CellIndex)
                .ToList();

            var podiumPlayers = new List<PodiumPlayerViewModel>();
            int position = FIRST_PODIUM_POSITION;

            foreach (TokenStateDto token in orderedTokens)
            {
                LobbyMemberViewModel member =
                    allMembers.FirstOrDefault(m => m.UserId == token.UserId);

                if (member == null)
                {
                    continue;
                }

                var podiumPlayer = new PodiumPlayerViewModel(
                    member.UserId,
                    member.UserName,
                    position,
                    DEFAULT_PODIUM_REWARD,
                    member.SkinImagePath);

                podiumPlayers.Add(podiumPlayer);

                position++;

                if (position > MAX_PODIUM_PLAYERS)
                {
                    break;
                }
            }

            return podiumPlayers;
        }

        private static int ResolveWinnerUserId(
            GetGameStateResponseDto stateResponse,
            List<PodiumPlayerViewModel> podiumPlayers)
        {
            if (stateResponse.WinnerUserId > INVALID_USER_ID)
            {
                return stateResponse.WinnerUserId;
            }

            if (podiumPlayers.Count == 0)
            {
                return INVALID_USER_ID;
            }

            return podiumPlayers[0].UserId;
        }

        private string ResolveWinnerName(
            int winnerUserId,
            List<PodiumPlayerViewModel> podiumPlayers,
            List<LobbyMemberViewModel> allMembers)
        {
            if (winnerUserId <= INVALID_USER_ID)
            {
                return _unknownWinnerName;
            }

            LobbyMemberViewModel cornerMember =
                allMembers.FirstOrDefault(member => member.UserId == winnerUserId);

            if (cornerMember != null && !string.IsNullOrWhiteSpace(cornerMember.UserName))
            {
                return cornerMember.UserName;
            }

            PodiumPlayerViewModel podiumMember =
                podiumPlayers.FirstOrDefault(player => player.UserId == winnerUserId);

            if (podiumMember != null && !string.IsNullOrWhiteSpace(podiumMember.UserName))
            {
                return podiumMember.UserName;
            }

            return _unknownWinnerName;
        }
        private void LogStateInfo(GetGameStateResponseDto stateResponse)
        {
            int tokensCount =
                stateResponse.Tokens == null
                    ? TOKENS_COUNT_NOT_AVAILABLE
                    : stateResponse.Tokens.Length;

            _logger.InfoFormat(
                LOG_PREFIX + "IsFinished={0}, Tokens={1}, WinnerUserId={2}",
                stateResponse.IsFinished,
                tokensCount,
                stateResponse.WinnerUserId);
        }
    }
}
