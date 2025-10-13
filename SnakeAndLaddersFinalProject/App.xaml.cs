using SnakeAndLaddersFinalProject.Globalization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;

namespace SnakeAndLaddersFinalProject
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        public Cursor GlobalCursor { get; private set; }
        protected override void OnStartup(StartupEventArgs e)
        {
            

            base.OnStartup(e);
            //  LocalizationManager.Current.SetCulture(
            //    ConfigurationManager.AppSettings["DefaultCulture"] ?? "es-MX");

           
            Uri uri = new Uri("pack://application:,,,/Assets/Cursors/pixel.cur");
            StreamResourceInfo res = GetResourceStream(uri);
            GlobalCursor = new Cursor(res.Stream);

            Mouse.OverrideCursor = GlobalCursor;




        }
    }
}
