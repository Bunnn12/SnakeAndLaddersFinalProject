using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using log4net;
using SnakeAndLaddersFinalProject.GameplayService;
using SnakeAndLaddersFinalProject.Mappers;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class LobbyBoardService
    {
        private const int MAX_ATTEMPTS = 5;
        private const int RETRY_DELAY_MS = 800;
        private const int INVALID_USER_ID = 0;
        private const int FALLBACK_LOCAL_USER_ID = 1;

        private static readonly ILog Logger =
            LogManager.GetLogger(typeof(LobbyBoardService));

        private readonly GameBoardClient gameBoardClient;

        public LobbyBoardService(GameBoardClient gameBoardClient)
        {
            this.gameBoardClient = gameBoardClient
                                   ?? throw new ArgumentNullException(nameof(gameBoardClient));
        }

        public async Task<GameBoardViewModel> CreateBoardForHostAsync(
            int lobbyId,
            string currentUserName,
            int currentUserId,
            CreateMatchOptions options,
            IList<LobbyMemberViewModel> members)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            LobbyMapper.MapSpecialTileBooleans(
                options.SpecialTiles,
                out bool enableBonusCells,
                out bool enableTrapCells,
                out bool enableTeleportCells);

            List<int> playerUserIds = members
                .Where(m => m != null && m.UserId != INVALID_USER_ID)
                .Select(m => m.UserId)
                .Distinct()
                .ToList();

            if (playerUserIds.Count == 0)
            {
                Logger.Error("CreateBoardForHostAsync: no valid player IDs to create the board.");
                return null;
            }

            var boardDto = gameBoardClient.CreateBoard(
                lobbyId,
                options.BoardSize,
                enableBonusCells,
                enableTrapCells,
                enableTeleportCells,
                options.Difficulty.ToString(),
                playerUserIds);

            int localUserId = ResolveLocalUserIdForBoard(currentUserId);

            var boardViewModel = new GameBoardViewModel(
                boardDto,
                lobbyId,
                localUserId,
                currentUserName);

            boardViewModel.InitializeCornerPlayers(members);
            boardViewModel.InitializeTokensFromLobbyMembers(members);

            var gameplayClient = new GameplayClient(boardViewModel);

            await boardViewModel.InitializeGameplayAsync(
                gameplayClient,
                currentUserName).ConfigureAwait(false);

            return boardViewModel;
        }

        public async Task<GameBoardViewModel> TryCreateBoardForGuestWithRetryAsync(
            int lobbyId,
            string currentUserName,
            int currentUserId,
            IList<LobbyMemberViewModel> members)
        {
            if (members == null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            for (int attempt = 1; attempt <= MAX_ATTEMPTS; attempt++)
            {
                Logger.InfoFormat(
                    "TryCreateBoardForGuestWithRetryAsync: intento {0}/{1} para obtener tablero GameId={2} (cliente UserId={3}).",
                    attempt,
                    MAX_ATTEMPTS,
                    lobbyId,
                    currentUserId);

                var boardDto = gameBoardClient.GetBoard(lobbyId);

                if (boardDto != null)
                {
                    int localUserId = ResolveLocalUserIdForBoard(currentUserId);

                    var boardViewModel = new GameBoardViewModel(
                        boardDto,
                        lobbyId,
                        localUserId,
                        currentUserName);

                    boardViewModel.InitializeCornerPlayers(members);
                    boardViewModel.InitializeTokensFromLobbyMembers(members);

                    var gameplayClient = new GameplayClient(boardViewModel);

                    Logger.InfoFormat(
                        "TryCreateBoardForGuestWithRetryAsync: inicializando gameplay GameId={0}, LocalUserId={1}.",
                        lobbyId,
                        localUserId);

                    await boardViewModel.InitializeGameplayAsync(
                        gameplayClient,
                        currentUserName).ConfigureAwait(false);

                    return boardViewModel;
                }

                Logger.WarnFormat(
                    "TryCreateBoardForGuestWithRetryAsync: el servidor aún no tiene tablero para LobbyId {0} (intento {1}).",
                    lobbyId,
                    attempt);

                await Task.Delay(RETRY_DELAY_MS).ConfigureAwait(false);
            }

            Logger.WarnFormat(
                "TryCreateBoardForGuestWithRetryAsync: el servidor no devolvió el tablero para LobbyId {0} tras varios intentos.",
                lobbyId);

            return null;
        }

        private static int ResolveLocalUserIdForBoard(int currentUserId)
        {
            if (currentUserId != INVALID_USER_ID)
            {
                return currentUserId;
            }

            Logger.Warn(
                "ResolveLocalUserIdForBoard: CurrentUserId no está establecido, se usará FALLBACK_LOCAL_USER_ID.");

            return FALLBACK_LOCAL_USER_ID;
        }
    }
}
