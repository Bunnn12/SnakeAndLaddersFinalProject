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
        private readonly IFriendsService _client;

        public FriendsApi()
        {
            _client = new FriendsServiceClient("NetTcpBinding_IFriendsService");
        }

        private static string TokenOrNull()
            => SessionContext.Current?.AuthToken;

        public List<FriendListItemDto> GetFriends()
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return new List<FriendListItemDto>();
            return new List<FriendListItemDto>(_client.GetFriends(t));
        }

        public List<FriendRequestItemDto> GetIncoming()
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return new List<FriendRequestItemDto>();
            return new List<FriendRequestItemDto>(_client.GetIncomingRequests(t));
        }

        public List<FriendRequestItemDto> GetOutgoing()
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return new List<FriendRequestItemDto>();
            return new List<FriendRequestItemDto>(_client.GetOutgoingRequests(t));
        }

        public List<UserBriefDto> SearchUsers(string term, int max = DEFAULT_MAX_RESULTS)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return new List<UserBriefDto>();
            return new List<UserBriefDto>(_client.SearchUsers(t, term ?? string.Empty, max));
        }

        public FriendLinkDto SendFriendRequest(int targetUserId)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return null;
            return _client.SendFriendRequest(t, targetUserId);
        }

        public void Accept(int linkId)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return;
            _client.AcceptFriendRequest(t, linkId);
        }

        public void Reject(int linkId)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return;
            _client.RejectFriendRequest(t, linkId);
        }

        public void Cancel(int linkId)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return;
            _client.CancelFriendRequest(t, linkId);
        }

        public void Remove(int linkId)
        {
            var t = TokenOrNull();
            if (string.IsNullOrWhiteSpace(t)) return;
            _client.RemoveFriend(t, linkId);
        }

        public void Dispose()
        {
            if (_client is IClientChannel ch)
            {
                try { if (ch.State == CommunicationState.Faulted) ch.Abort(); else ch.Close(); }
                catch { ch.Abort(); }
            }
        }
    }
}
