using System;


namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class AvatarProfileOptionViewModel
    {
        private const double UNLOCKED_OPACITY = 1.0;
        private const double LOCKED_OPACITY = 0.35;

        public string AvatarCode { get; }

        public bool IsUnlocked { get; }

        public bool IsCurrent { get; }

        public bool IsEnabled
        {
            get { return IsUnlocked; }
        }

        public double Opacity
        {
            get { return IsUnlocked ? UNLOCKED_OPACITY : LOCKED_OPACITY; }
        }

        public AvatarProfileOptionViewModel(
            string avatarCode,
            bool isUnlocked,
            bool isCurrent)
        {
            if (string.IsNullOrWhiteSpace(avatarCode))
            {
                throw new ArgumentException(nameof(avatarCode));
            }

            AvatarCode = avatarCode.Trim().ToUpperInvariant();
            IsUnlocked = isUnlocked;
            IsCurrent = isCurrent;
        }
    }
}
