﻿
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using price_comparator_site.Models;

namespace price_comparator_site.Data
{
    public partial class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Game> Games { get; set; }
        public DbSet<Price> Prices { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<GamePriceStatistics> GamePriceStatistics { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Game>()
                .HasIndex(g => new { g.StoreId, g.Name })
                .IsUnique();

            modelBuilder.Entity<Price>()
                .HasIndex(p => new { p.GameId, p.StoreId })
                .IsUnique();

            modelBuilder.Entity<GamePriceStatistics>()
                .HasNoKey()
                .ToView(null);
        }
    }
}
