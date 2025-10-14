using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;



namespace SnakeAndLaddersFinalProject
{
   
    public partial class BasicWindow : Window
    {
        public BasicWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var page = new SnakeAndLaddersFinalProject.Pages.LoginPage();

            MainFrame.Navigate(page);

  
            MainFrame.Navigate(new Pages.LoginPage());

        }
    }
}
