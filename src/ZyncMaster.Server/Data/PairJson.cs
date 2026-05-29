using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ZyncMaster.Server.Data;

// Serializes the domain Endpoint / MirrorResult records to camelCase JSON for storage
// in the SyncPairs row columns. Kept central so the on-disk shape is stable and a value
// converter (or a later migration) reads back exactly what was written.
internal static class PairJson
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
    };

    public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, Settings);

    public static T Deserialize<T>(string json) =>
        JsonConvert.DeserializeObject<T>(json, Settings)
            ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name} from stored JSON.");
}
