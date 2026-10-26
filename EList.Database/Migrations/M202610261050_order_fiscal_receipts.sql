-- Журнал фискальных чеков CloudKassir (W4)

DO $CREATE_FISCAL_RECEIPT_KIND$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_type t
		JOIN pg_namespace n ON n.oid = t.typnamespace
		WHERE n.nspname = 'public' AND t.typname = 'fiscal_receipt_kind'
	) THEN
		CREATE TYPE public.fiscal_receipt_kind AS ENUM ('income', 'income_return');
	END IF;
END
$CREATE_FISCAL_RECEIPT_KIND$;

DO $CREATE_FISCAL_RECEIPT_STATUS$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_type t
		JOIN pg_namespace n ON n.oid = t.typnamespace
		WHERE n.nspname = 'public' AND t.typname = 'fiscal_receipt_status'
	) THEN
		CREATE TYPE public.fiscal_receipt_status AS ENUM (
			'pending', 'queued', 'processed', 'failed', 'skipped'
		);
	END IF;
END
$CREATE_FISCAL_RECEIPT_STATUS$;

CREATE TABLE IF NOT EXISTS public.order_fiscal_receipts (
	id uuid PRIMARY KEY,
	order_id uuid NOT NULL REFERENCES public.orders (id),
	refund_id uuid NULL REFERENCES public.refunds (id),
	kind public.fiscal_receipt_kind NOT NULL,
	provider text NOT NULL DEFAULT 'cloudKassir',
	invoice_id text NOT NULL,
	provider_receipt_id text NULL,
	status public.fiscal_receipt_status NOT NULL DEFAULT 'pending',
	receipt_url text NULL,
	qr_code_url text NULL,
	amount numeric(18, 2) NOT NULL,
	currency text NOT NULL DEFAULT 'RUB',
	error_message text NULL,
	create_date timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	completed_at timestamptz NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_order_fiscal_receipts_invoice
	ON public.order_fiscal_receipts (invoice_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_order_fiscal_receipts_order_income
	ON public.order_fiscal_receipts (order_id)
	WHERE kind = 'income';

CREATE UNIQUE INDEX IF NOT EXISTS ux_order_fiscal_receipts_refund_return
	ON public.order_fiscal_receipts (refund_id)
	WHERE kind = 'income_return' AND refund_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_order_fiscal_receipts_provider_receipt_id
	ON public.order_fiscal_receipts (provider_receipt_id)
	WHERE provider_receipt_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_order_fiscal_receipts_status
	ON public.order_fiscal_receipts (status);
