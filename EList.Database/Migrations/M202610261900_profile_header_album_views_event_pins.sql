-- Profile header (cover-like), album last-viewed, my-events pins

ALTER TABLE public.accounts
	ADD COLUMN IF NOT EXISTS header_image_id uuid NULL;

ALTER TABLE public.accounts
	ADD COLUMN IF NOT EXISTS header_focus_x double precision NULL;

ALTER TABLE public.accounts
	ADD COLUMN IF NOT EXISTS header_focus_y double precision NULL;

ALTER TABLE public.accounts
	ADD COLUMN IF NOT EXISTS header_blur boolean NOT NULL DEFAULT false;

-- When a file was linked into an album (for "has new photos")
ALTER TABLE public.file_album_rls
	ADD COLUMN IF NOT EXISTS create_date timestamptz NOT NULL DEFAULT now();

CREATE INDEX IF NOT EXISTS ix_file_album_rls_album_create_date
	ON public.file_album_rls (album_id, create_date DESC)
	WHERE hidden = false;

CREATE TABLE IF NOT EXISTS public.media_album_views (
	account_id uuid NOT NULL REFERENCES public.accounts (id) ON DELETE CASCADE,
	album_id uuid NOT NULL REFERENCES public.media_albums (id) ON DELETE CASCADE,
	last_viewed_at timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT media_album_views_pk PRIMARY KEY (account_id, album_id)
);

CREATE INDEX IF NOT EXISTS ix_media_album_views_album
	ON public.media_album_views (album_id);

CREATE TABLE IF NOT EXISTS public.account_event_pins (
	account_id uuid NOT NULL REFERENCES public.accounts (id) ON DELETE CASCADE,
	event_id uuid NOT NULL REFERENCES public.events (id) ON DELETE CASCADE,
	pinned_at timestamptz NOT NULL DEFAULT now(),
	sort_order int NOT NULL DEFAULT 0,
	CONSTRAINT account_event_pins_pk PRIMARY KEY (account_id, event_id)
);

CREATE INDEX IF NOT EXISTS ix_account_event_pins_account_order
	ON public.account_event_pins (account_id, sort_order DESC, pinned_at DESC);
