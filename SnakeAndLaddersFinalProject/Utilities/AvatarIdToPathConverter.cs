using System;
using System.Globalization;
using System.Windows.Data;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public sealed class AvatarIdToPathConverter : IValueConverter
    {
        private const string CONVERT_BACK_NOT_SUPPORTED_MESSAGE = "AvatarIdToPathConverter does not support ConvertBack.";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string avatarId = value as string;
            return AvatarPathUtility.GetPackUri(avatarId);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(CONVERT_BACK_NOT_SUPPORTED_MESSAGE);
        }
    }
}
