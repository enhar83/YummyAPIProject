using System.Threading;
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Yummy.Entity;

namespace Yummy.Data.Context
{
    public class YummyDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public YummyDbContext(DbContextOptions<YummyDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Chef> Chefs { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<Gallery> Galleries { get; set; }
        public DbSet<DiningTable> DiningTables { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            #region Identity
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<AppRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            #endregion

            #region Pricing
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");
            #endregion

            #region Fluent API

            builder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Cascade); 

            builder.Entity<Message>()
                .HasOne(m => m.AppUser)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.AppUserId)
                .IsRequired() 
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .HasOne(r => r.AppUser)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .HasOne(r => r.DiningTable)
                .WithMany(t => t.Reservations)
                .HasForeignKey(r => r.DiningTableId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Reservation>()
                .Property(r => r.ReservationStatus)
                .HasConversion<string>();

            builder.Entity<Testimonial>()
                .HasOne(t => t.AppUser)
                .WithMany(u => u.Testimonials)
                .HasForeignKey(t => t.AppUserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<AppUser>()
                .Property(x => x.RefreshToken)
                .HasMaxLength(500);
            #endregion

            #region Seed Data
            builder.Entity<DiningTable>().HasData(
                new DiningTable { DiningTableId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TableNo = "Masa 1", Capacity = 2, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("22222222-2222-2222-2222-222222222222"), TableNo = "Masa 2", Capacity = 2, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("33333333-3333-3333-3333-333333333333"), TableNo = "Masa 3", Capacity = 4, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("44444444-4444-4444-4444-444444444444"), TableNo = "Masa 4", Capacity = 4, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("55555555-5555-5555-5555-555555555555"), TableNo = "Masa 5", Capacity = 4, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("66666666-6666-6666-6666-666666666666"), TableNo = "Masa 6", Capacity = 6, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("77777777-7777-7777-7777-777777777777"), TableNo = "Masa 7", Capacity = 6, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("88888888-8888-8888-8888-888888888888"), TableNo = "Masa 8", Capacity = 8, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("99999999-9999-9999-9999-999999999999"), TableNo = "VIP 1", Capacity = 10, IsActive = true },
                new DiningTable { DiningTableId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), TableNo = "VIP 2", Capacity = 12, IsActive = true }
            );
            #endregion
        }
    }
}
