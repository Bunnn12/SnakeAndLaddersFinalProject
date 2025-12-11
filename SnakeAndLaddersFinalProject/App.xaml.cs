using log4net;
using log4net.Config;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;

namespace SnakeAndLaddersFinalProject
{
    public partial class App : Application
    {
        private const string DEFAULT_LANGUAGE_CODE = "es-MX";
        private const string LOG_ROOT_FOLDER_NAME = "SnakeAndLadders";
        private const string LOG_FOLDER_NAME = "logs";
        private const string LOG_FILE_NAME = "_client.log";
        private const string CURSOR_RESOURCE_PATH = "Assets/Cursors/pixel.cur";

        private static readonly ILog Log = LogManager.GetLogger(typeof(App));

        public Cursor GlobalCursor { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            ConfigureCulture();
            ConfigureLogging();
            RegisterGlobalExceptionHandlers();
            InitializeGlobalCursor();

            Log.Info("Cliente Snakes & Ladders iniciado.");

            base.OnStartup(e);
        }

        private static void ConfigureCulture()
        {
            string languageCode =
                SnakeAndLaddersFinalProject.Properties.Settings.Default["languageCode"] as string;

            if (string.IsNullOrWhiteSpace(languageCode))
            {
                languageCode = DEFAULT_LANGUAGE_CODE;
            }

            var culture = new CultureInfo(languageCode);

            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            SnakeAndLaddersFinalProject.Properties.Langs.Lang.Culture = culture;
        }

        private static void ConfigureLogging()
        {
            string baseDirectory =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string logsDirectory = Path.Combine(
                baseDirectory,
                LOG_ROOT_FOLDER_NAME,
                LOG_FOLDER_NAME);

            Directory.CreateDirectory(logsDirectory);

            GlobalContext.Properties["LogFileName"] =
                Path.Combine(logsDirectory, LOG_FILE_NAME);

            XmlConfigurator.Configure(LogManager.GetRepository());
        }

        private void RegisterGlobalExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Log.Fatal(
                    "Excepción no controlada (AppDomain).",
                    args.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                Log.Error("Excepción WPF no controlada.", args.Exception);
            };
        }

        private void InitializeGlobalCursor()
        {
            try
            {
                var cursorUri = new Uri(CURSOR_RESOURCE_PATH, UriKind.Relative);
                StreamResourceInfo resourceInfo = GetResourceStream(cursorUri);

                if (resourceInfo == null || resourceInfo.Stream == null)
                {
                    Log.Warn("No se encontró el recurso de cursor global.");
                    return;
                }

                GlobalCursor = new Cursor(resourceInfo.Stream);
                Mouse.OverrideCursor = GlobalCursor;
            }
            catch (Exception ex)
            {
                Log.Warn("No se pudo aplicar el cursor global.", ex);
            }
        }
    }
}
