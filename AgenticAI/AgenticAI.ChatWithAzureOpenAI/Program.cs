using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

Console.WriteLine("Calling deployments");

#region Env variables

var defaultAzureCredential = new DefaultAzureCredential(
    new DefaultAzureCredentialOptions
    {
        ExcludeAzureCliCredential = false,
        ExcludeEnvironmentCredential = true,
        ExcludeManagedIdentityCredential = true,
        ExcludeVisualStudioCredential = true
    });
var projectEndpoint = Environment.GetEnvironmentVariable("ProjectEndpoint");
ArgumentException.ThrowIfNullOrEmpty(projectEndpoint);
var deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENTNAME") ?? "gpt-4o";

#endregion

var client = new AzureOpenAIClient(new Uri(projectEndpoint),
    defaultAzureCredential);
var chatClient = client.GetChatClient(deploymentName);
var requestOptions = new ChatCompletionOptions
{
    MaxOutputTokenCount = 4096,
    Temperature = 1.0f,
    TopP = 1.0f
};
var messages = new List<ChatMessage>
{
    new SystemChatMessage("You are a chat assistant that helps users with their queries. " +
                                  "You should provide helpful and accurate responses based on the user's input." +
                                  "If you don't know the answer say 'I don't know'." +
                                  "Tell the source of the data you are trained on."),
    new UserChatMessage("What is the stock price for Microsoft, code MSFT?")
};
var response = await chatClient.CompleteChatAsync(messages, requestOptions);
if (response.Value == null)
    throw new InvalidOperationException("No response from the chat service.");

var assistantMessage = response.Value.Content.FirstOrDefault()?.Text;
if (string.IsNullOrEmpty(assistantMessage))
    throw new InvalidOperationException("No content in the response from the chat service.");
Console.WriteLine("Assistant: " + assistantMessage);