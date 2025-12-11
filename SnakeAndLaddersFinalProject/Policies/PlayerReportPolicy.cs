using System;

namespace SnakeAndLaddersFinalProject.Policies
{
    public static class PlayerReportPolicy
    {
        private const int MIN_REGISTERED_USER_ID = 1;

        private const string PROPERTY_USER_ID = "UserId";
        private const string PROPERTY_USER_NAME = "UserName";

        public static bool CanCurrentUserReportTarget(int currentUserId, object memberDataContext)
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

        public static int GetMemberUserId(object dataContext)
        {
            return GetMemberUserIdInternal(dataContext);
        }

        public static string GetMemberUserName(object dataContext)
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
            var userIdProperty = dataContextType.GetProperty(PROPERTY_USER_ID);
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
            var userNameProperty = dataContextType.GetProperty(PROPERTY_USER_NAME);
            if (userNameProperty == null)
            {
                return string.Empty;
            }

            var value = userNameProperty.GetValue(dataContext, null);
            return value as string ?? string.Empty;
        }
    }
}
