using SnakeAndLaddersFinalProject.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using SnakeAndLaddersFinalProject.ViewModels;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class AuthClientHelper
    {
        private const string AUTH_ENDPOINT_CONFIGURATION_NAME = "BasicHttpBinding_IAuthService";

        public static async Task LogoutAsync()
        {
            SessionContext session = SessionContext.Current;

            string token = session?.AuthToken ?? string.Empty;

            if (string.IsNullOrWhiteSpace(token))
            {
                session?.Clear();
                return;
            }

            var client = new AuthService.AuthServiceClient(AUTH_ENDPOINT_CONFIGURATION_NAME);

            try
            {
                var request = new AuthService.LogoutRequestDto
                {
                    Token = token
                };

                await Task
                    .Run(() => client.Logout(request))
                    .ConfigureAwait(true);

                client.Close();
            }
            catch (EndpointNotFoundException)
            {
                client.Abort();
            }
            catch (Exception)
            {
                client.Abort();
            }
            finally
            {
                session.Clear();
            }
        }
    }
}
