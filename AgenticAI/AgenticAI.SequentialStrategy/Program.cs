#region Environment Variables

#pragma warning disable SKEXP0110
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Orchestration.Sequential;
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

var analystAgent = new ChatCompletionAgent
{
    Name = "Analyst",
    Instructions = """
                   You are a marketing analyst. Given a product description, identify:
                   - Key features
                   - Target audience
                   - Unique selling points
                   """,
    Description = "A agent that extracts key concepts from a product description.",
    Kernel = kernel,
};

var writerAgent = new ChatCompletionAgent
{
    Name = "Copywriter",
    Instructions = """
                   You are a marketing copywriter. Given a block of text describing features, audience, and USPs,
                   compose a compelling marketing copy (like a newsletter section) that highlights these points.
                   Output should be short (around 150 words), output just the copy as a single text block.
                   """,
    Description = "An agent that writes a marketing copy based on the extracted concepts.",
    Kernel = kernel,
};

var editorAgent = new ChatCompletionAgent
{
    Name = "Editor",
    Instructions = """
                   You are an editor. Given the draft copy, correct grammar, improve clarity, ensure consistent tone,
                   give format and make it polished. Output the final improved copy as a single text block.
                   """,
    Description = "An agent that formats and proofreads the marketing copy.",
    Kernel = kernel,
};

ChatHistory history = [];
SequentialOrchestration orchestration = new(analystAgent, writerAgent, editorAgent)
{
    ResponseCallback = responseCallback,
};

var runtime = new InProcessRuntime();
await runtime.StartAsync();

var result = await orchestration.InvokeAsync(
    "Star Wars lego set with iconic characters and vehicles, perfect for fans of all ages.",
    runtime);

var output = await result.GetValueAsync(TimeSpan.FromSeconds(20));
Console.WriteLine($"\n# RESULT: {output}");
Console.WriteLine("\n\nORCHESTRATION HISTORY");
foreach (var message in history)
{
    Console.WriteLine(message.Content);
}

ValueTask responseCallback(ChatMessageContent response)
{
    history.Add(response);
    return ValueTask.CompletedTask;
}