using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SaaSRestAPI.Models;

public partial class SaaSRestAPIDBContext : DbContext
{
    public SaaSRestAPIDBContext()
    {
    }

    public SaaSRestAPIDBContext(DbContextOptions<SaaSRestAPIDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<Todo> Todos { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-IMF8D9T\\MSSQLSERVER03;Database=SaaSRestAPIDB;Trusted_Connection=True;Trust Server Certificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.ItemId).HasName("PK__Items__727E83EBAE885128");

            entity.HasOne(d => d.BelongsToNavigation).WithMany(p => p.Items)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Items__BelongsTo__3F466844");
        });

        modelBuilder.Entity<Todo>(entity =>
        {
            entity.HasKey(e => e.TodoId).HasName("PK__Todos__958625720788395A");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Todos)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Todos__CreatedBy__3B75D760");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC6168DB13");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
