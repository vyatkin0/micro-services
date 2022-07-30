using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MicroIdentity.Models;

namespace MicroIdentity.Infrastructure
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
    {
        //Флаг указывающий, что миграция БД уже произведена
        static bool _migrated;

        //Таблица токенов обновления пользователей
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = null!;

        //Таблица пользователей арендаторов
        public DbSet<TenantUser> TenantUsers { get; set; } = null!;

        //Таблица ролей Micro (роль Micro - это набор ролей IdentityRole)
        public DbSet<MicroRole> MicroRoles { get; set; } = null!;

        //Таблица состава ролей Micro (роль Micro - это набор ролей IdentityRole)
        public DbSet<MicroRoleRole> MicroRoleRoles { get; set; } = null!;

        //Таблица назначений ролей Micro пользователям арендаторов
        public DbSet<TenantUserMicroRole> TenantUserMicroRoles { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
            if (!_migrated)
            {
                _migrated = true;
                Database.Migrate();
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<UserRefreshToken>().HasKey(rt => new { rt.UserId, rt.TokenId });

            builder.Entity<TenantUser>().HasIndex(ut => new { ut.TenantId, ut.UserId }).IsUnique();

            builder.Entity<MicroRoleRole>().HasKey(wr => new { wr.MicroRoleId, wr.RoleId });

            builder.Entity<TenantUserMicroRole>().HasKey(utmr => new { utmr.TenantUserId, utmr.MicroRoleId });

            builder.Entity<TenantUser>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.TenantUserUsers)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TenantUser>()
                .HasOne(ut => ut.Tenant)
                .WithMany(u => u.TenantUserTenants)
                .HasForeignKey(ut => ut.TenantId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AppRole>().HasData(
                new AppRole
                {
                    Id = 1,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                },
                new AppRole
                {
                    Id = 2,
                    Name = "User",
                    NormalizedName = "USER",
                },
                new AppRole
                {
                    Id = 3,
                    Name = "GetOrder",
                    NormalizedName = "GETORDER",
                },
                new AppRole
                {
                    Id = 4,
                    Name = "CreateOrder",
                    NormalizedName = "CREATEORDER",
                },
                new AppRole
                {
                    Id = 5,
                    Name = "DeleteOrder",
                    NormalizedName = "DELETEORDER",
                },
                new AppRole
                {
                    Id = 6,
                    Name = "UpdateOrder",
                    NormalizedName = "UPDATEORDER",
                }
            );
        }
    }
}