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
            var s = SessionContext.Current;
            return s != null
                   && s.IsAuthenticated
                   && !string.IsNullOrWhiteSpace(s.AuthToken);
        }
    }
}
