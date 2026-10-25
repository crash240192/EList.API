using EList.Models.Enums;
using EList.Models.Organizations;

namespace EList.Services.Impl.Payments
{
    /// <summary>
    /// Единые правила готовности org к продаже билетов / CreateOrder.
    /// ShopCode (ProviderSellerId) нужен только для T-Bank marketplace split (Shops[]);
    /// stub и бесплатные билеты — без ShopCode.
    /// </summary>
    public static class SellerPayoutGate
    {
        public sealed class Readiness
        {
            public bool Ok { get; init; }
            public string? ErrorMessage { get; init; }
            /// <summary>ShopCode для Init.Shops, или null = Init без сплита.</summary>
            public string? ResolvedShopCode { get; init; }
            public bool UsesMarketplaceSplit => !string.IsNullOrWhiteSpace(ResolvedShopCode);
        }

        /// <summary>
        /// Готовность к платным заказам (amount &gt; 0).
        /// </summary>
        public static Readiness AssessPaidTicketSales(OrganizationPayout? payout)
        {
            var settings = PaymentSettings.Load();

            if (settings.IsYooKassaStubProvider())
            {
                // Тестовый stub: Active в payout достаточно; ShopCode не обязателен
                // (YooKassaStubPaymentProvider не использует Shops).
                if (payout != null && payout.OnboardingStatus == ProviderOnboardingStatus.Active)
                {
                    return new Readiness
                    {
                        Ok = true,
                        ResolvedShopCode = NullIfEmpty(payout.ProviderSellerId)
                    };
                }

                return new Readiness
                {
                    Ok = false,
                    ErrorMessage =
                        "Для тестового провайдера (yookassaStub) завершите онбординг продавца " +
                        "(OnboardingStatus=Active). ShopCode не обязателен. " +
                        "Либо вызовите API онбординга org, либо поставьте Active в payout."
                };
            }

            // T-Bank: ShopCode = org.ProviderSellerId ?? manualShopCode.
            // Без кода Init идёт без Shops[] (вся сумма на терминал площадки) —
            // допустимо в test/DEMO, если allowTicketsWithoutShopCode=true.
            var shop = ResolveShopCode(payout, settings.TBank);
            if (!string.IsNullOrWhiteSpace(shop))
            {
                if (payout != null
                    && payout.OnboardingStatus != ProviderOnboardingStatus.Active
                    && string.IsNullOrWhiteSpace(settings.TBank.ManualShopCode))
                {
                    // Есть SellerId, но статус не Active — подозрительно
                    return new Readiness
                    {
                        Ok = false,
                        ErrorMessage =
                            "Онбординг продавца в Т-Банке не в статусе Active. " +
                            "Завершите онбординг точки (acqapi) или задайте payments:tbank:manualShopCode для DEMO."
                    };
                }

                return new Readiness { Ok = true, ResolvedShopCode = shop };
            }

            if (settings.TBank.AllowTicketsWithoutShopCode)
            {
                return new Readiness
                {
                    Ok = true,
                    ResolvedShopCode = null
                };
            }

            return new Readiness
            {
                Ok = false,
                ErrorMessage =
                    "Нет ShopCode продавца (organization_payout.provider_seller_id) и не задан " +
                    "payments:tbank:manualShopCode. Для теста: " +
                    "1) payments:provider=yookassaStub, или " +
                    "2) payments:tbank:manualShopCode=…, или " +
                    "3) payments:tbank:allowTicketsWithoutShopCode=true (Init без Shops)."
            };
        }

        /// <summary>
        /// Перед CanSellTickets / ticketsEnabled на событии — та же семантика, что у платных заказов.
        /// </summary>
        public static Readiness AssessCanEnableTicketSales(OrganizationPayout? payout)
            => AssessPaidTicketSales(payout);

        public static string? ResolveShopCode(OrganizationPayout? payout, TBankPaymentSettings? tbank = null)
        {
            tbank ??= PaymentSettings.Load().TBank;
            if (payout != null && !string.IsNullOrWhiteSpace(payout.ProviderSellerId))
                return payout.ProviderSellerId.Trim();
            return NullIfEmpty(tbank.ManualShopCode);
        }

        private static string? NullIfEmpty(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
