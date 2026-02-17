using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class StatusChangeConfiguration : IEntityTypeConfiguration<StatusChange>
{
    public void Configure(EntityTypeBuilder<StatusChange> builder)
    {
        builder.ToTable("status_changes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TaskItemId).HasColumnName("task_item_id").IsRequired();
        builder.Property(s => s.FromStatus).HasColumnName("from_status").IsRequired();
        builder.Property(s => s.ToStatus).HasColumnName("to_status").IsRequired();
        builder.Property(s => s.AssignedUserId).HasColumnName("assigned_user_id").IsRequired();
        builder.Property(s => s.ChangedAt).HasColumnName("changed_at").IsRequired();

        builder.HasOne(s => s.AssignedUser)
            .WithMany()
            .HasForeignKey(s => s.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for querying history by task
        builder.HasIndex(s => s.TaskItemId).HasDatabaseName("ix_status_changes_task_item_id");
    }
}
