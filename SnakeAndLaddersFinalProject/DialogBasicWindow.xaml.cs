using System;
using System.Windows;

namespace SnakeAndLaddersFinalProject
{
    public enum DialogButtons
    {
        Ok,
        OkCancel,
        YesNo,
        YesNoCancel
    }

    public enum DialogResultEx
    {
        None,
        Ok,
        Yes,
        No,
        Cancel
    }

    public partial class DialogBasicWindow : Window
    {
        public DialogBasicWindow()
        {
            InitializeComponent();
            Loaded += (_, __) => ApplyButtons();
        }

        // ==== Dependency Properties ====
        public static readonly DependencyProperty DialogTitleProperty =
            DependencyProperty.Register(nameof(DialogTitle), typeof(string), typeof(DialogBasicWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register(nameof(MessageText), typeof(string), typeof(DialogBasicWindow), new PropertyMetadata(""));

        public static readonly DependencyProperty IconSourceProperty =
            DependencyProperty.Register(nameof(IconSource), typeof(object), typeof(DialogBasicWindow), new PropertyMetadata(null));

        public static readonly DependencyProperty ButtonsProperty =
            DependencyProperty.Register(nameof(Buttons), typeof(DialogButtons), typeof(DialogBasicWindow),
                new PropertyMetadata(DialogButtons.Ok, OnButtonsChanged));

        public string DialogTitle
        {
            get => (string)GetValue(DialogTitleProperty);
            set => SetValue(DialogTitleProperty, value);
        }

        public string MessageText
        {
            get => (string)GetValue(MessageTextProperty);
            set => SetValue(MessageTextProperty, value);
        }

        // Puede ser ImageSource o string (pack URI). Mantengo object para flexibilidad.
        public object IconSource
        {
            get => GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public DialogButtons Buttons
        {
            get => (DialogButtons)GetValue(ButtonsProperty);
            set => SetValue(ButtonsProperty, value);
        }

        public DialogResultEx DialogResultEx { get; private set; } = DialogResultEx.None;

        private static void OnButtonsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DialogBasicWindow w && w.IsLoaded)
            {
                w.ApplyButtons();
            }
        }

        private void ApplyButtons()
        {
            // Oculta todo y muestra según enum
            BtnOk.Visibility = Visibility.Collapsed;
            BtnCancel.Visibility = Visibility.Collapsed;
            BtnYes.Visibility = Visibility.Collapsed;
            BtnNo.Visibility = Visibility.Collapsed;

            switch (Buttons)
            {
                case DialogButtons.Ok:
                    BtnOk.Visibility = Visibility.Visible;
                    break;

                case DialogButtons.OkCancel:
                    BtnOk.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;

                case DialogButtons.YesNo:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    break;

                case DialogButtons.YesNoCancel:
                    BtnYes.Visibility = Visibility.Visible;
                    BtnNo.Visibility = Visibility.Visible;
                    BtnCancel.Visibility = Visibility.Visible;
                    break;
            }
        }

        // ==== Botones ====
        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            DialogResultEx = DialogResultEx.Ok;
            DialogResult = true;
            Close();
        }

        private void OnYesClick(object sender, RoutedEventArgs e)
        {
            DialogResultEx = DialogResultEx.Yes;
            DialogResult = true;
            Close();
        }

        private void OnNoClick(object sender, RoutedEventArgs e)
        {
            DialogResultEx = DialogResultEx.No;
            DialogResult = false;
            Close();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            DialogResultEx = DialogResultEx.Cancel;
            DialogResult = false;
            Close();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResultEx = DialogResultEx.Cancel;
            DialogResult = false;
            Close();
        }

        // ==== Helper estático para uso rápido ====
        public static DialogResultEx Show(Window owner, string title, string message,
                                          DialogButtons buttons = DialogButtons.Ok,
                                          object iconSource = null)
        {
            var dlg = new DialogBasicWindow
            {
                Owner = owner,
                DialogTitle = title,
                MessageText = message,
                Buttons = buttons,
                IconSource = iconSource
            };

            _ = dlg.ShowDialog();
            return dlg.DialogResultEx;
        }
    }
}
