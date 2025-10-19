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
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        public Cursor GlobalCursor { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            // === 1) Bootstrap de logging (cliente) ===
            // Ruta: %LOCALAPPDATA%\SnakeAndLadders\logs\client.log
            var logsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SnakeAndLadders", "logs");
            Directory.CreateDirectory(logsDir);
            log4net.GlobalContext.Properties["LogFileName"] = Path.Combine(logsDir, "client.log");

            // Carga explícita del bloque <log4net> desde App.config
            XmlConfigurator.Configure(LogManager.GetRepository());

            // Captura de excepciones no controladas (útil para soporte)
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
