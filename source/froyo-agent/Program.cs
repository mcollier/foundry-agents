using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using froyo_recommendation_api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Extensions.AI;

// Creates an app host preconfigured for Foundry hosting environment.
var builder = AgentHost.CreateBuilder(args);

// Get the Foundry project endpoint from configuration or environment variable
var endpointValue = GetFoundryEndpointValue();
var projectEndpoint = new Uri(endpointValue);

// Get the model deployment name from configuration or environment variable
var deployment = GetModelDeploymentName();

// Get the appropriate TokenCredential based on the environment
TokenCredential credential = GetAuthenticationTokenCredential(builder);

string instructions = """
Your name is Froyo Fin. You're a frozen yogurt (froyo) recommendation agent for Froyo Foundry, perveyors of the finest frozen yogurt the world, catering the to tastes of
software engineers. You have deep knowledge of all things froyo, including flavors, toppings, and pairings. You are friendly, enthusiastic,
and always ready to help customers find their perfect froyo match. You can also provide fun facts about frozen yogurt and share
the latest promotions at Froyo Foundry. Your goal is to create a delightful and personalized froyo experience for every customer.

You cannot make up flavors. You can only recommend flavors that are in the Froyo Foundry flavor repository. If a user asks for a flavor that is not in the repository,
you should politely inform them that the flavor is not available and suggest other similar flavors from the repository.
""";

// Create an AI agent using the Foundry project endpoint, model deployment, and authentication credentials
// Note: Using AIProjectClient for Foundry Hosted Agent.
AIAgent froyoRecommendationAgent = new AIProjectClient(projectEndpoint, credential)
    .AsAIAgent(
        model: deployment,
        instructions: instructions,
        name: "froyo-recommendation-agent",
        description: "An AI agent that can answer questions about frozen yogurt.",
        tools: [
            AIFunctionFactory.Create(ListFlavorsTool.ListFlavors)
        ]
    );

// Registers the Azure AI Responses Server SDK and a specific AIAgent as the handler for all incoming requests
builder.Services.AddFoundryResponses(froyoRecommendationAgent);

// Register the "responses" protocol and map it to the Foundry responses endpoint
builder.RegisterProtocol("responses", endpoints => endpoints.MapFoundryResponses());

var app = builder.Build();

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
/// <param name="builder">The AgentHostBuilder instance.</param>
/// <returns>A TokenCredential instance for authentication.</returns>
static TokenCredential GetAuthenticationTokenCredential(AgentHostBuilder builder)
{
    TokenCredential credential;

    if (builder.WebApplicationBuilder.Environment.IsProduction() || builder.WebApplicationBuilder.Environment.IsStaging())
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
/// A <see cref="DelegatingHandler"/> that attaches a fresh Azure AD bearer token
/// to every outgoing HTTP request using the supplied <see cref="TokenCredential"/>.
/// </summary>
sealed class BearerTokenHandler(TokenCredential credential, params string[] scopes) : DelegatingHandler
{
    private readonly TokenRequestContext _context = new(scopes);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(_context, cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}