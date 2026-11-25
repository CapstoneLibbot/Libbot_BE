using LiBot.Domain.Identity;
using LiBot.Domain.Inventory;
using LiBot.Domain.Library;
using LiBot.Domain.Job;
using LiBot.Domain.Workflow;
using LiBot.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RobotEntity = LiBot.Domain.Robot.Robot;
using RobotLog = LiBot.Domain.Robot.RobotLog;
using ConfigBatteryRobot = LiBot.Domain.Robot.ConfigBatteryRobot;

namespace LiBot.Infrastructure.Persistence;

public class LiBotDbContext(DbContextOptions<LiBotDbContext> options) : DbContext(options)
{
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PersistPendingJobTaskState();
        DetachSimulationJobEvents();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await PersistPendingJobTaskStateAsync(cancellationToken);
        await DetachSimulationJobEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RobotEntity> Robots => Set<RobotEntity>();
    public DbSet<RobotLog> RobotLogs => Set<RobotLog>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookItem> BookItems => Set<BookItem>();
    public DbSet<LibraryMap> Maps => Set<LibraryMap>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Aisle> Aisles => Set<Aisle>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<ShelfLevel> ShelfLevels => Set<ShelfLevel>();
    public DbSet<Endpoint> Endpoints => Set<Endpoint>();
    public DbSet<EndpointGroup> EndpointGroups => Set<EndpointGroup>();
    public DbSet<EndpointGroupMember> EndpointGroupMembers => Set<EndpointGroupMember>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobTask> JobTasks => Set<JobTask>();
    public DbSet<JobStep> JobSteps => Set<JobStep>();
    public DbSet<JobEvent> JobEvents => Set<JobEvent>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<InventoryReport> InventoryReports => Set<InventoryReport>();
    public DbSet<PatrolReport> PatrolReports => Set<PatrolReport>();
    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<TaskStep> TaskSteps => Set<TaskStep>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<BookIssueDetail> BookIssueDetails => Set<BookIssueDetail>();
    public DbSet<InventoryReportDetail> InventoryReportDetails => Set<InventoryReportDetail>();
    public DbSet<ConfigBatteryRobot> BatteryRobotConfigs => Set<ConfigBatteryRobot>();

    private void PersistPendingJobTaskState()
    {
        foreach (var entry in ChangeTracker.Entries<JobTask>()
                     .Where(item => item.Entity.RuntimeId != "sim"
                         && (item.State == EntityState.Added || item.Entity.HasUnpersistedPersistenceState)))
        {
            var task = entry.Entity;
            var step = JobSteps.Local.Where(item => item.JobTaskId == task.Id)
                .OrderBy(item => item.Sequence).FirstOrDefault()
                ?? JobSteps.AsNoTracking().Where(item => item.JobTaskId == task.Id)
                    .OrderBy(item => item.Sequence).FirstOrDefault();
            if (step is null) continue;

            JobEvents.Add(new JobEvent(step.Id, JobEventPersistence.JobTaskStateEventType,
                task.SerializePersistenceState()));
            task.MarkPersistenceStatePersisted();
        }
    }

    private async Task PersistPendingJobTaskStateAsync(CancellationToken cancellationToken)
    {
        foreach (var entry in ChangeTracker.Entries<JobTask>()
                     .Where(item => item.Entity.RuntimeId != "sim"
                         && (item.State == EntityState.Added || item.Entity.HasUnpersistedPersistenceState)))
        {
            var task = entry.Entity;
            var step = JobSteps.Local.Where(item => item.JobTaskId == task.Id)
                .OrderBy(item => item.Sequence).FirstOrDefault();
            if (step is null)
                step = await JobSteps.AsNoTracking().Where(item => item.JobTaskId == task.Id)
                    .OrderBy(item => item.Sequence).FirstOrDefaultAsync(cancellationToken);
            if (step is null) continue;

            JobEvents.Add(new JobEvent(step.Id, JobEventPersistence.JobTaskStateEventType,
                task.SerializePersistenceState()));
            task.MarkPersistenceStatePersisted();
        }
    }

