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

        public bool IsDice
        {
            get { return SpecialType == GameSpecialCellType.Dice; }
        }

        public bool IsItem
        {
            get { return SpecialType == GameSpecialCellType.Item; }
        }

        public bool IsMessage
        {
            get { return SpecialType == GameSpecialCellType.Message; }
        }

        public bool IsStart { get; }

        public bool IsFinal { get; }

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

            IsStart = dto.IsStart;
            IsFinal = dto.IsFinal;
        }

        private static GameSpecialCellType MapSpecialType(ServiceSpecialCellType source)
        {
            switch (source)
            {
                case ServiceSpecialCellType.None:
                    return GameSpecialCellType.None;

                case ServiceSpecialCellType.Dice:
                    return GameSpecialCellType.Dice;

                case ServiceSpecialCellType.Item:
                    return GameSpecialCellType.Item;

                case ServiceSpecialCellType.Teleport:
                    return GameSpecialCellType.Message;

                default:
                    return GameSpecialCellType.None;
            }
        }
    }
}
