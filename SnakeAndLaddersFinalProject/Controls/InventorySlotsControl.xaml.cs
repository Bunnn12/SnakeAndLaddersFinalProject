using System.Windows;
using System.Windows.Controls;

namespace SnakeAndLaddersFinalProject.Controls
{
    public partial class InventorySlotsControl : UserControl
    {
        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register(
                nameof(IsEditable),
                typeof(bool),
                typeof(InventorySlotsControl),
                new PropertyMetadata(true));

        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }

        public InventorySlotsControl()
        {
            InitializeComponent();
        }
    }
}
