-- Focal point for event cover images (object-position percentages 0…100).

ALTER TABLE public.events
	ADD COLUMN IF NOT EXISTS cover_focus_x double precision NULL;

ALTER TABLE public.events
	ADD COLUMN IF NOT EXISTS cover_focus_y double precision NULL;
