-- Tariff billing: explicit next_charge_at + charge ledger (no negative balance).
ALTER TABLE public.wallets
	ADD COLUMN IF NOT EXISTS next_charge_at timestamptz NULL;

-- Backfill: if last charge known, approximate next = last_charge + tariff.period
UPDATE public.wallets w
SET next_charge_at = w.last_charge_date + t.period
FROM public.tariffs t
WHERE w.tariff_id = t.id
  AND w.last_charge_date IS NOT NULL
  AND w.next_charge_at IS NULL;

CREATE TABLE IF NOT EXISTS public.wallet_tariff_charges (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	wallet_id uuid NOT NULL,
	tariff_id uuid NOT NULL,
	amount numeric(12, 2) NOT NULL,
	currency char(3) NOT NULL DEFAULT 'RUB',
	charged_at timestamptz NOT NULL DEFAULT now(),
	next_charge_at timestamptz NOT NULL,
	balance_after numeric NOT NULL,
	CONSTRAINT wallet_tariff_charges_pk PRIMARY KEY (id),
	CONSTRAINT wallet_tariff_charges_wallet_fk FOREIGN KEY (wallet_id) REFERENCES public.wallets(id),
	CONSTRAINT wallet_tariff_charges_tariff_fk FOREIGN KEY (tariff_id) REFERENCES public.tariffs(id),
	CONSTRAINT wallet_tariff_charges_amount_chk CHECK (amount >= 0)
);

CREATE INDEX IF NOT EXISTS wallet_tariff_charges_wallet_id_idx
	ON public.wallet_tariff_charges (wallet_id);

CREATE INDEX IF NOT EXISTS wallets_next_charge_at_idx
	ON public.wallets (next_charge_at)
	WHERE tariff_id IS NOT NULL;
