using System;
using System.ServiceModel;
using SnakeAndLaddersFinalProject.Infrastructure;
using SnakeAndLaddersFinalProject.LobbyService;

namespace SnakeAndLaddersFinalProject.Services
{
    /// <summary>
    /// Encapsula la creación del cliente WCF duplex de Lobby
    /// y expone el proxy ya configurado con callbacks.
    /// </summary>
    internal sealed class LobbyClient : IDisposable
    {
        private const string LOBBY_ENDPOINT_NAME = "NetTcpBinding_ILobbyService";

        private readonly LobbyClientCallback callback;
        private readonly LobbyServiceClient client;

        private bool isDisposed;

        public LobbyClient(ILobbyEventsHandler eventsHandler)
        {
            if (eventsHandler == null)
            {
                throw new ArgumentNullException(nameof(eventsHandler));
            }

            callback = new LobbyClientCallback(eventsHandler);
            var context = new InstanceContext(callback);

            client = new LobbyServiceClient(context, LOBBY_ENDPOINT_NAME);
        }

        public LobbyServiceClient Proxy => client;

        public void SubscribePublicLobbies(int userId)
        {
            client.SubscribePublicLobbies(userId);
        }

        public void UnsubscribePublicLobbies(int userId)
        {
            client.UnsubscribePublicLobbies(userId);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            try
            {
                if (client.State == CommunicationState.Faulted)
                {
                    client.Abort();
                }
                else
                {
                    client.Close();
                }
            }
            catch
            {
                client.Abort();
            }
        }
    }
}
