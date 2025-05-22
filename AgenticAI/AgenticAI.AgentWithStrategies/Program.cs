/*
 * Example by Akshay Kokane
 *
 * Product Manager: The maestro of the project, crafting a development plan that translates user needs into actionable steps.
   Software Engineer: The coding wizard, responsible for implementing the plan and bringing the agent to life.
   Project Manager: The guardian of quality, ensuring the final product meets all specifications and receives the green light for release.
 *
 */

using AgenticAI.AgentWithStrategies;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

Console.WriteLine("Hello Multi-Agent with Azure OpenAI and agent strategies!");

var deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENTNAME");
var apiKey = Environment.GetEnvironmentVariable("APIKEY");
var endpoint = Environment.GetEnvironmentVariable("ENDPOINTURL");

ArgumentException.ThrowIfNullOrEmpty(deploymentName);
ArgumentException.ThrowIfNullOrEmpty(apiKey);
ArgumentException.ThrowIfNullOrEmpty(endpoint);

var builder = Kernel.CreateBuilder();
builder.AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);
var kernel = builder.Build();
var ProgamManager = """
                        You are a program manager which will take the requirement and create a plan for creating app. Program Manager understands the 
                        user requirements and form the detail documents with requirements and costing. 
                    """;

var SoftwareEngineer = """
                          You are Software Engieer, and your goal is develop web app using HTML and JavaScript (JS) by taking into consideration all
                          the requirements given by Program Manager. 
                       """;

var Manager = """
                  You are manager which will review software engineer code, and make sure all client requirements are completed.
                   Once all client requirements are completed, you can approve the request by just responding "approve"
              """;

ChatCompletionAgent ProgramManagerAgent =
    new()
    {
        Instructions = ProgamManager,
        Name = "ProgramManagerAgent",
        Kernel = kernel
    };

ChatCompletionAgent SoftwareEngineerAgent =
    new()
    {
        Instructions = SoftwareEngineer,
        Name = "SoftwareEngineerAgent",
        Kernel = kernel
    };

ChatCompletionAgent ProjectManagerAgent =
    new()
    {
        Instructions = Manager,
        Name = "ProjectManagerAgent",
        Kernel = kernel
    };

#pragma warning disable SKEXP0110
AgentGroupChat chat = new(ProgramManagerAgent, SoftwareEngineerAgent, ProjectManagerAgent)

{
    ExecutionSettings =
        new()
        {
            TerminationStrategy = new ApprovalTerminationStrategy

            {
                Agents = [ProjectManagerAgent],
                MaximumIterations = 6,
            }
        }
};

var input = """

            I want to develop calculator app. 
            Keep it very simple. And get final approval from manager.
            """;

chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
Console.WriteLine($"# {AuthorRole.User}: '{input}'");

await foreach (var content in chat.InvokeAsync())
{
    Console.WriteLine($"# {content.Role} - {content.AuthorName ?? "*"}: '{content.Content}'");
}

Console.WriteLine("Done with multi-agent strategy with Azure OpenAI.");