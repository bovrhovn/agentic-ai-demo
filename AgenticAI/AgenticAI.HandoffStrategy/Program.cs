#region Environment Variables

#pragma warning disable SKEXP0110
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using Microsoft.SemanticKernel.ChatCompletion;

var deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENTNAME");
var apiKey = Environment.GetEnvironmentVariable("APIKEY");
var endpoint = Environment.GetEnvironmentVariable("ENDPOINTURL");

ArgumentException.ThrowIfNullOrEmpty(deploymentName);
ArgumentException.ThrowIfNullOrEmpty(apiKey);
ArgumentException.ThrowIfNullOrEmpty(endpoint);

#endregion

#region Kernel setup

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
var kernel = builder.Build();

#endregion

ChatCompletionAgent triageAgent =
    new()
    {
        Instructions = "A customer support agent that triages issues.",
        Name = "TriageAgent",
        Kernel = kernel,
        Description = "A customer support agent that triages issues."
    };
ChatCompletionAgent statusAgent =
    new()
    {
        Instructions = "Handle order status requests.",
        Name = "OrderStatusAgent",
        Kernel = kernel,
        Description = "A customer support agent that checks order status."
    };
statusAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderStatusPlugin()));
ChatCompletionAgent returnAgent =
    new()
    {
        Instructions = "Handle order return requests.",
        Name = "OrderReturnAgent",
        Kernel = kernel,
        Description = "A customer support agent that handles order returns."
    };
returnAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderReturnPlugin()));

ChatCompletionAgent refundAgent =
    new()
    {
        Instructions = "Handle order refund requests.",
        Name = "OrderRefundAgent",
        Kernel = kernel,
        Description = "A customer support agent that handles order refund."
    };
refundAgent.Kernel.Plugins.Add(KernelPluginFactory.CreateFromObject(new OrderRefundPlugin()));

// Define user responses for InteractiveCallback 
Queue<string> responses = new();
var task = "I am a customer that needs help with my orders";
responses.Enqueue("I'd like to track the status of my order");
responses.Enqueue("My order ID is 123");
responses.Enqueue("I want to return another order of mine");
responses.Enqueue("Order ID 321");
responses.Enqueue("Broken item");
responses.Enqueue("No, bye");

// Define the orchestration
HandoffOrchestration orchestration =
    new(OrchestrationHandoffs
            .StartWith(triageAgent)
            .Add(triageAgent, statusAgent, returnAgent, refundAgent)
            .Add(statusAgent, triageAgent, "Transfer to this agent if the issue is not status related")
            .Add(returnAgent, triageAgent, "Transfer to this agent if the issue is not return related")
            .Add(refundAgent, triageAgent, "Transfer to this agent if the issue is not refund related"),
        triageAgent,
        statusAgent,
        returnAgent,
        refundAgent)
    {
        InteractiveCallback = () =>
        {
            string input = responses.Dequeue();
            Console.WriteLine($"\n# INPUT: {input}\n");
            return ValueTask.FromResult(new ChatMessageContent(AuthorRole.User, input));
        },
        ResponseCallback = content =>
        {
            Console.WriteLine($"\n# RESPONSE: {content.Content}\n");
            return ValueTask.CompletedTask;
        }
    };

// Start the runtime
InProcessRuntime runtime = new();
await runtime.StartAsync();

// Run the orchestration
Console.WriteLine($"\n# INPUT:\n{task}\n");
OrchestrationResult<string> result = await orchestration.InvokeAsync(task, runtime);

string text = await result.GetValueAsync(TimeSpan.FromSeconds(300));
Console.WriteLine($"\n# RESULT: {text}");

await runtime.RunUntilIdleAsync();

public sealed class OrderStatusPlugin
{
    [KernelFunction]
    public string CheckOrderStatus(string orderId) => $"Order {orderId} is shipped and will arrive in 2-3 days.";
}

public sealed class OrderReturnPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) =>
        $"Return for order {orderId} has been processed successfully.";
}

public sealed class OrderRefundPlugin
{
    [KernelFunction]
    public string ProcessReturn(string orderId, string reason) =>
        $"Refund for order {orderId} has been processed successfully.";
}