using System.Text.Json.Serialization;

namespace TwitchMemeAlertsAuto.Core
{
	[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(Supporters))]
	[JsonSerializable(typeof(Events))]
	[JsonSerializable(typeof(Current))]
	public partial class SerializationModeOptionsContext : JsonSerializerContext
	{
	}
}