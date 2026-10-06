using Newtonsoft.Json;

namespace EList.Services.Impl.Payments.TBank
{
    public class TBankShopDto
    {
        [JsonProperty("ShopCode")]
        public string ShopCode { get; set; } = string.Empty;

        [JsonProperty("Amount")]
        public long Amount { get; set; }

        [JsonProperty("Name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        /// <summary>Комиссия площадки в копейках (строка — как в API Т-Банка).</summary>
        [JsonProperty("Fee", NullValueHandling = NullValueHandling.Ignore)]
        public string? Fee { get; set; }
    }

    public class TBankInitRequest
    {
        [JsonProperty("TerminalKey")]
        public string TerminalKey { get; set; } = string.Empty;

        [JsonProperty("Amount")]
        public long Amount { get; set; }

        [JsonProperty("OrderId")]
        public string OrderId { get; set; } = string.Empty;

        [JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("SuccessURL", NullValueHandling = NullValueHandling.Ignore)]
        public string? SuccessURL { get; set; }

        [JsonProperty("FailURL", NullValueHandling = NullValueHandling.Ignore)]
        public string? FailURL { get; set; }

        [JsonProperty("NotificationURL", NullValueHandling = NullValueHandling.Ignore)]
        public string? NotificationURL { get; set; }

        [JsonProperty("PayType", NullValueHandling = NullValueHandling.Ignore)]
        public string? PayType { get; set; }

        [JsonProperty("Shops", NullValueHandling = NullValueHandling.Ignore)]
        public List<TBankShopDto>? Shops { get; set; }

        [JsonProperty("Token")]
        public string Token { get; set; } = string.Empty;
    }

    public class TBankPaymentIdRequest
    {
        [JsonProperty("TerminalKey")]
        public string TerminalKey { get; set; } = string.Empty;

        [JsonProperty("PaymentId")]
        public string PaymentId { get; set; } = string.Empty;

        [JsonProperty("Amount", NullValueHandling = NullValueHandling.Ignore)]
        public long? Amount { get; set; }

        [JsonProperty("Token")]
        public string Token { get; set; } = string.Empty;
    }

    public class TBankApiResponse
    {
        [JsonProperty("Success")]
        public bool Success { get; set; }

        [JsonProperty("ErrorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("Message")]
        public string? Message { get; set; }

        [JsonProperty("Details")]
        public string? Details { get; set; }

        [JsonProperty("TerminalKey")]
        public string? TerminalKey { get; set; }

        [JsonProperty("Status")]
        public string? Status { get; set; }

        [JsonProperty("PaymentId")]
        public string? PaymentId { get; set; }

        [JsonProperty("OrderId")]
        public string? OrderId { get; set; }

        [JsonProperty("Amount")]
        public long? Amount { get; set; }

        [JsonProperty("PaymentURL")]
        public string? PaymentURL { get; set; }
    }

    public class TBankWebhookNotification
    {
        [JsonProperty("TerminalKey")]
        public string? TerminalKey { get; set; }

        [JsonProperty("OrderId")]
        public string? OrderId { get; set; }

        [JsonProperty("Success")]
        public bool? Success { get; set; }

        [JsonProperty("Status")]
        public string? Status { get; set; }

        [JsonProperty("PaymentId")]
        public string? PaymentId { get; set; }

        [JsonProperty("ErrorCode")]
        public string? ErrorCode { get; set; }

        [JsonProperty("Amount")]
        public long? Amount { get; set; }

        [JsonProperty("Token")]
        public string? Token { get; set; }
    }

    public class TBankSmRegisterAuthResponse
    {
        [JsonProperty("token")]
        public string? Token { get; set; }

        [JsonProperty("Token")]
        public string? TokenPascal { get; set; }

        [JsonProperty("success")]
        public bool? Success { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        public string? ResolveToken() => !string.IsNullOrWhiteSpace(Token) ? Token : TokenPascal;
    }

    public class TBankSmRegisterShopResponse
    {
        [JsonProperty("shopCode")]
        public string? ShopCode { get; set; }

        [JsonProperty("ShopCode")]
        public string? ShopCodePascal { get; set; }

        [JsonProperty("code")]
        public string? Code { get; set; }

        [JsonProperty("success")]
        public bool? Success { get; set; }

        [JsonProperty("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("status")]
        public string? Status { get; set; }

        public string? ResolveShopCode()
        {
            if (!string.IsNullOrWhiteSpace(ShopCode))
                return ShopCode;
            if (!string.IsNullOrWhiteSpace(ShopCodePascal))
                return ShopCodePascal;
            return Code;
        }
    }
}
