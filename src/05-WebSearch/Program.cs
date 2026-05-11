using Azure.AI.Projects;
using Microsoft.Agents.AI.Foundry;
using Microsoft.Extensions.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

AITool webSearchTool = FoundryAITool.CreateWebSearchTool();

var agent = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You can look up information on the web. You always provide sources.
    """,
    tools: [webSearchTool],
    name: "WebSearchAgent"
    );

var session = await agent.CreateSessionAsync();

Console.WriteLine("Multi-turn conversation with web search (type 'exit' to quit)\n");

while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent > {response}");

    Console.WriteLine();
}
