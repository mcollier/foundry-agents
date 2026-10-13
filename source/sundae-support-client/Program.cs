using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.AI.Extensions.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using OpenAI;
using OpenAI.Responses;
using OpenAI.Conversations;

#pragma warning disable OPENAI001

const string userMessage = "Hi froyo-sundae-support";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    // Developer-local overrides; excluded from source control, so it may not exist.
    .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var foundrySection = configuration.GetSection("Foundry");
var projectName = GetRequiredSetting(foundrySection, "ProjectName");
var resourceName = GetRequiredSetting(foundrySection, "ResourceName");
var agentName = GetRequiredSetting(foundrySection, "FoundryAgentName");
var endpoint = $"https://{resourceName}.services.ai.azure.com/api/projects/{projectName}";

// Connect to your project using the endpoint from your project page.
// AzureCliCredential can use your Azure CLI identity; run `az login` for local authentication.
AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: new AzureCliCredential());

ProjectResponsesClient agentClient =
    projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(agentName);

// Reuse the agent-scoped SDK pipeline for conversations as well as responses.
ProjectConversationsClient conversationClient = new(
    agentClient.Pipeline,
    new OpenAIClientOptions { Endpoint = agentClient.Endpoint });

ConversationResource conversation = conversationClient.CreateProjectConversation();

ProjectResponsesClient responseClient =
    projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgentEndpoint(
        agentName, defaultConversationId: conversation.Id);

ResponseResult response = responseClient.CreateResponse(userMessage);

Console.WriteLine(response.GetOutputText());

// Keep asking follow-up questions in the same conversation until a blank line is entered.
while (true)
{
    Console.Write("You: ");

    // A null result means end of input (Ctrl+D or a closed/redirected stream).
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        break;
    }

    ResponseResult reply = responseClient.CreateResponse(input);
    Console.WriteLine(reply.GetOutputText());
}

// Returns the configured value for the given key, or throws if it is missing, empty, or whitespace.
static string GetRequiredSetting(IConfigurationSection section, string key)
{
    var value = section[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"{section.Path}:{key} is not configured.");
    }

    return value;
}