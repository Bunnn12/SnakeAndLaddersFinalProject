using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using log4net;
using SnakeAndLaddersFinalProject.GameBoardService;

using ClientBoardSizeOption = SnakeAndLaddersFinalProject.BoardSizeOption;
using ServiceBoardSizeOption = SnakeAndLaddersFinalProject.GameBoardService.BoardSizeOption;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class GameBoardClient
    {
        private const string ENDPOINT_NAME = "NetTcpBinding_IGameBoardService";
        private const int INVALID_USER_ID = 0;

        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardClient));

        public BoardDefinitionDto CreateBoard(
            int gameId,
            ClientBoardSizeOption boardSize,
            bool enableBonusCells,
            bool enableTrapCells,
            bool enableTeleportCells,
            string difficulty,
            IEnumerable<int> playerUserIds)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            int[] normalizedPlayers = (playerUserIds ?? Enumerable.Empty<int>())
                .Where(id => id != INVALID_USER_ID) // ⬅ invitado negativo pasa, solo bloquea 0
                .Distinct()
                .ToArray();

            if (normalizedPlayers.Length == 0)
            {
                Logger.Error("CreateBoard llamado sin jugadores válidos.");
                throw new InvalidOperationException("No hay jugadores válidos para crear el tablero.");
            }

            ServiceBoardSizeOption serviceBoardSize = MapBoardSize(boardSize);

            var client = new GameBoardServiceClient(ENDPOINT_NAME);

            try
            {
                var request = new CreateBoardRequestDto
                {
                    GameId = gameId,
                    BoardSize = serviceBoardSize,
                    EnableBonusCells = enableBonusCells,
                    EnableTrapCells = enableTrapCells,
                    EnableTeleportCells = enableTeleportCells,
                    Difficulty = difficulty,
                    PlayerUserIds = normalizedPlayers
                };

                Logger.InfoFormat(
                    "GameBoardClient.CreateBoard GameId={0}, ClientBoardSize={1}, ServiceBoardSize={2}, Players={3}",
                    gameId,
                    boardSize,
                    serviceBoardSize,
                    normalizedPlayers.Length);

                var response = client.CreateBoard(request);

                if (response == null || response.Board == null)
                {
                    const string message = "El servidor no devolvió ningún tablero.";
                    Logger.Error(message);
                    throw new InvalidOperationException(message);
                }

                client.Close();
                return response.Board;
            }
            catch (FaultException ex)
            {
                Logger.Error("FaultException al crear el tablero.", ex);
                client.Abort();
                throw new InvalidOperationException(
                    "El servidor rechazó la creación del tablero: " + ex.Message,
                    ex);
            }
            catch (CommunicationException ex)
            {
                Logger.Error("Error de comunicación al crear el tablero.", ex);
                client.Abort();
                throw new InvalidOperationException(
                    "Hubo un problema de comunicación al crear el tablero.",
                    ex);
            }
            catch (TimeoutException ex)
            {
                Logger.Error("Timeout al crear el tablero.", ex);
                client.Abort();
                throw new InvalidOperationException(
                    "El servidor tardó demasiado en crear el tablero.",
                    ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al crear el tablero.", ex);
                client.Abort();
                throw;
            }
        }

        public BoardDefinitionDto GetBoard(int gameId)
        {
            if (gameId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gameId));
            }

            var client = new GameBoardServiceClient(ENDPOINT_NAME);

            try
            {
                var board = client.GetBoard(gameId);
                client.Close();
                return board;
            }
            catch (FaultException ex)
            {
                Logger.Warn("FaultException al obtener el tablero.", ex);
                client.Abort();
                return null;
            }
            catch (CommunicationException ex)
            {
                Logger.Error("Error de comunicación al obtener el tablero.", ex);
                client.Abort();
                return null;
            }
            catch (TimeoutException ex)
            {
                Logger.Error("Timeout al obtener el tablero.", ex);
                client.Abort();
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al obtener el tablero.", ex);
                client.Abort();
                return null;
            }
        }

        private static ServiceBoardSizeOption MapBoardSize(ClientBoardSizeOption size)
        {
            switch (size)
            {
                case ClientBoardSizeOption.EightByEight:
                    return ServiceBoardSizeOption.EightByEight;

                case ClientBoardSizeOption.TwelveByTwelve:
                    return ServiceBoardSizeOption.TwelveByTwelve;

                case ClientBoardSizeOption.TenByTen:
                default:
                    return ServiceBoardSizeOption.TenByTen;
            }
        }
    }
}
