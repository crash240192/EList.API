-- Ticket check-in: кто и когда отметил присутствие
-- Safe to re-run (IF NOT EXISTS).

ALTER TABLE public.tickets
	ADD COLUMN IF NOT EXISTS checked_in_at timestamptz NULL;

ALTER TABLE public.tickets
	ADD COLUMN IF NOT EXISTS checked_in_by_account_id uuid NULL;

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'tickets_checked_in_by_account_fk'
	) THEN
		ALTER TABLE public.tickets
			ADD CONSTRAINT tickets_checked_in_by_account_fk
			FOREIGN KEY (checked_in_by_account_id) REFERENCES public.accounts(id);
	END IF;
END $$;

CREATE INDEX IF NOT EXISTS tickets_event_status_idx ON public.tickets (event_id, status);
CREATE INDEX IF NOT EXISTS tickets_checked_in_by_account_id_idx
	ON public.tickets (checked_in_by_account_id)
	WHERE checked_in_by_account_id IS NOT NULL;
