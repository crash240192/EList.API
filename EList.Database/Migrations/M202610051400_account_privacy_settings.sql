-- Profile privacy settings (1:1 with accounts). Defaults match pre-privacy behavior:
-- anyone may invite; age/gender/location hidden from others; birthday accent off.
CREATE TABLE IF NOT EXISTS public.account_privacy_settings
(
	account_id uuid NOT NULL,
	who_can_invite_me text NOT NULL DEFAULT 'Everyone',
	age_visibility text NOT NULL DEFAULT 'Nobody',
	gender_visibility text NOT NULL DEFAULT 'Nobody',
	show_birthday_today boolean NOT NULL DEFAULT false,
	location_visibility text NOT NULL DEFAULT 'Nobody',
	profile_photos_visibility text NOT NULL DEFAULT 'Everyone',
	updated_at timestamp with time zone NOT NULL DEFAULT now(),
	CONSTRAINT account_privacy_settings_pkey PRIMARY KEY (account_id),
	CONSTRAINT account_privacy_settings_account_fk
		FOREIGN KEY (account_id) REFERENCES public.accounts (id) ON DELETE CASCADE,
	CONSTRAINT account_privacy_settings_who_can_invite_chk
		CHECK (who_can_invite_me IN ('Everyone', 'Subscriptions', 'Subscribers', 'Mutual', 'Nobody')),
	CONSTRAINT account_privacy_settings_age_vis_chk
		CHECK (age_visibility IN ('Everyone', 'Subscriptions', 'Subscribers', 'Mutual', 'Nobody')),
	CONSTRAINT account_privacy_settings_gender_vis_chk
		CHECK (gender_visibility IN ('Everyone', 'Subscriptions', 'Subscribers', 'Mutual', 'Nobody')),
	CONSTRAINT account_privacy_settings_location_vis_chk
		CHECK (location_visibility IN ('Everyone', 'Subscriptions', 'Subscribers', 'Mutual', 'Nobody')),
	CONSTRAINT account_privacy_settings_photos_vis_chk
		CHECK (profile_photos_visibility IN ('Everyone', 'Subscriptions', 'Subscribers', 'Mutual', 'Nobody'))
);
