using Azure;
using Azure.AI.Agents.Persistent;
using Azure.Identity;

Console.WriteLine("Simple Connected Agent");

#region Env variables

var projectEndpoint = Environment.GetEnvironmentVariable("ProjectEndpoint");

if (string.IsNullOrEmpty(projectEndpoint))
{
    Console.WriteLine("Please set the ProjectEndpoint environment variable.");
    return;
}

Console.WriteLine("Connecting to the Azure OpenAI service at: " + projectEndpoint);

var modelDeploymentName = Environment.GetEnvironmentVariable("ModelDeploymentName");
if (string.IsNullOrEmpty(modelDeploymentName))
{
    Console.WriteLine("Please set the ModelDeploymentName environment variable.");
    return;
}

Console.WriteLine("Using model deployment: " + modelDeploymentName);

var stockAgentId = Environment.GetEnvironmentVariable("StockAgentId");
if (string.IsNullOrEmpty(stockAgentId))
{
    Console.WriteLine("Please set the StockAgentId environment variable.");
    return;
}

Console.WriteLine("Using stock agent: " + stockAgentId);

#endregion

#region Agent creation

PersistentAgentsClient client = new(projectEndpoint, new DefaultAzureCredential());

PersistentAgent stockAgent = client.Administration.GetAgent(stockAgentId, CancellationToken.None);
Console.WriteLine("Connected to stock agent: " + stockAgent.Name);

ConnectedAgentToolDefinition connectedAgentDefinition = new(new ConnectedAgentDetails(stockAgent.Id, stockAgent.Name, "Gets the stock price of a company"));

PersistentAgent mainAgent = client.Administration.CreateAgent(
    model: modelDeploymentName,
    name: "stock_price_bot",
    instructions: "Your job is to get the stock price of a company, using the available tools.",
    tools: [connectedAgentDefinition]
);

#endregion

#region Run the agent

PersistentAgentThread thread = client.Threads.CreateThread();

// Create message to thread
PersistentThreadMessage message = client.Messages.CreateMessage(
    thread.Id,
    MessageRole.User,
    "What is the stock price of Microsoft?");

Console.WriteLine("Sending messages to agents....");
// Run the agent
ThreadRun run = client.Runs.CreateRun(thread, mainAgent);
do
{
    Thread.Sleep(TimeSpan.FromMilliseconds(500));
    run = client.Runs.GetRun(thread.Id, run.Id);
}
while (run.Status == RunStatus.Queued
       || run.Status == RunStatus.InProgress);

Console.WriteLine("Run status: " + run.Status);
// Confirm that the run completed successfully
if (run.Status != RunStatus.Completed)
{
    throw new Exception("Run did not complete successfully, error: " + run.LastError?.Message);
}

#endregion

#region List and retrieve messages

Pageable<PersistentThreadMessage> messages = client.Messages.GetMessages(
    threadId: thread.Id,
    order: ListSortOrder.Ascending
);

foreach (var threadMessage in messages)
{
    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
    foreach (var contentItem in threadMessage.ContentItems)
    {
        switch (contentItem)
        {
            case MessageTextContent textItem:
            {
                var response = textItem.Text;
                if (textItem.Annotations != null)
                {
                    foreach (var annotation in textItem.Annotations)
                    {
                        if (annotation is MessageTextUriCitationAnnotation urlAnnotation)
                        {
                            response = response.Replace(urlAnnotation.Text, $" [{urlAnnotation.UriCitation.Title}]({urlAnnotation.UriCitation.Uri})");
                        }
                    }
                }
                Console.Write($"Agent response: {response}");
                break;
            }
            case MessageImageFileContent imageFileItem:
                Console.Write($"<image from ID: {imageFileItem.FileId}");
                break;
        }

        Console.WriteLine();
    }
}

#endregion

#region Cleanup

//cleanup
Console.WriteLine("Cleaning up agents and thread...");
client.Threads.DeleteThread(thread.Id);
client.Administration.DeleteAgent(mainAgent.Id);
client.Administration.DeleteAgent(stockAgent.Id);

#endregion
