using SnakeAndLaddersFinalProject.Authentication;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public static class SessionGuard
    {
        public static bool HasValidSession()
        {
            SessionContext sessionContext = SessionContext.Current;

            return sessionContext != null && sessionContext.IsAuthenticated;
        }
    }
}
