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
<<<<<<< HEAD
            var page = new SnakeAndLaddersFinalProject.Pages.StartPage();

            MainFrame.Navigate(page);

            

          
=======
            MainFrame.Navigate(new Pages.LoginPage());
>>>>>>> 370440b8c9877ca09b332834fa1fc66793420169
        }
    }
}
