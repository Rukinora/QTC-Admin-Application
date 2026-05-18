using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace QTC_Admin_Application.Models;

public partial class WorkflowContext : DbContext
{
    public WorkflowContext()
    {
    }

    public WorkflowContext(DbContextOptions<WorkflowContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ActivityLog> ActivityLogs { get; set; }

    public virtual DbSet<InfoKey> InfoKeys { get; set; }

    public virtual DbSet<Item> Items { get; set; }

    public virtual DbSet<ItemKeyValue> ItemKeyValues { get; set; }

    public virtual DbSet<ItemStatus> ItemStatuses { get; set; }

    public virtual DbSet<SearchView> SearchViews { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<SubStatus> SubStatuses { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<TaskHistory> TaskHistories { get; set; }

    public virtual DbSet<Workflow> Workflows { get; set; }

    public virtual DbSet<WorkflowStep> WorkflowSteps { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
        }
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.ToTable("ActivityLog");

            entity.HasOne(d => d.Status).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workflow).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.WorkflowStep).WithMany(p => p.ActivityLogs)
                .HasForeignKey(d => d.WorkflowStepId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InfoKey>(entity =>
        {
            entity.ToTable("InfoKey");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.ManagerDisplay).HasMaxLength(1);
            entity.Property(e => e.SearchType).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.UserDisplay).HasMaxLength(1);

            entity.HasOne(d => d.Workflow).WithMany(p => p.InfoKeys)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.ToTable("Item");

            entity.HasIndex(e => new { e.WorkflowId, e.CurrentWorkflowStepId, e.ItemKey }, "UC_WorkflowItem").IsUnique();

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.ItemKey).HasMaxLength(400);
            entity.Property(e => e.Payload).HasMaxLength(1000);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.VerCol)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.ItemStatus).WithMany(p => p.Items)
                .HasForeignKey(d => d.ItemStatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Workflow).WithMany(p => p.Items)
                .HasForeignKey(d => d.WorkflowId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ItemKeyValue>(entity =>
        {
            entity.ToTable("ItemKeyValue");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.InfoKeyValue).HasMaxLength(200);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.Url).HasColumnName("URL");

            entity.HasOne(d => d.InfoKey).WithMany(p => p.ItemKeyValues)
                .HasForeignKey(d => d.InfoKeyId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Item).WithMany(p => p.ItemKeyValues)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<ItemStatus>(entity =>
        {
            entity.ToTable("ItemStatus");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.StatusName).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<SearchView>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("SearchView");

            entity.Property(e => e.AssignedBy).HasMaxLength(200);
            entity.Property(e => e.AssignedTo).HasMaxLength(200);
            entity.Property(e => e.CompletedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.DataType).HasMaxLength(50);
            entity.Property(e => e.InfoKeyValue).HasMaxLength(200);
            entity.Property(e => e.IsPaused).HasMaxLength(1);
            entity.Property(e => e.ItemKey).HasMaxLength(400);
            entity.Property(e => e.ManagerDisplay).HasMaxLength(1);
            entity.Property(e => e.StatusName).HasMaxLength(100);
            entity.Property(e => e.TaskRedirectUrl).HasColumnName("TaskRedirectURL");
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.UserDisplay).HasMaxLength(1);
            entity.Property(e => e.VerCol)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
            entity.Property(e => e.WorkflowStepEnabled).HasMaxLength(1);
            entity.Property(e => e.WorkflowStepName).HasMaxLength(200);
            entity.Property(e => e.WorkflowStepType).HasMaxLength(200);
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.StatusName).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<SubStatus>(entity =>
        {
            entity.ToTable("SubStatus");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.SubStatusName).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.ToTable("Task");

            entity.Property(e => e.AssignedBy).HasMaxLength(200);
            entity.Property(e => e.AssignedTo).HasMaxLength(200);
            entity.Property(e => e.CompletedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.IsPaused).HasMaxLength(1);
            entity.Property(e => e.TaskRedirectUrl).HasColumnName("TaskRedirectURL");
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.VerCol)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Status).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.SubStatus).WithMany(p => p.Tasks).HasForeignKey(d => d.SubStatusId);

            entity.HasOne(d => d.WorkflowStep).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.WorkflowStepId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TaskHistory>(entity =>
        {
            entity.ToTable("TaskHistory");

            entity.Property(e => e.AssignedBy).HasMaxLength(200);
            entity.Property(e => e.AssignedTo).HasMaxLength(200);
            entity.Property(e => e.CompletedBy).HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.IsPaused).HasMaxLength(1);
            entity.Property(e => e.TaskRedirectUrl).HasColumnName("TaskRedirectURL");
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
        });

        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.ToTable("Workflow");

            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.RecordLimit).HasDefaultValue(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
            entity.Property(e => e.WorkflowName).HasMaxLength(200);
            entity.Property(e => e.WorkflowType).HasMaxLength(200);
        });

        modelBuilder.Entity<WorkflowStep>(entity =>
        {
            entity.ToTable("WorkflowStep");

            entity.Property(e => e.AuthUrlforAssignment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("AuthURLForAssignment");
            entity.Property(e => e.CreatedBy).HasMaxLength(200);
            entity.Property(e => e.Enabled).HasMaxLength(1);
            entity.Property(e => e.SearchConfig).HasMaxLength(4000);
            entity.Property(e => e.StepDesc).HasMaxLength(500);
            entity.Property(e => e.StepName).HasMaxLength(200);
            entity.Property(e => e.StepType).HasMaxLength(200);
            entity.Property(e => e.UpdatedBy).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
