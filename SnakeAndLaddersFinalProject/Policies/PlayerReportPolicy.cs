using System;

namespace SnakeAndLaddersFinalProject.Policies
{
    public sealed class PlayerReportPolicy
    {
        private const int MIN_REGISTERED_USER_ID = 1;

        public bool CanCurrentUserReportTarget(int currentUserId, object memberDataContext)
        {
            if (currentUserId < MIN_REGISTERED_USER_ID)
            {
                return false;
            }

            int memberUserId = GetMemberUserIdInternal(memberDataContext);

            if (memberUserId < MIN_REGISTERED_USER_ID)
            {
                return false;
            }

            if (memberUserId == currentUserId)
            {
                return false;
            }

            return true;
        }

        public int GetMemberUserId(object dataContext)
        {
            return GetMemberUserIdInternal(dataContext);
        }

        public string GetMemberUserName(object dataContext)
        {
            return GetMemberUserNameInternal(dataContext);
        }

        private static int GetMemberUserIdInternal(object dataContext)
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
            return value is int memberUserId ? memberUserId : 0;
        }

        private static string GetMemberUserNameInternal(object dataContext)
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
