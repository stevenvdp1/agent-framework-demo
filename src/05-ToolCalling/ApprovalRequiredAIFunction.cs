using Microsoft.Extensions.AI;

namespace ToolCalling;

public class ApprovalRequiredAIFunction(AIFunction innerFunction) : DelegatingAIFunction(innerFunction)
{
    protected override async ValueTask<object?> InvokeCoreAsync(
        AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var args = string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"));
        Console.Write($"\n⚠️  Tool '{Name}' wants to execute with args: {args}. Allow? (y/n): ");

        var response = Console.ReadLine();
        if (response?.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase) == true)
            return await base.InvokeCoreAsync(arguments, cancellationToken);

        return "Tool call was denied by the user.";
    }
}
