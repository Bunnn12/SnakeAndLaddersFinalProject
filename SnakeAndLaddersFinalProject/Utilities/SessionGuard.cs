using SnakeAndLaddersFinalProject.Authentication;

namespace SnakeAndLaddersFinalProject.Utilities
{
    /// <summary>
    /// Solo utilidades de verificación. No navega, no muestra diálogos.
    /// </summary>
    public static class SessionGuard
    {
        public static bool HasValidSession()
        {
            SessionContext session = SessionContext.Current;

            return session.IsAuthenticated;
        }
    }
}
