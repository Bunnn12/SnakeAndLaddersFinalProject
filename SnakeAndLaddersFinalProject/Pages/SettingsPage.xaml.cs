using SnakeAndLaddersFinalProject.Properties.Langs;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace SnakeAndLaddersFinalProject.Pages
{
    /// <summary>
    /// Página de ajustes de idioma.
    /// - Lee/escribe el código de idioma en Settings (languageCode).
    /// - Aplica la cultura a hilo y a Lang.Culture.
    /// - Recarga la UI para reflejar los textos localizados.
    /// </summary>
    public partial class SettingsPage : Page
    {
        private const string LanguageSettingKey = "languageCode";
        private const string DefaultLanguageCode = "es-MX";

        private readonly Action _returnAction;

        

        public SettingsPage()
        {
            InitializeComponent();
            InitializeLanguageSelection();
            
        }

        public SettingsPage(Action returnAction)
        {
            InitializeComponent();
            InitializeLanguageSelection();
            _returnAction = returnAction;
            RefreshLocalTexts();
        }

        /// <summary>
        /// Inicializa el ComboBox con la selección guardada.
        /// Asume que en XAML hay ComboBoxItems con Tag = "es-MX"/"en-US"/"pt-BR"/"zh-CN".
        /// </summary>
        private void InitializeLanguageSelection()
        {
            try
            {
                var current = GetSavedLanguageCode();
                var count = cmbLanguage.Items.Count;

                for (var i = 0; i < count; i++)
                {
                    var item = cmbLanguage.Items[i] as ComboBoxItem;
                    if (item != null && string.Equals((string)item.Tag, current, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbLanguage.SelectedIndex = i;
                        return;
                    }
                }

                // Si no se encontró coincidencia, selecciona el default.
                cmbLanguage.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo inicializar el idioma: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Lee el código de idioma guardado; si no existe, devuelve el default.
        /// </summary>
        private string GetSavedLanguageCode()
        {
            var raw = SnakeAndLaddersFinalProject.Properties.Settings.Default[LanguageSettingKey] as string;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return DefaultLanguageCode;
            }
            return raw;
        }

        /// <summary>
        /// Guarda el código de idioma en Settings.
        /// </summary>
        private void SaveLanguageCode(string languageCode)
        {
            SnakeAndLaddersFinalProject.Properties.Settings.Default[LanguageSettingKey] = languageCode;
            SnakeAndLaddersFinalProject.Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Aplica la cultura al hilo actual y al recurso fuertemente tipado Lang.
        /// Requiere que Lang.resx tenga Access Modifier = Public.
        /// </summary>
        private void ApplyCulture(string languageCode)
        {
            var culture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;

            // Recursos fuertemente tipados
            SnakeAndLaddersFinalProject.Properties.Langs.Lang.Culture = culture;
        }

        /// <summary>
        /// Recarga la UI para que los textos localizados (_x:Static o bindings) se reevalúen.
        /// Usa la ventana principal típica "MainWindow". Si tu shell tiene otro nombre, cámbialo aquí.
        /// </summary>


        /// <summary>
        /// Handler del ComboBox (SelectionChanged) configurado en XAML.
        /// </summary>
        /// 
        private void RefreshLocalTexts()
        {
            try
            {
                // Actualiza SOLO los controles de esta página.
                // Ajusta los nombres a los tuyos reales.
                lblTitle.Content = Lang.btnSettingsText;
                lblSound.Content = Lang.lblSound;
                lblMusic.Content = Lang.lblMusic;
                lblLanguag.Content = Lang.lblLanguage;
                
            }
            catch (Exception ex)
            {
                // evitar romper la UI por un control faltante
                System.Diagnostics.Debug.WriteLine("RefreshLocalTexts: " + ex.Message);
            }
        }
        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = cmbLanguage.SelectedItem as ComboBoxItem;
            if (item == null) return;

            try
            {
                var code = (string)item.Tag; // "es-MX" | "en-US" | "pt-BR" | "zh-CN"

                // Guardar preferencia
                var st = SnakeAndLaddersFinalProject.Properties.Settings.Default;
                st["languageCode"] = code;
                st.Save();

                // Aplicar cultura
                var culture = new System.Globalization.CultureInfo(code);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;

                Lang.Culture = culture;

                // ✅ Refresca ESTA SettingsPage (no la recrees)
                RefreshLocalTexts();
                // Listo: el back ahora te llevará a la página anterior real.
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo aplicar el idioma: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnBackClick(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1) Si hay NavigationService con historial, úsalo
                var nav = System.Windows.Navigation.NavigationService.GetNavigationService(this);
                if (nav != null && nav.CanGoBack)
                {
                    nav.GoBack();
                    return;
                }

                // 2) Busca un Frame ancestro y usa su journal
                var frame = FindAncestorFrame(this);
                if (frame != null && frame.CanGoBack)
                {
                    frame.GoBack();
                    return;
                }

                // 3) Si el host definió cómo volver, úsalo
                if (_returnAction != null)
                {
                    _returnAction();
                    return;
                }

                // 4) Último recurso: nada que hacer
                MessageBox.Show("No hay historial ni acción de retorno definida.", "Aviso",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo regresar: " + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Frame FindAncestorFrame(DependencyObject start)
        {
            var current = start;
            while (current != null)
            {
                var f = current as Frame;
                if (f != null) return f;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}

