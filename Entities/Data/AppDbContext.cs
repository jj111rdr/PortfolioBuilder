using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Portfolio_Builder.Entities.Models;

namespace Portfolio_Builder.Entities.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Portfolio> Portfolios { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Portfolio>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())", "DF_Portfolios_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Portfolios_CreatedAt");

            entity.HasOne(d => d.UsernameNavigation).WithOne(p => p.Portfolio)
                .HasPrincipalKey<User>(p => p.Username)
                .HasForeignKey<Portfolio>(d => d.Username);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())", "DF_Users_Id");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())", "DF_Users_CreatedAt");
            entity.Property(e => e.Role).HasDefaultValue("user", "DF_Users_Role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