    private void DetachSimulationJobEvents()
    {
        var addedEvents = ChangeTracker.Entries<JobEvent>()
            .Where(item => item.State == EntityState.Added)
            .ToList();
        if (addedEvents.Count == 0) return;

        var simulationStepIds = new HashSet<Guid>();
        var unresolvedStepIds = new List<Guid>();
        foreach (var entry in addedEvents)
        {
            var step = JobSteps.Local.FirstOrDefault(item => item.Id == entry.Entity.JobStepId);
            var task = step is null
                ? null
                : JobTasks.Local.FirstOrDefault(item => item.Id == step.JobTaskId);
            if (task is null)
                unresolvedStepIds.Add(entry.Entity.JobStepId);
            else if (string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
                simulationStepIds.Add(entry.Entity.JobStepId);
        }

        if (unresolvedStepIds.Count > 0)
        {
            var ids = unresolvedStepIds.Distinct().ToArray();
            var persistedSimulationIds = (from step in JobSteps.AsNoTracking()
                                          join task in JobTasks.AsNoTracking() on step.JobTaskId equals task.Id
                                          where ids.Contains(step.Id) && task.RuntimeId == "sim"
                                          select step.Id).ToList();
            simulationStepIds.UnionWith(persistedSimulationIds);
        }

        foreach (var entry in addedEvents.Where(item => simulationStepIds.Contains(item.Entity.JobStepId)))
            entry.State = EntityState.Detached;
    }

    private async Task DetachSimulationJobEventsAsync(CancellationToken cancellationToken)
    {
        var addedEvents = ChangeTracker.Entries<JobEvent>()
            .Where(item => item.State == EntityState.Added)
            .ToList();
        if (addedEvents.Count == 0) return;

        var simulationStepIds = new HashSet<Guid>();
        var unresolvedStepIds = new List<Guid>();
        foreach (var entry in addedEvents)
        {
            var step = JobSteps.Local.FirstOrDefault(item => item.Id == entry.Entity.JobStepId);
            var task = step is null
                ? null
                : JobTasks.Local.FirstOrDefault(item => item.Id == step.JobTaskId);
            if (task is null)
                unresolvedStepIds.Add(entry.Entity.JobStepId);
            else if (string.Equals(task.RuntimeId, "sim", StringComparison.OrdinalIgnoreCase))
                simulationStepIds.Add(entry.Entity.JobStepId);
        }

        if (unresolvedStepIds.Count > 0)
        {
            var ids = unresolvedStepIds.Distinct().ToArray();
            var persistedSimulationIds = await (from step in JobSteps.AsNoTracking()
                                                join task in JobTasks.AsNoTracking() on step.JobTaskId equals task.Id
                                                where ids.Contains(step.Id) && task.RuntimeId == "sim"
                                                select step.Id).ToListAsync(cancellationToken);
            simulationStepIds.UnionWith(persistedSimulationIds);
        }

        foreach (var entry in addedEvents.Where(item => simulationStepIds.Contains(item.Entity.JobStepId)))
            entry.State = EntityState.Detached;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("ACCOUNT");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(450);
            entity.Property(x => x.Email).HasMaxLength(450);
            entity.Property(x => x.Status).HasConversion<string>();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AUDIT_LOG");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("NOTIFICATION");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<RobotEntity>(entity =>
        {
            entity.ToTable("ROBOT");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SerialNumber).HasMaxLength(450);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<RobotLog>(entity =>
        {
            entity.ToTable("ROBOT_LOG");
            entity.HasKey(x => x.Id);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BookCategory>(entity =>
        {
            entity.ToTable("BOOK_CATEGORY");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Book>(entity =>
        {
            entity.ToTable("BOOK");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Isbn).HasMaxLength(450);
            entity.HasOne<BookCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<BookItem>(entity =>
        {
            entity.ToTable("BOOK_ITEM");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Barcode).HasMaxLength(450);
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.ExpectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.CurrentShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });

        modelBuilder.Entity<LibraryMap>(entity =>
        {
            entity.ToTable("MAP");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.Ignore(x => x.CreatedAtUtc).Ignore(x => x.UpdatedAtUtc);
        });
        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("FLOOR");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONE");
            entity.HasKey(x => x.Id);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("AISLE");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.ToTable("SHELF");
            entity.HasKey(x => x.Id);
            entity.HasOne<Aisle>().WithMany().HasForeignKey(x => x.AisleId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.StartEndpointId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.EndEndpointId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<ShelfLevel>(entity =>
        {
            entity.ToTable("SHELF_LEVEL");
            entity.HasKey(x => x.Id);
            entity.HasOne<Shelf>().WithMany().HasForeignKey(x => x.ShelfId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<Endpoint>(entity =>
        {
            entity.ToTable("ENDPOINT");
            entity.HasKey(x => x.Id);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<EndpointGroup>(entity =>
        {
            entity.ToTable("ENDPOINT_GROUP");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<EndpointGroupMember>(entity =>
        {
            entity.ToTable("ENDPOINT_GROUP_MEMBER");
            entity.HasKey(x => x.Id);
            entity.HasOne<EndpointGroup>().WithMany().HasForeignKey(x => x.EndpointGroupId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.EndpointId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ConfigBatteryRobot>(entity =>
        {
            entity.ToTable("CONFIG_BATTERY_ROBOT");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Job>(entity =>
        {
            entity.ToTable("JOB");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.CreatedBy);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<JobTask>(entity =>
        {
            entity.ToTable("JOB_TASK");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Description);
            entity.Ignore(x => x.ExecutionState);
            entity.Ignore(x => x.PersistencePayload);
            entity.Ignore(x => x.StartedAtUtc);
            entity.Ignore(x => x.CompletedAtUtc);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.RuntimeId).HasColumnName("runtime_id").IsRequired();
            entity.Property(x => x.ExecutionId).HasColumnName("execution_id");
            entity.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<JobStep>(entity =>
        {
            entity.ToTable("JOB_STEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TaskStep>().WithMany().HasForeignKey(x => x.TaskStepId).OnDelete(DeleteBehavior.NoAction);
            entity.Property(x => x.Configuration).IsRequired();
        });
        modelBuilder.Entity<JobEvent>(entity =>
        {
            entity.ToTable("JOB_EVENT");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.EventType);
            entity.HasOne<JobStep>().WithMany().HasForeignKey(x => x.JobStepId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("FEEDBACK");
            entity.HasKey(x => x.Id);
            entity.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<InventoryReport>(entity =>
        {
            entity.ToTable("INVENTORY_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<PatrolReport>(entity =>
        {
            entity.ToTable("PATROL_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Job>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WORKFLOW");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.IsActive);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        });
        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("WORKFLOW_TASK");
            entity.HasKey(x => x.Id);
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<TaskStep>(entity =>
        {
            entity.ToTable("TASK_STEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("SCHEDULE");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.CreatedBy);
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<BookIssue>(entity =>
        {
            entity.ToTable("BOOK_ISSUE"); entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<BookIssueDetail>(entity =>
        {
            entity.ToTable("BOOK_ISSUE_DETAIL"); entity.HasKey(x => x.Id);
            entity.HasOne<BookIssue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BookItem>().WithMany().HasForeignKey(x => x.BookItemId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<InventoryReportDetail>(entity =>
        {
            entity.ToTable("INVENTORY_REPORT_DETAIL"); entity.HasKey(x => x.Id);
            entity.HasOne<InventoryReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BookItem>().WithMany().HasForeignKey(x => x.BookItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.ExpectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.DetectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
        });

        // Keep the EF schema limited to the columns defined in the SQL ERD.
        IgnoreAuditColumns<BookCategory, Book, BookItem, Aisle, Floor, Shelf, ShelfLevel, Endpoint,
            EndpointGroup, EndpointGroupMember, Feedback, JobTask, JobStep, BookIssue, BookIssueDetail,
            InventoryReportDetail, ConfigBatteryRobot>(modelBuilder);
        IgnoreUpdatedAt<AuditLog, Notification, Zone, WorkflowTask, TaskStep, InventoryReport, PatrolReport>(modelBuilder);
        IgnoreAuditColumns<RobotLog>(modelBuilder);
        modelBuilder.Entity<JobEvent>().Ignore(x => x.CreatedAtUtc).Ignore(x => x.UpdatedAtUtc);

        ApplySnakeCaseColumnNames(modelBuilder);
        ConfigureLogicalErdColumnNames(modelBuilder);
    }

    private static void ApplySnakeCaseColumnNames(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
                property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static void ConfigureLogicalErdColumnNames(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<User>().Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        modelBuilder.Entity<User>().Property(x => x.LastLoginAtUtc).HasColumnName("last_login_at");
        modelBuilder.Entity<AuditLog>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<Notification>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<Zone>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<WorkflowTask>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<WorkflowTask>().Property(x => x.SequenceNumber).HasColumnName("sequence_no");
        modelBuilder.Entity<TaskStep>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<InventoryReport>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<PatrolReport>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<Schedule>().Property(x => x.CreatedAtUtc).HasColumnName("created_at");
        modelBuilder.Entity<Schedule>().Property(x => x.UpdatedAtUtc).HasColumnName("updated_at");
        modelBuilder.Entity<Schedule>().Property(x => x.LastRunAtUtc).HasColumnName("last_run_at");
        modelBuilder.Entity<Schedule>().Property(x => x.NextRunAtUtc).HasColumnName("next_run_at");
        modelBuilder.Entity<LibraryMap>().Property(x => x.MapData).HasColumnName("map_data");
        modelBuilder.Entity<Book>().Property(x => x.Image).HasColumnName("image_url");
        modelBuilder.Entity<Job>().Property(x => x.CreatedAtUtc).HasColumnName("create_at");
        modelBuilder.Entity<Job>().Property(x => x.StartedAtUtc).HasColumnName("started_at");
        modelBuilder.Entity<Job>().Property(x => x.CompletedAtUtc).HasColumnName("completed_at");
        modelBuilder.Entity<RobotLog>().Property(x => x.RecordedAtUtc).HasColumnName("record_at");
        modelBuilder.Entity<JobEvent>().Property(x => x.EventTimeUtc).HasColumnName("event_time");
        modelBuilder.Entity<JobTask>().Property(x => x.SequenceNo).HasColumnName("sequence_no");
        modelBuilder.Entity<EndpointGroupMember>().Property(x => x.EndpointGroupId).HasColumnName("enpoint_group_id");
        modelBuilder.Entity<InventoryReport>().Property(x => x.TotalShelves).HasColumnName("total_shelfs");
        modelBuilder.Entity<InventoryReport>().Property(x => x.TotalBooksScanned).HasColumnName("total_book_scaned");
    }

    private static string ToSnakeCase(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsUpper(character) && index > 0 &&
                (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]) ||
                 (index + 1 < value.Length && char.IsLower(value[index + 1]))))
                result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }

    private static void IgnoreAuditColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ModelBuilder modelBuilder)
        where T1 : Entity where T2 : Entity where T3 : Entity where T4 : Entity where T5 : Entity where T6 : Entity
        where T7 : Entity where T8 : Entity where T9 : Entity where T10 : Entity where T11 : Entity where T12 : Entity
        where T13 : Entity where T14 : Entity where T15 : Entity where T16 : Entity where T17 : Entity
    {
        IgnoreAuditColumns<T1>(modelBuilder); IgnoreAuditColumns<T2>(modelBuilder); IgnoreAuditColumns<T3>(modelBuilder);
        IgnoreAuditColumns<T4>(modelBuilder); IgnoreAuditColumns<T5>(modelBuilder); IgnoreAuditColumns<T6>(modelBuilder);
        IgnoreAuditColumns<T7>(modelBuilder); IgnoreAuditColumns<T8>(modelBuilder); IgnoreAuditColumns<T9>(modelBuilder);
        IgnoreAuditColumns<T10>(modelBuilder); IgnoreAuditColumns<T11>(modelBuilder); IgnoreAuditColumns<T12>(modelBuilder);
        IgnoreAuditColumns<T13>(modelBuilder); IgnoreAuditColumns<T14>(modelBuilder); IgnoreAuditColumns<T15>(modelBuilder);
        IgnoreAuditColumns<T16>(modelBuilder); IgnoreAuditColumns<T17>(modelBuilder);
    }

    private static void IgnoreAuditColumns<T>(ModelBuilder modelBuilder) where T : Entity
    {
        modelBuilder.Entity<T>().Ignore(x => x.CreatedAtUtc).Ignore(x => x.UpdatedAtUtc);
    }

    private static void IgnoreUpdatedAt<T1, T2, T3, T4, T5, T6, T7>(ModelBuilder modelBuilder)
        where T1 : Entity where T2 : Entity where T3 : Entity where T4 : Entity where T5 : Entity where T6 : Entity where T7 : Entity
    {
        IgnoreUpdatedAt<T1>(modelBuilder); IgnoreUpdatedAt<T2>(modelBuilder); IgnoreUpdatedAt<T3>(modelBuilder);
        IgnoreUpdatedAt<T4>(modelBuilder); IgnoreUpdatedAt<T5>(modelBuilder); IgnoreUpdatedAt<T6>(modelBuilder); IgnoreUpdatedAt<T7>(modelBuilder);
    }

    private static void IgnoreUpdatedAt<T>(ModelBuilder modelBuilder) where T : Entity => modelBuilder.Entity<T>().Ignore(x => x.UpdatedAtUtc);
}
