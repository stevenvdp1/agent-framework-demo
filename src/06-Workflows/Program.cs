using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Shared;

var settings = AgentFactory.LoadSettings();
var client = AgentFactory.CreateClient(settings);

var researcher = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: "You are a researcher. Gather key facts about the given topic. Be concise and factual.",
    name: "Researcher");

var writer = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: "You are a technical writer. Take the research provided and write a clear, structured summary with headers.",
    name: "Writer");

var editor = client.AsAIAgent(
    model: settings.DeploymentName,
    instructions: "You are an editor. Review the text for clarity, grammar, and structure. Output the final polished version.",
    name: "Editor");

var builder = new WorkflowBuilder(researcher);
builder.AddEdge(researcher, writer);
builder.AddEdge(writer, editor);
var workflow = builder.Build();

Console.WriteLine("Running workflow: Researcher -> Writer -> Editor\n");

var run = await InProcessExecution.RunStreamingAsync(workflow, "The history and significance of developer conferences in Belgium");
await foreach (var evt in run.WatchStreamAsync())
{
    if (evt is ExecutorCompletedEvent completed)
    {
        Console.WriteLine($"\n--- {completed.ExecutorId} completed ---\n");
        Console.WriteLine(completed.Data);
    }

    if (evt is WorkflowOutputEvent output)
    {
        Console.WriteLine($"\n=== Final Output ===\n");
        Console.WriteLine(output.Data);
    }
}
