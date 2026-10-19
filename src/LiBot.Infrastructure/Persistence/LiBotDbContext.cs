using LiBot.Domain.Identity;
using LiBot.Domain.Library;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using Microsoft.EntityFrameworkCore;
using RobotEntity = LiBot.Domain.Robot.Robot;
using RobotLog = LiBot.Domain.Robot.RobotLog;
using RobotConfigLog = LiBot.Domain.Robot.RobotConfigLog;

namespace LiBot.Infrastructure.Persistence;

public class LiBotDbContext(DbContextOptions<LiBotDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RobotEntity> Robots => Set<RobotEntity>();
    public DbSet<RobotLog> RobotLogs => Set<RobotLog>();
    public DbSet<RobotConfigLog> RobotConfigLogs => Set<RobotConfigLog>();
    public DbSet<BookCategory> BookCategories => Set<BookCategory>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookItem> BookItems => Set<BookItem>();
    public DbSet<LibraryMap> Maps => Set<LibraryMap>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<Zone> Zones => Set<Zone>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Aisle> Aisles => Set<Aisle>();
    public DbSet<Shelf> Shelves => Set<Shelf>();
    public DbSet<ShelfLevel> ShelfLevels => Set<ShelfLevel>();
    public DbSet<Endpoint> Endpoints => Set<Endpoint>();
    public DbSet<EndpointGroup> EndpointGroups => Set<EndpointGroup>();
    public DbSet<EndpointGroupMember> EndpointGroupMembers => Set<EndpointGroupMember>();
    public DbSet<Mission> Jobs => Set<Mission>();
    public DbSet<JobTask> JobTasks => Set<JobTask>();
    public DbSet<JobStep> JobSteps => Set<JobStep>();
    public DbSet<JobEvent> JobEvents => Set<JobEvent>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<InventoryReport> InventoryReports => Set<InventoryReport>();
    public DbSet<PatrolReport> PatrolReports => Set<PatrolReport>();
    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<TaskStep> TaskSteps => Set<TaskStep>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<ScheduledJobTask> ScheduledJobTasks => Set<ScheduledJobTask>();
    public DbSet<ScheduledJobStep> ScheduledJobSteps => Set<ScheduledJobStep>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("ACCOUNT");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique().HasFilter("[Username] IS NOT NULL");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AUDIT_LOG");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("NOTIFICATION");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RobotEntity>(entity =>
        {
            entity.ToTable("ROBOT");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.SerialNumber).IsUnique();
        });
        modelBuilder.Entity<RobotLog>(entity =>
        {
            entity.ToTable("ROBOT_LOG");
            entity.HasKey(x => x.Id);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<RobotConfigLog>(entity =>
        {
            entity.ToTable("CONFIG_LOG");
            entity.HasKey(x => x.Id);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.SetNull);
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
            entity.HasIndex(x => x.Isbn).IsUnique();
            entity.HasOne<BookCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<BookItem>(entity =>
        {
            entity.ToTable("BOOK_ITEM");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Barcode).IsUnique();
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.ExpectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.CurrentShelfLevelId).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<LibraryMap>(entity =>
        {
            entity.ToTable("MAP");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Floor>(entity =>
        {
            entity.ToTable("FLOOR");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<Zone>(entity =>
        {
            entity.ToTable("ZONE");
            entity.HasKey(x => x.Id);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("AREA");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("AISLE");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.EndpointId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.ToTable("SHELF");
            entity.HasKey(x => x.Id);
            entity.HasOne<Aisle>().WithMany().HasForeignKey(x => x.AisleId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ShelfLevel>(entity =>
        {
            entity.ToTable("SHELF_LEVEL");
            entity.HasKey(x => x.Id);
            entity.HasOne<Shelf>().WithMany().HasForeignKey(x => x.ShelfId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Endpoint>(entity =>
        {
            entity.ToTable("ENDPOINT");
            entity.HasKey(x => x.Id);
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.Cascade);
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
            entity.HasOne<EndpointGroup>().WithMany().HasForeignKey(x => x.EndpointGroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.EndpointId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.EndpointGroupId, x.EndpointId }).IsUnique();
        });

        modelBuilder.Entity<Mission>(entity =>
        {
            entity.ToTable("JOB");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.SourceEndpointId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.DestinationEndpointId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<JobTask>(entity =>
        {
            entity.ToTable("JOB_TASK");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RuntimeId).HasMaxLength(32).HasDefaultValue("real");
            entity.Property(x => x.ExecutionId).HasMaxLength(64);
            entity.Property(x => x.ExecutionState).HasColumnType("nvarchar(max)");
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<RobotEntity>().WithMany().HasForeignKey(x => x.RobotId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<JobStep>(entity =>
        {
            entity.ToTable("JOB_STEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TaskStep>().WithMany().HasForeignKey(x => x.TaskStepId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<JobEvent>(entity =>
        {
            entity.ToTable("JOB_EVENT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("FEEDBACK");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("REPORT");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<InventoryReport>(entity =>
        {
            entity.ToTable("INVENTORY_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<PatrolReport>(entity =>
        {
            entity.ToTable("PATROL_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Report>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WORKFLOW");
            entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("WORKFLOW_TASK");
            entity.HasKey(x => x.Id);
            entity.HasOne<WorkflowDefinition>().WithMany().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<TaskStep>(entity =>
        {
            entity.ToTable("TASK_STEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.ToTable("SCHEDULE");
            entity.HasKey(x => x.Id);
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ScheduledJob>(entity =>
        {
            entity.ToTable("SCHEDULE_JOB");
            entity.HasKey(x => x.Id);
            entity.HasOne<Schedule>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ScheduledJobTask>(entity =>
        {
            entity.ToTable("SCHEDULE_JOBTASK");
            entity.HasKey(x => x.Id);
            entity.HasOne<ScheduledJob>().WithMany().HasForeignKey(x => x.ScheduledJobId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ScheduledJobStep>(entity =>
        {
            entity.ToTable("SCHEDULE_JOBSTEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<ScheduledJobTask>().WithMany().HasForeignKey(x => x.ScheduledJobTaskId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
