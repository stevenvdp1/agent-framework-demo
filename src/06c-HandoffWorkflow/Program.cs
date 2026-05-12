using Azure.AI.Projects;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using Shared;

var settings = AgentFactory.LoadSettings();
var projectClient = AgentFactory.CreateClient(settings);

IChatClient chatClient = projectClient.ProjectOpenAIClient.GetChatClient(settings.DeploymentName).AsIChatClient();

var analyst = chatClient.AsAIAgent(
    instructions: """
        You are the Analyst agent. Your job is to analyze the user's coding request
        and break it down into a structured plan of small, actionable tasks.
        Each task should be categorized as: "frontend", "dotnet" (backend), or "testing".
        Present the plan as a numbered list with the category in brackets, e.g.:
        1. [frontend] Create the React component for the login form
        2. [dotnet] Add the authentication endpoint to the API
        3. [testing] Write unit tests for the authentication service
        Keep each task small and specific. Aim for 3-6 tasks total.
        Once your plan is ready, hand off to the Orchestrator to begin execution.
    """,
    "Analyst");

var orchestrator = chatClient.AsAIAgent(
    instructions: """
        You are the Orchestrator agent. You receive a task plan from the Analyst.
        Your job is to dispatch each task to the correct specialist agent, one at a time.
        Read the plan and identify the next uncompleted task.
        For [frontend] tasks, hand off to FESpecialist.
        For [dotnet] tasks, hand off to DotnetSpecialist.
        For [testing] tasks, hand off to TestingSpecialist.
        When handing off, include the specific task description for the specialist.
        When a specialist returns, check if there are remaining tasks and dispatch the next one.
        Once ALL tasks are completed, hand off to the Summarizer with the full results.
    """,
    "Orchestrator");

var feSpecialist = chatClient.AsAIAgent(
    instructions: """
        You are the Frontend Specialist agent. You handle frontend development tasks.
        When you receive a task, describe how you would implement it:
        - Specify the files you would create or modify
        - Outline the key code structure (components, hooks, styles)
        - Mention any npm packages or dependencies needed
        - Note any API endpoints you would consume
        Keep your response focused and concise (5-10 lines of explanation).
        When you are finished with your task, hand off back to the Orchestrator
        so it can dispatch the next task.
    """,
    "FESpecialist");

var dotnetSpecialist = chatClient.AsAIAgent(
    instructions: """
        You are the .NET/Backend Specialist agent. You handle backend and API tasks.
        When you receive a task, describe how you would implement it:
        - Specify the C# files, controllers, or services you would create or modify
        - Outline the key classes, methods, and data models
        - Mention any NuGet packages or middleware needed
        - Note any database changes or migrations required
        Keep your response focused and concise (5-10 lines of explanation).
        When you are finished with your task, hand off back to the Orchestrator
        so it can dispatch the next task.
    """,
    "DotnetSpecialist");

var testingSpecialist = chatClient.AsAIAgent(
    instructions: """
        You are the Testing Specialist agent. You handle all testing-related tasks.
        When you receive a task, describe how you would implement the tests:
        - Specify the test files and test class names
        - Outline the key test cases (happy path, edge cases, error scenarios)
        - Mention the testing framework and any mocking libraries needed
        - Note what dependencies would need to be mocked or stubbed
        Keep your response focused and concise (5-10 lines of explanation).
        When you are finished with your task, hand off back to the Orchestrator
        so it can dispatch the next task.
    """,
    "TestingSpecialist");

var summarizer = chatClient.AsAIAgent(
    instructions: """
        You are the Summarizer agent. You receive the completed results of all tasks.
        Your job is to create a clear, concise summary of everything that was accomplished.
        Structure your summary as:
        1. A brief overview of the original request
        2. A list of completed tasks grouped by area (Frontend, Backend, Testing)
        3. Any cross-cutting concerns or integration points between the tasks
        4. Suggested next steps or follow-up items
        Keep the summary professional and actionable.
        Do not hand off to any other agent — you are the final step.
    """,
    "Summarizer");

var builder = AgentWorkflowBuilder.CreateHandoffBuilderWith(analyst);

builder.WithHandoff(analyst, orchestrator,
    "Hand off the task plan to the Orchestrator for execution");

builder.WithHandoffs(orchestrator,
    [feSpecialist, dotnetSpecialist, testingSpecialist]);

builder.WithHandoff(orchestrator, summarizer,
    "Hand off to Summarizer when all tasks are completed");

builder.WithHandoffs(
    [feSpecialist, dotnetSpecialist, testingSpecialist],
    orchestrator,
    "Return to Orchestrator after completing the assigned task");

builder.EnableReturnToPrevious();

var workflow = builder.Build();

Console.WriteLine("Handoff Workflow: Analyst -> Orchestrator -> [Specialists] -> Summarizer\n");

// Quick test: does the analyst agent work at all?
Console.WriteLine("[TEST] Testing analyst agent directly...");
var testSession = await analyst.CreateSessionAsync();
var testResult = await analyst.RunAsync("Build a login form", testSession);
Console.WriteLine($"[TEST] Analyst response: {testResult}");
Console.WriteLine("[TEST] Direct agent test complete.\n");

await using var run = await InProcessExecution.OpenStreamingAsync(workflow);

bool hadError = false;
do
{
    Console.Write("> ");
    string userInput = Console.ReadLine() ?? string.Empty;

    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        break;

    await run.TrySendMessageAsync(userInput);
    string? speakingAgent = null;

    await foreach (var evt in run.WatchStreamAsync())
    {
        Console.Error.WriteLine($"[DEBUG] {evt.GetType().Name}: {string.Join(", ", evt.GetType().GetProperties().Select(p => $"{p.Name}={p.GetValue(evt)}"))}");

        switch (evt)
        {
            case AgentResponseUpdateEvent update:
                if (speakingAgent == null || speakingAgent != update.Update.AuthorName)
                {
                    speakingAgent = update.Update.AuthorName;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"\n\n--- [{speakingAgent}] ---\n");
                    Console.ResetColor();
                }
                Console.Write(update.Update.Text);
                break;

            case WorkflowErrorEvent workflowError:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error.");
                Console.ResetColor();
                hadError = true;
                break;

            case WorkflowWarningEvent warning when warning.Data is string message:
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(message);
                Console.ResetColor();
                break;
        }
    }

    Console.WriteLine();
} while (!hadError);
