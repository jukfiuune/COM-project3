using System;

namespace COM_project3.Core.Entities
{
    public class RefreshToken
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string TokenHash { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsRevoked { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}
