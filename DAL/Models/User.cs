using System;

namespace SecureBank.DAL.Models
{
    /// <summary>
    /// Represents the user entity stored in the SecureBank database.
    /// </summary>
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "Customer";
        public bool IsActive { get; set; } = true;
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
