-- Message attachments + system album kind (DiscussionPhotos).

ALTER TABLE public.event_album_parameters
	ADD COLUMN IF NOT EXISTS system_kind smallint NULL;

COMMENT ON COLUMN public.event_album_parameters.system_kind IS
	'Системный тип альбома: 1 = DiscussionPhotos. NULL = обычный пользовательский альбом.';

-- Жёсткая уникальность: один системный kind на событие
CREATE TABLE IF NOT EXISTS public.event_system_albums (
	event_id uuid NOT NULL,
	system_kind smallint NOT NULL,
	album_id uuid NOT NULL,
	CONSTRAINT pk_event_system_albums PRIMARY KEY (event_id, system_kind),
	CONSTRAINT ux_event_system_albums_album UNIQUE (album_id)
);

CREATE TABLE IF NOT EXISTS public.message_files (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	message_id uuid NOT NULL,
	file_id uuid NOT NULL,
	sort_order int NOT NULL DEFAULT 0,
	CONSTRAINT pk_message_files PRIMARY KEY (id),
	CONSTRAINT ux_message_files_msg_file UNIQUE (message_id, file_id)
);

CREATE INDEX IF NOT EXISTS ix_message_files_file_id ON public.message_files (file_id);
CREATE INDEX IF NOT EXISTS ix_message_files_message_id ON public.message_files (message_id);

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'fk_message_files_message'
	) THEN
		ALTER TABLE public.message_files
			ADD CONSTRAINT fk_message_files_message
			FOREIGN KEY (message_id) REFERENCES public.message(id) ON DELETE CASCADE;
	END IF;
END $$;
