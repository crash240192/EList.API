using Newtonsoft.Json;

namespace EList.Models.Orders
{
    /// <summary>
    /// Упрощённая форма notification ЮKassa (payment.*).
    /// Stub шлёт тот же JSON для отладки.
    /// </summary>
    public class YooKassaWebhookNotification
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "notification";

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("object")]
        public YooKassaPaymentObject Object { get; set; }
    }

    public class YooKassaPaymentObject
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("paid")]
        public bool? Paid { get; set; }

        [JsonProperty("amount")]
        public YooKassaAmount Amount { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, string> Metadata { get; set; }
    }

    public class YooKassaAmount
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; } = "RUB";
    }
}
