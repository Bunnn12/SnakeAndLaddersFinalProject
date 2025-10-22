using System.Windows;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class PageBackground
    {
        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.RegisterAttached(
                "Key",
                typeof(string),
                typeof(PageBackground),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.Inherits));

        public static void SetKey(DependencyObject element, string value)
            => element.SetValue(KeyProperty, value);

        public static string GetKey(DependencyObject element)
            => (string)element.GetValue(KeyProperty);
    }
}
