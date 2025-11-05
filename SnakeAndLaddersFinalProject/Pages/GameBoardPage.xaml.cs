using System;
using System.Windows.Controls;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class GameBoardPage : Page
    {
        private readonly GameBoardViewModel viewModel;

        public GameBoardPage(CreateMatchOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            InitializeComponent();

            viewModel = new GameBoardViewModel(options);
            DataContext = viewModel;
        }
    }
}
