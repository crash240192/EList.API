-- PeriodPurchase: отмена тарифа с пропорциональным возвратом за неиспользованные дни.

CREATE TABLE IF NOT EXISTS public.wallet_tariff_refunds (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	wallet_id uuid NOT NULL,
	deposit_id uuid NOT NULL,
	amount numeric(18, 2) NOT NULL,
	currency varchar(8) NOT NULL DEFAULT 'RUB',
	status public.refund_status NOT NULL DEFAULT 'pending',
	reason varchar NULL,
	provider_refund_id varchar NULL,
	idempotency_key varchar NULL,
	remaining_days numeric(12, 4) NOT NULL DEFAULT 0,
	period_days int NOT NULL DEFAULT 0,
	create_date timestamptz NOT NULL DEFAULT now(),
	succeeded_at timestamptz NULL,
	CONSTRAINT wallet_tariff_refunds_pk PRIMARY KEY (id),
	CONSTRAINT wallet_tariff_refunds_wallet_fk FOREIGN KEY (wallet_id) REFERENCES public.wallets (id),
	CONSTRAINT wallet_tariff_refunds_deposit_fk FOREIGN KEY (deposit_id) REFERENCES public.wallet_deposits (id),
	CONSTRAINT wallet_tariff_refunds_amount_chk CHECK (amount >= 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_wallet_tariff_refunds_idempotency
	ON public.wallet_tariff_refunds (wallet_id, idempotency_key)
	WHERE idempotency_key IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_wallet_tariff_refunds_wallet
	ON public.wallet_tariff_refunds (wallet_id, create_date DESC);

CREATE INDEX IF NOT EXISTS ix_wallet_tariff_refunds_deposit
	ON public.wallet_tariff_refunds (deposit_id);

CREATE INDEX IF NOT EXISTS ix_wallet_tariff_refunds_provider
	ON public.wallet_tariff_refunds (provider_refund_id)
	WHERE provider_refund_id IS NOT NULL;
