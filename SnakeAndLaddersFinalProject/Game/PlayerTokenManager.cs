using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using SnakeAndLaddersFinalProject.ViewModels.Models;

namespace SnakeAndLaddersFinalProject.Game
{
    public sealed class PlayerTokenManager
    {
        private const double TOKEN_OFFSET_DIAGONAL = 0.18;
        private const string DEFAULT_PLAYER_NAME_FORMAT = "Jugador {0}";

        private static readonly Point[] _tokenCellOffsets =
        {
            new Point(-TOKEN_OFFSET_DIAGONAL, -TOKEN_OFFSET_DIAGONAL),
            new Point(TOKEN_OFFSET_DIAGONAL, -TOKEN_OFFSET_DIAGONAL),
            new Point(-TOKEN_OFFSET_DIAGONAL, TOKEN_OFFSET_DIAGONAL),
            new Point(TOKEN_OFFSET_DIAGONAL, TOKEN_OFFSET_DIAGONAL)
        };

        private readonly ObservableCollection<PlayerTokenViewModel> _playerTokens;
        private readonly IReadOnlyDictionary<int, Point> _cellCentersByIndex;

        public PlayerTokenManager(
            ObservableCollection<PlayerTokenViewModel> playerTokens,
            IReadOnlyDictionary<int, Point> cellCentersByIndex)
        {
            _playerTokens = playerTokens
                ?? throw new ArgumentNullException(nameof(playerTokens));

            _cellCentersByIndex = cellCentersByIndex
                ?? throw new ArgumentNullException(nameof(cellCentersByIndex));
        }

        public ObservableCollection<PlayerTokenViewModel> PlayerTokens
        {
            get { return _playerTokens; }
        }

        public PlayerTokenViewModel CreateFromLobbyMember(
            LobbyMemberViewModel member,
            int startCellIndex)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            var playerToken = new PlayerTokenViewModel(
                member.UserId,
                member.UserName,
                member.CurrentSkinId,
                member.CurrentSkinUnlockedId,
                startCellIndex);

            _playerTokens.Add(playerToken);
            UpdateTokenPositionFromCell(playerToken, startCellIndex);

            return playerToken;
        }

        public void ResetAllTokensToCell(int cellIndex)
        {
            foreach (PlayerTokenViewModel playerToken in _playerTokens)
            {
                UpdateTokenPositionFromCell(playerToken, cellIndex);
            }
        }

        public PlayerTokenViewModel GetOrCreateTokenForUser(
            int userId,
            int initialCellIndex)
        {
            PlayerTokenViewModel existingToken = _playerTokens
                .FirstOrDefault(playerToken => playerToken.UserId == userId);

            if (existingToken != null)
            {
                return existingToken;
            }

            string userName = string.Format(
                CultureInfo.CurrentCulture,
                DEFAULT_PLAYER_NAME_FORMAT,
                userId);

            var newToken = new PlayerTokenViewModel(
                userId,
                userName,
                null,
                null,
                initialCellIndex);

            _playerTokens.Add(newToken);
            UpdateTokenPositionFromCell(newToken, initialCellIndex);

            return newToken;
        }

        public void UpdateTokenPositionFromCell(
            PlayerTokenViewModel token,
            int cellIndex)
        {
            if (token == null)
            {
                return;
            }

            if (!_cellCentersByIndex.TryGetValue(cellIndex, out Point center))
            {
                return;
            }

            token.CurrentCellIndex = cellIndex;

            List<PlayerTokenViewModel> tokensInCell = _playerTokens
                .Where(playerToken => playerToken.CurrentCellIndex == cellIndex)
                .OrderBy(playerToken => playerToken.UserId)
                .ToList();

            if (tokensInCell.Count == 1)
            {
                PlayerTokenViewModel onlyToken = tokensInCell[0];
                onlyToken.X = center.X;
                onlyToken.Y = center.Y;
                return;
            }

            for (int index = 0; index < tokensInCell.Count; index++)
            {
                PlayerTokenViewModel currentToken = tokensInCell[index];

                int slotIndex = index;
                if (slotIndex >= _tokenCellOffsets.Length)
                {
                    slotIndex = _tokenCellOffsets.Length - 1;
                }

                Point offset = _tokenCellOffsets[slotIndex];

                currentToken.X = center.X + offset.X;
                currentToken.Y = center.Y + offset.Y;
            }
        }
    }
}
