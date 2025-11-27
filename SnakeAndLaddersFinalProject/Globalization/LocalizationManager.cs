using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Threading;
using SnakeAndLaddersFinalProject.Properties.Langs; 

namespace SnakeAndLaddersFinalProject.Globalization
{
    public sealed class LocalizationManager : INotifyPropertyChanged
    {
        public static LocalizationManager Current { get; } = new LocalizationManager();

        public static readonly string DEFAULT_CULTURE_CODE =
            ConfigurationManager.AppSettings["DefaultCulture"] ?? "es-MX";

        public event PropertyChangedEventHandler PropertyChanged;
        private LocalizationManager() { }

        public CultureInfo CurrentCulture
        {
            get => Lang.Culture ?? CultureInfo.CurrentUICulture; 
            private set
            {
                if (!Equals(Lang.Culture, value))
                {
                    Lang.Culture = value;
                    OnPropertyChanged(nameof(CurrentCulture));
                    OnPropertyChanged("Item[]"); 
                }
            }
        }

        public void ApplyCulture(string cultureCode = null)
        {
            var code = string.IsNullOrWhiteSpace(cultureCode) ? DEFAULT_CULTURE_CODE : cultureCode;
            var culture = new CultureInfo(code);

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            CurrentCulture = culture;
        }

        public void SetCulture(string code)
        {
            var cultureInfo = new CultureInfo(code);
            Lang.Culture = cultureInfo;                      
            Thread.CurrentThread.CurrentUICulture = cultureInfo; 
            Thread.CurrentThread.CurrentCulture = cultureInfo; 

            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged("Item[]");              
        }

        
        public string this[string resourceKey]
            => Lang.ResourceManager.GetString(resourceKey, Lang.Culture) ?? resourceKey;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
