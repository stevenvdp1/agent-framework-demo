using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using RAG;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var dataPath = Path.Combine(AppContext.BaseDirectory, "Documents", "techorama.json");
TechoramaSearch.Load(dataPath);

var searchProvider = new TextSearchProvider(
    TechoramaSearch.SearchAdapter);

var agent = client.AsAIAgent(
    settings.DeploymentName,
    instructions: """
        You are a helpful assistant for Techorama 2026, a Microsoft technology conference.
        If you cant answer the question be honest.
    """,
    name: "RAGAgent")
    .AsBuilder()
    // .UseAIContextProviders(searchProvider)
    .Build();

var session = await agent.CreateSessionAsync();

Console.WriteLine("Ask questions about Techorama 2026 (type 'exit' to quit)\n");

while (true)
{
    Console.Write("User > ");
    var input = Console.ReadLine();
    Console.WriteLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent > {response}");

    Console.WriteLine();
}
