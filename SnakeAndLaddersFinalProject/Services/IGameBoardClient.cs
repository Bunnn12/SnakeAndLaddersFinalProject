using SnakeAndLaddersFinalProject.GameBoardService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Services
{
    public interface IGameBoardClient
    {
        BoardDefinitionDto CreateBoard(
            int gameId,
            BoardSizeOption boardSize,
            bool enableBonusCells,
            bool enableTrapCells,
            bool enableTeleportCells, string difficulty);

        BoardDefinitionDto GetBoard(int gameId);
    }
}
