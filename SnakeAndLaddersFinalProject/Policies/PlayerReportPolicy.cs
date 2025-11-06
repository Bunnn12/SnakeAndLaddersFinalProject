using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Policies
{
    public sealed class PlayerReportPolicy
    {
        private const int MIN_REGISTERED_USER_ID = 1;
        private const string GUEST_NAME_PREFIX = "Guest";

        public bool CanCurrentUserReportTarget(int currentUserId, object memberDataContext)
        {
            if (currentUserId < MIN_REGISTERED_USER_ID)
            {
                return false;
            }

            if (IsGuestMember(memberDataContext))
            {
                return false;
            }

            int memberUserId = GetMemberUserId(memberDataContext);

            if (memberUserId == currentUserId)
            {
                return false;
            }

            return true;
        }

        private static bool IsGuestMember(object dataContext)
        {
            int memberUserId = GetMemberUserId(dataContext);
            string memberUserName = GetMemberUserName(dataContext);

            if (memberUserId < MIN_REGISTERED_USER_ID)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(memberUserName) &&
                memberUserName.StartsWith(GUEST_NAME_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static int GetMemberUserId(object dataContext)
        {
            if (dataContext == null)
            {
                return 0;
            }

            var dataContextType = dataContext.GetType();
            var userIdProperty = dataContextType.GetProperty("UserId");
            if (userIdProperty == null)
            {
                return 0;
            }

            var value = userIdProperty.GetValue(dataContext, null);
            if (value is int memberUserId)
            {
                return memberUserId;
            }

            return 0;
        }

        private static string GetMemberUserName(object dataContext)
        {
            if (dataContext == null)
            {
                return string.Empty;
            }

            var dataContextType = dataContext.GetType();
            var userNameProperty = dataContextType.GetProperty("UserName");
            if (userNameProperty == null)
            {
                return string.Empty;
            }

            var value = userNameProperty.GetValue(dataContext, null);
            return value as string ?? string.Empty;
        }
    }
}
