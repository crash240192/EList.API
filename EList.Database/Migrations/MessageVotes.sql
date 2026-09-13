-- Likes/dislikes for event page comments (conversation messages).

do $CREATE_MESSAGE_VOTE_VALUE$
BEGIN
	if not exists (select 1 from pg_type where typname = 'message_vote_value')
	then
		CREATE TYPE public.message_vote_value AS ENUM ('like', 'dislike');
	end if;
end $CREATE_MESSAGE_VOTE_VALUE$;

CREATE TABLE IF NOT EXISTS public.message_votes (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	message_id uuid NOT NULL,
	account_id uuid NOT NULL,
	value public.message_vote_value NOT NULL,
	create_date timestamptz DEFAULT NOW() NOT NULL,
	update_date timestamptz DEFAULT NOW() NOT NULL,
	CONSTRAINT message_votes_pk PRIMARY KEY (id),
	CONSTRAINT message_votes_message_fk FOREIGN KEY (message_id) REFERENCES public.message(id) ON DELETE CASCADE,
	CONSTRAINT message_votes_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id) ON DELETE CASCADE,
	CONSTRAINT message_votes_unique UNIQUE (message_id, account_id)
);

CREATE INDEX IF NOT EXISTS message_votes_message_id_idx
	ON public.message_votes (message_id);

CREATE INDEX IF NOT EXISTS message_votes_account_id_idx
	ON public.message_votes (account_id);
