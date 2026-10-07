using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

var environmentName = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var foundrySection = configuration.GetSection("Foundry");
var projectName = GetRequiredSetting(foundrySection, "ProjectName");
var resourceName = GetRequiredSetting(foundrySection, "ResourceName");
var foundryAgentName = GetRequiredSetting(foundrySection, "FoundryAgentName");
var model = GetRequiredSetting(foundrySection, "Model");
var foundryProjectEndpoint = $"https://{resourceName}.services.ai.azure.com/api/projects/{projectName}";

Console.WriteLine($"Foundry project endpoint: {foundryProjectEndpoint}");

// Create a project client to call the Foundry API
var projectClient = new AIProjectClient(
    endpoint: new Uri(foundryProjectEndpoint),
    tokenProvider: new AzureCliCredential());

// Read the instructions from the file at /workspaces/foundry-agents/docs/sundae-support-instructions.md
var instructions = File.ReadAllText("/workspaces/foundry-agents/docs/sundae-support-instructions.md");

// Create an agent with a model and system prompt / instructions.
ProjectsAgentDefinition agentDefinition = new DeclarativeAgentDefinition(model)
{
    Instructions = instructions
};

ProjectsAgentVersion agent = projectClient.AgentAdministrationClient.CreateAgentVersion(
    agentName: foundryAgentName,
    options: new ProjectsAgentVersionCreationOptions(agentDefinition)
);

Console.WriteLine($"Created agent version: {agent.Version}, id: {agent.Id}, name: {agent.Name}");

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
