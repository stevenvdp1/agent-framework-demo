using Azure.AI.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.VectorStores;

namespace Shared;

public static class AgentFactory
{
    public static AgentSettings LoadSettings()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.development.json")
            .Build();

        var settings = new AgentSettings();
        config.GetSection("Agent").Bind(settings);
        return settings;
    }

    public static AIProjectClient CreateClient(AgentSettings settings)
    {
        return new AIProjectClient(
            new Uri(settings.Endpoint),
            new DefaultAzureCredential());
    }

    public static AzureOpenAIClient CreateOpenAIClient(AgentSettings settings)
    {
        return new AzureOpenAIClient(
            new Uri(settings.AzureOpenAIEndpoint),
            new DefaultAzureCredential());
    }
}

