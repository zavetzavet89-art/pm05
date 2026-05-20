using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using _05pmuch.Models;
using _05pmuch.Services;

namespace _05pmuch.Data
{
    public static class DatabaseBootstrap
    {
        private static readonly string[] RequiredTables =
        {
            "Roles", "Users", "LoginAttempts",
            "Countries", "Tours", "Clients", "Bookings", "Payments"
        };

        public static string GetDatabasePath()
        {
            var dataDir = AppDomain.CurrentDomain.GetData("DataDirectory") as string
                          ?? AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(dataDir, "app.db");
        }

        public static bool IsDatabaseValid(string path)
        {
            return File.Exists(path)
                   && !IsEmptyFile(path)
                   && HasAllRequiredTables(path);
        }

        public static void EnsureReady()
        {
            var path = GetDatabasePath();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            if (!File.Exists(path) || IsEmptyFile(path))
            {
                if (File.Exists(path))
                    SafeDeleteDatabaseFile(path);
                CreateSchema(path);
            }
            else
            {
                UpgradeSchema(path);
            }

            if (!IsDatabaseValid(path))
            {
                throw new InvalidOperationException(
                    "Не удалось подготовить базу данных.\nУдалите app.db и запустите снова:\n" + path);
            }

            using (var db = new ApplicationDbContext())
            {
                SeedRolesIfEmpty(db);
                SeedDefaultUsersIfEmpty(db);
                SeedSampleDataIfEmpty(db);
            }
        }

