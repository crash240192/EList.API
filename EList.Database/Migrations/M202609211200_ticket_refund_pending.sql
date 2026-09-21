-- Add intermediate ticket status for pending refunds (before provider confirms).
DO $$
BEGIN
	IF EXISTS (SELECT 1 FROM pg_type WHERE typname = 'ticket_status')
	   AND NOT EXISTS (
		SELECT 1
		FROM pg_enum e
		JOIN pg_type t ON t.oid = e.enumtypid
		WHERE t.typname = 'ticket_status' AND e.enumlabel = 'refund_pending'
	   )
	THEN
		ALTER TYPE public.ticket_status ADD VALUE 'refund_pending';
	END IF;
END $$;
