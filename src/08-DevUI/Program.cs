using DevUI;
using Microsoft.Agents.AI.DevUI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);
var chatClient = client.ProjectOpenAIClient.GetChatClient(settings.DeploymentName).AsIChatClient();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChatClient(chatClient);

var knightTools = new KnightTools();
var weatherTool = new MedievalWeatherTool();

builder.AddAIAgent("MedievalAssistant", """
    You are a helpful assistant who speaks as though living in medieval times.
    Use archaic language, refer to technology in medieval metaphors,
    and maintain a knightly demeanor at all times.
    """)
    .WithAITools([
        AIFunctionFactory.Create(knightTools.GenerateHeraldry),
        AIFunctionFactory.Create(knightTools.GetKnightStats),
        AIFunctionFactory.Create(weatherTool.PredictWeather),
    ]);

builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.MapOpenAIResponses();
app.MapOpenAIConversations();

if (app.Environment.IsDevelopment())
{
    app.MapDevUI();
}

app.Run();
