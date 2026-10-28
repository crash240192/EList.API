-- Человекочитаемый OrderId для эквайринга (tariff:A7K9M2X3), lookup по webhook.
ALTER TABLE public.wallet_deposits
	ADD COLUMN IF NOT EXISTS provider_order_id varchar NULL;

CREATE UNIQUE INDEX IF NOT EXISTS wallet_deposits_provider_order_uidx
	ON public.wallet_deposits (provider_order_id)
	WHERE provider_order_id IS NOT NULL;
