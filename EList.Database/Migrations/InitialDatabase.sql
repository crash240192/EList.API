
create or replace function public.uuid_generate_v4()
returns uuid language 'c' cost 1 volatile strict as '$libdir/uuid-ossp', 'uuid_generate_v4';

do $create_uuid_generator$
begin
	create or replace function public.uuid_generate_v4()
returns uuid language 'c' cost 1 volatile strict as '$libdir/uuid-ossp', 'uuid_generate_v4';
end $create_uuid_generator$;

do $CREATE_EVENT_RATING_TYPES$
BEGIN
	if not exists (select 1 from pg_type where typname = 'event_rating_type')
	then 
		CREATE TYPE public.event_rating_type AS ENUM ('expectation', 'summary');
	end if;
end $CREATE_EVENT_RATING_TYPES$;

do $CREATE_NOTIFICATION_TYPES$
BEGIN
	if not exists (select 1 from pg_type where typname = 'system_notification_type')
	then 
		CREATE TYPE public.system_notification_type AS ENUM ('account_created', 'password_has_been_changed', 'new_authorization');
	end if;
end $CREATE_NOTIFICATION_TYPES$;

do $CREATE_GENDERS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'gender')
	then 
		CREATE TYPE public.gender AS ENUM ('male', 'female');
	end if;
end $CREATE_GENDERS$;

CREATE TABLE public.system_notifications (
	id uuid NOT NULL DEFAULT uuid_generate_v4(),
	"type" public.system_notification_type NOT NULL,
	"header" varchar(255) NOT NULL,
	message text NOT NULL,
	short_message text NOT NULL,
	constraint system_notifications_pk primary key (id)
);

insert into public.system_notifications
("type", "header", message, short_message) values 
('account_created', 
'Завершение регистрации в EList', 
'Аригату в хату, бисёнены!<br/><br/>Ваш аккаунт зарегистрирован в сервисе EList.<br/><br/>Для доступа в систему введите код активации:<br/>#ACTIVATION_CODE#<br/><br/><i>Это письмо отправлено автоматически.</i><br/><i>Отвечать на него не нужно.', 
'Код активации для доступа в систему: #ACTIVATION_CODE#.'),

('password_has_been_changed', 
'Пароль был успешно изменён', 
'Аригату в хату, бисёнены!<br/><br/>Пароль в EList для #ACCOUNT# был успешно изменён.<br/><br/><i>Это письмо отправлено автоматически.</i><br/><i>Отвечать на него не нужно.',
'Пароль был успешно изменён.'),

('new_authorization', 
'Авторизация с нового устройства',
'Аригату в хату, бисёнены!<br/><br/>Обнаружена попытка входа с нового устройства.<br/><br/>Для доступа в систему введите код активации:<br/> #ACTIVATION_CODE#<br/><br/> В случае, если вы не пытались авторизоваться с другого устройства, можете проигнорировать данное сообщение.<br/><i>Это письмо отправлено автоматически.</i><br/><i>Отвечать на него не нужно.',
'Для авторизации с нового устройства введите код активации #ACTIVATION_CODE#');



CREATE TABLE public.contact_types (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	localization_path varchar NOT NULL,
	name varchar(100) not null,
	description varchar not null,
	mask varchar NULL,
	allow_notifications bool not null default false,
	CONSTRAINT contact_type_pk PRIMARY KEY (id)
);

create table public.tariff_validators(
	id uuid not null DEFAULT uuid_generate_v4(),
	cost_limit decimal null,
	persons_limit int null,
	allow_private bool not null default false,
	age_limit int null,
	max_period int NULL,
	max_events_count int NULL,
	allow_multidays_events bool DEFAULT false NOT NULL,
	allow_gender_segregation bool null default false,
	constraint tariff_validator_pk primary key (id)
);

CREATE TABLE public.tariffs (
	id uuid NOT NULL DEFAULT uuid_generate_v4(),
	"name" varchar NOT NULL,
	"cost" numeric NOT NULL,
	"period" interval NOT NULL,
	validator_id uuid not null,
	CONSTRAINT tariff_pk PRIMARY KEY (id),
	constraint tariff_validator_fk foreign key (validator_id) references public.tariff_validators(id)
);

