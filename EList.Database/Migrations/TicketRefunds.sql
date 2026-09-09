-- Refunds: optional list of ticket ids covered by this refund (JSON array of UUIDs)
ALTER TABLE public.refunds
	ADD COLUMN IF NOT EXISTS ticket_ids jsonb NULL;
