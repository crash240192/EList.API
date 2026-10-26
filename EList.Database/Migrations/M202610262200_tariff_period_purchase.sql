-- PeriodPurchase: покупка тарифа на период (без автопродления с баланса).
-- wallet_deposits расширяем под purchase/extend/upgrade; TopUp (kind=0) — только WalletAutoRenew.

ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS tariff_id uuid NULL REFERENCES public.tariffs (id);

ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS purchase_kind smallint NOT NULL DEFAULT 0;

ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS credit_applied numeric(18, 2) NOT NULL DEFAULT 0;

ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS period_start timestamptz NULL;

ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS period_end timestamptz NULL;

CREATE INDEX IF NOT EXISTS ix_wallet_deposits_wallet_tariff
	ON public.wallet_deposits (wallet_id, tariff_id)
	WHERE tariff_id IS NOT NULL;
