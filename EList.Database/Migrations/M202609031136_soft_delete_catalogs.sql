-- Soft-delete (active) for lookup catalogs used in events and contacts.
-- Existing rows default to active = true.

ALTER TABLE IF EXISTS public.event_categories
    ADD COLUMN IF NOT EXISTS active bool NOT NULL DEFAULT true;

ALTER TABLE IF EXISTS public.event_types
    ADD COLUMN IF NOT EXISTS active bool NOT NULL DEFAULT true;

ALTER TABLE IF EXISTS public.contact_types
    ADD COLUMN IF NOT EXISTS active bool NOT NULL DEFAULT true;

CREATE INDEX IF NOT EXISTS event_categories_active_idx
    ON public.event_categories (active);

CREATE INDEX IF NOT EXISTS event_types_active_idx
    ON public.event_types (active);

CREATE INDEX IF NOT EXISTS contact_types_active_idx
    ON public.contact_types (active);
