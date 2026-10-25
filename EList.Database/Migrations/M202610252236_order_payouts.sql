-- Журнал A2C-выплат по сделкам Мультирасчётов (P3)

DO $CREATE_ORDER_PAYOUT_KIND$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_type t
		JOIN pg_namespace n ON n.oid = t.typnamespace
		WHERE n.nspname = 'public' AND t.typname = 'order_payout_kind'
	) THEN
		CREATE TYPE public.order_payout_kind AS ENUM ('seller', 'platform_commission');
	END IF;
END
$CREATE_ORDER_PAYOUT_KIND$;

DO $CREATE_ORDER_PAYOUT_STATUS$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_type t
		JOIN pg_namespace n ON n.oid = t.typnamespace
		WHERE n.nspname = 'public' AND t.typname = 'order_payout_status'
	) THEN
		CREATE TYPE public.order_payout_status AS ENUM (
			'pending', 'initiated', 'succeeded', 'failed', 'skipped'
		);
	END IF;
END
$CREATE_ORDER_PAYOUT_STATUS$;

CREATE TABLE IF NOT EXISTS public.order_payouts (
	id uuid PRIMARY KEY,
	order_id uuid NOT NULL REFERENCES public.orders (id),
	kind public.order_payout_kind NOT NULL,
	amount numeric(18, 2) NOT NULL,
	currency text NOT NULL DEFAULT 'RUB',
	provider public.payment_provider NOT NULL,
	provider_deal_id text NULL,
	partner_id text NULL,
	provider_payment_id text NULL,
	provider_order_id text NULL,
	final_payout boolean NOT NULL DEFAULT false,
	status public.order_payout_status NOT NULL DEFAULT 'pending',
	error_message text NULL,
	create_date timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	completed_at timestamptz NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_order_payouts_order_kind
	ON public.order_payouts (order_id, kind);

CREATE INDEX IF NOT EXISTS ix_order_payouts_status
	ON public.order_payouts (status);

CREATE INDEX IF NOT EXISTS ix_order_payouts_provider_payment_id
	ON public.order_payouts (provider_payment_id)
	WHERE provider_payment_id IS NOT NULL;
