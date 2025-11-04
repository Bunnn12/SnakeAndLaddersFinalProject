using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Authentication
{
    public sealed class SessionContext
    {
        private static readonly SessionContext _current = new SessionContext();
        public static SessionContext Current => _current;

        private SessionContext() { }

        public int UserId { get; set; } = 0;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AuthToken { get; set; } = string.Empty;
        public string ProfilePhotoId { get; set; }

        public bool IsAuthenticated =>
            UserId > 0 && !string.IsNullOrWhiteSpace(UserName);
    }
}
