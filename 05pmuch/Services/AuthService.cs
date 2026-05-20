using System;
using System.Collections.Generic;
using System.Linq;
using _05pmuch.Data;
using _05pmuch.Models;

namespace _05pmuch.Services
{
    public enum LoginResult
    {
        Success,
        InvalidCredentials,
        LockedOut
    }

    public class AuthService
    {
        private const int MaxFailedAttempts = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromSeconds(60);

        public bool Register(string login, string password, string fullName, int roleId = RoleIds.User)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));
            if (string.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            var normalized = login.Trim();

            using (var db = new ApplicationDbContext())
            {
                var exists = db.Users.Any(u => u.Login.ToLower() == normalized.ToLower());
                if (exists) return false;

                var salt = PasswordHasher.GenerateSalt();
                var hash = PasswordHasher.ComputeHash(password, salt);

                db.Users.Add(new User
                {
                    Login = normalized,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? normalized : fullName.Trim(),
                    PasswordHash = hash,
                    PasswordSalt = salt,
                    RoleId = roleId,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });

                db.SaveChanges();
                return true;
            }
        }

        public LoginResult Login(string login, string password)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));
            if (password == null) throw new ArgumentNullException(nameof(password));

            var normalized = login.Trim();

            using (var db = new ApplicationDbContext())
            {
                bool locked = IsLockedOutInternal(db, normalized);

                var attempt = new LoginAttempt
                {
                    Login = normalized,
                    AttemptedAt = DateTime.Now,
                    IsSuccessful = false
                };

                if (locked)
                    return LoginResult.LockedOut;

                var user = db.Users.FirstOrDefault(u => u.Login.ToLower() == normalized.ToLower());
                if (user == null || !user.IsActive)
                {
                    db.LoginAttempts.Add(attempt);
                    db.SaveChanges();
                    return LoginResult.InvalidCredentials;
                }

                var computed = PasswordHasher.ComputeHash(password, user.PasswordSalt);
                if (SlowEquals(computed, user.PasswordHash))
                {
                    attempt.IsSuccessful = true;
                    db.LoginAttempts.Add(attempt);
                    db.SaveChanges();
                    return LoginResult.Success;
                }

                db.LoginAttempts.Add(attempt);
                db.SaveChanges();

                if (IsLockedOutInternal(db, normalized))
                    return LoginResult.LockedOut;

                return LoginResult.InvalidCredentials;
            }
        }

        public User GetUserByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;

            var normalized = login.Trim();
            using (var db = new ApplicationDbContext())
            {
                return db.Users.FirstOrDefault(u => u.Login.ToLower() == normalized.ToLower());
            }
        }

        public bool IsLockedOut(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));

            using (var db = new ApplicationDbContext())
            {
                return IsLockedOutInternal(db, login.Trim());
            }
        }

        public TimeSpan GetLockoutRemaining(string login)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));

            using (var db = new ApplicationDbContext())
            {
                return GetLockoutRemainingInternal(db, login.Trim());
            }
        }

        public void RecordLoginAttempt(string login, bool isSuccessful)
        {
            if (string.IsNullOrWhiteSpace(login)) throw new ArgumentNullException(nameof(login));
            using (var db = new ApplicationDbContext())
            {
                db.LoginAttempts.Add(new LoginAttempt
                {
                    Login = login.Trim(),
                    AttemptedAt = DateTime.Now,
                    IsSuccessful = isSuccessful
                });
                db.SaveChanges();
            }
        }

        private bool IsLockedOutInternal(ApplicationDbContext db, string loginNormalized)
        {
            var loginKey = loginNormalized.ToLower();
            var attempts = db.LoginAttempts
                .Where(a => a.Login.ToLower() == loginKey)
                .OrderByDescending(a => a.AttemptedAt)
                .Take(10)
                .ToList();

            if (!TryGetThirdFailureTime(attempts, out var thirdFailureTime))
                return false;

            return DateTime.Now - thirdFailureTime < LockoutDuration;
        }

        private TimeSpan GetLockoutRemainingInternal(ApplicationDbContext db, string loginNormalized)
        {
            var loginKey = loginNormalized.ToLower();
            var attempts = db.LoginAttempts
                .Where(a => a.Login.ToLower() == loginKey)
                .OrderByDescending(a => a.AttemptedAt)
                .Take(10)
                .ToList();

            if (!TryGetThirdFailureTime(attempts, out var thirdFailureTime))
                return TimeSpan.Zero;

            var elapsed = DateTime.Now - thirdFailureTime;
            if (elapsed < LockoutDuration)
                return LockoutDuration - elapsed;

            return TimeSpan.Zero;
        }

        private static bool TryGetThirdFailureTime(List<LoginAttempt> attempts, out DateTime thirdFailureTime)
        {
            thirdFailureTime = default;
            int consecutiveFailures = 0;

            foreach (var a in attempts)
            {
                if (a.IsSuccessful)
                    break;

                consecutiveFailures++;
                if (consecutiveFailures == MaxFailedAttempts)
                {
                    thirdFailureTime = a.AttemptedAt;
                    return true;
                }
            }

            return false;
        }

        private static bool SlowEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            var ba = System.Text.Encoding.UTF8.GetBytes(a);
            var bb = System.Text.Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length) return false;
            int diff = 0;
            for (int i = 0; i < ba.Length; i++) diff |= ba[i] ^ bb[i];
            return diff == 0;
        }
    }
}
