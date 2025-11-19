using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Game
{
    public sealed class PlayerTokenManager
    {
        private const double TOKEN_OFFSET_DIAGONAL = 0.18;

        private static readonly Point[] TOKEN_CELL_OFFSETS =
        {
            new Point(-TOKEN_OFFSET_DIAGONAL, -TOKEN_OFFSET_DIAGONAL), 
            new Point(TOKEN_OFFSET_DIAGONAL, -TOKEN_OFFSET_DIAGONAL),  
            new Point(-TOKEN_OFFSET_DIAGONAL, TOKEN_OFFSET_DIAGONAL),  
            new Point(TOKEN_OFFSET_DIAGONAL, TOKEN_OFFSET_DIAGONAL)    
        };

        private readonly ObservableCollection<PlayerTokenViewModel> playerTokens;
        private readonly IReadOnlyDictionary<int, Point> cellCentersByIndex;

        public PlayerTokenManager(
            ObservableCollection<PlayerTokenViewModel> playerTokens,
            IReadOnlyDictionary<int, Point> cellCentersByIndex)
        {
            this.playerTokens = playerTokens
                ?? throw new ArgumentNullException(nameof(playerTokens));

            this.cellCentersByIndex = cellCentersByIndex
                ?? throw new ArgumentNullException(nameof(cellCentersByIndex));
        }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return playerTokens; }
        }

        public PlayerTokenViewModel CreateFromLobbyMember(
            LobbyMemberViewModel member,
            int startCellIndex)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var token = new PlayerTokenViewModel(
                member.UserId,
                member.UserName,
                member.CurrentSkinUnlockedId,
                startCellIndex);

            playerTokens.Add(token);
            UpdateTokenPositionFromCell(token, startCellIndex);

            return token;
        }

        public void ResetAllTokensToCell(int cellIndex)
        {
            foreach (var token in playerTokens)
            {
                UpdateTokenPositionFromCell(token, cellIndex);
            }
        }

        public PlayerTokenViewModel GetOrCreateTokenForUser(
            int userId,
            int initialCellIndex)
        {
            var token = playerTokens.FirstOrDefault(t => t.UserId == userId);
            if (token != null)
            {
                return token;
            }

            token = new PlayerTokenViewModel(
                userId,
                $"Jugador {userId}",
                null,
                initialCellIndex);

            playerTokens.Add(token);
            UpdateTokenPositionFromCell(token, initialCellIndex);

            return token;
        }

        public void UpdateTokenPositionFromCell(
            PlayerTokenViewModel token,
            int cellIndex)
        {
            if (token == null)
            {
                return;
            }

            if (!cellCentersByIndex.TryGetValue(cellIndex, out Point center))
            {
                return;
            }

            token.CurrentCellIndex = cellIndex;

            var tokensInCell = playerTokens
                .Where(t => t.CurrentCellIndex == cellIndex)
                .OrderBy(t => t.UserId)
                .ToList();

            if (tokensInCell.Count == 1)
            {
                var onlyToken = tokensInCell[0];
                onlyToken.X = center.X;
                onlyToken.Y = center.Y;
                return;
            }

            for (int i = 0; i < tokensInCell.Count; i++)
            {
                var currentToken = tokensInCell[i];

                int slotIndex = i;
                if (slotIndex >= TOKEN_CELL_OFFSETS.Length)
                {
                    slotIndex = TOKEN_CELL_OFFSETS.Length - 1;
                }

                Point offset = TOKEN_CELL_OFFSETS[slotIndex];

                currentToken.X = center.X + offset.X;
                currentToken.Y = center.Y + offset.Y;
            }
        }
    }
}
