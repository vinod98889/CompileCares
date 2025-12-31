// CompileCares.Core/Entities/User.cs
using CompileCares.Core.Common;
using CompileCares.Core.Entities.Doctors;

namespace CompileCares.Core.Entities
{
    public class User : BaseEntity
    {
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Role { get; private set; } // "Doctor", "Admin", "Staff"
        public bool IsActive { get; private set; }

        // For doctors
        public Guid? DoctorId { get; private set; }
        public virtual Doctor? Doctor { get; private set; }

        private User() { } // For EF Core

        public User(string firstName, string lastName, string email, string passwordHash, string role, Guid? doctorId = null)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PasswordHash = passwordHash;
            Role = role;
            DoctorId = doctorId;
            IsActive = true;
        }
    }
}