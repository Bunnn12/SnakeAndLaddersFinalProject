using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SnakeAndLaddersFinalProject.Game.Gameplay
{
    public sealed class PodiumBuilder
    {
        private const string UNKNOWN_WINNER_NAME = "Desconocido";

        private readonly ILog logger;

        public PodiumBuilder(ILog logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PodiumViewModel BuildPodium(
            GetGameStateResponseDto stateResponse,
            CornerPlayersViewModel cornerPlayers)
        {
            if (stateResponse == null)
            {
                logger.Warn("PodiumBuilder.BuildPodium: stateResponse es null.");
                return null;
            }

            logger.InfoFormat(
                "PodiumBuilder.BuildPodium: IsFinished={0}, Tokens={1}, WinnerUserId={2}",
                stateResponse.IsFinished,
                stateResponse.Tokens == null ? -1 : stateResponse.Tokens.Length,
                stateResponse.WinnerUserId);

            if (!stateResponse.IsFinished)
            {
                logger.Info("PodiumBuilder.BuildPodium: la partida no está terminada.");
                return null;
            }

            if (stateResponse.Tokens == null || stateResponse.Tokens.Length == 0)
            {
                logger.Warn("PodiumBuilder.BuildPodium: no hay tokens en el estado.");
                return null;
            }

            List<LobbyMemberViewModel> allMembers = BuildMembersFromCornerPlayers(cornerPlayers);

            if (allMembers.Count == 0)
            {
                logger.Warn("PodiumBuilder.BuildPodium: no hay miembros en CornerPlayers.");
                return null;
            }

            List<PodiumPlayerViewModel> podiumPlayers =
                BuildPodiumPlayers(stateResponse, allMembers);

            if (podiumPlayers.Count == 0)
            {
                logger.Warn("PodiumBuilder.BuildPodium: no se pudieron construir jugadores del podio.");
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
                .Select(g => g.First())
                .ToList();
        }

        private static List<PodiumPlayerViewModel> BuildPodiumPlayers(
            GetGameStateResponseDto stateResponse,
            List<LobbyMemberViewModel> allMembers)
        {
            List<TokenStateDto> orderedTokens = stateResponse.Tokens
                .OrderByDescending(t => t.CellIndex)
                .ToList();

            List<PodiumPlayerViewModel> podiumPlayers = new List<PodiumPlayerViewModel>();
            int position = 1;

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

                if (position > 3)
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
            if (stateResponse.WinnerUserId > 0)
            {
                return stateResponse.WinnerUserId;
            }

            if (podiumPlayers.Count == 0)
            {
                return 0;
            }

            return podiumPlayers[0].UserId;
        }

        private static string ResolveWinnerName(
            int winnerUserId,
            List<PodiumPlayerViewModel> podiumPlayers,
            List<LobbyMemberViewModel> allMembers)
        {
            if (winnerUserId <= 0)
            {
                return UNKNOWN_WINNER_NAME;
            }

            LobbyMemberViewModel winnerMember = allMembers
                .FirstOrDefault(m => m.UserId == winnerUserId);

            if (winnerMember != null &&
                !string.IsNullOrWhiteSpace(winnerMember.UserName))
            {
                return winnerMember.UserName;
            }

            PodiumPlayerViewModel podiumWinner = podiumPlayers
                .FirstOrDefault(p => p.UserId == winnerUserId);

            if (podiumWinner != null &&
                !string.IsNullOrWhiteSpace(podiumWinner.UserName))
            {
                return podiumWinner.UserName;
            }

            return UNKNOWN_WINNER_NAME;
        }
    }
}
