using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;

using froyo_fulfillment_agent.Services;
using froyo_fulfillment_agent.Tools;

using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;

using OpenTelemetry.Metrics;


// Host the agent as a Foundry hosted agent using the Responses API.
// var builder = WebApplication.CreateBuilder(args);
var builder = AgentHost.CreateBuilder(args);

// AgentHost already registers an OpenTelemetry pipeline (Microsoft OpenTelemetry distro) for traces, metrics, and logs.
// It exports to Application Insights when APPLICATIONINSIGHTS_CONNECTION_STRING is set (injected by Foundry for hosted
// agents) or OTLP when OTEL_EXPORTER_OTLP_ENDPOINT is set. Plug the agent's source and meter into that pipeline
// instead of creating separate providers, which would export everything twice.

builder.ConfigureTracing(tracing =>
{
    tracing.AddSource(FroyoTools.TelemetrySourceName);
});

builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
{
    metrics.AddMeter(FroyoTools.TelemetrySourceName);
});


// Get the Foundry project endpoint from configuration or environment variable
var endpointValue = GetFoundryEndpointValue();
var projectEndpoint = new Uri(endpointValue);

// Get the model deployment name from configuration or environment variable
var deployment = GetModelDeploymentName();

var toolboxName = GetFoundryToolboxName();

// Get the appropriate TokenCredential based on the environment
TokenCredential credential = GetAuthenticationTokenCredential(builder.WebApplicationBuilder);

var instructions = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "agent-instructions.md"));

// Stock and pricing tools. The menu and allergen knowledge base comes from the Foundry toolbox below.
// The agent is built before the container, so the repository is created here and passed to the tools.
// Swap InMemoryInventoryRepository for a database-backed IInventoryRepository later.
IInventoryRepository inventory = new InMemoryInventoryRepository();
var froyoTools = new FroyoTools(inventory);

// Create the agent via the AI project client using the Responses API.
// Spread the custom froyo function tools into the agent's tool collection.
// Only the agent is instrumented (not the chat client) to avoid duplicating prompts/responses across spans.
// Sensitive data (prompts, responses, tool arguments/results) is intentionally recorded in all environments.
AIAgent agent = new AIProjectClient(projectEndpoint, credential)
    .AsAIAgent(
        model: deployment,
        instructions: instructions,
        name: "Froyo Fulfillment Agent",
        description: "This agent handles froyo fulfillment requests.",
        tools: [
            .. froyoTools.AsAIFunctions()
        ]
    )
    .AsBuilder()
    .UseOpenTelemetry(sourceName: FroyoTools.TelemetrySourceName, configure: cfg => cfg.EnableSensitiveData = true)
    .Build();

builder.Services.AddFoundryResponses(agent);

// Register Foundry Toolbox: connects to the MCP proxy at startup and makes tools available.
// The toolbox name must match a toolbox registered in your Foundry project.
// When FOUNDRY_PROJECT_ENDPOINT is absent (e.g., in local development without Foundry
// infrastructure), startup succeeds without error and no toolbox tools are loaded.
builder.Services.AddFoundryToolboxes(credential, toolboxName);

var app = builder.Build();
app.App.MapFoundryResponses();
app.Run();

/// <summary>
/// Get the Foundry project endpoint from configuration or environment variable.
/// </summary>
/// <returns>The Foundry project endpoint as a string.</returns>
string GetFoundryEndpointValue()
{
    var endpoint = builder.Configuration["Foundry:ProjectEndpoint"];
    if (string.IsNullOrWhiteSpace(endpoint))
    {
        endpoint = builder.Configuration["FOUNDRY_PROJECT_ENDPOINT"];
    }

    if (string.IsNullOrWhiteSpace(endpoint))
    {
        throw new InvalidOperationException(
            "Foundry:ProjectEndpoint is not set. Configure Foundry:ProjectEndpoint or FOUNDRY_PROJECT_ENDPOINT.");
    }

    return endpoint;
}

/// <summary>
/// Get the model deployment name from configuration or environment variable.
/// </summary>
/// <returns>The model deployment name.</returns>
string GetModelDeploymentName()
{
    var deploymentName = builder.Configuration["Foundry:ModelDeploymentName"];
    if (string.IsNullOrWhiteSpace(deploymentName))
    {
        deploymentName = builder.Configuration["AZURE_AI_MODEL_DEPLOYMENT_NAME"];
        Console.WriteLine("Foundry:ModelDeploymentName is not set. Using AZURE_AI_MODEL_DEPLOYMENT_NAME: " + deploymentName);
    }

    if (string.IsNullOrWhiteSpace(deploymentName))
    {
        deploymentName = "gpt-4o";
        Console.WriteLine("Foundry:ModelDeploymentName is not set. Using default deployment name: " + deploymentName);
    }

    return deploymentName;
}

/// <summary>
/// Get the appropriate TokenCredential based on the environment.
/// </summary>
/// <param name="builder">The WebApplicationBuilder instance.</param>
/// <returns>A TokenCredential instance for authentication.</returns>
static TokenCredential GetAuthenticationTokenCredential(WebApplicationBuilder builder)
{
    TokenCredential credential;

    if (builder.Environment.IsProduction() || builder.Environment.IsStaging())
    {
        credential = new ManagedIdentityCredential(ManagedIdentityId.SystemAssigned);
    }
    else
    {
        credential = new ChainedTokenCredential(
            new VisualStudioCredential(),
            new VisualStudioCodeCredential(),
            new AzureCliCredential()
        );
    }

    return credential;
}

/// <summary>
/// Get the Foundry toolbox name from configuration or environment variable.
/// </summary>
/// <returns>The Foundry toolbox name as a string.</returns>
string GetFoundryToolboxName()
{
    var toolboxName = builder.Configuration["Foundry:ToolboxName"];
    if (string.IsNullOrWhiteSpace(toolboxName))
    {
        toolboxName = builder.Configuration["FOUNDRY_TOOLBOX_NAME"];
    }

    if (string.IsNullOrWhiteSpace(toolboxName))
    {
        throw new InvalidOperationException(
            "Foundry:ToolboxName is not set. Configure Foundry:ToolboxName or FOUNDRY_TOOLBOX_NAME.");
    }

    return toolboxName;
}