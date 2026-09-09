using System.Globalization;
using EList.Models.Orders;
using Newtonsoft.Json;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Сборка payload в формате notification ЮKassa (для stub и тестов).
    /// </summary>
    public static class YooKassaWebhookPayloadFactory
    {
        public static string BuildPaymentSucceeded(
            string providerPaymentId,
            Guid orderId,
            decimal amount,
            string currency = "RUB")
        {
            var notification = new YooKassaWebhookNotification
            {
                Type = "notification",
                Event = "payment.succeeded",
                Object = new YooKassaPaymentObject
                {
                    Id = providerPaymentId,
                    Status = "succeeded",
                    Paid = true,
                    Amount = new YooKassaAmount
                    {
                        Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                        Currency = string.IsNullOrWhiteSpace(currency) ? "RUB" : currency
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["orderId"] = orderId.ToString("D")
                    }
                }
            };

            return JsonConvert.SerializeObject(notification);
        }

        public static string BuildPaymentCanceled(
            string providerPaymentId,
            Guid orderId,
            decimal amount,
            string currency = "RUB")
        {
            var notification = new YooKassaWebhookNotification
            {
                Type = "notification",
                Event = "payment.canceled",
                Object = new YooKassaPaymentObject
                {
                    Id = providerPaymentId,
                    Status = "canceled",
                    Paid = false,
                    Amount = new YooKassaAmount
                    {
                        Value = amount.ToString("0.00", CultureInfo.InvariantCulture),
                        Currency = string.IsNullOrWhiteSpace(currency) ? "RUB" : currency
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["orderId"] = orderId.ToString("D")
                    }
                }
            };

            return JsonConvert.SerializeObject(notification);
        }
    }
}
