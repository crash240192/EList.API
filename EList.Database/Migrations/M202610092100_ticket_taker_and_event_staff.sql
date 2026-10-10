-- W6a: роль билетёра + назначение staff на событие

DO $$
BEGIN
	IF EXISTS (SELECT 1 FROM pg_type WHERE typname = 'organization_member_role')
	   AND NOT EXISTS (
		SELECT 1
		FROM pg_enum e
		JOIN pg_type t ON t.oid = e.enumtypid
		WHERE t.typname = 'organization_member_role' AND e.enumlabel = 'ticket_taker'
	   )
	THEN
		ALTER TYPE public.organization_member_role ADD VALUE 'ticket_taker';
	END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.event_ticket_staff (
	id uuid NOT NULL DEFAULT uuid_generate_v4(),
	event_id uuid NOT NULL,
	account_id uuid NOT NULL,
	can_check_in boolean NOT NULL DEFAULT true,
	can_view_stats boolean NOT NULL DEFAULT true,
	create_date timestamptz NOT NULL DEFAULT now(),
	update_date timestamptz NULL,
	CONSTRAINT event_ticket_staff_pk PRIMARY KEY (id),
	CONSTRAINT event_ticket_staff_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT event_ticket_staff_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT event_ticket_staff_event_account_uidx UNIQUE (event_id, account_id)
);

CREATE INDEX IF NOT EXISTS event_ticket_staff_event_id_idx
	ON public.event_ticket_staff (event_id);
CREATE INDEX IF NOT EXISTS event_ticket_staff_account_id_idx
	ON public.event_ticket_staff (account_id);
