using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var start = client.AsAIAgent(
settings.DeploymentName,
    instructions: """
        You ask the user a subject for a joke. And pass the user message to other agents.
    """,
    name: "start"
);

var knight = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Sir Galahad, a noble knight with a dry sense of humor.
        Given a subject, tell a short, complete joke about it in your own style — chivalrous, dramatic, and earnest.
        Stay in character. Keep it to 3-4 sentences max.
    """,
    name: "Knight");

var priest = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Father O'Malley, a witty Irish priest who loves wordplay.
        Given a subject, tell a short, complete joke about it in your own style — pious but playful, with a clever twist.
        Stay in character. Keep it to 3-4 sentences max.
    """,
    name: "Priest");

var cutthroat = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are Slick Eddie, a street-smart cutthroat with a razor-sharp tongue.
        Given a subject, tell a short, complete joke about it in your own style — irreverent, punchy, and unexpected.
        Stay in character. Keep it to 3-4 sentences max.
    """,
    name: "Cutthroat");

var judge = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are a comedy judge. You will receive three jokes from different comedians: a Knight, a Priest, and a Cutthroat.
        Present each joke with the comedian's name, then rank them from best to worst.
        For each joke give a short critique (one sentence) explaining why it landed or didn't.
        Announce the winner with flair.
    """,
    name: "Judge");

var workflow = new WorkflowBuilder(start)
    .AddFanOutEdge(start, [knight, priest, cutthroat])
    .AddFanInBarrierEdge([knight, priest, cutthroat], judge)
    .Build();

Console.WriteLine("Running: [Knight | Priest | Cutthroat] (concurrent) -> Judge\n");

Console.Write("Give me a subject for a joke > ");
var inputMessage = Console.ReadLine();
Console.WriteLine();


await using StreamingRun run = await InProcessExecution.RunStreamingAsync(workflow, inputMessage);
await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

List<ChatMessage> result = new();
await foreach (WorkflowEvent evt in run.WatchStreamAsync())
{
    if (evt is AgentResponseUpdateEvent e)
    {
        Console.Write(e.Update.Text);
    }
    else if (evt is WorkflowOutputEvent outputEvt)
    {
        result = outputEvt.As<List<ChatMessage>>()!;
        break;
    }
}

// Display aggregated results from all agents
Console.WriteLine("===== Final Aggregated Results =====");
foreach (var message in result)
{
    Console.WriteLine($"{message.Role}: {message.Text}");
}