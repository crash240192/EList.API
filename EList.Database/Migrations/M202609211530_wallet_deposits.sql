-- Tariff-wallet top-up payments (separate from ticket orders / YooKassa split).
DO $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'wallet_deposit_status')
	THEN
		CREATE TYPE public.wallet_deposit_status AS ENUM (
			'pending', 'succeeded', 'canceled', 'failed');
	END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.wallet_deposits (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	wallet_id uuid NOT NULL,
	amount numeric(12, 2) NOT NULL,
	currency char(3) NOT NULL DEFAULT 'RUB',
	status public.wallet_deposit_status NOT NULL DEFAULT 'pending',
	provider public.payment_provider NULL,
	provider_payment_id varchar NULL,
	idempotency_key varchar NULL,
	create_date timestamptz NOT NULL DEFAULT now(),
	paid_at timestamptz NULL,
	CONSTRAINT wallet_deposits_pk PRIMARY KEY (id),
	CONSTRAINT wallet_deposits_wallet_fk FOREIGN KEY (wallet_id) REFERENCES public.wallets(id),
	CONSTRAINT wallet_deposits_amount_chk CHECK (amount > 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS wallet_deposits_idempotency_uidx
	ON public.wallet_deposits (wallet_id, idempotency_key)
	WHERE idempotency_key IS NOT NULL;

CREATE INDEX IF NOT EXISTS wallet_deposits_wallet_id_idx
	ON public.wallet_deposits (wallet_id);

CREATE UNIQUE INDEX IF NOT EXISTS wallet_deposits_provider_payment_uidx
	ON public.wallet_deposits (provider, provider_payment_id)
	WHERE provider IS NOT NULL AND provider_payment_id IS NOT NULL;
