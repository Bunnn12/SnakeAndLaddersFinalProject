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
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Rows = dto.Rows;
            Columns = dto.Columns;

            // ----- CELDAS -----
            var cellVms = new ObservableCollection<GameBoardCellViewModel>();

            if (dto.Cells != null)
            {
                foreach (BoardCellDto cellDto in dto.Cells)
                {
                    cellVms.Add(new GameBoardCellViewModel(cellDto));
                }
            }

            Cells = cellVms;

            // Para pasárselo a GameBoardConnectionViewModel
            IList<GameBoardCellViewModel> cellList = Cells.ToList();

            // ----- CONEXIONES (serpientes / escaleras) -----
            var connectionVms = new ObservableCollection<GameBoardConnectionViewModel>();

            if (dto.Links != null)
            {
                foreach (BoardLinkDto link in dto.Links)
                {
                    // OJO: aquí asumo que en BoardLinkDto tienes:
                    //   int StartIndex
                    //   int EndIndex
                    //   bool IsSnake
                    //
                    // Si el bool se llama distinto (p.e. IsSnakeLink), cámbialo abajo.

                    bool isLadder = link.IsLadder;   // ajusta el nombre si hace falta

                    var connVm = new GameBoardConnectionViewModel(
                        link.StartIndex,
                        link.EndIndex,
                        isLadder,
                        Rows,
                        Columns,
                        cellList);

                    connectionVms.Add(connVm);
                }
            }

            Connections = connectionVms;

            // Debug para ver qué llega
            System.Diagnostics.Debug.WriteLine(
                $"Board: Cells={Cells.Count}, Links={Connections.Count}");

            foreach (var c in Connections)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Connection: {c.StartIndex} -> {c.EndIndex} | IsLadder={c.IsLadder} | " +
                    $"({c.StartX},{c.StartY}) -> ({c.EndX},{c.EndY})");
            }
        }
    }
}
