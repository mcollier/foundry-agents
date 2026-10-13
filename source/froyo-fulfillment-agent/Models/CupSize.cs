using System.Text.Json.Serialization;

namespace froyo_fulfillment_agent.Models;

/// <summary>
/// Froyo Foundry serving sizes ("storage tiers").
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CupSize>))]
public enum CupSize
{
    Byte,
    Kilobyte,
    Megabyte,
    Terabyte,
}
