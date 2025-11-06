using System.Windows.Controls;
using log4net;
using SnakeAndLaddersFinalProject.ViewModels;

namespace SnakeAndLaddersFinalProject.Pages
{
    public partial class GameBoardPage : Page
    {

        public GameBoardPage()
        {
            InitializeComponent();

        }

        public GameBoardViewModel ViewModel
        {
            get { return DataContext as GameBoardViewModel; }
        }
    }
}