CREATE TABLE public.event_categories2 (
	id uuid NOT NULL default public.uuid_generate_v4(),
	name varchar(100) not null,
	localization_path varchar(255) NOT null,
	description varchar(255),
	ico bytea not null,
	color varchar(7) CHECK (color ~* '^#[a-f0-9]{6}$') null,
	CONSTRAINT event_category_pk2 PRIMARY KEY (id)
);

CREATE TABLE public.event_types (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	localization_path varchar(255) NOT NULL,
	name varchar(100) not null,
	category_id uuid NOT NULL,
	ico bytea NOT NULL,
	description varchar(255),
	CONSTRAINT event_type_pk PRIMARY KEY (id),
	CONSTRAINT event_type_event_category_fk FOREIGN KEY (category_id) REFERENCES public.event_categories(id)
);

create table public.accounts(
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	active bool not null default true,
	latitude numeric null default 0,
	longitude numeric null default 0,
	login varchar(50) null,
	password_hash text not null,
	registration_date timestamptz NOT NULL,
	last_seen_date timestamptz,
	last_action_date timestamptz,
	wallet_id uuid NULL,
	constraint accounts_pk primary key (id),	
    CONSTRAINT account_wallet_fk FOREIGN KEY (wallet_id) REFERENCES public.wallets(id)
);

create table public.authorization_token(
	token uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	active bool not null default true,
	account_id uuid not null,
	client_hash varchar(75) not null,
	activation_key varchar(10) not null,
	activation_attempts_remaining int not null,
	creation_date timestamptz not null,
	authorization_date timestamptz not null,
	constraint authorization_token_pk primary key (token),
	constraint authorization_token_account_fk foreign key (account_id) references public.accounts(id)
);

CREATE TABLE public.person_info(
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(), 
	account_id uuid not null,
	first_name varchar(50) NOT NULL,
	last_name varchar(50) NOT NULL,
	patronymic varchar(50) NULL,
	gender gender NULL,
	birthdate timestamp NULL,
	CONSTRAINT persons_data_pk PRIMARY KEY (id),
	constraint persons_data_account foreign key (account_id) references public.accounts (id)
);

CREATE TABLE public.wallets (
	id uuid NOT NULL DEFAULT uuid_generate_v4(),
	balance numeric NOT NULL,
	paid_date timestamptz NULL,
	tariff_id uuid NULL,
	last_charge_date timestamptz NULL,
	CONSTRAINT wallet_pk PRIMARY KEY (id),
	CONSTRAINT wallet_tariff_fk FOREIGN KEY (tariff_id) REFERENCES public.tariffs(id)
);






CREATE TABLE public.contact_data (
	id uuid NULL DEFAULT public.uuid_generate_v4(),
	type_id uuid NULL,
	is_authorization_contact bool not null default false,
	show bool not null default true,
	value varchar NULL,
	CONSTRAINT contact_data_pk PRIMARY KEY (id),
	CONSTRAINT contact_data_contact_type_fk FOREIGN KEY (type_id) REFERENCES public.contact_types(id)
);

create table public.contact_account_rls(
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	contact_data_id uuid not null,
	account_id uuid not null,
	constraint contact_account_pk primary key (id),
	constraint contact_account_fk foreign key (account_id) references public.accounts(id),
	constraint contact_account_contact_data_fk foreign key (contact_data_id) references public.contact_data(id)
);

CREATE TABLE public.subscriptions (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	subscriber_id uuid NOT NULL,
	subscribed_to_id uuid NOT NULL,
	notify_participated bool default true,
	notify_event_created bool default true,
	notify_subscribed bool default true,
	CONSTRAINT person_subscription_pk PRIMARY KEY (id),
	CONSTRAINT person_subscriber_fk FOREIGN KEY (subscriber_id) REFERENCES public.accounts(id),
	CONSTRAINT person_subscribed_to_fk FOREIGN KEY (subscribed_to_id) REFERENCES public.accounts(id)
);

CREATE TABLE public.organizations (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	active bool not null default true,
	"name" varchar(255) NOT NULL,
	address varchar(255) NOT NULL,
	latitude numeric null default 0,
	longitude numeric null default 0,
	wallet_id uuid NOT NULL,
	CONSTRAINT organization_pk PRIMARY KEY (id),
	constraint organization_wallet_fk foreign key (wallet_id) references public.wallets (id)
);


