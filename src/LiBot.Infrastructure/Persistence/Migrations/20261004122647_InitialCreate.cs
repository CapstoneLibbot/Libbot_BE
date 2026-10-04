using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LiBot.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCOUNT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCOUNT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BOOK_CATEGORY",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOOK_CATEGORY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ENDPOINT_GROUP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENDPOINT_GROUP", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FLOOR",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FLOOR", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_ITEM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_ITEM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MAP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    MapData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAP", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "REPORT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BatteryLevel = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AUDIT_LOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDIT_LOG", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AUDIT_LOG_ACCOUNT_AccountId",
                        column: x => x.AccountId,
                        principalTable: "ACCOUNT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_ACCOUNT_AccountId",
                        column: x => x.AccountId,
                        principalTable: "ACCOUNT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ScheduleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScheduleTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LastRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_ACCOUNT_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "ACCOUNT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BOOK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Isbn = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Author = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Publisher = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublicationYear = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOOK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOOK_BOOK_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BOOK_CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AREA",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AREA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AREA_FLOOR_FloorId",
                        column: x => x.FloorId,
                        principalTable: "FLOOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ENDPOINT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PositionX = table.Column<double>(type: "float", nullable: false),
                    PositionY = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENDPOINT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ENDPOINT_MAP_MapId",
                        column: x => x.MapId,
                        principalTable: "MAP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ZONE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MapId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZoneType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxSpeed = table.Column<double>(type: "float", nullable: false),
                    PreferredDirection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorridorWidth = table.Column<double>(type: "float", nullable: true),
                    AllowTurnaround = table.Column<bool>(type: "bit", nullable: false),
                    MinX = table.Column<double>(type: "float", nullable: false),
                    MinY = table.Column<double>(type: "float", nullable: false),
                    MaxX = table.Column<double>(type: "float", nullable: false),
                    MaxY = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ZONE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ZONE_MAP_MapId",
                        column: x => x.MapId,
                        principalTable: "MAP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "INVENTORY_REPORT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalShelves = table.Column<int>(type: "int", nullable: false),
                    TotalBooksScanned = table.Column<int>(type: "int", nullable: false),
                    TotalAvailable = table.Column<int>(type: "int", nullable: false),
                    TotalMisplaced = table.Column<int>(type: "int", nullable: false),
                    TotalMissing = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_REPORT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INVENTORY_REPORT_REPORT_ReportId",
                        column: x => x.ReportId,
                        principalTable: "REPORT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PATROL_REPORT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    ObservationCount = table.Column<int>(type: "int", nullable: false),
                    WarningCount = table.Column<int>(type: "int", nullable: false),
                    FallenBookCount = table.Column<int>(type: "int", nullable: false),
                    UnknownObstacleCount = table.Column<int>(type: "int", nullable: false),
                    OtherWarningCount = table.Column<int>(type: "int", nullable: false),
                    PatrolStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATROL_REPORT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATROL_REPORT_REPORT_ReportId",
                        column: x => x.ReportId,
                        principalTable: "REPORT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CONFIG_LOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RobotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfigType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIG_LOG", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CONFIG_LOG_ACCOUNT_AccountId",
                        column: x => x.AccountId,
                        principalTable: "ACCOUNT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CONFIG_LOG_ROBOT_RobotId",
                        column: x => x.RobotId,
                        principalTable: "ROBOT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ROBOT_LOG",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RobotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentBatteryLevel = table.Column<double>(type: "float", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROBOT_LOG", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROBOT_LOG_ROBOT_RobotId",
                        column: x => x.RobotId,
                        principalTable: "ROBOT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WORKFLOW_TASK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WORKFLOW_TASK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WORKFLOW_TASK_WORKFLOW_WorkflowId",
                        column: x => x.WorkflowId,
                        principalTable: "WORKFLOW",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AISLE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FloorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISLE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AISLE_ENDPOINT_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "ENDPOINT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AISLE_FLOOR_FloorId",
                        column: x => x.FloorId,
                        principalTable: "FLOOR",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ENDPOINT_GROUP_MEMBER",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENDPOINT_GROUP_MEMBER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_EndpointId",
                        column: x => x.EndpointId,
                        principalTable: "ENDPOINT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ENDPOINT_GROUP_MEMBER_ENDPOINT_GROUP_EndpointGroupId",
                        column: x => x.EndpointGroupId,
                        principalTable: "ENDPOINT_GROUP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JOB",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceEndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationEndpointId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOB_ACCOUNT_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "ACCOUNT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JOB_ENDPOINT_DestinationEndpointId",
                        column: x => x.DestinationEndpointId,
                        principalTable: "ENDPOINT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_JOB_ENDPOINT_SourceEndpointId",
                        column: x => x.SourceEndpointId,
                        principalTable: "ENDPOINT",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TASK_STEP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TASK_STEP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TASK_STEP_WORKFLOW_TASK_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WORKFLOW_TASK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SHELF",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AisleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SHELF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SHELF_AISLE_AisleId",
                        column: x => x.AisleId,
                        principalTable: "AISLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FEEDBACK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FEEDBACK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FEEDBACK_JOB_JobId",
                        column: x => x.JobId,
                        principalTable: "JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JOB_EVENT",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOB_EVENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOB_EVENT_JOB_JobId",
                        column: x => x.JobId,
                        principalTable: "JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JOB_TASK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RobotId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOB_TASK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOB_TASK_JOB_JobId",
                        column: x => x.JobId,
                        principalTable: "JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JOB_TASK_ROBOT_RobotId",
                        column: x => x.RobotId,
                        principalTable: "ROBOT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOB",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOB", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOB_JOB_JobId",
                        column: x => x.JobId,
                        principalTable: "JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOB_SCHEDULE_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "SCHEDULE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SHELF_LEVEL",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShelfId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SHELF_LEVEL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SHELF_LEVEL_SHELF_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "SHELF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JOB_STEP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOB_STEP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOB_STEP_JOB_TASK_JobTaskId",
                        column: x => x.JobTaskId,
                        principalTable: "JOB_TASK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JOB_STEP_TASK_STEP_TaskStepId",
                        column: x => x.TaskStepId,
                        principalTable: "TASK_STEP",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOBTASK",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOBTASK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBTASK_SCHEDULE_JOB_ScheduledJobId",
                        column: x => x.ScheduledJobId,
                        principalTable: "SCHEDULE_JOB",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBTASK_WORKFLOW_TASK_WorkflowTaskId",
                        column: x => x.WorkflowTaskId,
                        principalTable: "WORKFLOW_TASK",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BOOK_ITEM",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Barcode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpectedShelfLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentShelfLevelId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastInventoryAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BOOK_ITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BOOK_ITEM_BOOK_BookId",
                        column: x => x.BookId,
                        principalTable: "BOOK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BOOK_ITEM_SHELF_LEVEL_CurrentShelfLevelId",
                        column: x => x.CurrentShelfLevelId,
                        principalTable: "SHELF_LEVEL",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BOOK_ITEM_SHELF_LEVEL_ExpectedShelfLevelId",
                        column: x => x.ExpectedShelfLevelId,
                        principalTable: "SHELF_LEVEL",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SCHEDULE_JOBSTEP",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledJobTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCHEDULE_JOBSTEP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SCHEDULE_JOBSTEP_SCHEDULE_JOBTASK_ScheduledJobTaskId",
                        column: x => x.ScheduledJobTaskId,
                        principalTable: "SCHEDULE_JOBTASK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_Email",
                table: "ACCOUNT",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNT_Username",
                table: "ACCOUNT",
                column: "Username",
                unique: true,
                filter: "[Username] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_EndpointId",
                table: "AISLE",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_AISLE_FloorId",
                table: "AISLE",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_AREA_FloorId",
                table: "AREA",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_AUDIT_LOG_AccountId",
                table: "AUDIT_LOG",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_CategoryId",
                table: "BOOK",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_Isbn",
                table: "BOOK",
                column: "Isbn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_ITEM_Barcode",
                table: "BOOK_ITEM",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_ITEM_BookId",
                table: "BOOK_ITEM",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_ITEM_CurrentShelfLevelId",
                table: "BOOK_ITEM",
                column: "CurrentShelfLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_BOOK_ITEM_ExpectedShelfLevelId",
                table: "BOOK_ITEM",
                column: "ExpectedShelfLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIG_LOG_AccountId",
                table: "CONFIG_LOG",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIG_LOG_RobotId",
                table: "CONFIG_LOG",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_ENDPOINT_MapId",
                table: "ENDPOINT",
                column: "MapId");

            migrationBuilder.CreateIndex(
                name: "IX_ENDPOINT_GROUP_MEMBER_EndpointGroupId_EndpointId",
                table: "ENDPOINT_GROUP_MEMBER",
                columns: new[] { "EndpointGroupId", "EndpointId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ENDPOINT_GROUP_MEMBER_EndpointId",
                table: "ENDPOINT_GROUP_MEMBER",
                column: "EndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEDBACK_JobId",
                table: "FEEDBACK",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_REPORT_ReportId",
                table: "INVENTORY_REPORT",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_CreatedBy",
                table: "JOB",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_DestinationEndpointId",
                table: "JOB",
                column: "DestinationEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_SourceEndpointId",
                table: "JOB",
                column: "SourceEndpointId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_EVENT_JobId",
                table: "JOB_EVENT",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_STEP_JobTaskId",
                table: "JOB_STEP",
                column: "JobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_STEP_TaskStepId",
                table: "JOB_STEP",
                column: "TaskStepId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_TASK_JobId",
                table: "JOB_TASK",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_TASK_RobotId",
                table: "JOB_TASK",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_AccountId",
                table: "NOTIFICATION",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PATROL_REPORT_ReportId",
                table: "PATROL_REPORT",
                column: "ReportId");

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_SerialNumber",
                table: "ROBOT",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ROBOT_LOG_RobotId",
                table: "ROBOT_LOG",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_CreatedBy",
                table: "SCHEDULE",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOB_JobId",
                table: "SCHEDULE_JOB",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOB_ScheduleId",
                table: "SCHEDULE_JOB",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBSTEP_ScheduledJobTaskId",
                table: "SCHEDULE_JOBSTEP",
                column: "ScheduledJobTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBTASK_ScheduledJobId",
                table: "SCHEDULE_JOBTASK",
                column: "ScheduledJobId");

            migrationBuilder.CreateIndex(
                name: "IX_SCHEDULE_JOBTASK_WorkflowTaskId",
                table: "SCHEDULE_JOBTASK",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SHELF_AisleId",
                table: "SHELF",
                column: "AisleId");

            migrationBuilder.CreateIndex(
                name: "IX_SHELF_LEVEL_ShelfId",
                table: "SHELF_LEVEL",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_TASK_STEP_WorkflowTaskId",
                table: "TASK_STEP",
                column: "WorkflowTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_WORKFLOW_TASK_WorkflowId",
                table: "WORKFLOW_TASK",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ZONE_MapId",
                table: "ZONE",
                column: "MapId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AREA");

            migrationBuilder.DropTable(
                name: "AUDIT_LOG");

            migrationBuilder.DropTable(
                name: "BOOK_ITEM");

            migrationBuilder.DropTable(
                name: "CONFIG_LOG");

            migrationBuilder.DropTable(
                name: "ENDPOINT_GROUP_MEMBER");

            migrationBuilder.DropTable(
                name: "FEEDBACK");

            migrationBuilder.DropTable(
                name: "INVENTORY_ITEM");

            migrationBuilder.DropTable(
                name: "INVENTORY_REPORT");

            migrationBuilder.DropTable(
                name: "JOB_EVENT");

            migrationBuilder.DropTable(
                name: "JOB_STEP");

            migrationBuilder.DropTable(
                name: "NOTIFICATION");

            migrationBuilder.DropTable(
                name: "PATROL_REPORT");

            migrationBuilder.DropTable(
                name: "ROBOT_LOG");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOBSTEP");

            migrationBuilder.DropTable(
                name: "ZONE");

            migrationBuilder.DropTable(
                name: "BOOK");

            migrationBuilder.DropTable(
                name: "SHELF_LEVEL");

            migrationBuilder.DropTable(
                name: "ENDPOINT_GROUP");

            migrationBuilder.DropTable(
                name: "JOB_TASK");

            migrationBuilder.DropTable(
                name: "TASK_STEP");

            migrationBuilder.DropTable(
                name: "REPORT");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOBTASK");

            migrationBuilder.DropTable(
                name: "BOOK_CATEGORY");

            migrationBuilder.DropTable(
                name: "SHELF");

            migrationBuilder.DropTable(
                name: "ROBOT");

            migrationBuilder.DropTable(
                name: "SCHEDULE_JOB");

            migrationBuilder.DropTable(
                name: "WORKFLOW_TASK");

            migrationBuilder.DropTable(
                name: "AISLE");

            migrationBuilder.DropTable(
                name: "JOB");

            migrationBuilder.DropTable(
                name: "SCHEDULE");

            migrationBuilder.DropTable(
                name: "WORKFLOW");

            migrationBuilder.DropTable(
                name: "FLOOR");

            migrationBuilder.DropTable(
                name: "ENDPOINT");

            migrationBuilder.DropTable(
                name: "ACCOUNT");

            migrationBuilder.DropTable(
                name: "MAP");
        }
    }
}
