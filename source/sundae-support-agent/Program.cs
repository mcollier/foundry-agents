using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var foundrySection = configuration.GetSection("Foundry");
var projectName = foundrySection["ProjectName"] ?? throw new InvalidOperationException("Foundry:ProjectName is not configured.");
var resourceName = foundrySection["ResourceName"] ?? throw new InvalidOperationException("Foundry:ResourceName is not configured.");
var foundryAgentName = foundrySection["FoundryAgentName"] ?? throw new InvalidOperationException("Foundry:FoundryAgentName is not configured.");
var model = foundrySection["Model"] ?? throw new InvalidOperationException("Foundry:Model is not configured.");
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
