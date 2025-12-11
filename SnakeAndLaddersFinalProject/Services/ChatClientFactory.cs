using System;
using System.Configuration;
using System.ServiceModel;
using SnakeAndLaddersFinalProject.ChatService;

namespace SnakeAndLaddersFinalProject.Services
{
    public static class ChatClientFactory
    {
        private const string CHAT_BINDING_KEY = "ChatBinding";
        private const string CHAT_ENDPOINT_ADDRESS_KEY = "ChatEndpointAddress";

        private const string DEFAULT_CHAT_BINDING = "netTcpBinding";
        private const string DEFAULT_CHAT_ENDPOINT_ADDRESS =
            "net.tcp://localhost:8087/chat";

        private const int MAX_RECEIVED_MESSAGE_SIZE_BYTES = 1_048_576;

        public static IChatService CreateFromConfig(InstanceContext instanceContext)
        {
            if (instanceContext == null)
            {
                throw new ArgumentNullException(nameof(instanceContext));
            }

            string bindingName = ConfigurationManager.AppSettings[CHAT_BINDING_KEY]
                ?? DEFAULT_CHAT_BINDING;

            string address = ConfigurationManager.AppSettings[CHAT_ENDPOINT_ADDRESS_KEY]
                ?? DEFAULT_CHAT_ENDPOINT_ADDRESS;

            if (IsHttpBinding(bindingName))
            {
                var wsBinding = new WSDualHttpBinding();
                var wsEndpoint = new EndpointAddress(address);

                return new DuplexChannelFactory<IChatService>(
                        instanceContext,
                        wsBinding,
                        wsEndpoint)
                    .CreateChannel();
            }

            var netTcpBinding = new NetTcpBinding(SecurityMode.None)
            {
                MaxReceivedMessageSize = MAX_RECEIVED_MESSAGE_SIZE_BYTES
            };

            var endpoint = new EndpointAddress(address);

            return new DuplexChannelFactory<IChatService>(
                    instanceContext,
                    netTcpBinding,
                    endpoint)
                .CreateChannel();
        }

        private static bool IsHttpBinding(string bindingName)
        {
            if (string.IsNullOrWhiteSpace(bindingName))
            {
                return false;
            }

            string normalized = bindingName.Trim();

            return normalized.Equals("basicHttpBinding", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("wsDualHttpBinding", StringComparison.OrdinalIgnoreCase);
        }
    }
}
