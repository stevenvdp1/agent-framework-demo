using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

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

Console.WriteLine("Running: [Knight | Priest | Cutthroat] (concurrent) -> Judge\n");

Console.Write("Give me a subject for a joke > ");
var inputMessage = Console.ReadLine();
Console.WriteLine();

Console.WriteLine("Comedians are writing their jokes concurrently...\n");

var comedians = new[] { ("Knight", knight), ("Priest", priest), ("Cutthroat", cutthroat) };

var jokeTasks = comedians.Select(async c =>
{
    var session = await c.Item2.CreateSessionAsync();
    var joke = await c.Item2.RunAsync(inputMessage!, session);
    return (Name: c.Item1, Joke: joke);
});

var jokes = await Task.WhenAll(jokeTasks);

var judgeInput = string.Join("\n\n", jokes.Select(j => $"[{j.Name}]: {j.Joke}"));

Console.WriteLine("All jokes are in! Sending to the Judge...\n");

var judgeSession = await judge.CreateSessionAsync();
var verdict = await judge.RunAsync(judgeInput, judgeSession);
Console.WriteLine(verdict);
