/* Demonstrates the creation of a simple process that has multiple steps, takes
   user input, interacts with the chat completion service, and demonstrates cycles
   in the process.
*/

using System.Diagnostics.CodeAnalysis;
using AgenticAI.SemanticKernelProcesses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Process.Tools;

Console.WriteLine("Semantic Kernel Processes Demo");

#region Env variables

var deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENTNAME");
var apiKey = Environment.GetEnvironmentVariable("APIKEY");
var endpoint = Environment.GetEnvironmentVariable("ENDPOINTURL");

ArgumentException.ThrowIfNullOrEmpty(deploymentName);
ArgumentException.ThrowIfNullOrEmpty(apiKey);
ArgumentException.ThrowIfNullOrEmpty(endpoint);

#endregion

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
var kernel = builder.Build();

ProcessBuilder process = new("ChatBot");
var introStep = process.AddStepFromType<IntroStep>();
var userInputStep = process.AddStepFromType<ChatUserInputStep>();
var responseStep = process.AddStepFromType<ChatBotResponseStep>();

// Define the behavior when the process receives an external event
process
    .OnInputEvent(ChatBotEvents.StartProcess)
    .SendEventTo(new ProcessFunctionTargetBuilder(introStep));

// When the intro is complete, notify the userInput step
introStep
    .OnFunctionResult()
    .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep));

// When the userInput step emits an exit event, send it to the end step
userInputStep
    .OnEvent(ChatBotEvents.Exit)
    .StopProcess();

// When the userInput step emits a user input event, send it to the assistantResponse step
userInputStep
    .OnEvent(CommonEvents.UserInputReceived)
    .SendEventTo(new ProcessFunctionTargetBuilder(responseStep, parameterName: "userMessage"));
// When the assistantResponse step emits a response, send it to the userInput step

responseStep
    .OnEvent(ChatBotEvents.AssistantResponseGenerated)
    .SendEventTo(new ProcessFunctionTargetBuilder(userInputStep));

// Build the process to get a handle that can be started
var kernelProcess = process.Build();
// Generate a Mermaid diagram for the process and print it to the console
var mermaidGraph = kernelProcess.ToMermaid();
Console.WriteLine($"=== Start - Mermaid Diagram for '{process.Name}' ===");
Console.WriteLine(mermaidGraph);
Console.WriteLine($"=== End - Mermaid Diagram for '{process.Name}' ===");

// Generate an image from the Mermaid diagram
var generatedImagePath = await MermaidRenderer.GenerateMermaidImageAsync(mermaidGraph, "ChatBotProcess.png");
Console.WriteLine($"Diagram generated at: {generatedImagePath}");

// Start the process with an initial external event
await using var runningProcess =
    await kernelProcess.StartAsync(kernel, new KernelProcessEvent() { Id = ChatBotEvents.StartProcess, Data = null });

// <summary>
/// The simplest implementation of a process step. IntroStep
/// </summary>
[Experimental("SKEXP0080")]
sealed class IntroStep : KernelProcessStep
{
    /// <summary>
    /// Prints an introduction message to the console.
    /// </summary>
    [KernelFunction]
    public void PrintIntroMessage()
    {
        System.Console.WriteLine("Welcome to Processes in Semantic Kernel.\n");
    }
}

/// <summary>
/// A step that elicits user input.
/// </summary>
[Experimental("SKEXP0080")]
sealed class ChatUserInputStep : ScriptedUserInputStep
{
    protected override void PopulateUserInputs(UserInputState state)
    {
        state.UserInputs.Add("Hello");
        state.UserInputs.Add("How tall is the tallest mountain?");
        state.UserInputs.Add("How low is the lowest valley?");
        state.UserInputs.Add("How wide is the widest river?");
        state.UserInputs.Add("exit");
        state.UserInputs.Add("This text will be ignored because exit process condition was already met at this point.");
    }

    public override async ValueTask GetUserInputAsync(KernelProcessStepContext context)
    {
        var userMessage = this.GetNextUserMessage();

        if (string.Equals(userMessage, "exit", StringComparison.OrdinalIgnoreCase))
        {
            // exit condition met, emitting exit event
            await context.EmitEventAsync(new() { Id = ChatBotEvents.Exit, Data = userMessage });
            return;
        }

        // emitting userInputReceived event
        await context.EmitEventAsync(new() { Id = CommonEvents.UserInputReceived, Data = userMessage });
    }
}

/// <summary>
/// A step that takes the user input from a previous step and generates a response from the chat completion service.
/// </summary>
[Experimental("SKEXP0080")]
sealed class ChatBotResponseStep : KernelProcessStep<ChatBotState>
{
    public static class ProcessFunctions
    {
        public const string GetChatResponse = nameof(GetChatResponse);
    }

    /// <summary>
    /// The internal state object for the chat bot response step.
    /// </summary>
    internal ChatBotState? _state;

    /// <summary>
    /// ActivateAsync is the place to initialize the state object for the step.
    /// </summary>
    /// <param name="state">An instance of <see cref="ChatBotState"/></param>
    /// <returns>A <see cref="ValueTask"/></returns>
    public override ValueTask ActivateAsync(KernelProcessStepState<ChatBotState> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Generates a response from the chat completion service.
    /// </summary>
    /// <param name="context">The context for the current step and process. <see cref="KernelProcessStepContext"/></param>
    /// <param name="userMessage">The user message from a previous step.</param>
    /// <param name="_kernel">A <see cref="Kernel"/> instance.</param>
    /// <returns></returns>
    [KernelFunction(ProcessFunctions.GetChatResponse)]
    public async Task GetChatResponseAsync(KernelProcessStepContext context, string userMessage, Kernel _kernel)
    {
        _state!.ChatMessages.Add(new(AuthorRole.User, userMessage));
        var chatService = _kernel.Services.GetRequiredService<IChatCompletionService>();
        var response =
            await chatService.GetChatMessageContentAsync(_state.ChatMessages).ConfigureAwait(false);
        if (response == null)
        {
            throw new InvalidOperationException("Failed to get a response from the chat completion service.");
        }

        System.Console.ForegroundColor = ConsoleColor.Yellow;
        System.Console.WriteLine($"ASSISTANT: {response.Content}");
        System.Console.ResetColor();

        // Update state with the response
        _state.ChatMessages.Add(response);

        // emit event: assistantResponse
        await context.EmitEventAsync(new KernelProcessEvent
            { Id = ChatBotEvents.AssistantResponseGenerated, Data = response });
    }
}

/// <summary>
/// The state object for the <see cref="ChatBotResponseStep"/>.
/// </summary>
sealed class ChatBotState
{
    internal ChatHistory ChatMessages { get; } = new();
}

/// <summary>
/// A class that defines the events that can be emitted by the chat bot process. This is
/// not required but used to ensure that the event names are consistent.
/// </summary>
static class ChatBotEvents
{
    public const string StartProcess = "startProcess";
    public const string IntroComplete = "introComplete";
    public const string AssistantResponseGenerated = "assistantResponseGenerated";
    public const string Exit = "exit";
}