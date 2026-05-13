using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Shared;

using var otel = new ObservabilitySetup(consoleExporter: false);

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var knight = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Sir Galahad, a noble knight. Your job is to set the scene for a classic "A knight, a priest and a cutthroat walk into..." joke.
        Given a subject, write the opening of the joke: describe the setting, introduce the three characters arriving together,
        and have the knight speak first — setting up the situation with earnest, chivalrous bravado.
        Keep it to 2-3 sentences. End on a moment that invites the priest to respond.
    """,
    name: "Knight")
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var priest = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Father O'Malley, a witty priest. You are continuing a joke that was started by a knight.
        Take the knight's setup and escalate the tension with a pious but subtly absurd observation.
        Add a twist or misunderstanding that raises the comedic stakes.
        Keep it to 2-3 sentences. End on a moment that sets up the cutthroat for the punchline.
    """,
    name: "Priest")
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var cutthroat = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Slick Eddie, a street-smart cutthroat with a sharp tongue. You are delivering the punchline of a joke.
        The knight set the scene and the priest escalated the tension. Now you deliver the final, unexpected punchline.
        Make it punchy, irreverent, and funny. One or two sentences max. Land the joke hard.
    """,
    name: "Cutthroat")
    .AsBuilder()
    .UseOpenTelemetry(ObservabilitySetup.SourceName, cfg => cfg.EnableSensitiveData = true)
    .Build();

var workflow = new WorkflowBuilder(knight)
    .AddEdge(knight, priest)
    .AddEdge(priest, cutthroat)
    .WithOpenTelemetry()
    .Build();

Console.WriteLine("Running workflow: Knight -> Priest -> Cutthroat\n");

Console.Write("Give me a subject for a joke > ");
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
    else if (evt is ExecutorCompletedEvent completed)
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
