using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LyricsFinder.Models;

namespace LyricsFinder.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<FavoriteSong> FavoriteSongs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // SearchHistory configuration
            builder.Entity<SearchHistory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SearchTerm).IsRequired().HasMaxLength(500);
                entity.Property(e => e.SearchedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.SearchedAt);
                entity.HasIndex(e => e.UserId);
            });

            // FavoriteSong configuration
            builder.Entity<FavoriteSong>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Artist).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.AddedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.AddedAt);
            });

            // ApplicationUser configuration
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
    }
} 