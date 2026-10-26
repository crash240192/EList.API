-- PeriodPurchase: депозит с AmountDue=0 (полное покрытие credit при Extend/Upgrade)
-- должен сохраняться для аудита entitlement; CHECK amount > 0 это запрещал.

ALTER TABLE public.wallet_deposits
	DROP CONSTRAINT IF EXISTS wallet_deposits_amount_chk;

ALTER TABLE public.wallet_deposits
	ADD CONSTRAINT wallet_deposits_amount_chk CHECK (amount >= 0);
