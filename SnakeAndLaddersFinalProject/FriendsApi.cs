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

        private readonly IFriendsService _friendsServiceClient;

        public FriendsApi()
        {
            _friendsServiceClient = new FriendsServiceClient("NetTcpBinding_IFriendsService");
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

            return new List<FriendListItemDto>(_friendsServiceClient.GetFriends(token));
        }

        public List<FriendRequestItemDto> GetIncoming()
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<FriendRequestItemDto>();
            }

            return new List<FriendRequestItemDto>(_friendsServiceClient.GetIncomingRequests(token));
        }

        public List<FriendRequestItemDto> GetOutgoing()
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<FriendRequestItemDto>();
            }

            return new List<FriendRequestItemDto>(_friendsServiceClient.GetOutgoingRequests(token));
        }

        public List<UserBriefDto> SearchUsers(string term, int max = DEFAULT_MAX_RESULTS)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return new List<UserBriefDto>();
            }

            string searchTerm = term ?? string.Empty;

            return new List<UserBriefDto>(_friendsServiceClient.SearchUsers(token, searchTerm, max));
        }

        public FriendLinkDto SendFriendRequest(int targetUserId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return _friendsServiceClient.SendFriendRequest(token, targetUserId);
        }

        public void Accept(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            _friendsServiceClient.AcceptFriendRequest(token, linkId);
        }

        public void Reject(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            _friendsServiceClient.RejectFriendRequest(token, linkId);
        }

        public void Cancel(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            _friendsServiceClient.CancelFriendRequest(token, linkId);
        }

        public void Remove(int linkId)
        {
            string token = GetTokenOrNull();
            if (string.IsNullOrWhiteSpace(token))
            {
                return;
            }

            _friendsServiceClient.RemoveFriend(token, linkId);
        }

        public void Dispose()
        {
            if (_friendsServiceClient is IClientChannel clientChannel)
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