        private static void CreateSchema(string path)
        {
            using (var conn = Open(path))
            using (var tx = conn.BeginTransaction())
            {
                Execute(conn, tx, @"
CREATE TABLE Roles (
    Id INTEGER PRIMARY KEY NOT NULL,
    Name TEXT NOT NULL
);");

                Execute(conn, tx, @"
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Login TEXT NOT NULL,
    FullName TEXT NOT NULL,
    PasswordHash TEXT NOT NULL,
    PasswordSalt TEXT NOT NULL,
    RoleId INTEGER NOT NULL,
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL
);");

                Execute(conn, tx, @"
CREATE TABLE LoginAttempts (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Login TEXT NOT NULL,
    AttemptedAt DATETIME NOT NULL,
    IsSuccessful INTEGER NOT NULL
);");

                Execute(conn, tx, @"
CREATE TABLE Countries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    CountryName TEXT NOT NULL
);");

                Execute(conn, tx, @"
CREATE TABLE Tours (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TourName TEXT NOT NULL,
    CountryId INTEGER NOT NULL,
    BaseCost REAL NOT NULL,
    HotelStars INTEGER NOT NULL DEFAULT 3,
    IsAvailable INTEGER NOT NULL DEFAULT 1
);");

                Execute(conn, tx, @"
CREATE TABLE Clients (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    FullName TEXT NOT NULL,
    PassportNumber TEXT NOT NULL DEFAULT '',
    PhoneNumber TEXT NOT NULL DEFAULT '',
    LoyaltyPoints INTEGER NOT NULL DEFAULT 0,
    CurrentDiscount REAL NOT NULL DEFAULT 0
);");

                Execute(conn, tx, @"
CREATE TABLE Bookings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    ClientId INTEGER NOT NULL,
    TourId INTEGER NOT NULL,
    DateCreated DATETIME NOT NULL,
    FinalAmount REAL NOT NULL,
    BookingStatus TEXT NOT NULL
);");

                Execute(conn, tx, @"
CREATE TABLE Payments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    BookingId INTEGER NOT NULL,
    SumPaid REAL NOT NULL,
    DatePaid DATETIME NOT NULL
);");

                tx.Commit();
            }

            SQLiteConnection.ClearAllPools();
        }

        private static void UpgradeSchema(string path)
        {
            using (var conn = Open(path))
            {
                EnsureTable(conn, "Roles", @"
CREATE TABLE Roles (
    Id INTEGER PRIMARY KEY NOT NULL,
    Name TEXT NOT NULL
);");

                EnsureColumn(conn, "Users", "FullName", "TEXT NOT NULL DEFAULT ''");
                EnsureColumn(conn, "Users", "IsActive", "INTEGER NOT NULL DEFAULT 1");
                EnsureColumn(conn, "Users", "PasswordSalt", "TEXT NOT NULL DEFAULT ''");
                Execute(conn, null, "UPDATE Users SET FullName = Login WHERE FullName = '' OR FullName IS NULL");

                EnsureTable(conn, "Countries", @"
CREATE TABLE Countries (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    CountryName TEXT NOT NULL
);");

                EnsureTable(conn, "Tours", @"
CREATE TABLE Tours (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TourName TEXT NOT NULL,
    CountryId INTEGER NOT NULL,
    BaseCost REAL NOT NULL,
    HotelStars INTEGER NOT NULL DEFAULT 3,
    IsAvailable INTEGER NOT NULL DEFAULT 1
);");

                EnsureColumn(conn, "Tours", "HotelStars", "INTEGER NOT NULL DEFAULT 3");
                EnsureColumn(conn, "Tours", "IsAvailable", "INTEGER NOT NULL DEFAULT 1");

                EnsureTable(conn, "Clients", @"
CREATE TABLE Clients (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    FullName TEXT NOT NULL,
    PassportNumber TEXT NOT NULL DEFAULT '',
    PhoneNumber TEXT NOT NULL DEFAULT '',
    LoyaltyPoints INTEGER NOT NULL DEFAULT 0,
    CurrentDiscount REAL NOT NULL DEFAULT 0
);");

                EnsureColumn(conn, "Clients", "LoyaltyPoints", "INTEGER NOT NULL DEFAULT 0");
                EnsureColumn(conn, "Clients", "CurrentDiscount", "REAL NOT NULL DEFAULT 0");

                EnsureTable(conn, "Bookings", @"
CREATE TABLE Bookings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    ClientId INTEGER NOT NULL,
    TourId INTEGER NOT NULL,
    DateCreated DATETIME NOT NULL,
    FinalAmount REAL NOT NULL DEFAULT 0,
    BookingStatus TEXT NOT NULL
);");

                EnsureColumn(conn, "Bookings", "FinalAmount", "REAL NOT NULL DEFAULT 0");

                EnsureTable(conn, "Payments", @"
CREATE TABLE Payments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    BookingId INTEGER NOT NULL,
    SumPaid REAL NOT NULL,
    DatePaid DATETIME NOT NULL
);");
            }

            SQLiteConnection.ClearAllPools();
        }

        private static SQLiteConnection Open(string path)
        {
            var conn = new SQLiteConnection($"Data Source={path};Version=3;");
            conn.Open();
            return conn;
        }

        private static void EnsureTable(SQLiteConnection conn, string table, string createSql)
        {
            if (!TableExists(conn, table))
                Execute(conn, null, createSql);
        }

        private static void EnsureColumn(SQLiteConnection conn, string table, string column, string definition)
        {
            if (!ColumnExists(conn, table, column))
                Execute(conn, null, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
        }

        private static bool ColumnExists(IDbConnection conn, string table, string column)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info([{table}])";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            return false;
        }

        private static void Execute(SQLiteConnection conn, SQLiteTransaction tx, string sql)
        {
            using (var cmd = conn.CreateCommand())
            {
                if (tx != null)
                    cmd.Transaction = tx;
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private static bool IsEmptyFile(string path) => new FileInfo(path).Length == 0;

        private static bool HasAllRequiredTables(string path)
        {
            try
            {
                using (var conn = Open(path))
                {
                    foreach (var table in RequiredTables)
                    {
                        if (!TableExists(conn, table))
                            return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TableExists(IDbConnection conn, string tableName)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @name";
                var p = cmd.CreateParameter();
                p.ParameterName = "@name";
                p.Value = tableName;
                cmd.Parameters.Add(p);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void SafeDeleteDatabaseFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            var directory = Path.GetDirectoryName(path);
            var baseName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(baseName))
                return;

            SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            foreach (var file in Directory.GetFiles(directory, baseName + "*"))
            {
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    try
                    {
                        if (File.Exists(file))
                            File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                        break;
                    }
                    catch (IOException) when (attempt < 4)
                    {
                        System.Threading.Thread.Sleep(150);
                    }
                }
            }
        }

        private static void SeedRolesIfEmpty(ApplicationDbContext db)
        {
            if (db.Roles.Any())
                return;

            db.Roles.Add(new Role { Id = RoleIds.Admin, Name = "admin" });
            db.Roles.Add(new Role { Id = RoleIds.Operator, Name = "operator" });
            db.Roles.Add(new Role { Id = RoleIds.User, Name = "user" });
            db.SaveChanges();
        }

        private static void SeedDefaultUsersIfEmpty(ApplicationDbContext db)
        {
            if (db.Users.Any())
                return;

            AddUser(db, "admin", "Директор / Администратор", "admin123", RoleIds.Admin);
            AddUser(db, "operator", "Менеджер по продажам", "oper123", RoleIds.Operator);
            AddUser(db, "user", "Справочный аккаунт", "user123", RoleIds.User);
            db.SaveChanges();
        }

        private static void SeedSampleDataIfEmpty(ApplicationDbContext db)
        {
            if (db.Countries.Any())
                return;

            var greece = new Country { CountryName = "Греция" };
            var spain = new Country { CountryName = "Испания" };
            var thailand = new Country { CountryName = "Таиланд" };
            var italy = new Country { CountryName = "Италия" };
            db.Countries.Add(greece);
            db.Countries.Add(spain);
            db.Countries.Add(thailand);
            db.Countries.Add(italy);
            db.SaveChanges();

            db.Tours.Add(new Tour
            {
                TourName = "Золотые пески",
                CountryId = greece.Id,
                BaseCost = 72000m,
                HotelStars = 4,
                IsAvailable = true
            });
            db.Tours.Add(new Tour
            {
                TourName = "Крит, all inclusive",
                CountryId = greece.Id,
                BaseCost = 95000m,
                HotelStars = 5,
                IsAvailable = true
            });
            db.Tours.Add(new Tour
            {
                TourName = "Барcelona Express",
                CountryId = spain.Id,
                BaseCost = 68000m,
                HotelStars = 3,
                IsAvailable = true
            });
            db.Tours.Add(new Tour
            {
                TourName = "Пхукет Premium",
                CountryId = thailand.Id,
                BaseCost = 110000m,
                HotelStars = 5,
                IsAvailable = true
            });
            db.Tours.Add(new Tour
            {
                TourName = "Римская классика",
                CountryId = italy.Id,
                BaseCost = 82000m,
                HotelStars = 4,
                IsAvailable = true
            });
            db.Tours.Add(new Tour
            {
                TourName = "Бюджетный отдых в Паттайе",
                CountryId = thailand.Id,
                BaseCost = 45000m,
                HotelStars = 2,
                IsAvailable = true
            });
            db.SaveChanges();

            db.Clients.Add(new Client
            {
                FullName = "Петрова Анна Сергеевна",
                PassportNumber = "4510 123456",
                PhoneNumber = "+7 (916) 555-12-34",
                LoyaltyPoints = 350,
                CurrentDiscount = 5m
            });
            db.Clients.Add(new Client
            {
                FullName = "Сидоров Иван Петрович",
                PassportNumber = "4508 987654",
                PhoneNumber = "+7 (903) 111-22-33",
                LoyaltyPoints = 0,
                CurrentDiscount = 0m
            });
            db.Clients.Add(new Client
            {
                FullName = "Козлова Мария Александровна",
                PassportNumber = "4012 556677",
                PhoneNumber = "+7 (925) 777-88-99",
                LoyaltyPoints = 1200,
                CurrentDiscount = 10m
            });
            db.SaveChanges();
        }

        private static void AddUser(ApplicationDbContext db, string login, string fullName, string password, int roleId)
        {
            var salt = PasswordHasher.GenerateSalt();
            db.Users.Add(new User
            {
                Login = login,
                FullName = fullName,
                PasswordHash = PasswordHasher.ComputeHash(password, salt),
                PasswordSalt = salt,
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
        }
    }
}
