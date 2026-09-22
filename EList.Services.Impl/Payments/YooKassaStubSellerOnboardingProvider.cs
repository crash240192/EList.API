using System.Collections.Concurrent;
using EList.Models.Enums;
using EList.Services.Interfaces;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// In-memory stub онбординга продавца в ЮKassa.
    /// Переключение на реальную ЮKassa — смена регистрации ISellerOnboardingProvider в DI.
    /// </summary>
    public class YooKassaStubSellerOnboardingProvider : ISellerOnboardingProvider
    {
        private readonly ConcurrentDictionary<string, StubSeller> _sellers = new();

        public PaymentProvider Kind => PaymentProvider.Yookassa;

        public bool CompletesImmediately => true;

        public Task<SellerOnboardingStartResult> StartAsync(SellerOnboardingRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.OrganizationId == Guid.Empty)
                throw new ArgumentException("OrganizationId is required", nameof(request));

            var providerSellerId = $"stub_seller_{request.OrganizationId:N}";
            var seller = new StubSeller
            {
                ProviderSellerId = providerSellerId,
                OrganizationId = request.OrganizationId,
                Status = ProviderOnboardingStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _sellers[providerSellerId] = seller;

            return Task.FromResult(new SellerOnboardingStartResult
            {
                ProviderSellerId = providerSellerId,
                ConfirmationUrl = null,
                Status = ProviderOnboardingStatus.Active
            });
        }

        public Task<SellerOnboardingStatusInfo> GetStatusAsync(string providerSellerId)
        {
            if (string.IsNullOrWhiteSpace(providerSellerId)
                || !_sellers.TryGetValue(providerSellerId, out var seller))
            {
                return Task.FromResult(new SellerOnboardingStatusInfo
                {
                    ProviderSellerId = providerSellerId,
                    Status = ProviderOnboardingStatus.None
                });
            }

            return Task.FromResult(new SellerOnboardingStatusInfo
            {
                ProviderSellerId = seller.ProviderSellerId,
                Status = seller.Status
            });
        }

        private sealed class StubSeller
        {
            public string ProviderSellerId { get; set; }
            public Guid OrganizationId { get; set; }
            public ProviderOnboardingStatus Status { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }
    }
}
