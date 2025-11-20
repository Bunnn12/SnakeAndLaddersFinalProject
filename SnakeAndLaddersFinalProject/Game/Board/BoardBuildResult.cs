using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Game.Board
{
    public sealed class BoardBuildResult
    {
        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }
        public Dictionary<int, Point> CellCentersByIndex { get; }
        public Dictionary<int, BoardLinkDto> LinksByStartIndex { get; }
        public int StartCellIndex { get; }

        public BoardBuildResult(
            ObservableCollection<GameBoardCellViewModel> cells,
            ObservableCollection<GameBoardConnectionViewModel> connections,
            Dictionary<int, Point> cellCentersByIndex,
            Dictionary<int, BoardLinkDto> linksByStartIndex,
            int startCellIndex)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (connections == null)
            {
                throw new ArgumentNullException(nameof(connections));
            }

            if (cellCentersByIndex == null)
            {
                throw new ArgumentNullException(nameof(cellCentersByIndex));
            }

            if (linksByStartIndex == null)
            {
                throw new ArgumentNullException(nameof(linksByStartIndex));
            }

            Cells = cells;
            Connections = connections;
            CellCentersByIndex = cellCentersByIndex;
            LinksByStartIndex = linksByStartIndex;
            StartCellIndex = startCellIndex;
        }
    }
}
