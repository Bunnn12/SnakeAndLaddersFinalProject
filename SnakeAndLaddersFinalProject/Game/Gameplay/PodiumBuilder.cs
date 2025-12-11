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
        private readonly string _unknownWinnerName = Lang.PodiumUnknownWinnerNameText;
        private const int FIRST_PODIUM_POSITION = 1;
        private const int MAX_PODIUM_PLAYERS = 3;
        private const int INVALID_USER_ID = 0;

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
                _logger.Warn("PodiumBuilder.BuildPodium: stateResponse is null.");
                return null;
            }

            _logger.InfoFormat(
                "PodiumBuilder.BuildPodium: IsFinished={0}, Tokens={1}, WinnerUserId={2}",
                stateResponse.IsFinished,
                stateResponse.Tokens == null ? -1 : stateResponse.Tokens.Length,
                stateResponse.WinnerUserId);

            if (!stateResponse.IsFinished)
            {
                _logger.Info("PodiumBuilder.BuildPodium: the game is not finished.");
                return null;
            }

            if (stateResponse.Tokens == null || stateResponse.Tokens.Length == 0)
            {
                _logger.Warn("PodiumBuilder.BuildPodium: there are no tokens in state.");
                return null;
            }

            List<LobbyMemberViewModel> allMembers = BuildMembersFromCornerPlayers(cornerPlayers);

            if (allMembers.Count == 0)
            {
                _logger.Warn("PodiumBuilder.BuildPodium: there are no members in CornerPlayers.");
                return null;
            }

            List<PodiumPlayerViewModel> podiumPlayers =
                BuildPodiumPlayers(stateResponse, allMembers);

            if (podiumPlayers.Count == 0)
            {
                _logger.Warn("PodiumBuilder.BuildPodium: no podium players could be built");
                return null;
            }

            int winnerUserId = ResolveWinnerUserId(stateResponse, podiumPlayers);
            string winnerName = ResolveWinnerName(winnerUserId, podiumPlayers, allMembers);

            PodiumViewModel podiumViewModel = new PodiumViewModel();
            podiumViewModel.Initialize(
                winnerUserId,
                winnerName,
                podiumPlayers.AsReadOnly());

            return podiumViewModel;
        }

        private static List<LobbyMemberViewModel> BuildMembersFromCornerPlayers(
            CornerPlayersViewModel cornerPlayers)
        {
            List<LobbyMemberViewModel> allMembers = new List<LobbyMemberViewModel>();

            if (cornerPlayers == null)
            {
                return allMembers;
            }

            if (cornerPlayers.TopLeftPlayer != null)
            {
                allMembers.Add(cornerPlayers.TopLeftPlayer);
            }

            if (cornerPlayers.TopRightPlayer != null)
            {
                allMembers.Add(cornerPlayers.TopRightPlayer);
            }

            if (cornerPlayers.BottomLeftPlayer != null)
            {
                allMembers.Add(cornerPlayers.BottomLeftPlayer);
            }

            if (cornerPlayers.BottomRightPlayer != null)
            {
                allMembers.Add(cornerPlayers.BottomRightPlayer);
            }

            return allMembers
                .GroupBy(m => m.UserId)
                .Select(group => group.First())
                .ToList();
        }

        private static List<PodiumPlayerViewModel> BuildPodiumPlayers(
            GetGameStateResponseDto stateResponse,
            List<LobbyMemberViewModel> allMembers)
        {
            List<TokenStateDto> orderedTokens = stateResponse.Tokens
                .OrderByDescending(token => token.CellIndex)
                .ToList();

            List<PodiumPlayerViewModel> podiumPlayers = new List<PodiumPlayerViewModel>();
            int position = FIRST_PODIUM_POSITION;

            foreach (TokenStateDto token in orderedTokens)
            {
                LobbyMemberViewModel member = allMembers
                    .FirstOrDefault(m => m.UserId == token.UserId);

                if (member == null)
                {
                    continue;
                }

                PodiumPlayerViewModel podiumPlayer = new PodiumPlayerViewModel(
                    member.UserId,
                    member.UserName,
                    position,
                    0,
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

            LobbyMemberViewModel winnerMember = allMembers
                .FirstOrDefault(member => member.UserId == winnerUserId);

            if (winnerMember != null &&
                !string.IsNullOrWhiteSpace(winnerMember.UserName))
            {
                return winnerMember.UserName;
            }

            PodiumPlayerViewModel podiumWinner = podiumPlayers
                .FirstOrDefault(player => player.UserId == winnerUserId);

            if (podiumWinner != null &&
                !string.IsNullOrWhiteSpace(podiumWinner.UserName))
            {
                return podiumWinner.UserName;
            }

            return _unknownWinnerName;
        }
    }
}
