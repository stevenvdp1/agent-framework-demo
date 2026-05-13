using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Observability;
using Shared;

using var otel = new ObservabilitySetup(consoleExporter: false);

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var questTools = new QuestTools();
var supplyTools = new SupplyTools();

var questGiver = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
        You are the Royal Quest Master. Given a region from the user, use the GetQuestDetails tool
        to look up the active quest for that region. Present the quest dramatically with flair.
        Mention the quest name, objective, and danger level.
    """,
    name: "QuestGiver",
    tools: [AIFunctionFactory.Create(questTools.GetQuestDetails)])
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var quartermaster = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
        You are Bertram the Quartermaster. Based on the quest described in the previous message,
        use the CheckInventory tool to check availability of 2-3 items that would be essential
        for the quest. Report what's in stock and what the adventurer should bring.
    """,
    name: "Quartermaster",
    tools: [AIFunctionFactory.Create(supplyTools.CheckInventory)])
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var herald = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
        You are the Royal Herald. Summarize the full quest briefing in a dramatic proclamation:
        the quest details from the Quest Master, and the supply report from the Quartermaster.
        Keep it concise but grand. End with "So it is proclaimed!"
    """,
    name: "Herald")
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var workflow = new WorkflowBuilder(questGiver)
    .AddEdge(questGiver, quartermaster)
    .AddEdge(quartermaster, herald)
    .WithOpenTelemetry()
    .Build();

Console.WriteLine("Running workflow: QuestGiver -> Quartermaster -> Herald\n");

Console.Write("Choose a region (forest, mountains, swamp, desert) > ");
var inputMessage = Console.ReadLine();
Console.WriteLine();

await using var run = await InProcessExecution.RunStreamingAsync(
    workflow,
    new ChatMessage(ChatRole.User, inputMessage));

await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

string? currentExecutor = null;

await foreach (var evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent agentUpdate)
    {
        if (agentUpdate.ExecutorId != currentExecutor)
        {
            if (currentExecutor is not null)
                Console.WriteLine();
            currentExecutor = agentUpdate.ExecutorId;
            Console.WriteLine($"\n[{currentExecutor}]:");
        }
        Console.Write(agentUpdate.Data);
    }
    else if (evt is ExecutorCompletedEvent)
    {
        currentExecutor = null;
    }
    else if (evt is WorkflowErrorEvent workflowError)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error.");
        Console.ResetColor();
    }
    else if (evt is ExecutorFailedEvent executorFailed)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Executor '{executorFailed.ExecutorId}' failed: {executorFailed.Data}");
        Console.ResetColor();
    }
}