CREATE TABLE public.organization_accounts_rls (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	account_id uuid NOT NULL,
	organization_id uuid NOT NULL,
	CONSTRAINT organization_accounts_pk PRIMARY KEY (id),
	CONSTRAINT organization_accounts_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts (id),
	CONSTRAINT organization_accounts_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id)
);

create table public.event_parameters(
	id uuid not null default public.uuid_generate_v4(),
	"cost" decimal NULL,
	private bool NULL,
	max_persons_count int NULL,
	age_limit int null,
	allowed_gender gender null,
	allow_users_to_invite bool null,
	CONSTRAINT event_parameters_pk PRIMARY KEY (id)
);

CREATE TABLE public.events (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	start_time timestamptz NOT NULL,
	end_time timestamptz NOT NULL,
	"name" varchar(255) NOT NULL,
	latitude numeric NOT NULL default 0,
	longitude numeric NOT NULL default 0,
	"location" geography(Point, 4326) not null,
	description text NULL,
	address varchar NULL,
	active bool NOT NULL default true,
	event_parameters_id uuid null,
	create_date timestamptz NOT NULL,
	update_date timestamptz NOT NULL,
	cover_image_id uuid NULL,
	CONSTRAINT event_pk PRIMARY KEY (id),
	constraint event_parameters_fk foreign key (event_parameters_id) references public.event_parameters(id)
);

CREATE OR REPLACE FUNCTION update_event_location()
RETURNS TRIGGER AS $$
BEGIN
    NEW.location = ST_SetSRID(
        ST_MakePoint(NEW.longitude, NEW.latitude),
        4326
    )::geography;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_event_location
	BEFORE INSERT OR UPDATE OF latitude, longitude
		ON events
		FOR EACH ROW
			EXECUTE FUNCTION update_event_location();


create table public.event_type_rls(
	id uuid not null default public.uuid_generate_v4(),
	event_id uuid not null,
	event_type_id uuid not null,
	CONSTRAINT event_type_rl_pk PRIMARY KEY (id),
	constraint event_type_rl_event foreign key (event_id) references public.events(id),
	constraint event_type_rl_event_type foreign key (event_type_id) references public.event_types(id)
);

CREATE TABLE public.participations (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	account_id uuid NOT NULL,
	event_id uuid NOT NULL,
	CONSTRAINT participation_pk PRIMARY KEY (id),
	CONSTRAINT participation_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT participation_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id)
);

CREATE TABLE public.invitations (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	inviter_id uuid NOT NULL,
	inviter_org_id uuid null,
	invited_id uuid NOT NULL,
	event_id uuid NOT NULL,
	creation_date timestamptz NOT NULL,
	CONSTRAINT invitation_pk PRIMARY KEY (id),
	CONSTRAINT invitation_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT invitation_inviter_fk FOREIGN KEY (inviter_id) REFERENCES public.accounts(id),
	CONSTRAINT invitation_invited_fk FOREIGN KEY (invited_id) REFERENCES public.accounts(id),
	CONSTRAINT invitation_inviter_org_fk FOREIGN KEY (inviter_org_id) REFERENCES public.organizations(id)
);

CREATE TABLE public.event_organizators (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	event_id uuid NOT NULL,
	account_id uuid NULL,
	organization_id uuid null,
	CONSTRAINT event_organizators_pk PRIMARY KEY (id),
	CONSTRAINT event_organizators_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT event_organizators_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT event_organizators_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id)
);

CREATE TABLE public.persons_rating (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	voter_id uuid NOT NULL,
	account_id uuid NOT NULL,
	"comment" text NOT NULL,
	event_id uuid NOT NULL,
	value int NOT NULL,
	CONSTRAINT person_rating_pk PRIMARY KEY (id),
	CONSTRAINT person_rating_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT person_rating_voter_fk FOREIGN KEY (voter_id) REFERENCES public.accounts(id),
	CONSTRAINT person_rating_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id)
);

CREATE TABLE public.events_rating (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	voter_id uuid NOT NULL,
	event_id uuid NOT NULL,
	"comment" text NULL,
	value int NOT NULL,
	rating_type event_rating_type NOT NULL,
	CONSTRAINT event_rating_pk PRIMARY KEY (id),
	CONSTRAINT event_rating_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT event_rating_account_fk FOREIGN KEY (voter_id) REFERENCES public.accounts(id)
);

