using System;
using System.ServiceModel;
using log4net;
using SnakeAndLaddersFinalProject.GameBoardService;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class GameBoardClient : IGameBoardClient
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GameBoardClient));

        private const string GAME_BOARD_ENDPOINT = "NetTcpBinding_IGameBoardService";

        public BoardDefinitionDto CreateBoard(
    int gameId,
    BoardSizeOption boardSize,
    bool enableBonusCells,
    bool enableTrapCells,
    bool enableTeleportCells)
        {
            GameBoardServiceClient client = null;

            try
            {
                client = new GameBoardServiceClient(GAME_BOARD_ENDPOINT);

                var request = new CreateBoardRequestDto
                {
                    GameId = gameId,
                    BoardSize = MapBoardSize(boardSize),
                    EnableBonusCells = enableBonusCells,
                    EnableTrapCells = enableTrapCells,
                    EnableTeleportCells = enableTeleportCells
                };


                Logger.InfoFormat(
                    "Calling CreateBoard. GameId={0}, BoardSize={1}, EnableBonusCells={2}, EnableTrapCells={3}, EnableTeleportCells={4}",
                    request.GameId,
                    request.BoardSize,
                    request.EnableBonusCells,
                    request.EnableTrapCells,
                    request.EnableTeleportCells);

                var response = client.CreateBoard(request);

                if (response == null || response.Board == null)
                {
                    const string message = "El servicio devolvió una respuesta vacía al crear el tablero.";
                    Logger.Warn(message);
                    throw new InvalidOperationException(message);
                }

                client.Close();
                return response.Board;
            }
            catch (FaultException ex)
            {
                Logger.Warn("Fallo de negocio al crear el tablero.", ex);

                if (client != null)
                {
                    client.Abort();
                }

                throw;
            }
            catch (CommunicationException ex)
            {
                Logger.Error("Error de comunicación al llamar a CreateBoard.", ex);

                if (client != null)
                {
                    client.Abort();
                }

                throw;
            }
            catch (TimeoutException ex)
            {
                Logger.Error("Timeout al llamar a CreateBoard.", ex);

                if (client != null)
                {
                    client.Abort();
                }

                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al llamar a CreateBoard.", ex);

                if (client != null)
                {
                    client.Abort();
                }

                throw;
            }
        }


        public BoardDefinitionDto GetBoard(int gameId)
        {
            try
            {
                using (var client = new GameBoardServiceClient(GAME_BOARD_ENDPOINT))
                {
                    Logger.InfoFormat("Calling GetBoard. GameId={0}", gameId);

                    var board = client.GetBoard(gameId);

                    if (board == null)
                    {
                        Logger.WarnFormat("GetBoard returned null for GameId={0}.", gameId);
                    }

                    return board;
                }
            }
            catch (FaultException ex)
            {
                Logger.Warn("Fallo de negocio al obtener el tablero.", ex);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error("Error inesperado al llamar a GetBoard.", ex);
                throw;
            }
        }

        private static GameBoardService.BoardSizeOption MapBoardSize(BoardSizeOption boardSize)
        {
            switch (boardSize)
            {
                case BoardSizeOption.EightByEight:
                    return GameBoardService.BoardSizeOption.EightByEight;

                case BoardSizeOption.TwelveByTwelve:
                    return GameBoardService.BoardSizeOption.TwelveByTwelve;

                case BoardSizeOption.TenByTen:
                default:
                    return GameBoardService.BoardSizeOption.TenByTen;
            }
        }

    }
}
