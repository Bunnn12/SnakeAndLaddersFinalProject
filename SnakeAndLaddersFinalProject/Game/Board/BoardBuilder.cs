using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.ViewModels;
using SnakeAndLaddersFinalProject.ViewModels.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Game.Board
{
    public static class BoardBuilder
    {
        private const int MIN_INDEX = 1;
        private const double CELL_CENTER_VERTICAL_ADJUST = -0.18;

        public static BoardBuildResult Build(BoardDefinitionDto boardDefinition)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            ObservableCollection<GameBoardCellViewModel> cells = new ObservableCollection<GameBoardCellViewModel>();
            ObservableCollection<GameBoardConnectionViewModel> connections = new ObservableCollection<GameBoardConnectionViewModel>();
            Dictionary<int, Point> cellCentersByIndex = new Dictionary<int, Point>();
            Dictionary<int, BoardLinkDto> linksByStartIndex = new Dictionary<int, BoardLinkDto>();

            int rows = boardDefinition.Rows;
            int columns = boardDefinition.Columns;

            Dictionary<int, BoardCellDto> cellsByIndex = boardDefinition
                .Cells
                .ToDictionary(boardCell => boardCell.Index);

            for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
            {
                int rowFromBottom = (rows - 1) - rowFromTop;

                for (int columnFromLeft = 0; columnFromLeft < columns; columnFromLeft++)
                {
                    int zeroBasedIndex = (rowFromBottom * columns) + columnFromLeft;
                    int index = zeroBasedIndex + MIN_INDEX;

                    BoardCellDto boardCellDto;
                    if (!cellsByIndex.TryGetValue(index, out boardCellDto))
                    {
                        string message = string.Format("No se encontró la celda con índice {0}.", index);
                        throw new InvalidOperationException(message);
                    }

                    GameBoardCellViewModel cellViewModel = new GameBoardCellViewModel(boardCellDto);
                    cells.Add(cellViewModel);

                    double centerX = columnFromLeft + 0.5;
                    double centerY = rowFromTop + 0.5 + CELL_CENTER_VERTICAL_ADJUST;

                    Point cellCenter = new Point(centerX, centerY);
                    cellCentersByIndex[index] = cellCenter;
                }
            }

            if (boardDefinition.Links != null)
            {
                foreach (BoardLinkDto link in boardDefinition.Links)
                {
                    if (!linksByStartIndex.ContainsKey(link.StartIndex))
                    {
                        linksByStartIndex[link.StartIndex] = link;
                    }

                    GameBoardConnectionViewModel connectionViewModel = new GameBoardConnectionViewModel(
                        link,
                        rows,
                        columns,
                        cells);

                    connections.Add(connectionViewModel);
                }
            }

            int startCellIndex = MIN_INDEX;
            GameBoardCellViewModel startCellViewModel = cells.FirstOrDefault(cell => cell.IsStart);

            if (startCellViewModel != null)
            {
                startCellIndex = startCellViewModel.Index;
            }

            BoardBuildResult buildResult = new BoardBuildResult(
                cells,
                connections,
                cellCentersByIndex,
                linksByStartIndex,
                startCellIndex);

            return buildResult;
        }
    }
}
