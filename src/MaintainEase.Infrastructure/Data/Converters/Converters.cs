// Updated Converters/CustomValueConverters.cs

using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MaintainEase.Core.Domain.ValueObjects;
using MaintainEase.Core.Domain.IsraeliMarket.ValueObjects;

namespace MaintainEase.Infrastructure.Data.Converters
{
    public class MoneyConverter : ValueConverter<Money, string>
    {
        public MoneyConverter()
            : base(
                v => MoneyToString(v),
                v => StringToMoney(v))
        {
        }

        private static string MoneyToString(Money money)
        {
            return $"{money.Amount}|{money.Currency}";
        }

        private static Money StringToMoney(string value)
        {
            var parts = value.Split('|');
            return new Money(decimal.Parse(parts[0]), parts[1]);
        }
    }

    public class JsonValueConverter<T> : ValueConverter<T, string> where T : class
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public JsonValueConverter()
            : base(
                v => SerializeToJson(v),
                v => DeserializeFromJson(v))
        {
        }

        private static string SerializeToJson(T value)
        {
            return JsonSerializer.Serialize(value, Options);
        }

        private static T DeserializeFromJson(string json)
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
    }

    public class AddressConverter : JsonValueConverter<Address>
    {
        public AddressConverter() : base() { }
    }

    public class IdentificationConverter : JsonValueConverter<Identification>
    {
        public IdentificationConverter() : base() { }
    }

    public class TabuExtractConverter : JsonValueConverter<TabuExtract>
    {
        public TabuExtractConverter() : base() { }
    }

    public class ArnonaZoneConverter : JsonValueConverter<ArnonaZone>
    {
        public ArnonaZoneConverter() : base() { }
    }
}
