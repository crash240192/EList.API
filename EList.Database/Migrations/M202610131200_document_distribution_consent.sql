-- Согласие на распространение ПДн + флаг «требует соглашения» у документов

DO $ADD_DISTRIBUTION_CONSENT$
BEGIN
	IF NOT EXISTS (
		SELECT 1
		FROM pg_enum e
		JOIN pg_type t ON t.oid = e.enumtypid
		JOIN pg_namespace n ON n.oid = t.typnamespace
		WHERE n.nspname = 'public'
		  AND t.typname = 'document_type'
		  AND e.enumlabel = 'distribution_consent'
	)
	THEN
		ALTER TYPE public.document_type ADD VALUE 'distribution_consent';
	END IF;
END
$ADD_DISTRIBUTION_CONSENT$;

ALTER TABLE public.documents
	ADD COLUMN IF NOT EXISTS requires_agreement boolean NOT NULL DEFAULT true;

UPDATE public.documents
SET requires_agreement = false
WHERE type = 'policy'::public.document_type
  AND requires_agreement = true;
