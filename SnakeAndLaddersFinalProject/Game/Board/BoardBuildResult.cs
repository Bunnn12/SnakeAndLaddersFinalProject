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
            Cells = cells ?? throw new ArgumentNullException(nameof(cells));
            Connections = connections ?? throw new ArgumentNullException(nameof(connections));
            CellCentersByIndex = cellCentersByIndex ?? throw new ArgumentNullException(
                nameof(cellCentersByIndex));
            LinksByStartIndex = linksByStartIndex ?? throw new ArgumentNullException(
                nameof(linksByStartIndex));
            StartCellIndex = startCellIndex;
        }
    }
}
