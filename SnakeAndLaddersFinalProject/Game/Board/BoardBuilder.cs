using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace SnakeAndLaddersFinalProject.Game.Board
{
    public static class BoardBuilder
    {
        private const int MIN_INDEX = 1;

        private const double CELL_CENTER_VERTICAL_ADJUST = -0.18;
        private const double CELL_CENTER_OFFSET = 0.5;
        private const int MIN_BOARD_DIMENSION = 1;

        private const string ERROR_CELL_NOT_FOUND_MESSAGE =
            "No se encontró la celda con índice {0}.";

        private const string ERROR_INVALID_ROWS_MESSAGE =
            "El número de filas del tablero debe ser mayor que cero.";

        private const string ERROR_INVALID_COLUMNS_MESSAGE =
            "El número de columnas del tablero debe ser mayor que cero.";

        private const string ERROR_EMPTY_CELLS_MESSAGE =
            "El tablero no contiene celdas definidas.";

        public static BoardBuildResult Build(BoardDefinitionDto boardDefinition)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            ValidateBoardDefinition(boardDefinition);

            var cellCentersByIndex = new Dictionary<int, Point>();
            var linksByStartIndex = new Dictionary<int, BoardLinkDto>();

            ObservableCollection<GameBoardCellViewModel> cells = BuildCells(
                boardDefinition,
                cellCentersByIndex);

            ObservableCollection<GameBoardConnectionViewModel> connections =
                BuildConnections(
                    boardDefinition,
                    cells,
                    linksByStartIndex);

            int startCellIndex = ResolveStartCellIndex(cells);

            return new BoardBuildResult(
                cells,
                connections,
                cellCentersByIndex,
                linksByStartIndex,
                startCellIndex);
        }

        private static void ValidateBoardDefinition(BoardDefinitionDto boardDefinition)
        {
            if (boardDefinition.Rows < MIN_BOARD_DIMENSION)
            {
                throw new InvalidOperationException(ERROR_INVALID_ROWS_MESSAGE);
            }

            if (boardDefinition.Columns < MIN_BOARD_DIMENSION)
            {
                throw new InvalidOperationException(ERROR_INVALID_COLUMNS_MESSAGE);
            }

            if (boardDefinition.Cells == null || !boardDefinition.Cells.Any())
            {
                throw new InvalidOperationException(ERROR_EMPTY_CELLS_MESSAGE);
            }
        }

        private static ObservableCollection<GameBoardCellViewModel> BuildCells(
            BoardDefinitionDto boardDefinition,
            IDictionary<int, Point> cellCentersByIndex)
        {
            int rows = boardDefinition.Rows;
            int columns = boardDefinition.Columns;

            Dictionary<int, BoardCellDto> cellsByIndex = boardDefinition.Cells.ToDictionary(
                boardCell => boardCell.Index);

            var cells = new ObservableCollection<GameBoardCellViewModel>();

            for (int rowFromTop = 0; rowFromTop < rows; rowFromTop++)
            {
                for (int columnFromLeft = 0; columnFromLeft < columns; columnFromLeft++)
                {
                    int index = GetCellIndex(rowFromTop, columnFromLeft);

                    if (!cellsByIndex.TryGetValue(index, out BoardCellDto boardCellDto))
                    {
                        string message = string.Format(
                            CultureInfo.CurrentCulture,
                            ERROR_CELL_NOT_FOUND_MESSAGE,
                            index);

                        throw new InvalidOperationException(message);
                    }

                    var cellViewModel = new GameBoardCellViewModel(boardCellDto);
                    cells.Add(cellViewModel);

                    double centerX = columnFromLeft + CELL_CENTER_OFFSET;
                    double centerY = rowFromTop + CELL_CENTER_OFFSET +
                        CELL_CENTER_VERTICAL_ADJUST;

                    var cellCenter = new Point(centerX, centerY);
                    cellCentersByIndex[index] = cellCenter;
                }
            }

            int GetCellIndex(int rowFromTopIndex, int columnFromLeftIndex)
            {
                int rowFromBottomIndex = (rows - MIN_INDEX) - rowFromTopIndex;
                int zeroBasedIndex = (rowFromBottomIndex * columns) +
                    columnFromLeftIndex;

                return zeroBasedIndex + MIN_INDEX;
            }

            return cells;
        }

        private static ObservableCollection<GameBoardConnectionViewModel> BuildConnections(
            BoardDefinitionDto boardDefinition,
            ObservableCollection<GameBoardCellViewModel> cells,
            IDictionary<int, BoardLinkDto> linksByStartIndex)
        {
            var connections = new ObservableCollection<GameBoardConnectionViewModel>();

            if (boardDefinition.Links == null)
            {
                return connections;
            }

            int rows = boardDefinition.Rows;
            int columns = boardDefinition.Columns;

            foreach (BoardLinkDto link in boardDefinition.Links)
            {
                if (!linksByStartIndex.ContainsKey(link.StartIndex))
                {
                    linksByStartIndex[link.StartIndex] = link;
                }

                var connectionViewModel = new GameBoardConnectionViewModel(
                    link,
                    rows,
                    columns,
                    cells);

                connections.Add(connectionViewModel);
            }

            return connections;
        }

        private static int ResolveStartCellIndex(
            ObservableCollection<GameBoardCellViewModel> cells)
        {
            GameBoardCellViewModel startCell = cells.FirstOrDefault(
                cell => cell.IsStart);

            if (startCell == null)
            {
                return MIN_INDEX;
            }

            return startCell.Index;
        }
    }
}
