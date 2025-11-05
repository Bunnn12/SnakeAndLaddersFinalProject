using SnakeAndLaddersFinalProject.Game;
using SnakeAndLaddersFinalProject.GameBoardService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        public int Rows { get; }

        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }

        public GameBoardViewModel(BoardDefinitionDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Rows = dto.Rows;
            Columns = dto.Columns;

            var cellViewModels = new ObservableCollection<GameBoardCellViewModel>();

            if (dto.Cells != null)
            {
                foreach (BoardCellDto cellDto in dto.Cells)
                {
                    var cellViewModel = new GameBoardCellViewModel(cellDto);
                    cellViewModels.Add(cellViewModel);
                }
            }

            Cells = cellViewModels;
        }
    }

}
