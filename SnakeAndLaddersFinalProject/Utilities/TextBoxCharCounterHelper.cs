using System;
using System.Globalization;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.Properties.Langs;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class TextBoxCharCounterHelper
    {
        private const int MIN_VALID_MAX_LENGTH = 1;

        public static void AttachCounter(
            TextBox textBox,
            TextBlock counterTextBlock,
            int maxLength)
        {
            if (textBox == null)
            {
                throw new ArgumentNullException(nameof(textBox));
            }

            if (counterTextBlock == null)
            {
                throw new ArgumentNullException(nameof(counterTextBlock));
            }

            if (maxLength < MIN_VALID_MAX_LENGTH)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            }

            textBox.MaxLength = maxLength;
            textBox.TextChanged += (object sender, TextChangedEventArgs e) =>
            {
                UpdateCounter(textBox, counterTextBlock, maxLength);
            };

            UpdateCounter(textBox, counterTextBlock, maxLength);
        }

        private static void UpdateCounter(
            TextBox textBox,
            TextBlock counterTextBlock,
            int maxLength)
        {
            int currentLength = textBox.Text?.Length ?? 0;

            if (currentLength > maxLength)
            {
                currentLength = maxLength;
            }

            counterTextBlock.Text = string.Format(CultureInfo.InvariantCulture,
                Lang.TextBoxCharCounterFmt, currentLength, maxLength);
        }
    }
}
