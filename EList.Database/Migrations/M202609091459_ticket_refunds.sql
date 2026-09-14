-- Historical snapshot; changes are already included in M202608250914_develop_incremental / InitialDatabase. Not registered as a FluentMigrator step.

-- Refunds: optional list of ticket ids covered by this refund (JSON array of UUIDs)
ALTER TABLE public.refunds
	ADD COLUMN IF NOT EXISTS ticket_ids jsonb NULL;
