using System;
using SnakeAndLaddersFinalProject.GameBoardService;
using GameSpecialCellType = SnakeAndLaddersFinalProject.Game.SpecialCellType;
using ServiceSpecialCellType = SnakeAndLaddersFinalProject.GameBoardService.SpecialCellType;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class GameBoardCellViewModel
    {
        public int Index { get; }

        public int Row { get; }

        public int Column { get; }

        public bool IsDark { get; }

        public GameSpecialCellType SpecialType { get; }

        public bool HasSpecial
        {
            get { return SpecialType != GameSpecialCellType.None; }
        }

        public bool IsBonus
        {
            get { return SpecialType == GameSpecialCellType.Bonus; }
        }

        public bool IsTrap
        {
            get { return SpecialType == GameSpecialCellType.Trap; }
        }

        public bool IsTeleport
        {
            get { return SpecialType == GameSpecialCellType.Teleport; }
        }

        public GameBoardCellViewModel(BoardCellDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            Index = dto.Index;
            Row = dto.Row;
            Column = dto.Column;
            IsDark = dto.IsDark;

            SpecialType = MapSpecialType(dto.SpecialType);
        }

        private static GameSpecialCellType MapSpecialType(ServiceSpecialCellType source)
        {
            switch (source)
            {
                case ServiceSpecialCellType.None:
                    return GameSpecialCellType.None;

                case ServiceSpecialCellType.Bonus:
                    return GameSpecialCellType.Bonus;

                case ServiceSpecialCellType.Trap:
                    return GameSpecialCellType.Trap;

                case ServiceSpecialCellType.Teleport:
                    return GameSpecialCellType.Teleport;

                default:
                    return GameSpecialCellType.None;
            }
        }
    }
}
