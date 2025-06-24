# Demos how to use agentic AI

Repository with demos about Azure Agentic AI approach with demos to show how to use it.

Agents developed using Foundry Agent Service have the following elements:

1. Model: A deployed generative AI model that enables the agent to reason and generate natural language responses to prompts. You can use common OpenAI models and a selection of models from the Azure AI Foundry model catalog.
2. Knowledge: data sources that enable the agent to ground prompts with contextual data. Potential knowledge sources include Internet search results from Microsoft Bing, an Azure AI Search index, or your own data and documents.
3. Tools: Programmatic functions that enable the agent to automate actions. Built-in tools to access knowledge in Azure AI Search and Bing are provided as well as a code interpreter tool that you can use to generate and run Python code. You can also create custom tools using your own code or Azure Functions.

![Azure AI foundry](https://learn.microsoft.com/en-us/azure/ai-services/agents/media/agent-service-the-glue.png)

## Demo structure

The demos are structured in the following way:

- **AgenticAI.SimpleAgent**: A simple agent that uses another agent as a tool to get stock price. Code is in [`AgenticAI/AgenticAI.SimpleAgent`](AgenticAI/AgenticAI.SimpleAgent).
- **AgenticAI.SimpleAgentWithPlugin**: A simple agent that uses another agent as a tool - writer and retrieval. Code is in [`AgenticAI/AgenticAI.SimpleAgent`](AgenticAI/AgenticAI.SimpleAgentWihPlugin).
- **AgenticAI.AgentWithStrategies**: Simple demonstration to use different agents with different execution strategies to control the flow with models only. Code is in [`AgenticAI/AgenticAI.AgentWithStrategies`](AgenticAI/AgenticAI.AgentWithStrategies).
- **AgenticAI.SemanticKernelProcceses**: Simple demonstration to use Semantic Kernel to demonstrate how to create a simple process with a loop and a conditional exit. Code is in [`AgenticAI/AgenticAI.SemanticKernelProcesses`](AgenticAI/AgenticAI.SemanticKernelProcesses).
- **AgenticAI.HandoffStrategy**: Demonstration of how to use handoff strategy to control the flow of the agent. Code is in [`AgenticAI/AgenticAI.HandoffStrategy`](AgenticAI/AgenticAI.HandoffStrategy).
- **AgenticAI.SequentialStrategy**: Demonstration of how to use sequential strategy to control the flow of the agent. Code is in [`AgenticAI/AgenticAI.SequentialStrategy`](AgenticAI/AgenticAI.SequentialStrategy).

To run the demo, you need to set up the environment by installing [.NET](https://dot.net). You can use editors like [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) to help running the demos.
Dotnet CLI is a cross-platform toolchain for developing, building, running, and publishing .NET applications. You can use it to run the demos from the command line.

You'll need to have [Azure AI Foundry](https://ai.azure.com/) created and configured (model deployed). You will need to pass them as environment variables or in the code.

Go to the folder where the demo is located and run the following command (below is an example for [PowerShell](https://learn.microsoft.com/en-us/powershell/)):

``` powershell

Set-EnvironmentVariable -Name "DEPLOYMENTNAME" -Value "<your-endpoint-to-the-deployed-model>" -Scope Process
Set-EnvironmentVariable -Name "APIKEY" -Value "<your-api-key-from-model-deployed>" -Scope Process
Set-EnvironmentVariable -Name "ENDPOINTURL" -Value "<your-endpoint-to-the-deployed-model>" -Scope Process
Set-EnvironmentVariable -Name "ProjectEndpoint" -Value "<your-endpoint-to-azure-foundry-ai-project>" -Scope Process
## Bing connection id is in the format of /subscriptions/<subscription_id>/resourceGroups/<resource_group_name>/providers/Microsoft.CognitiveServices/accounts/<ai_service_name>/projects/<project_name>/connections/<connection_name>
Set-EnvironmentVariable -Name "BING_CONNECTION_ID" -Value "Bing connection id" -Scope Process

dotnet run

```
Pictures are from [Microsoft Docs webpage](https://learn.microsoft.com/en-us/semantic-kernel/frameworks/agent/agent-orchestration/handoff?pivots=programming-language-csharp).

# Additional Resources

1. [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/)
2. [Azure AI Foundry Agent Service](https://learn.microsoft.com/en-us/azure/ai-foundry/agent-service/)
3. [Azure AI Foundry Agent Service - Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/agent-service/quickstart/)
4. [AI agent foundamentals](https://learn.microsoft.com/en-us/training/modules/ai-agent-fundamentals)