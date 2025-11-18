using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SnakeAndLaddersFinalProject.GameBoardService;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardViewModel
    {
        private const int MIN_INDEX = 1;

        public int Rows { get; }
        public int Columns { get; }

        public ObservableCollection<GameBoardCellViewModel> Cells { get; }
        public ObservableCollection<GameBoardConnectionViewModel> Connections { get; }

        public CornerPlayersViewModel CornerPlayers { get; }

        /// <summary>
        /// Fichas de jugadores sobre el tablero.
        /// </summary>
        public ObservableCollection<PlayerTokenViewModel> PlayerTokens { get; }

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
            CornerPlayers = new CornerPlayersViewModel();
            PlayerTokens = new ObservableCollection<PlayerTokenViewModel>();

            BuildCells(boardDefinition.Cells);
            BuildConnections(boardDefinition.Links);
        }

        public void InitializeCornerPlayers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            CornerPlayers.InitializeFromLobbyMembers(lobbyMembers);
        }

        /// <summary>
        /// Inicializa las fichas de los jugadores en la casilla inicial.
        /// </summary>
        public void InitializeTokensFromLobbyMembers(IList<LobbyMemberViewModel> lobbyMembers)
        {
            PlayerTokens.Clear();

            if (lobbyMembers == null || lobbyMembers.Count == 0)
            {
                return;
            }

            // Buscar la casilla inicial (IsStart = true); si no hay, usar índice 1
            int startIndex = MIN_INDEX;
            var startCell = Cells.FirstOrDefault(c => c.IsStart);
            if (startCell != null)
            {
                startIndex = startCell.Index;
            }

            foreach (var member in lobbyMembers)
            {
                // CurrentSkinId viene del LobbyMemberViewModel
                PlayerTokens.Add(
                    new PlayerTokenViewModel(
                        member.UserId,
                        member.UserName,
                        member.CurrentSkinId,
                        startIndex));
            }
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
