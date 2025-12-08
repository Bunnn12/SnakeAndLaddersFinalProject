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
    public partial class SettingsPage : Page
    {
        private const string LANGUAGE_SETTING_KEY = "languageCode";
        private const string DEFAULT_LANGUAGE_CODE = "es-MX";

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

        private void InitializeLanguageSelection()
        {
            try
            {
                var savedLanguageCode = GetSavedLanguageCode();
                var languageItemsCount = cmbLanguage.Items.Count;

                for (var i = 0; i < languageItemsCount; i++)
                {
                    var languageItem = cmbLanguage.Items[i] as ComboBoxItem;
                    if (languageItem != null && string.Equals((string)languageItem.Tag, savedLanguageCode, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbLanguage.SelectedIndex = i;
                        return;
                    }
                }

                cmbLanguage.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.SettingsLanguageInitError, Lang.UiTitleError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetSavedLanguageCode()
        {
            var savedLanguageCode = SnakeAndLaddersFinalProject.Properties.Settings.Default[LANGUAGE_SETTING_KEY] as string;
            if (string.IsNullOrWhiteSpace(savedLanguageCode))
            {
                return DEFAULT_LANGUAGE_CODE;
            }
            return savedLanguageCode;
        }
        private void RefreshLocalTexts()
        {
            try
            {
                lblTitle.Content = Lang.btnSettingsText;
                lblSound.Content = Lang.lblSound;
                lblMusic.Content = Lang.lblMusic;
                lblLanguag.Content = Lang.lblLanguage;
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("RefreshLocalTexts: " + ex.Message);
            }
        }
        private void LanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedLanguageItem = cmbLanguage.SelectedItem as ComboBoxItem;
            if (selectedLanguageItem == null) return;

            try
            {
                var selectedLanguageCode = (string)selectedLanguageItem.Tag;

                var appSettings = SnakeAndLaddersFinalProject.Properties.Settings.Default;
                appSettings["languageCode"] = selectedLanguageCode;
                appSettings.Save();

                var culture = new System.Globalization.CultureInfo(selectedLanguageCode);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;

                Lang.Culture = culture;

                RefreshLocalTexts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.SettingsLanguageApplyError, Lang.UiTitleError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Back(object sender, RoutedEventArgs e)
        {
            try
            {
                var navigationService = System.Windows.Navigation.NavigationService.GetNavigationService(this);
                if (navigationService != null && navigationService.CanGoBack)
                {
                    navigationService.GoBack();
                    return;
                }

                var ancestorFrame = FindAncestorFrame(this);
                if (ancestorFrame != null && ancestorFrame.CanGoBack)
                {
                    ancestorFrame.GoBack();
                    return;
                }

                if (_returnAction != null)
                {
                    _returnAction();
                    return;
                }

                MessageBox.Show(Lang.UiNavigationNoHistory, Lang.UiTitleInfo,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show( Lang.UiNavigationBackError, Lang.UiTitleError,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static Frame FindAncestorFrame(DependencyObject startElement)
        {
            var currentElement = startElement;
            while (currentElement != null)
            {
                var frame = currentElement as Frame;
                if (frame != null) return frame;
                currentElement = VisualTreeHelper.GetParent(currentElement);
            }
            return null;
        }
    }
}

