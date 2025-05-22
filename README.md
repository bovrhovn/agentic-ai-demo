# Demos how to use agentic AI

Repository with demos about Azure Agentic AI approach with demos to show how to use it.

Agents developed using Foundry Agent Service have the following elements:

1. Model: A deployed generative AI model that enables the agent to reason and generate natural language responses to prompts. You can use common OpenAI models and a selection of models from the Azure AI Foundry model catalog.
2. Knowledge: data sources that enable the agent to ground prompts with contextual data. Potential knowledge sources include Internet search results from Microsoft Bing, an Azure AI Search index, or your own data and documents.
3. Tools: Programmatic functions that enable the agent to automate actions. Built-in tools to access knowledge in Azure AI Search and Bing are provided as well as a code interpreter tool that you can use to generate and run Python code. You can also create custom tools using your own code or Azure Functions.

![Azure AI foundry](https://learn.microsoft.com/en-us/azure/ai-services/agents/media/agent-service-the-glue.png)

## Demo structure

The demos are structured in the following way:

- **AgenticAI.SimpleAgent**: A simple agent that uses another agent as a tool to get stock price. Code is in `AgenticAI/AgenticAI.SimpleAgent`.


To run the demo, you need to set up the environment by installing [.NET](https://dot.net). You can use editors like [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) to help running the demos.
Dotnet CLI is a cross-platform toolchain for developing, building, running, and publishing .NET applications. You can use it to run the demos from the command line.

Go to the folder where the demo is located and run the following command:

``` powershell

dotnet run

```

# Additional Resources

1. [Azure AI Foundry](https://learn.microsoft.com/en-us/azure/ai-foundry/)
2. [Azure AI Foundry Agent Service](https://learn.microsoft.com/en-us/azure/ai-foundry/agent-service/)
3. [Azure AI Foundry Agent Service - Quickstart](https://learn.microsoft.com/en-us/azure/ai-foundry/agent-service/quickstart/)
4. [AI agent foundamentals](https://learn.microsoft.com/en-us/training/modules/ai-agent-fundamentals)