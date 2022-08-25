using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ConsoleApp
{
    internal class Order
    {
        public Order(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
        public string Name { get; set; }

        [JsonProperty("temp")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ShelfType ShelfType { get; set; }

        public int ShelfLife { get; set; }
        public decimal DecayRate { get; set; }
    }
}