create table public.auto_invitations (
	id uuid not null default public.uuid_generate_v4(),
	account_id uuid not null,
	constraint auto_invitation_pk primary key (id),
	constraint auto_invitation_person_fk foreign key (account_id) references public.accounts(id)
);

create table public.auto_invitation_organization_rls (
	id uuid not null default public.uuid_generate_v4(),
	auto_invitation_id uuid not null,
	inviter_organization_id uuid not null,
	constraint auto_invitation_organization_pk primary key (id),
	constraint auto_invitation_organization_fk foreign key (inviter_organization_id) references public.organizations(id)
);

create table public.auto_invitation_inviter_rls (
	id uuid not null default public.uuid_generate_v4(),
	auto_invitation_id uuid not null,
	inviter_id uuid not null,
	constraint auto_invitation_inviter_pk primary key (id),
	constraint auto_invitation_invitation_fk foreign key (auto_invitation_id) references public.auto_invitations (id),
	constraint auto_invitation_inviter_fk foreign key (inviter_id) references public.accounts(id)
);

insert into public.contact_types 
(id, localization_path, name, description, mask, allow_notifications) values 
('1d69590d-06ea-4778-a37c-d591b8f25df8', '$.contactData.contactTypes.phone', 'Телефон', 'Телефон', '_ (___) ___-__-__', true),
('8887c160-70b1-4591-903e-8289eb7f5e0a', '$.contactData.contactTypes.email', 'Электронная почта', 'Электронная почта', '_@_._', true);



-- photo
create table public.accounts_avatars_history(
	id uuid not null default public.uuid_generate_v4(),
	account_id uuid not null,
	photo_id uuid not null,
	assignment_date timestamptz not null,
	constraint accounts_avatars_history_pk primary key (id),
	constraint accounts_avatars_history_account_fk foreign key (account_id) references public.accounts (id)
);

create table public.organization_avatars_history(
	id uuid not null default public.uuid_generate_v4(),
	organization_id uuid not null,
	photo_id uuid not null,
	assignment_date timestamptz not null,
	constraint organization_avatars_history_pk primary key (id),
	constraint organization_avatars_history_organization_fk foreign key (organization_id) references public.organizations (id)
);

create table public.media_albums(
	id uuid not null default public.uuid_generate_v4(),
	"name" varchar(255) null,
	description text NULL,
	create_date timestamptz not null default NOW(),
	update_date timestamptz not null default NOW(),
	wallpaper_id uuid NULL,
	constraint media_album_pk primary key (id),
	constraint media_album_account_fk foreign key (event_id) references public.accounts (id),
	constraint media_album_event_fk foreign key (event_id) references public.events (id)
);

create table public.event_album_parameters(
	album_id uuid not null default public.uuid_generate_v4(),
	head_album bool not null default false,
	participants_readonly bool not null default false,
	private_album bool not null default false,
	constraint event_album_parameters_pk primary key (album_id),
	constraint event_album_parameters_album_fk foreign key (album_id) references public.media_albums (id)
);


create table public.file_album_rls(
	id uuid not null default public.uuid_generate_v4(),
	file_id uuid not null,
	album_id uuid not null,
	constraint file_event_album_pk primary key (id),
	constraint file_event_album_album_fk foreign key (album_id) references public.media_albums (id)
);

CREATE TABLE public.account_album_rls (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	album_id uuid NOT NULL,
	account_id uuid NOT NULL,
	CONSTRAINT account_album_relation_unique UNIQUE (id),
	CONSTRAINT account_album_relation_accounts_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT account_album_relation_media_albums_fk FOREIGN KEY (album_id) REFERENCES public.media_albums(id)
);

CREATE TABLE public.event_album_rls (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	event_id uuid NOT NULL,
	album_id uuid NOT NULL,
	CONSTRAINT event_album_rls_unique UNIQUE (id),
	CONSTRAINT event_album_rls_events_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT event_album_rls_media_albums_fk FOREIGN KEY (album_id) REFERENCES public.media_albums(id)
);

