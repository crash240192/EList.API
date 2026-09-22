-- Store wallet balance snapshot after successful deposit (for operations history).
ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS balance_after numeric NULL;
