using LiBot.Domain.Identity;
using LiBot.Domain.Inventory;
using LiBot.Domain.Library;
using LiBot.Domain.Mission;
using LiBot.Domain.Workflow;
using LiBot.Domain.Common;
using Microsoft.EntityFrameworkCore;
using RobotEntity = LiBot.Domain.Robot.Robot;
using RobotLog = LiBot.Domain.Robot.RobotLog;

namespace LiBot.Infrastructure.Persistence;

public class LiBotDbContext(DbContextOptions<LiBotDbContext> options) : DbContext(options)
{
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
    // Retained only to keep the removed Area implementation source-compatible during the API transition.
    // It is explicitly ignored below and is not part of the ERD schema.
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
    public DbSet<InventoryReport> InventoryReports => Set<InventoryReport>();
    public DbSet<PatrolReport> PatrolReports => Set<PatrolReport>();
    public DbSet<WorkflowDefinition> Workflows => Set<WorkflowDefinition>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<TaskStep> TaskSteps => Set<TaskStep>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<BookReceipt> BookReceipts => Set<BookReceipt>();
    public DbSet<BookReceiptDetail> BookReceiptDetails => Set<BookReceiptDetail>();
    public DbSet<BookIssue> BookIssues => Set<BookIssue>();
    public DbSet<BookIssueDetail> BookIssueDetails => Set<BookIssueDetail>();
    public DbSet<InventoryReportDetail> InventoryReportDetails => Set<InventoryReportDetail>();

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
            entity.HasIndex(x => x.SerialNumber).IsUnique();
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
            entity.HasIndex(x => x.Isbn).IsUnique();
            entity.HasOne<BookCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<BookItem>(entity =>
        {
            entity.ToTable("BOOK_ITEM");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Barcode).IsUnique();
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.ExpectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.CurrentShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasAlternateKey(x => new { x.Id, x.BookId });
        });

        modelBuilder.Entity<LibraryMap>(entity =>
        {
            entity.ToTable("MAP");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.NoAction);
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
            entity.HasOne<LibraryMap>().WithMany().HasForeignKey(x => x.MapId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Ignore<Area>();
        modelBuilder.Entity<Aisle>(entity =>
        {
            entity.ToTable("AISLE");
            entity.HasKey(x => x.Id);
            entity.HasOne<Floor>().WithMany().HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Shelf>(entity =>
        {
            entity.ToTable("SHELF");
            entity.HasKey(x => x.Id);
            entity.Ignore(x => x.Code);
            entity.Ignore(x => x.EndpointId);
            entity.HasOne<Aisle>().WithMany().HasForeignKey(x => x.AisleId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.StartEndpointId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Endpoint>().WithMany().HasForeignKey(x => x.EndEndpointId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<ShelfLevel>(entity =>
        {
            entity.ToTable("SHELF_LEVEL");
            entity.HasKey(x => x.Id);
            entity.HasOne<Shelf>().WithMany().HasForeignKey(x => x.ShelfId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Endpoint>(entity =>
        {
            entity.ToTable("ENDPOINT");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EndpointType).HasDefaultValue("WAYPOINT");
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
            entity.HasIndex(x => new { x.EndpointGroupId, x.EndpointId }).IsUnique();
        });

        modelBuilder.Entity<Mission>(entity =>
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
            entity.Ignore(x => x.RuntimeId);
            entity.Ignore(x => x.ExecutionId);
            entity.Ignore(x => x.ExecutionState);
            entity.Property(x => x.Name).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<JobStep>(entity =>
        {
            entity.ToTable("JOB_STEP");
            entity.HasKey(x => x.Id);
            entity.HasOne<JobTask>().WithMany().HasForeignKey(x => x.JobTaskId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<TaskStep>().WithMany().HasForeignKey(x => x.TaskStepId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<JobEvent>(entity =>
        {
            entity.ToTable("JOB_EVENT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("FEEDBACK");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<InventoryReport>(entity =>
        {
            entity.ToTable("INVENTORY_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<PatrolReport>(entity =>
        {
            entity.ToTable("PATROL_REPORT");
            entity.HasKey(x => x.Id);
            entity.HasOne<Mission>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
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

        modelBuilder.Entity<BookReceipt>(entity =>
        {
            entity.ToTable("BOOK_RECEIPT"); entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<BookReceiptDetail>(entity =>
        {
            entity.ToTable("BOOK_RECEIPT_DETAIL"); entity.HasKey(x => x.Id);
            entity.HasOne<BookReceipt>().WithMany().HasForeignKey(x => x.ReceiptId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BookItem>().WithMany().HasForeignKey(x => new { x.BookItemId, x.BookId }).HasPrincipalKey(x => new { x.Id, x.BookId }).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.ReceiptId, x.BookItemId }).IsUnique();
        });
        modelBuilder.Entity<BookIssue>(entity =>
        {
            entity.ToTable("BOOK_ISSUE"); entity.HasKey(x => x.Id);
        });
        modelBuilder.Entity<BookIssueDetail>(entity =>
        {
            entity.ToTable("BOOK_ISSUE_DETAIL"); entity.HasKey(x => x.Id);
            entity.HasOne<BookIssue>().WithMany().HasForeignKey(x => x.IssueId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<Book>().WithMany().HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BookItem>().WithMany().HasForeignKey(x => new { x.BookItemId, x.BookId }).HasPrincipalKey(x => new { x.Id, x.BookId }).OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(x => new { x.IssueId, x.BookItemId }).IsUnique();
        });
        modelBuilder.Entity<InventoryReportDetail>(entity =>
        {
            entity.ToTable("INVENTORY_REPORT_DETAIL"); entity.HasKey(x => x.Id);
            entity.HasOne<InventoryReport>().WithMany().HasForeignKey(x => x.ReportId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<BookItem>().WithMany().HasForeignKey(x => x.BookItemId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.ExpectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ShelfLevel>().WithMany().HasForeignKey(x => x.DetectedShelfLevelId).OnDelete(DeleteBehavior.NoAction);
        });

        // Logical ERD deliberately omits audit columns from these entities.
        IgnoreAuditColumns<BookCategory, Book, BookItem, Aisle, Floor, Shelf, ShelfLevel, Endpoint,
            EndpointGroup, EndpointGroupMember, Feedback, JobTask, JobStep, BookReceipt, BookReceiptDetail,
            BookIssue, BookIssueDetail, InventoryReportDetail>(modelBuilder);
        IgnoreUpdatedAt<AuditLog, Notification, Zone, WorkflowTask, TaskStep, InventoryReport, PatrolReport>(modelBuilder);
        IgnoreAuditColumns<RobotLog>(modelBuilder);
    }

    private static void IgnoreAuditColumns<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ModelBuilder modelBuilder)
        where T1 : Entity where T2 : Entity where T3 : Entity where T4 : Entity where T5 : Entity where T6 : Entity
        where T7 : Entity where T8 : Entity where T9 : Entity where T10 : Entity where T11 : Entity where T12 : Entity
        where T13 : Entity where T14 : Entity where T15 : Entity where T16 : Entity where T17 : Entity where T18 : Entity
    {
        IgnoreAuditColumns<T1>(modelBuilder); IgnoreAuditColumns<T2>(modelBuilder); IgnoreAuditColumns<T3>(modelBuilder);
        IgnoreAuditColumns<T4>(modelBuilder); IgnoreAuditColumns<T5>(modelBuilder); IgnoreAuditColumns<T6>(modelBuilder);
        IgnoreAuditColumns<T7>(modelBuilder); IgnoreAuditColumns<T8>(modelBuilder); IgnoreAuditColumns<T9>(modelBuilder);
        IgnoreAuditColumns<T10>(modelBuilder); IgnoreAuditColumns<T11>(modelBuilder); IgnoreAuditColumns<T12>(modelBuilder);
        IgnoreAuditColumns<T13>(modelBuilder); IgnoreAuditColumns<T14>(modelBuilder); IgnoreAuditColumns<T15>(modelBuilder);
        IgnoreAuditColumns<T16>(modelBuilder); IgnoreAuditColumns<T17>(modelBuilder); IgnoreAuditColumns<T18>(modelBuilder);
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
