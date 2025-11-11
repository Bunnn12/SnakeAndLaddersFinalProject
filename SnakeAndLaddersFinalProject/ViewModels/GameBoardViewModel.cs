using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnakeAndLaddersFinalProject.GameBoardService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }

        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public GameBoardViewModel(BoardDefinitionDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            Rows = dto.Rows;
            Columns = dto.Columns;

            var cellVms = new ObservableCollection<GameBoardCellViewModel>();
            foreach (var cellDto in dto.Cells ?? Array.Empty<BoardCellDto>())
            {
                cellVms.Add(new GameBoardCellViewModel(cellDto));
            }
            Cells = cellVms;

            var connVms = new ObservableCollection<GameBoardConnectionViewModel>();
            // usamos la misma lista de celdas para buscar índices
            IList<GameBoardCellViewModel> cellsList = cellVms.ToList();

            foreach (var link in dto.Links ?? Array.Empty<BoardLinkDto>())
            {

                connVms.Add(new GameBoardConnectionViewModel(link, Rows, Columns, cellsList));
            }

            Connections = connVms;
        }

    }
}
