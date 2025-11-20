using System;
using System.Collections.Generic;
using System.ServiceModel;
using SnakeAndLaddersFinalProject.Authentication;
using SnakeAndLaddersFinalProject.FriendsService;

namespace SnakeAndLaddersFinalProject.Services
{
    public sealed class FriendsApi : IDisposable
    {
        private const int DEFAULT_MAX_RESULTS = 20;

        private readonly IFriendsService friendsServiceClient;

        public FriendsApi()
        {
            friendsServiceClient = new FriendsServiceClient("NetTcpBinding_IFriendsService");
        }

        private static string GetTokenOrNull()
        {
            return SessionContext.Current?.AuthToken;
        }

        public List<FriendListItemDto> GetFriends()
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<FriendListItemDto>();
            }

            return new List<FriendListItemDto>(friendsServiceClient.GetFriends(token));
        }

        public List<FriendRequestItemDto> GetIncoming()
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<FriendRequestItemDto>();
            }

            return new List<FriendRequestItemDto>(friendsServiceClient.GetIncomingRequests(token));
        }

        public List<FriendRequestItemDto> GetOutgoing()
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<FriendRequestItemDto>();
            }

            return new List<FriendRequestItemDto>(friendsServiceClient.GetOutgoingRequests(token));
        }

        public List<UserBriefDto> SearchUsers(string term, int max = DEFAULT_MAX_RESULTS)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<UserBriefDto>();
            }

            string searchTerm = term ?? string.Empty;

            return new List<UserBriefDto>(friendsServiceClient.SearchUsers(token, searchTerm, max));
        }

        public FriendLinkDto SendFriendRequest(int targetUserId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return friendsServiceClient.SendFriendRequest(token, targetUserId);
        }

        public void Accept(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            friendsServiceClient.AcceptFriendRequest(token, linkId);
        }

        public void Reject(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            friendsServiceClient.RejectFriendRequest(token, linkId);
        }

        public void Cancel(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            friendsServiceClient.CancelFriendRequest(token, linkId);
        }

        public void Remove(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            friendsServiceClient.RemoveFriend(token, linkId);
        }

        public void Dispose()
        {
            if (friendsServiceClient is IClientChannel clientChannel)
            {
                try
                {
                    if (clientChannel.State == CommunicationState.Faulted)
                    {
                        clientChannel.Abort();
                    }
                    else
                    {
                        clientChannel.Close();
                    }
                }
                catch
                {
                    clientChannel.Abort();
                }
            }
        }
    }
}
