using System.Collections.Concurrent;
using EList.Common.Configuration;
using EList.Models.Enums;
using EList.Services.Interfaces;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// In-memory stub ЮKassa для локальных экспериментов.
    /// Переключение на реальную ЮKassa — смена регистрации IPaymentProvider в DI.
    /// </summary>
    public class YooKassaStubPaymentProvider : IPaymentProvider
    {
        private readonly ConcurrentDictionary<string, StubPayment> _payments = new();

        public PaymentProvider Kind => PaymentProvider.Yookassa;

        public bool SupportsManualComplete => true;

        public Task<PaymentCreationResult> CreatePaymentAsync(PaymentCreationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Amount < 0)
                throw new ArgumentOutOfRangeException(nameof(request.Amount));

            var providerPaymentId = $"stub_{Guid.NewGuid():N}";
            var returnUrl = ResolveReturnUrl(request.ReturnUrl);
            var confirmationUrl =
                $"{returnUrl}?orderId={request.OrderId:D}&paymentId={providerPaymentId}&stub=1";

            var payment = new StubPayment
            {
                ProviderPaymentId = providerPaymentId,
                OrderId = request.OrderId,
                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "RUB" : request.Currency,
                Status = PaymentProviderStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _payments[providerPaymentId] = payment;

            // Бесплатные заказы не должны сюда попадать; на всякий случай сразу Succeeded.
            if (request.Amount == 0)
            {
                payment.Status = PaymentProviderStatus.Succeeded;
                payment.PaidAt = DateTimeOffset.UtcNow;
            }

            return Task.FromResult(new PaymentCreationResult
            {
                ProviderPaymentId = providerPaymentId,
                ConfirmationUrl = confirmationUrl,
                Status = payment.Status
            });
        }

        public Task<PaymentStatusInfo> GetStatusAsync(string providerPaymentId)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId)
                || !_payments.TryGetValue(providerPaymentId, out var payment))
            {
                return Task.FromResult(new PaymentStatusInfo
                {
                    ProviderPaymentId = providerPaymentId,
                    Status = PaymentProviderStatus.Failed
                });
            }

            return Task.FromResult(new PaymentStatusInfo
            {
                ProviderPaymentId = payment.ProviderPaymentId,
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency
            });
        }

        public Task CompleteManuallyAsync(string providerPaymentId)
        {
            if (string.IsNullOrWhiteSpace(providerPaymentId))
                throw new ArgumentException("providerPaymentId is required", nameof(providerPaymentId));

            // После рестарта процесса in-memory запись может пропасть — создаём Succeeded на лету.
            _payments.AddOrUpdate(
                providerPaymentId,
                _ => new StubPayment
                {
                    ProviderPaymentId = providerPaymentId,
                    Status = PaymentProviderStatus.Succeeded,
                    CreatedAt = DateTimeOffset.UtcNow,
                    PaidAt = DateTimeOffset.UtcNow,
                    Currency = "RUB"
                },
                (_, existing) =>
                {
                    existing.Status = PaymentProviderStatus.Succeeded;
                    existing.PaidAt = DateTimeOffset.UtcNow;
                    return existing;
                });

            return Task.CompletedTask;
        }

        private static string ResolveReturnUrl(string? requestReturnUrl)
        {
            if (!string.IsNullOrWhiteSpace(requestReturnUrl))
                return requestReturnUrl.Trim();

            if (ConfigurationManager.AppSettings.Contains("payments:returnUrl")
                && !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["payments:returnUrl"]))
            {
                return ConfigurationManager.AppSettings["payments:returnUrl"];
            }

            return "https://localhost/payments/return";
        }

        private sealed class StubPayment
        {
            public string ProviderPaymentId { get; set; }
            public Guid OrderId { get; set; }
            public decimal Amount { get; set; }
            public string Currency { get; set; }
            public PaymentProviderStatus Status { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? PaidAt { get; set; }
        }
    }
}
