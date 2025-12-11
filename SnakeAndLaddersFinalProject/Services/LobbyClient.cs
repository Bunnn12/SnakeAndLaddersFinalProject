using System;
using System.ServiceModel;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;

namespace SnakeAndLaddersFinalProject.Services
{
    internal sealed class LobbyClient : IDisposable
    {
        private const string LOBBY_ENDPOINT_NAME = "NetTcpBinding_ILobbyService";

        private readonly LobbyClientCallback _callback;
        private readonly LobbyServiceClient _client;
        private bool _isDisposed;

        public LobbyClient(ILobbyEventsHandler eventsHandler)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException(nameof(eventsHandler));
            }

            _callback = new LobbyClientCallback(eventsHandler);
            var context = new InstanceContext(_callback);
            _client = new LobbyServiceClient(context, LOBBY_ENDPOINT_NAME);
        }

        public LobbyServiceClient Proxy
        {
            get { return _client; }
        }

        public void SubscribePublicLobbies(int userId)
        {
            _client.SubscribePublicLobbies(userId);
        }

        public void UnsubscribePublicLobbies(int userId)
        {
            _client.UnsubscribePublicLobbies(userId);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            try
            {
                if (_client.State == CommunicationState.Faulted)
                {
                    _client.Abort();
                }
                else
                {
                    _client.Close();
                }
            }
            catch
            {
                _client.Abort();
            }
        }
    }
}
