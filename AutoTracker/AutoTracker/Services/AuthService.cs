using AutoTracker.Data;
using AutoTracker.Models;
using AutoTracker.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace AutoTracker.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<AppUser> _passwordHasher;

        public AuthService(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<AppUser>();
        }

        public bool EmailExists(string email) => _context.AppUsers.Any(u => u.Email == email.Trim().ToLowerInvariant());
        public bool HasUsers() => _context.AppUsers.Any();

        public AppUser Register(RegisterViewModel model)
        {
            var firstUser = !_context.AppUsers.Any(u => u.Role == "Administrator");
            var company = _context.Companies.OrderBy(c => c.Id).FirstOrDefault();
            if (company == null)
            {
                company = new Company { Name = "Default Company", IsActive = true, CreatedAt = DateTime.Now };
                _context.Companies.Add(company);
                _context.SaveChanges();
            }

            var user = new AppUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                // Public registration is deliberately locked down:
                // only the very first user can become Owner. Everyone after that starts inactive.
                Role = firstUser ? "Administrator" : "Technician",
                IsActive = firstUser,
                CompanyId = company.Id,
                CreatedAt = DateTime.Now
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            _context.AppUsers.Add(user);
            _context.SaveChanges();
            return user;
        }

        public AppUser? Login(LoginViewModel model)
        {
            var email = model.Email.Trim().ToLowerInvariant();
            var user = _context.AppUsers.FirstOrDefault(u => u.Email == email && u.IsActive);
            if (user == null) return null;
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed) return null;
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            user.LastLoginAtUtc = DateTime.UtcNow;
            _context.SaveChanges();
            return user;
        }
    }
}
