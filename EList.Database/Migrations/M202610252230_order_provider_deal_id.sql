-- Мультирасчёты Deal NN: идентификатор сделки Т‑Банка (SpAccumulationId / DealId)

ALTER TABLE public.orders
	ADD COLUMN IF NOT EXISTS provider_deal_id text NULL;

CREATE INDEX IF NOT EXISTS ix_orders_provider_deal_id
	ON public.orders (provider_deal_id)
	WHERE provider_deal_id IS NOT NULL;
