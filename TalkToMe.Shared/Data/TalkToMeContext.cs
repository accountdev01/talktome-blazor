using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using TalkToMe.Shared.Models;

namespace TalkToMe.Shared.Data;

public partial class TalkToMeContext : DbContext
{
    public TalkToMeContext()
    {
    }

    public TalkToMeContext(DbContextOptions<TalkToMeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<ComplaintAttachment> ComplaintAttachments { get; set; }

    public virtual DbSet<Configuration> Configurations { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Status).HasDefaultValue(1);
            entity.Property(e => e.Title).HasMaxLength(36);
        });

        modelBuilder.Entity<ComplaintAttachment>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.ComplaintId).HasMaxLength(36);
            entity.Property(e => e.FileName).HasMaxLength(150);
            entity.Property(e => e.FilePath).HasMaxLength(255);
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Complaint).WithMany(p => p.ComplaintAttachments)
                .HasForeignKey(d => d.ComplaintId)
                .HasConstraintName("FK_ComplaintAttachments_Complaints");
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.Key);

            entity.Property(e => e.Key).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasDefaultValue("System");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Value).HasMaxLength(100);
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .HasDefaultValueSql("(newid())");
            entity.Property(e => e.Description).HasMaxLength(150);
            entity.Property(e => e.Source)
                .HasMaxLength(10)
                .IsFixedLength();
            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.User).HasMaxLength(100);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
