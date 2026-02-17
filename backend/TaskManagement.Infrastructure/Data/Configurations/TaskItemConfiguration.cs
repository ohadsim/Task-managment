using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("id");
        builder.Property(t => t.TaskType).HasColumnName("task_type").IsRequired().HasMaxLength(50);
        builder.Property(t => t.Title).HasColumnName("title").IsRequired().HasMaxLength(200);
        builder.Property(t => t.CurrentStatus).HasColumnName("current_status").IsRequired();
        builder.Property(t => t.IsClosed).HasColumnName("is_closed").IsRequired().HasDefaultValue(false);
        builder.Property(t => t.AssignedUserId).HasColumnName("assigned_user_id").IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").IsRequired();

        // jsonb column for type-specific data
        builder.Property(t => t.CustomData)
            .HasColumnName("custom_data")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, JsonOptions)
                     ?? new Dictionary<string, object>()
            )
            .Metadata.SetValueComparer(new ValueComparer<Dictionary<string, object>>(
                (d1, d2) => JsonSerializer.Serialize(d1, JsonOptions) == JsonSerializer.Serialize(d2, JsonOptions),
                d => JsonSerializer.Serialize(d, JsonOptions).GetHashCode(),
                d => JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(d, JsonOptions), JsonOptions)!
            ));

        builder.HasOne(t => t.AssignedUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(t => t.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.StatusHistory)
            .WithOne(s => s.TaskItem)
            .HasForeignKey(s => s.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => t.AssignedUserId).HasDatabaseName("ix_tasks_assigned_user_id");
        builder.HasIndex(t => t.TaskType).HasDatabaseName("ix_tasks_task_type");
        builder.HasIndex(t => new { t.AssignedUserId, t.IsClosed }).HasDatabaseName("ix_tasks_user_closed");
    }
}
