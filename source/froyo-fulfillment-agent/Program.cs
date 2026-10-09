using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI.Foundry.Hosting;
using Microsoft.Agents.AI;
using Azure.AI.Projects;
using Microsoft.Extensions.AI;
using froyo_fulfillment_agent.Services;
using froyo_fulfillment_agent.Tools;

// Host the agent as a Foundry hosted agent using the Responses API.
var builder = WebApplication.CreateBuilder(args);

// Get the Foundry project endpoint from configuration or environment variable
var endpointValue = GetFoundryEndpointValue();
var projectEndpoint = new Uri(endpointValue);

// Get the model deployment name from configuration or environment variable
var deployment = GetModelDeploymentName();

// Get the appropriate TokenCredential based on the environment
TokenCredential credential = GetAuthenticationTokenCredential(builder);

var instructions = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "agent-instructions.md"));

// Hardcoded inventory for now; swap InMemoryInventoryRepository for a database-backed IInventoryRepository later.
var inventoryTools = new InventoryTools(new InventoryService(new InMemoryInventoryRepository()));

// Create the agent via the AI project client using the Responses API.
AIAgent agent = new AIProjectClient(projectEndpoint, credential)
    .AsAIAgent(
        model: deployment,
        instructions: instructions,
        name: "Froyo Fulfillment Agent",
        description: "This agent handles froyo fulfillment requests.",
        tools: [
            .. inventoryTools.AsAIFunctions()
        ]
    );

builder.Services.AddFoundryResponses(agent);

var app = builder.Build();
app.MapFoundryResponses();
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

// var builder = WebApplication.CreateBuilder(args);

// // Add services to the container.
// // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

// var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

// app.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
