using LiBot.Application.Robot;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Application.Services;
using LiBot.Application.Services.Robot;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow.BookGuidance;
using LiBot.Application.Workflow.FacilityGuidance;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Robot;
using LiBot.Infrastructure.Repositories;
using LiBot.Infrastructure.Services;
using LiBot.Infrastructure.RobotRuntime;
using LiBot.Infrastructure.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiBot.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LiBot")
            ?? throw new InvalidOperationException("Connection string 'LiBot' was not configured.");

        services.AddDbContext<LiBotDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped(typeof(IGenericRepository<>), typeof(EfGenericRepository<>));
        services.AddScoped(typeof(ILibraryResourceRepository<>), typeof(EfLibraryResourceRepository<>));
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Aisle, AisleDto>, LibraryResourceService<LiBot.Domain.Library.Aisle, AisleDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Area, AreaDto>, LibraryResourceService<LiBot.Domain.Library.Area, AreaDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Book, BookDto>, LibraryResourceService<LiBot.Domain.Library.Book, BookDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.BookCategory, BookCategoryDto>, LibraryResourceService<LiBot.Domain.Library.BookCategory, BookCategoryDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.BookItem, BookItemDto>, LibraryResourceService<LiBot.Domain.Library.BookItem, BookItemDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Endpoint, EndpointDto>, LibraryResourceService<LiBot.Domain.Library.Endpoint, EndpointDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.EndpointGroup, EndpointGroupDto>, LibraryResourceService<LiBot.Domain.Library.EndpointGroup, EndpointGroupDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.EndpointGroupMember, EndpointGroupMemberDto>, LibraryResourceService<LiBot.Domain.Library.EndpointGroupMember, EndpointGroupMemberDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Floor, FloorDto>, LibraryResourceService<LiBot.Domain.Library.Floor, FloorDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Shelf, ShelfDto>, LibraryResourceService<LiBot.Domain.Library.Shelf, ShelfDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.ShelfLevel, ShelfLevelDto>, LibraryResourceService<LiBot.Domain.Library.ShelfLevel, ShelfLevelDto>>();
        services.AddScoped<ILibraryResourceService<LiBot.Domain.Library.Zone, ZoneDto>, LibraryResourceService<LiBot.Domain.Library.Zone, ZoneDto>>();
        services.AddScoped<IBookGuidanceRepository, BookGuidanceRepository>();
        services.AddScoped<IFacilityGuidanceRepository, FacilityGuidanceRepository>();
        services.AddScoped<IGuidanceWorkflowRepository, GuidanceWorkflowRepository>();
        services.AddScoped<ILibraryMapRepository, LibraryMapRepository>();
        services.AddScoped<ILibraryMapService, LibraryMapService>();
        services.AddScoped<IMissionDataRepository, MissionDataRepository>();
        services.AddScoped<IMissionDataService, MissionDataService>();
        services.AddScoped<IRobotTaskRepository, RobotTaskRepository>();
        services.AddScoped<IWorkflowDataRepository, WorkflowDataRepository>();
        services.AddScoped<IWorkflowDataService, WorkflowDataService>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowManagementService, WorkflowManagementService>();
        services.AddScoped<IScheduleService, ScheduleService>();
        services.AddOptions<LiBotTcpOptions>()
            .Bind(configuration.GetSection(LiBotTcpOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host)
                    && Uri.CheckHostName(options.Host) != UriHostNameType.Unknown
                    && options.Port is >= 1 and <= 65535
                    && options.ConnectTimeoutSeconds is >= 1 and <= LiBotTcpOptions.MaximumTimeoutSeconds
                    && options.ResponseTimeoutSeconds is >= 1 and <= LiBotTcpOptions.MaximumTimeoutSeconds
                    && options.RuntimeEndpoints.ContainsKey("real")
                    && options.RuntimeEndpoints.All(item =>
                        !string.IsNullOrWhiteSpace(item.Key)
                        && !string.IsNullOrWhiteSpace(item.Value.Host)
                        && Uri.CheckHostName(item.Value.Host) != UriHostNameType.Unknown
                        && item.Value.Port is >= 1 and <= 65535),
                "LiBot host, port, timeouts, and runtime endpoints must be configured in the BE .env file.")
            .ValidateOnStart();
        services.AddSingleton<ILiBotTcpClient, LiBotTcpClient>();
        services.AddSingleton<BookGuidanceRuntimeRegistry>();
        services.AddSingleton<FacilityGuidanceRuntimeRegistry>();
        services.AddScoped<IBookGuidanceService, BookGuidanceService>();
        services.AddScoped<IFacilityGuidanceService, FacilityGuidanceService>();
        services.AddScoped<IWorkflowDocumentRepository, WorkflowDocumentRepository>();
        services.AddScoped<IWorkflowDocumentService, WorkflowDocumentService>();
        services.AddSingleton<RobotRuntimeService>();
        services.AddSingleton<IRobotRuntimeService>(provider => provider.GetRequiredService<RobotRuntimeService>());
        services.AddHostedService(provider => provider.GetRequiredService<RobotRuntimeService>());

        return services;
    }
}
