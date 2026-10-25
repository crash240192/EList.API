-- Метка закрытия сделки Мультирасчётов (FinalPayout или closeSpDeal)

ALTER TABLE public.orders
	ADD COLUMN IF NOT EXISTS provider_deal_closed_at timestamptz NULL;

CREATE INDEX IF NOT EXISTS ix_orders_deal_open
	ON public.orders (paid_at)
	WHERE provider_deal_id IS NOT NULL
	  AND provider_deal_closed_at IS NULL
	  AND status = 'paid'::public.order_status;
