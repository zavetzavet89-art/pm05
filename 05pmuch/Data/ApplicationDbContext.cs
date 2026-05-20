using System.Data.Entity;
using _05pmuch.Models;

namespace _05pmuch.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
            : base("ApplicationDbContext")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<LoginAttempt>().ToTable("LoginAttempts");
            modelBuilder.Entity<Country>().ToTable("Countries");
            modelBuilder.Entity<Tour>().ToTable("Tours");
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<Payment>().ToTable("Payments");
            base.OnModelCreating(modelBuilder);
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
    }
}
