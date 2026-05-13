using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var agent = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
    You are a helpful assistant.
    You talk like you are living medievel times.
    """,
    name: "MultiTurnAgent");

// var session = await agent.CreateSessionAsync();

Console.WriteLine("Multi-turn conversation (type 'exit' to quit)\n");

var intro = await agent.RunAsync("Introduce yourself");
Console.WriteLine($"Agent > {intro}", intro);
Console.WriteLine();
while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    Console.WriteLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    Console.Write("Agent > ");
    await foreach (var update in agent.RunStreamingAsync(input))
    {
        Console.Write(update);
    }
    Console.WriteLine("\n");
}
