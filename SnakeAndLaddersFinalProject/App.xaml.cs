using SnakeAndLaddersFinalProject.Globalization;
using System;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;
using log4net;
using log4net.Config;

namespace SnakeAndLaddersFinalProject
{
    
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        public Cursor GlobalCursor { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
           
            var logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SnakeAndLadders", "logs");
            Directory.CreateDirectory(logsDir);
            log4net.GlobalContext.Properties["LogFileName"] = Path.Combine(logsDir, "client.log");

            
            XmlConfigurator.Configure(LogManager.GetRepository());

            
            AppDomain.CurrentDomain.UnhandledException += (s, ev) =>
                Log.Fatal("Excepción no controlada (AppDomain).", ev.ExceptionObject as Exception);

            this.DispatcherUnhandledException += (s, ev) =>
            {
                Log.Error("Excepción WPF no controlada.", ev.Exception);
                
            };

            
            try
            {
                Uri uri = new Uri("pack://application:,,,/Assets/Cursors/pixel.cur");
                StreamResourceInfo res = GetResourceStream(uri);
                GlobalCursor = new Cursor(res.Stream);
                Mouse.OverrideCursor = GlobalCursor;
            }
            catch (Exception ex)
            {
                Log.Warn("No se pudo aplicar el cursor global.", ex);
            }

            Log.Info("Cliente Snakes & Ladders iniciado.");

            base.OnStartup(e);
        }
    }
}
