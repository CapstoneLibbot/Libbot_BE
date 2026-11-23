using System.Text.Json;
using LiBot.Application.Interfaces;
using LiBot.Application.Models;
using LiBot.Infrastructure.Workflow;
using LiBot.Infrastructure.Persistence;
using LiBot.Infrastructure.Repositories;
using LiBot.Domain.Library;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LiBot.Tests.ZonePolicy;

public sealed class InventoryScopeTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ResolveAndAdvancePreserveBothEndpointPosesAndLegacyAlias()
    {
        var map = Guid.NewGuid();
        var first = new InventoryNavigationTargetRecord(Guid.NewGuid(), "start", 1, 2, 0, map);
        var last = new InventoryNavigationTargetRecord(Guid.NewGuid(), "end", 1, 3, 0, map);
        var shelf = new InventoryShelfScopeRecord(Guid.NewGuid(), "A", "A", first,
            new[] { new InventoryShelfLevelRecord(Guid.NewGuid(), 1), new InventoryShelfLevelRecord(Guid.NewGuid(), 2) }, first, last, true);
        var service = new InventoryWorkflowService(new InventoryRepositoryStub(shelf));
        var mission = Guid.NewGuid();
        var resolveRequest = new InventoryWorkflowActionRequest(mission, "sim", "execution", "RESOLVE_INVENTORY_SCOPE",
            JsonSerializer.SerializeToElement(new { floor_id = Guid.NewGuid(), target_shelves = new[] { shelf.ShelfId } }), "resolve");
        var resolve = await service.ExecuteAsync(resolveRequest, true, default);
        var cachedResolve = await service.ExecuteAsync(resolveRequest, true, default);
        var cachedScope = JsonSerializer.SerializeToElement(cachedResolve.Data, Json).GetProperty("shelves");
        Assert.Equal(3, cachedScope[0].GetProperty("end_endpoint").GetProperty("y").GetDouble());
        var scope = JsonSerializer.SerializeToElement(resolve.Data, Json).GetProperty("shelves");
        var advanceRequest = new InventoryWorkflowActionRequest(mission, "sim", "execution", "ADVANCE_INVENTORY_SCOPE",
            JsonSerializer.SerializeToElement(new { shelves = scope, current_shelf_index = -1 }), "advance");
        var advance = await service.ExecuteAsync(advanceRequest, true, default);
        var cachedAdvance = await service.ExecuteAsync(advanceRequest, true, default);
        var cachedCurrent = JsonSerializer.SerializeToElement(cachedAdvance.Data, Json).GetProperty("current_shelf");
        Assert.Equal(2, cachedCurrent.GetProperty("start_endpoint").GetProperty("y").GetDouble());
        var current = JsonSerializer.SerializeToElement(advance.Data, Json).GetProperty("current_shelf");
        Assert.Equal(first.EndpointId, current.GetProperty("endpoint").GetProperty("endpoint_id").GetGuid());
        Assert.Equal(first.EndpointId, current.GetProperty("start_endpoint").GetProperty("endpoint_id").GetGuid());
        Assert.Equal(last.EndpointId, current.GetProperty("end_endpoint").GetProperty("endpoint_id").GetGuid());
        Assert.Equal(map, current.GetProperty("end_endpoint").GetProperty("map_id").GetGuid());
        Assert.Equal(3, current.GetProperty("end_endpoint").GetProperty("y").GetDouble());
    }

    [Fact]
    public void EndpointGeometryMustBeLateralAndBelongToOneMap()
    {
        var map = Guid.NewGuid();
        var first = new InventoryNavigationTargetRecord(Guid.NewGuid(), "start", 1, 2, 0, map);
        var last = new InventoryNavigationTargetRecord(Guid.NewGuid(), "end", 1, 3, 0, map);
        InventoryEndpointValidator.Validate(first, last);
        Assert.Throws<InvalidOperationException>(() => InventoryEndpointValidator.Validate(first, first));
        Assert.Throws<InvalidOperationException>(() => InventoryEndpointValidator.Validate(first, last with { MapId = Guid.NewGuid() }));
        Assert.Throws<InvalidOperationException>(() => InventoryEndpointValidator.Validate(first, last with { X = 1.2 }));
        Assert.Throws<InvalidOperationException>(() => InventoryEndpointValidator.Validate(first, last with { Theta = 0.2 }));
    }

    [Fact]
    public async Task RepositoryReturnsBothRealEndpointsAndRejectsInactiveEndpoint()
    {
        var options = new DbContextOptionsBuilder<LiBotDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new LiBotDbContext(options);
        var floor = new Floor("Floor", 1);
        var map = new LibraryMap("Map", "1", 100, 100, "", floor.Id);
        var aisle = new Aisle(floor.Id, "Aisle");
        var start = new Endpoint(map.Id, "Start", 1, 2, 0);
        var end = new Endpoint(map.Id, "End", 1, 3, 0);
        var shelf = new Shelf(aisle.Id, start.Id, end.Id, "Shelf");
        db.AddRange(floor, map, aisle, start, end, shelf,
            new ShelfLevel(shelf.Id, 1), new ShelfLevel(shelf.Id, 2));
        await db.SaveChangesAsync();
        var repository = new InventoryWorkflowRepository(db);
        var items = await repository.ResolveScopeAsync(floor.Id, map.Id, new[] { shelf.Id }, default);
        var item = Assert.Single(items);
        Assert.Equal(start.Id, item.StartEndpoint!.EndpointId);
        Assert.Equal(end.Id, item.EndEndpoint!.EndpointId);
        Assert.Equal(map.Id, item.EndEndpoint.MapId);
        end.Deactivate();
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.ResolveScopeAsync(floor.Id, map.Id, new[] { shelf.Id }, default));
    }

    private sealed class InventoryRepositoryStub(InventoryShelfScopeRecord shelf) : IInventoryWorkflowRepository
    {
        public Task<IReadOnlyList<InventoryShelfScopeRecord>> ResolveScopeAsync(Guid floorId, Guid? mapId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InventoryShelfScopeRecord>>(new[] { shelf });
        public Task<InventoryShelfScopeRecord?> GetShelfScopeAsync(Guid shelfId, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<InventoryBookItemRecord>> GetBookItemsAsync(IReadOnlyCollection<Guid> levels, IReadOnlyCollection<string> codes, CancellationToken ct) => throw new NotImplementedException();
        public Task<int> UpdateBookItemsAsync(IReadOnlyCollection<InventoryBookItemUpdate> updates, CancellationToken ct) => throw new NotImplementedException();
        public Task<Guid> AddJobEventAsync(Guid missionId, string eventType, string? data, CancellationToken ct) => throw new NotImplementedException();
        public Task<IReadOnlyList<string?>> GetJobEventDataAsync(Guid missionId, string eventType, CancellationToken ct) => throw new NotImplementedException();
        public Task<Guid> SaveInventoryReportAsync(Guid missionId, string scopeType, int totalShelves, int totalBooksScanned, int totalAvailable, int totalMisplaced, int totalMissing, string data, CancellationToken ct) => throw new NotImplementedException();
        public Task<InventoryReportRecord?> GetInventoryReportAsync(Guid missionId, CancellationToken ct) => throw new NotImplementedException();
    }
}
