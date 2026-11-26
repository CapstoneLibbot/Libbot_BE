using System.Text.Json.Nodes;
using LiBot.Application.Models;
using LiBot.Application.Services;
using LiBot.Application.Services.Workflow;
using LiBot.Application.Workflow;
using LiBot.Domain.Library;
using LiBot.Domain.Workflow;
using Xunit;

namespace LiBot.Tests.ZonePolicy;

public sealed class InventoryDispatchWorkflowTests
{
    [Fact]
    public void NewInventoryTemplateEnablesShelfScanning()
    {
        var move = InventoryTemplate().Steps.Single(step => step.Name == "MOVE_TO_INVENTORY_SHELF");
        Assert.True(move.Configuration.GetProperty("params").GetProperty("_inventory_shelf_scan").GetBoolean());
    }

    [Fact]
    public async Task SavedInventoryWorkflowEnablesScanInDispatchedDocumentWithoutUpdatingSource()
    {
        var source = CreateSource("BOOKSHELF_INVENTORY", null);
        var service = new WorkflowDocumentService(new Repository(source));
        var document = await service.BuildRobotDocumentAsync(source.Workflow.Id, "sim", default);
        var move = document!.Steps.Single(step => step.GetProperty("name").GetString() == "MOVE_TO_INVENTORY_SHELF");
        Assert.True(move.GetProperty("params").GetProperty("_inventory_shelf_scan").GetBoolean());
        Assert.Equal("${current_shelf.endpoint}", move.GetProperty("params").GetProperty("target").GetString());
        Assert.Equal("I50", move.GetProperty("transitions").GetProperty("on_success").GetString());
        var savedMove = source.Steps.Single(step => step.Configuration.Contains("MOVE_TO_INVENTORY_SHELF"));
        Assert.DoesNotContain("_inventory_shelf_scan", savedMove.Configuration);
    }

    [Fact]
    public async Task ExplicitOptOutAndOtherWorkflowTypesRemainUnchanged()
    {
        var disabled = CreateSource("BOOKSHELF_INVENTORY", false);
        var disabledDocument = await new WorkflowDocumentService(new Repository(disabled))
            .BuildRobotDocumentAsync(disabled.Workflow.Id, "sim", default);
        var disabledMove = disabledDocument!.Steps.Single(step => step.GetProperty("name").GetString() == "MOVE_TO_INVENTORY_SHELF");
        Assert.False(disabledMove.GetProperty("params").GetProperty("_inventory_shelf_scan").GetBoolean());

        var other = CreateSource("GENERAL", null);
        var otherDocument = await new WorkflowDocumentService(new Repository(other))
            .BuildRobotDocumentAsync(other.Workflow.Id, "sim", default);
        var otherMove = otherDocument!.Steps.Single(step => step.GetProperty("name").GetString() == "MOVE_TO_INVENTORY_SHELF");
        Assert.False(otherMove.GetProperty("params").TryGetProperty("_inventory_shelf_scan", out _));
    }

    [Fact]
    public async Task InventoryFlagDoesNotDependOnScanStepName()
    {
        var source = CreateSource("BOOKSHELF_INVENTORY", null);
        var scan = source.Tasks.Single(task => task.Name == "SCAN_QR_ON_BOTH_SHELF_LEVELS");
        scan.Update(scan.SequenceNumber, "OTHER_SCAN", scan.Description);
        var document = await new WorkflowDocumentService(new Repository(source))
            .BuildRobotDocumentAsync(source.Workflow.Id, "sim", default);
        var move = document!.Steps.Single(step => step.GetProperty("name").GetString() == "MOVE_TO_INVENTORY_SHELF");
        Assert.True(move.GetProperty("params").GetProperty("_inventory_shelf_scan").GetBoolean());
    }

    private static WorkflowTemplateResponse InventoryTemplate() =>
        WorkflowTemplates.All.Single(item => item.Id == "bookshelf-inventory");

    private static WorkflowDocumentSource CreateSource(string type, bool? scanEnabled)
    {
        var workflow = new WorkflowDefinition("Inventory", null);
        workflow.SetType(type);
        var tasks = new List<WorkflowTask>();
        var steps = new List<TaskStep>();
        foreach (var template in InventoryTemplate().Steps)
        {
            var task = new WorkflowTask(workflow.Id, template.Sequence, template.Name);
            var config = JsonNode.Parse(template.Configuration.GetRawText())!;
            if (template.Name == "MOVE_TO_INVENTORY_SHELF")
            {
                var parameters = config["params"]!.AsObject();
                if (scanEnabled is null) parameters.Remove("_inventory_shelf_scan");
                else parameters["_inventory_shelf_scan"] = scanEnabled.Value;
            }
            tasks.Add(task);
            steps.Add(new TaskStep(task.Id, template.Type, config.ToJsonString()));
        }
        return new WorkflowDocumentSource(workflow, tasks, steps);
    }

    private sealed class Repository(WorkflowDocumentSource source) : IWorkflowDocumentRepository
    {
        public Task<WorkflowDocumentSource?> GetDocumentSourceAsync(Guid workflowId, CancellationToken cancellationToken) =>
            Task.FromResult<WorkflowDocumentSource?>(source);
        public Task<Endpoint?> GetActiveEndpointAsync(Guid? endpointId, string? endpointName, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Inventory context targets must not be resolved at dispatch time.");
    }
}
