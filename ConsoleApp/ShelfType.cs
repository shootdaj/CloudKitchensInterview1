using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConsoleApp
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum ShelfType
    {
        [EnumMember(Value = "hot")] Hot,
        [EnumMember(Value = "cold")] Cold,
        [EnumMember(Value = "frozen")] Frozen,
        [EnumMember(Value = "overflow")] Overflow
    }
}