CREATE TABLE public.participants_white_list (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	event_id uuid NOT NULL,
	account_id uuid NOT NULL,
	CONSTRAINT participants_white_list_pk PRIMARY KEY (id),
	CONSTRAINT participants_white_list_events_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT participants_white_list_accounts_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id)
);

CREATE TABLE public.participants_black_list (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	event_id uuid NOT NULL,
	account_id uuid NOT NULL,
	CONSTRAINT participants_black_list_pk PRIMARY KEY (id),
	CONSTRAINT participants_black_list_events_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT participants_black_list_accounts_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id)
);


CREATE TABLE public.conversation (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	event_id uuid NULL,
	"name" varchar NULL,
	create_date timestamptz DEFAULT NOW() NOT NULL,
	update_date timestamptz DEFAULT NOW() NOT NULL,
	CONSTRAINT conversation_pk PRIMARY KEY (id),
	CONSTRAINT conversation_events_fk FOREIGN KEY (event_id) REFERENCES public.events(id)
);
CREATE INDEX conversation_event_id_idx ON public.conversation (event_id);

CREATE TABLE public.message (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	conversation_id uuid NOT NULL,
	message_text text NULL,
	account_id uuid NULL,
	organization_id uuid NULL,
	reply_to uuid NULL,
	replied bool DEFAULT false NOT NULL,
	create_date timestamptz DEFAULT NOW() NOT NULL,
	update_date timestamptz DEFAULT NOW() NOT NULL,
	CONSTRAINT message_pk PRIMARY KEY (id),
	CONSTRAINT message_accounts_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT message_organizations_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id),
	CONSTRAINT message_message_fk FOREIGN KEY (reply_to) REFERENCES public.message(id),
	CONSTRAINT message_conversation_fk FOREIGN KEY (conversation_id) REFERENCES public.conversation(id)
);
CREATE INDEX message_account_id_idx ON public.message (account_id);
CREATE INDEX message_reply_to_idx ON public.message (reply_to);
CREATE INDEX message_organization_id_idx ON public.message (organization_id);
CREATE INDEX message_conversation_id_idx ON public.message (conversation_id);


CREATE TABLE public.notifications (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	account_id uuid NOT NULL,
	related_account_id uuid NULL,
	"type" varchar(50) NULL,
	event_id uuid NULL,
	title varchar(100) NULL,
	message varchar(255) NULL,
	created_at timestamptz DEFAULT NOW() NOT NULL,
	read_at timestamptz NULL,
	"data" jsonb NULL,
	CONSTRAINT notifications_pk PRIMARY KEY (id),
	CONSTRAINT notifications_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT notifications_related_account_fk FOREIGN KEY (related_account_id) REFERENCES public.accounts(id),
	CONSTRAINT notifications_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id)
);



/*

create table public.photo_account_rls(
	id uuid not null default public.uuid_generate_v4(),
	photo_id uuid not null,
	account_id uuid not null,
	constraint photo_account_pk primary key (id),
	constraint photo_account_account_fk foreign key (account_id) references public.accounts (id),
	constraint photo_account_photo_fk foreign key (photo_id) references public.photos (id)
);

create table public.video_account_rls(
	id uuid not null default public.uuid_generate_v4(),
	video_id uuid not null,
	account_id uuid not null,
	constraint video_account_pk primary key (id),
	constraint video_account_account_fk foreign key (account_id) references public.accounts (id),
	constraint video_accounts_video_fk foreign key (video_id) references public.videos (id)
);


CREATE TABLE public.photos (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	file_path varchar NOT NULL,
	hash text not null,
	CONSTRAINT photo_pk PRIMARY KEY (id)
);

CREATE TABLE public.videos (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	file_path varchar NOT NULL,
	hash text NOT NULL,
	CONSTRAINT video_pk PRIMARY KEY (id)
);


create table public.chat (
	id uuid not null default public.uuid_generate_v4(),
	event_id uuid null,
	constraint chat_pk primary key (id)
);



create table public.chat_administrator (
	id uuid not null default public.uuid_generate_v4(),
	person_id uuid null,
	organization_id uuid null,
	constraint chat_administrator_pk primary key (id),
	constraint chat_organization_fk foreign key (organization_id) references public.organization(id),
	constraint chat_person_fk foreign key (person_id) references public.person(id)
);*/