namespace SnakeAndLaddersFinalProject.Authentication
{
    /// <summary>
    /// Holds the current authenticated session data for the client application.
    /// </summary>
    public sealed class SessionContext
    {
        private const int USER_ID_NOT_SET = 0;

        private static readonly SessionContext CurrentSessionContext = new SessionContext();

        public static SessionContext Current => CurrentSessionContext;

        private SessionContext()
        {
        }

        public int UserId { get; set; } = USER_ID_NOT_SET;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string AuthToken { get; set; } = string.Empty;

        public string ProfilePhotoId { get; set; }

        public string CurrentSkinId { get; set; }

        public int? CurrentSkinUnlockedId { get; set; }

        /// <summary>
        /// Indicates whether the current session has a valid authenticated user.
        /// </summary>
        public bool IsAuthenticated =>
            UserId > USER_ID_NOT_SET &&
            !string.IsNullOrWhiteSpace(UserName);
    }
}
