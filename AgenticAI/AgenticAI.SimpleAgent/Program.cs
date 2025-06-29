﻿using Azure;
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

var modelDeploymentName = Environment.GetEnvironmentVariable("DEPLOYMENTNAME") ?? "gpt-4o";
Console.WriteLine("Using model deployment: " + modelDeploymentName);

var bingConnectionId = Environment.GetEnvironmentVariable("BING_CONNECTION_ID");
ArgumentException.ThrowIfNullOrEmpty(bingConnectionId);


var defaultAzureCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    ExcludeAzureCliCredential = false,
    ExcludeEnvironmentCredential = true,
    ExcludeInteractiveBrowserCredential = true,
    ExcludeVisualStudioCredential = true
});

#endregion

#region Agent creation

PersistentAgentsClient client = new(projectEndpoint, defaultAzureCredential);
var searchConfig = new BingGroundingSearchConfiguration(bingConnectionId)
{
    Count = 5, Freshness = "Day"
};

var bingGroundingTool = new BingGroundingToolDefinition(
    new BingGroundingSearchToolParameters([searchConfig])
);

var instructions = "Use the bing grounding tool to answer questions. " +
                   "Tell me the time of the data you are using to answer the question.";
PersistentAgent agentWithBingTool = client.Administration.CreateAgent(
    model: modelDeploymentName,
    name: "AgentWithBingTool",
    instructions: instructions,
    tools: [bingGroundingTool]
);

 var connectedAgentName = "stock_price_agent";
 ConnectedAgentToolDefinition connectedAgentDefinition =
     new(new ConnectedAgentDetails(agentWithBingTool.Id, connectedAgentName, "Gets the stock price of a company"));

PersistentAgent mainAgent = client.Administration.CreateAgent(
    model: modelDeploymentName,
    name: "stock_price_bot",
    instructions:
    "Your job is to get the stock price of a company, using the available tools. " +
    "If no data is available, respond with 'I don't know'. Put the information about the date of the data.",
    tools: [connectedAgentDefinition]
);

#endregion

#region Run the agent

PersistentAgentThread thread = client.Threads.CreateThread();

// Create message to thread
PersistentThreadMessage message = client.Messages.CreateMessage(
    thread.Id,
    MessageRole.User,
    "What is the stock price of Microsoft? Give me your answer in EUR.");

Console.WriteLine("Sending messages to agents....");
// Run the agent
ThreadRun run = client.Runs.CreateRun(thread, mainAgent);
do
{
    Thread.Sleep(TimeSpan.FromMilliseconds(500));
    run = client.Runs.GetRun(thread.Id, run.Id);
} while (run.Status == RunStatus.Queued
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
                            response = response.Replace(urlAnnotation.Text,
                                $" [{urlAnnotation.UriCitation.Title}]({urlAnnotation.UriCitation.Uri})");
                        }
                    }
                }

                Console.Write($"Agent response: {response}");
                break;
            }
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
client.Administration.DeleteAgent(agentWithBingTool.Id);

#endregion