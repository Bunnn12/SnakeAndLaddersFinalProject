using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnakeAndLaddersFinalProject.GameBoardService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        private const int MIN_INDEX = 1;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public GameBoardViewModel(BoardDefinitionDto boardDefinition)
        {
            if (boardDefinition == null)
            {
                throw new ArgumentNullException(nameof(boardDefinition));
            }

            Rows = boardDefinition.Rows;
            Columns = boardDefinition.Columns;

            Cells = new ObservableCollection<GameBoardCellViewModel>();
            Connections = new ObservableCollection<GameBoardConnectionViewModel>();

            BuildCells(boardDefinition.Cells);
            BuildConnections(boardDefinition.Links);
        }

        private void BuildCells(IList<BoardCellDto> cellDtos)
        {
            if (cellDtos == null)
            {
                throw new ArgumentNullException(nameof(cellDtos));
            }

            var cellsByIndex = cellDtos.ToDictionary(c => c.Index);

            for (int rowFromTop = 0; rowFromTop < Rows; rowFromTop++)
            {
                int rowFromBottom = (Rows - 1) - rowFromTop;

                for (int columnFromLeft = 0; columnFromLeft < Columns; columnFromLeft++)
                {
                    int zeroBasedIndex = (rowFromBottom * Columns) + columnFromLeft;
                    int index = zeroBasedIndex + MIN_INDEX;

                    if (!cellsByIndex.TryGetValue(index, out var cellDto))
                    {
                        throw new InvalidOperationException(
                            $"No se encontró la celda con índice {index}.");
                    }

                    Cells.Add(new GameBoardCellViewModel(cellDto));
                }
            }
        }

        private void BuildConnections(IList<BoardLinkDto> links)
        {
            if (links == null)
            {
                return;
            }

            foreach (var link in links)
            {
                var connectionViewModel = new GameBoardConnectionViewModel(
                    link,
                    Rows,
                    Columns,
                    Cells);

                Connections.Add(connectionViewModel);
            }
        }
    }
}
