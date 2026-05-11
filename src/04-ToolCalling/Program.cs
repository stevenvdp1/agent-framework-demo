using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;
using ToolCalling;
using ApprovalRequiredAIFunction = ToolCalling.ApprovalRequiredAIFunction;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var tools = new KnightTools();
var weatherTool = new MedievalWeatherTool();

var agent = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: """
        Thou art the Grand Royal Scribe. Thy tongue is that of the Bard.
            1. Ask the traveler their name and weapon.
            2. Roll their stats and use those stats to 'Forge' a shield description.
            3. ALWAYS check the 'PredictWeather' tool for the traveler's region 
            so they know if their armor will rust in the rain!
    """,
    name: "ToolCallingAgent",
    tools: [
        AIFunctionFactory.Create(tools.GetKnightStats),
        AIFunctionFactory.Create(weatherTool.PredictWeather),
        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(tools.GenerateHeraldry))
    ]);

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

    var response = await agent.RunAsync(input, session);
    Console.WriteLine($"Agent > {response}");

    Console.WriteLine();
}