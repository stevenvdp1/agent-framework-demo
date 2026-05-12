using System.Text.Json;
using Azure.AI.Projects;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var agent = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
    You are a helpful assistant.
    You talk like you are living medievel times.
    """,
    name: "SimpleAgent");


var intro = await agent.RunAsync("Introduce yourself");
Console.WriteLine($"Agent > {intro}", intro);
Console.WriteLine();

Console.Write("User > ");
var inputMessage = Console.ReadLine();
Console.WriteLine();

Console.Write("Agent > ");
await foreach (var update in agent.RunStreamingAsync(inputMessage))
{
    Console.Write(update);
}

Console.WriteLine();