using EList.Models.Enums;

namespace EList.Models.Wallets
{
    /// <summary>Запрос на пополнение тарифного кошелька (stub/ЮKassa).</summary>
    public class CreateWalletDepositRequest
    {
        public Guid WalletId { get; set; }

        /// <summary>Сумма пополнения в рублях (&gt; 0).</summary>
        public decimal Amount { get; set; }

        public string? Currency { get; set; }

        public string? ReturnUrl { get; set; }

        public string? IdempotencyKey { get; set; }
    }
}
