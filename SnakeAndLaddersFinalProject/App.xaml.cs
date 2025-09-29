using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SnakeAndLaddersFinalProject.Globalization;

namespace SnakeAndLaddersFinalProject
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            

            base.OnStartup(e);
            LocalizationManager.Current.SetCulture(
                ConfigurationManager.AppSettings["DefaultCulture"] ?? "es-MX");
        }
    }
}
