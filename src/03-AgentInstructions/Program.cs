using Azure.AI.Projects;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var agentsDir = Path.Combine(AppContext.BaseDirectory, "Agents");
var instructions = await File.ReadAllTextAsync(Directory.GetFiles(agentsDir, "helpful-assistant.md").First());
// var instructions = await File.ReadAllTextAsync(Directory.GetFiles(agentsDir, "story-teller.md").First());
// var instructions = await File.ReadAllTextAsync(Directory.GetFiles(agentsDir, "dotnet-expert.md").First());

var agent = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions:  instructions,
    name: "MultiTurnAgent");

var session = await agent.CreateSessionAsync();

Console.WriteLine("Multi-turn conversation (type 'exit' to quit)\n");

var msg = await agent.RunAsync("Introduce yourself", session);
Console.WriteLine($"Agent > {msg}", msg);

while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    Console.Write("Agent > ");
    await foreach (var update in agent.RunStreamingAsync(input, session))
    {
        Console.Write(update);
    }
    Console.WriteLine("\n");
}
