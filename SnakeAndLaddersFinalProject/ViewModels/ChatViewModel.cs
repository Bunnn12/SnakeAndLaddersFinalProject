using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.ViewModels
{
    public sealed class ChatViewModel : INotifyPropertyChanged
    {
        private IChatService proxy;
        private string newMessage = string.Empty;

        public ObservableCollection<ChatMessageVm> Messages { get; } = new ObservableCollection<ChatMessageVm>();

        public string NewMessage
        {
            get => newMessage;
            set { newMessage = value; OnPropertyChanged(nameof(NewMessage)); }
        }

        public bool IsAutoScrollEnabled { get; set; } = true;
        public string StatusText { get; private set; } = "Ready";
        public string CurrentUserName { get; }

        public ICommand SendMessageCommand { get; }
        public ICommand CopyMessageCommand { get; }
        public ICommand QuoteMessageCommand { get; }
        public ICommand OpenStickersCommand { get; }

        public ChatViewModel()
        {
            // nombre de usuario a usar en cabecera
            CurrentUserName = ConfigurationManager.AppSettings["Chat:UserName"];
            if (string.IsNullOrWhiteSpace(CurrentUserName))
                CurrentUserName = Environment.UserName;

            proxy = CreateProxyFromConfig();

            // Cargar últimos mensajes del server
            try
            {
                var recent = proxy.GetRecent(50);
                foreach (var dto in recent)
                    Messages.Add(new ChatMessageVm(dto, CurrentUserName));
            }
            catch (Exception ex)
            {
                StatusText = $"Chat offline: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));
            }

            SendMessageCommand = new RelayCommand(_ => Send(), _ => CanSend());
            CopyMessageCommand = new RelayCommand(m => Copy(m as ChatMessageVm));
            QuoteMessageCommand = new RelayCommand(m => Quote(m as ChatMessageVm));
            OpenStickersCommand = new RelayCommand(_ => ShowStickers());
        }

        private static IChatService CreateProxyFromConfig()
        {
            string bindingName = ConfigurationManager.AppSettings["ChatBinding"] ?? "netTcpBinding";
            string address = ConfigurationManager.AppSettings["ChatEndpointAddress"] ?? "net.tcp://localhost:8087/chat";

            if (bindingName.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase))
            {
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress(address);
                return new ChannelFactory<IChatService>(binding, endpoint).CreateChannel();
            }
            else
            {
                var binding = new NetTcpBinding();
                var endpoint = new EndpointAddress(address);
                return new ChannelFactory<IChatService>(binding, endpoint).CreateChannel();
            }
        }

        private bool CanSend() => !string.IsNullOrWhiteSpace(NewMessage);

        private void Send()
        {
            var text = (NewMessage ?? string.Empty).Trim();
            if (text.Length == 0) return;

            // 1) pinta local de inmediato (optimista)
            var localDto = new ChatMessageDto
            {
                Sender = CurrentUserName,
                Text = text,
                TimestampUtc = DateTime.UtcNow
            };
            var localVm = new ChatMessageVm(localDto, CurrentUserName);
            Messages.Add(localVm);

            // limpia input
            NewMessage = string.Empty;
            OnPropertyChanged(nameof(NewMessage));

            // 2) intenta enviar al servidor
            try
            {
                var resp = proxy.SendMessage(new SendMessageRequest { Message = localDto });
                if (!resp.Ok)
                {
                    StatusText = "No se pudo enviar (resp.Ok=false).";
                    OnPropertyChanged(nameof(StatusText));
                }
            }
            catch (Exception ex)
            {
                // 3) marca visualmente que no salió
                StatusText = $"No se pudo enviar: {ex.Message}";
                OnPropertyChanged(nameof(StatusText));

                // opcional: agregar sufijo al texto local
                var idx = Messages.IndexOf(localVm);
                if (idx >= 0)
                {
                    var notSent = new ChatMessageDto
                    {
                        Sender = localDto.Sender,
                        Text = localDto.Text + "  (no enviado)",
                        TimestampUtc = localDto.TimestampUtc
                    };
                    Messages[idx] = new ChatMessageVm(notSent, CurrentUserName);
                }

                // intenta reconstruir canal para el próximo intento
                try { proxy = CreateProxyFromConfig(); } catch { /* swallow */ }
            }
        }


        private static void Copy(ChatMessageVm vm)
        {
            if (vm == null) return;
            Clipboard.SetText(vm.Text ?? "");
        }

        private void Quote(ChatMessageVm vm)
        {
            if (vm == null) return;
            NewMessage = $"> {vm.Text}\n" + NewMessage;
            OnPropertyChanged(nameof(NewMessage));
        }

        private void ShowStickers()
        {
            // Por ahora un placeholder
            MessageBox.Show("Stickers pronto ✨", "Stickers", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
