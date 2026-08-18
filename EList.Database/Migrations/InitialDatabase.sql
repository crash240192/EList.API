
CREATE EXTENSION IF NOT EXISTS postgis;

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
		CREATE TYPE public.system_notification_type AS ENUM ('account_created', 'password_has_been_changed', 'new_authorization', 'reset_password_request');
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
'Для авторизации с нового устройства введите код активации #ACTIVATION_CODE#'),

('reset_password_request', 
'Запрос на смену пароля',
'Аригату в хату, бисёнены!<br/><br/>Тут говорят ты пароль хочешь сменить? Вот код для подтверждения смены пароля: <br/> #ACTIVATION_CODE#<br/><br/> В случае, если вы не пытались авторизоваться с другого устройства, можете проигнорировать данное сообщение.<br/><i>Это письмо отправлено автоматически.</i><br/><i>Отвечать на него не нужно.',
'Код смены пароля #ACTIVATION_CODE#');



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
	for_organization bool not null,
	CONSTRAINT tariff_pk PRIMARY KEY (id),
	constraint tariff_validator_fk foreign key (validator_id) references public.tariff_validators(id)
);

CREATE TABLE public.event_categories (
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
	ico varchar NOT NULL,
	description varchar(255),
	CONSTRAINT event_type_pk PRIMARY KEY (id),
	CONSTRAINT event_type_event_category_fk FOREIGN KEY (category_id) REFERENCES public.event_categories(id)
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
	first_name varchar(50) NULL,
	last_name varchar(50) NULL,
	patronymic varchar(50) NULL,
	gender gender NULL,
	birthdate timestamp NULL,
	CONSTRAINT persons_data_pk PRIMARY KEY (id),
	constraint persons_data_account foreign key (account_id) references public.accounts (id)
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
	tickets_enabled bool not null default false,
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
	viewed bool DEFAULT false NOT NULL,
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
('1d69590d-06ea-4778-a37c-d591b8f25df8', '$.contactData.contactTypes.phone', 'Телефон', 'Телефон', '^\+7\s\(\d{3}\)\s\d{3}-\d{2}-\d{2}$', true),
('8887c160-70b1-4591-903e-8289eb7f5e0a', '$.contactData.contactTypes.email', 'Электронная почта', 'Электронная почта', '^[^\s@]+@[^\s@]+\.[^\s@]+$', true);



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
	constraint media_album_pk primary key (id)
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
	participants_only_visible bool NOT NULL DEFAULT false,
	participants_readonly bool NOT NULL DEFAULT false,
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




-- agreements
CREATE TABLE public.anonymous_age_agreements (
	id uuid DEFAULT public.uuid_generate_v4() NOT NULL,
	jwt uuid NOT NULL,
	agreement_date timestamptz DEFAULT now() NOT NULL,
	client_info varchar NOT NULL,
	CONSTRAINT anonymous_age_agreements_pk PRIMARY KEY (id)
);


CREATE TYPE public.document_type AS ENUM (
	'policy',
	'consent',
	'agreement',
	'organization_agreement',
	'ticketing_agreement');

	CREATE TABLE public.documents (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	"header" varchar NOT NULL,
	"text" text NOT NULL,
	hash varchar NOT NULL,
	"type" public.document_type NOT NULL,
	"version" varchar NOT NULL,
	creation_date timestamptz DEFAULT now() NOT NULL,
	CONSTRAINT documents_pk PRIMARY KEY (id)
);


CREATE TABLE public.account_agreement_rls (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	account_id uuid NOT NULL,
	document_id uuid NOT NULL,
	agreement_date timestamptz not null,
	CONSTRAINT account_agreement_rls_pk PRIMARY KEY (id),
	CONSTRAINT account_agreement_rls_accounts_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT account_agreement_rls_document_fk FOREIGN KEY (document_id) REFERENCES public.documents(id)
);

CREATE TABLE public.organization_agreement_rls (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	organization_id uuid NOT NULL,
	document_id uuid NOT NULL,
	agreement_date timestamptz NOT NULL,
	CONSTRAINT organization_agreement_rls_pk PRIMARY KEY (id),
	CONSTRAINT organization_agreement_rls_organizations_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id),
	CONSTRAINT organization_agreement_rls_documents_fk FOREIGN KEY (document_id) REFERENCES public.documents(id)
);


--agreements


-- =============================================================================
-- Organizations (расширение) + юридические реквизиты + платежи/билеты
--
-- Упрощение относительно черновика Claude:
--   * users -> accounts; document_versions/acceptances уже есть (documents/account_agreement_rls)
--   * birthdate/age уже в person_info/event_parameters — не дублируем
--   * убраны org_type/plan, invitations, audit_log, append-only триггеры
--   * роли менеджеров: owner | manager (без author/admin)
--   * информационная организация: минимум полей, can_sell_tickets = false
--   * юридически оформленная: organization_legal + organization_payout, can_sell_tickets после verified
-- =============================================================================

do $CREATE_ORG_MEMBER_ROLES$
BEGIN
	if not exists (select 1 from pg_type where typname = 'organization_member_role')
	then
		CREATE TYPE public.organization_member_role AS ENUM ('owner', 'manager');
	end if;
end $CREATE_ORG_MEMBER_ROLES$;

do $CREATE_ORG_VERIFICATION_STATUS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'organization_verification_status')
	then
		CREATE TYPE public.organization_verification_status AS ENUM ('unverified', 'pending', 'verified', 'rejected');
	end if;
end $CREATE_ORG_VERIFICATION_STATUS$;

do $CREATE_ORG_LEGAL_FORM$
BEGIN
	if not exists (select 1 from pg_type where typname = 'organization_legal_form')
	then
		CREATE TYPE public.organization_legal_form AS ENUM ('self_employed', 'ip', 'legal_entity');
	end if;
end $CREATE_ORG_LEGAL_FORM$;

do $CREATE_PAYMENT_PROVIDER$
BEGIN
	if not exists (select 1 from pg_type where typname = 'payment_provider')
	then
		CREATE TYPE public.payment_provider AS ENUM ('yookassa', 'tbank', 'sberpay', 'payanyway', 'paygine', 'other');
	end if;
end $CREATE_PAYMENT_PROVIDER$;

do $CREATE_ORDER_STATUS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'order_status')
	then
		CREATE TYPE public.order_status AS ENUM (
			'pending', 'authorized', 'paid', 'canceled', 'refunded', 'partially_refunded', 'failed');
	end if;
end $CREATE_ORDER_STATUS$;

do $CREATE_TICKET_STATUS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'ticket_status')
	then
		CREATE TYPE public.ticket_status AS ENUM ('issued', 'used', 'refunded', 'void');
	end if;
end $CREATE_TICKET_STATUS$;

do $CREATE_REFUND_STATUS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'refund_status')
	then
		CREATE TYPE public.refund_status AS ENUM ('pending', 'succeeded', 'failed');
	end if;
end $CREATE_REFUND_STATUS$;

do $CREATE_PROVIDER_ONBOARDING_STATUS$
BEGIN
	if not exists (select 1 from pg_type where typname = 'provider_onboarding_status')
	then
		CREATE TYPE public.provider_onboarding_status AS ENUM ('none', 'pending', 'active', 'rejected');
	end if;
end $CREATE_PROVIDER_ONBOARDING_STATUS$;


-- organizations: информационный профиль + флаг продажи билетов
ALTER TABLE public.organizations
	ADD COLUMN IF NOT EXISTS description text NULL,
	ADD COLUMN IF NOT EXISTS created_by_account_id uuid NULL,
	ADD COLUMN IF NOT EXISTS verification_status public.organization_verification_status NOT NULL DEFAULT 'unverified',
	ADD COLUMN IF NOT EXISTS verification_reject_reason text NULL,
	ADD COLUMN IF NOT EXISTS can_sell_tickets bool NOT NULL DEFAULT false,
	ADD COLUMN IF NOT EXISTS create_date timestamptz NOT NULL DEFAULT now(),
	ADD COLUMN IF NOT EXISTS update_date timestamptz NOT NULL DEFAULT now();

-- простая организация может быть без адреса и кошелька
ALTER TABLE public.organizations ALTER COLUMN address DROP NOT NULL;
ALTER TABLE public.organizations ALTER COLUMN wallet_id DROP NOT NULL;

ALTER TABLE public.organizations DROP CONSTRAINT IF EXISTS organization_created_by_fk;
ALTER TABLE public.organizations
	ADD CONSTRAINT organization_created_by_fk
		FOREIGN KEY (created_by_account_id) REFERENCES public.accounts(id);

ALTER TABLE public.organizations DROP CONSTRAINT IF EXISTS organization_sell_tickets_verified_chk;
ALTER TABLE public.organizations
	ADD CONSTRAINT organization_sell_tickets_verified_chk
		CHECK (can_sell_tickets = false OR verification_status = 'verified');

CREATE INDEX IF NOT EXISTS organizations_created_by_account_id_idx
	ON public.organizations (created_by_account_id);
CREATE INDEX IF NOT EXISTS organizations_verification_status_idx
	ON public.organizations (verification_status);


-- organization_accounts_rls: владелец и менеджеры
ALTER TABLE public.organization_accounts_rls
	ADD COLUMN IF NOT EXISTS role public.organization_member_role NOT NULL DEFAULT 'manager',
	ADD COLUMN IF NOT EXISTS active bool NOT NULL DEFAULT true,
	ADD COLUMN IF NOT EXISTS invited_by uuid NULL,
	ADD COLUMN IF NOT EXISTS joined_at timestamptz NOT NULL DEFAULT now();

ALTER TABLE public.organization_accounts_rls DROP CONSTRAINT IF EXISTS organization_accounts_unique;
ALTER TABLE public.organization_accounts_rls
	ADD CONSTRAINT organization_accounts_unique UNIQUE (organization_id, account_id);

ALTER TABLE public.organization_accounts_rls DROP CONSTRAINT IF EXISTS organization_accounts_invited_by_fk;
ALTER TABLE public.organization_accounts_rls
	ADD CONSTRAINT organization_accounts_invited_by_fk
		FOREIGN KEY (invited_by) REFERENCES public.accounts(id);

CREATE UNIQUE INDEX IF NOT EXISTS organization_accounts_one_owner_idx
	ON public.organization_accounts_rls (organization_id)
	WHERE role = 'owner';

CREATE INDEX IF NOT EXISTS organization_accounts_account_id_idx
	ON public.organization_accounts_rls (account_id);


-- юридические данные (1:1), нужны для верификации и продажи билетов
CREATE TABLE IF NOT EXISTS public.organization_legal (
	organization_id uuid NOT NULL,
	legal_form public.organization_legal_form NOT NULL,
	inn varchar(12) NULL,
	ogrn varchar(15) NULL,
	kpp varchar(9) NULL,
	legal_address varchar(500) NULL,
	head_name varchar(255) NULL,
	head_basis varchar(255) NULL,
	verified_at timestamptz NULL,
	CONSTRAINT organization_legal_pk PRIMARY KEY (organization_id),
	CONSTRAINT organization_legal_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id)
);


-- платёжные реквизиты / онбординг у провайдера (1:1)
CREATE TABLE IF NOT EXISTS public.organization_payout (
	organization_id uuid NOT NULL,
	bank_account varchar(34) NULL,
	bik varchar(9) NULL,
	bank_name varchar(255) NULL,
	tax_regime varchar(50) NULL,
	provider public.payment_provider NULL,
	provider_seller_id varchar NULL,
	onboarding_status public.provider_onboarding_status NOT NULL DEFAULT 'none',
	updated_by uuid NULL,
	update_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT organization_payout_pk PRIMARY KEY (organization_id),
	CONSTRAINT organization_payout_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id),
	CONSTRAINT organization_payout_updated_by_fk FOREIGN KEY (updated_by) REFERENCES public.accounts(id)
);


-- заказы (сплит: сумма продавца + комиссия сервиса)
CREATE TABLE IF NOT EXISTS public.orders (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	event_id uuid NOT NULL,
	buyer_account_id uuid NOT NULL,
	seller_organization_id uuid NOT NULL,
	quantity int NOT NULL,
	amount_total numeric(12, 2) NOT NULL,
	amount_seller numeric(12, 2) NOT NULL,
	amount_commission numeric(12, 2) NOT NULL,
	currency char(3) NOT NULL DEFAULT 'RUB',
	status public.order_status NOT NULL DEFAULT 'pending',
	provider public.payment_provider NULL,
	provider_payment_id varchar NULL,
	idempotency_key varchar NULL,
	create_date timestamptz NOT NULL DEFAULT now(),
	paid_at timestamptz NULL,
	CONSTRAINT orders_pk PRIMARY KEY (id),
	CONSTRAINT orders_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT orders_buyer_account_fk FOREIGN KEY (buyer_account_id) REFERENCES public.accounts(id),
	CONSTRAINT orders_seller_organization_fk FOREIGN KEY (seller_organization_id) REFERENCES public.organizations(id),
	CONSTRAINT orders_quantity_chk CHECK (quantity > 0),
	CONSTRAINT orders_amount_total_chk CHECK (amount_total >= 0),
	CONSTRAINT orders_amount_seller_chk CHECK (amount_seller >= 0),
	CONSTRAINT orders_amount_commission_chk CHECK (amount_commission >= 0),
	CONSTRAINT orders_split_sum_chk CHECK (amount_total = amount_seller + amount_commission)
);

CREATE UNIQUE INDEX IF NOT EXISTS orders_provider_payment_uidx
	ON public.orders (provider, provider_payment_id)
	WHERE provider_payment_id IS NOT NULL;
CREATE UNIQUE INDEX IF NOT EXISTS orders_idempotency_uidx
	ON public.orders (idempotency_key)
	WHERE idempotency_key IS NOT NULL;
CREATE INDEX IF NOT EXISTS orders_event_id_idx ON public.orders (event_id);
CREATE INDEX IF NOT EXISTS orders_buyer_account_id_idx ON public.orders (buyer_account_id);
CREATE INDEX IF NOT EXISTS orders_seller_organization_id_idx ON public.orders (seller_organization_id);
CREATE INDEX IF NOT EXISTS orders_status_idx ON public.orders (status);


-- билеты
CREATE TABLE IF NOT EXISTS public.tickets (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	order_id uuid NOT NULL,
	event_id uuid NOT NULL,
	holder_account_id uuid NOT NULL,
	status public.ticket_status NOT NULL DEFAULT 'issued',
	code varchar NOT NULL,
	issued_at timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT tickets_pk PRIMARY KEY (id),
	CONSTRAINT tickets_code_unique UNIQUE (code),
	CONSTRAINT tickets_order_fk FOREIGN KEY (order_id) REFERENCES public.orders(id),
	CONSTRAINT tickets_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT tickets_holder_account_fk FOREIGN KEY (holder_account_id) REFERENCES public.accounts(id)
);

CREATE INDEX IF NOT EXISTS tickets_order_id_idx ON public.tickets (order_id);
CREATE INDEX IF NOT EXISTS tickets_holder_account_id_idx ON public.tickets (holder_account_id);
CREATE INDEX IF NOT EXISTS tickets_event_id_idx ON public.tickets (event_id);


-- возвраты
CREATE TABLE IF NOT EXISTS public.refunds (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	order_id uuid NOT NULL,
	amount numeric(12, 2) NOT NULL,
	reason varchar NULL,
	provider_refund_id varchar NULL,
	status public.refund_status NOT NULL DEFAULT 'pending',
	create_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT refunds_pk PRIMARY KEY (id),
	CONSTRAINT refunds_order_fk FOREIGN KEY (order_id) REFERENCES public.orders(id),
	CONSTRAINT refunds_amount_chk CHECK (amount > 0)
);

CREATE UNIQUE INDEX IF NOT EXISTS refunds_provider_refund_uidx
	ON public.refunds (provider_refund_id)
	WHERE provider_refund_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS refunds_order_id_idx ON public.refunds (order_id);


-- журнал webhook провайдера (идемпотентность колбэков)
CREATE TABLE IF NOT EXISTS public.payment_webhook_events (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	provider public.payment_provider NOT NULL,
	provider_event_id varchar NOT NULL,
	order_id uuid NULL,
	payload jsonb NULL,
	received_at timestamptz NOT NULL DEFAULT now(),
	processed_at timestamptz NULL,
	CONSTRAINT payment_webhook_events_pk PRIMARY KEY (id),
	CONSTRAINT payment_webhook_events_order_fk FOREIGN KEY (order_id) REFERENCES public.orders(id),
	CONSTRAINT payment_webhook_events_provider_event_unique UNIQUE (provider, provider_event_id)
);

CREATE INDEX IF NOT EXISTS payment_webhook_events_order_id_idx
	ON public.payment_webhook_events (order_id);

-- флаг продажи билетов на уровне мероприятия
ALTER TABLE public.event_parameters
	ADD COLUMN IF NOT EXISTS tickets_enabled bool NOT NULL DEFAULT false;

-- контактные данные организаций (аналог contact_account_rls)
CREATE TABLE IF NOT EXISTS public.contact_organization_rls (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	contact_data_id uuid NOT NULL,
	organization_id uuid NOT NULL,
	CONSTRAINT contact_organization_pk PRIMARY KEY (id),
	CONSTRAINT contact_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id),
	CONSTRAINT contact_organization_contact_data_fk FOREIGN KEY (contact_data_id) REFERENCES public.contact_data(id)
);

CREATE UNIQUE INDEX IF NOT EXISTS contact_organization_unique_uidx
	ON public.contact_organization_rls (organization_id, contact_data_id);
CREATE INDEX IF NOT EXISTS contact_organization_organization_id_idx
	ON public.contact_organization_rls (organization_id);
CREATE INDEX IF NOT EXISTS contact_organization_contact_data_id_idx
	ON public.contact_organization_rls (contact_data_id);

-- шаблоны создания мероприятий
CREATE TABLE IF NOT EXISTS public.event_templates (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	owner_account_id uuid NULL,
	owner_organization_id uuid NULL,
	"name" varchar(255) NOT NULL,
	template_body jsonb NOT NULL,
	create_date timestamptz NOT NULL DEFAULT now(),
	update_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT event_templates_pk PRIMARY KEY (id),
	CONSTRAINT event_templates_owner_account_fk FOREIGN KEY (owner_account_id) REFERENCES public.accounts(id),
	CONSTRAINT event_templates_owner_organization_fk FOREIGN KEY (owner_organization_id) REFERENCES public.organizations(id),
	CONSTRAINT event_templates_owner_check CHECK (
		(owner_account_id IS NOT NULL AND owner_organization_id IS NULL)
		OR (owner_account_id IS NULL AND owner_organization_id IS NOT NULL)
	)
);

CREATE INDEX IF NOT EXISTS event_templates_owner_account_id_idx
	ON public.event_templates (owner_account_id);
CREATE INDEX IF NOT EXISTS event_templates_owner_organization_id_idx
	ON public.event_templates (owner_organization_id);

-- параметры доступа к диалогам мероприятия (на случай если conversation уже создана)
ALTER TABLE public.conversation
	ADD COLUMN IF NOT EXISTS participants_only_visible bool NOT NULL DEFAULT false;
ALTER TABLE public.conversation
	ADD COLUMN IF NOT EXISTS participants_readonly bool NOT NULL DEFAULT false;

-- =============================================================================
-- Bug reports (отдельная схема)
-- =============================================================================
CREATE SCHEMA IF NOT EXISTS bugreports;

-- Enum MUST live in public (not bugreports).
-- Npgsql resolves PG enums by unqualified typname / search_path; a type only in
-- bugreports.bug_report_status yields: "A PostgreSQL type with the name
-- 'bug_report_status' was not found in the database".
-- Also: "IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = ...)" matches ANY
-- schema — so CREATE TYPE bugreports.... is skipped when public already has it.
do $CREATE_BUG_REPORT_STATUS$
BEGIN
	if not exists (
		select 1
		from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'bug_report_status' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.bug_report_status AS ENUM ('pending', 'resolved', 'cancelled');
	end if;
end $CREATE_BUG_REPORT_STATUS$;

CREATE TABLE IF NOT EXISTS bugreports.categories (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	code varchar(64) NOT NULL,
	"name" varchar(255) NOT NULL,
	active bool NOT NULL DEFAULT true,
	sort_order int NOT NULL DEFAULT 0,
	create_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT bug_report_categories_pk PRIMARY KEY (id),
	CONSTRAINT bug_report_categories_code_unique UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS bugreports.reports (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	reporter_account_id uuid NOT NULL,
	category_id uuid NOT NULL,
	description text NOT NULL,
	status public.bug_report_status NOT NULL DEFAULT 'pending',
	create_date timestamptz NOT NULL DEFAULT now(),
	update_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT bug_reports_pk PRIMARY KEY (id),
	CONSTRAINT bug_reports_reporter_fk FOREIGN KEY (reporter_account_id) REFERENCES public.accounts(id),
	CONSTRAINT bug_reports_category_fk FOREIGN KEY (category_id) REFERENCES bugreports.categories(id)
);

-- If an earlier script created bugreports.bug_report_status and the column uses it,
-- move the column to public.bug_report_status and drop the schema-local type.
do $FIX_BUG_REPORT_STATUS_SCHEMA$
BEGIN
	if exists (
		select 1
		from information_schema.columns
		where table_schema = 'bugreports'
		  and table_name = 'reports'
		  and column_name = 'status'
		  and udt_schema = 'bugreports'
		  and udt_name = 'bug_report_status'
	)
	then
		ALTER TABLE bugreports.reports
			ALTER COLUMN status DROP DEFAULT,
			ALTER COLUMN status TYPE public.bug_report_status
				USING status::text::public.bug_report_status,
			ALTER COLUMN status SET DEFAULT 'pending'::public.bug_report_status;
	end if;

	BEGIN
		DROP TYPE IF EXISTS bugreports.bug_report_status;
	EXCEPTION
		WHEN dependent_objects_still_exist THEN
			NULL; -- still referenced; leave it
	END;
end $FIX_BUG_REPORT_STATUS_SCHEMA$;

CREATE INDEX IF NOT EXISTS bug_reports_reporter_account_id_idx ON bugreports.reports (reporter_account_id);
CREATE INDEX IF NOT EXISTS bug_reports_category_id_idx ON bugreports.reports (category_id);
CREATE INDEX IF NOT EXISTS bug_reports_status_idx ON bugreports.reports (status);
CREATE INDEX IF NOT EXISTS bug_reports_create_date_idx ON bugreports.reports (create_date DESC);

CREATE TABLE IF NOT EXISTS bugreports.report_files (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	report_id uuid NOT NULL,
	file_id uuid NOT NULL,
	CONSTRAINT bug_report_files_pk PRIMARY KEY (id),
	CONSTRAINT bug_report_files_report_fk FOREIGN KEY (report_id) REFERENCES bugreports.reports(id) ON DELETE CASCADE,
	CONSTRAINT bug_report_files_report_file_unique UNIQUE (report_id, file_id)
);

CREATE INDEX IF NOT EXISTS bug_report_files_report_id_idx ON bugreports.report_files (report_id);

INSERT INTO bugreports.categories (code, name, sort_order)
SELECT v.code, v.name, v.sort_order
FROM (VALUES
	('ui', 'Интерфейс', 10),
	('events', 'Мероприятия', 20),
	('organizations', 'Организации', 30),
	('payments', 'Платежи и билеты', 40),
	('auth', 'Авторизация / аккаунт', 50),
	('media', 'Медиа и альбомы', 60),
	('other', 'Другое', 100)
) AS v(code, name, sort_order)
WHERE NOT EXISTS (
	SELECT 1 FROM bugreports.categories c WHERE c.code = v.code
);

-- /organizations + payments


/*
create table public.chat_administrator (
	id uuid not null default public.uuid_generate_v4(),
	person_id uuid null,
	organization_id uuid null,
	constraint chat_administrator_pk primary key (id),
	constraint chat_organization_fk foreign key (organization_id) references public.organization(id),
	constraint chat_person_fk foreign key (person_id) references public.person(id)
);*/
-- =============================================================================
-- Content moderation (жалобы на контент + роли площадки)
-- =============================================================================

do $CREATE_PLATFORM_ROLE$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'platform_role' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.platform_role AS ENUM ('superuser', 'admin', 'moderator');
	end if;
end $CREATE_PLATFORM_ROLE$;

do $CREATE_REPORT_TARGET_TYPE$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_target_type' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_target_type AS ENUM ('event', 'message');
	end if;
end $CREATE_REPORT_TARGET_TYPE$;

do $CREATE_REPORT_TARGET_SCOPE$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_target_scope' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_target_scope AS ENUM ('event', 'message', 'both');
	end if;
end $CREATE_REPORT_TARGET_SCOPE$;

do $CREATE_REPORT_SEVERITY$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_severity' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_severity AS ENUM ('community', 'safety');
	end if;
end $CREATE_REPORT_SEVERITY$;

do $CREATE_REPORT_QUEUE$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_queue' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_queue AS ENUM ('organizers', 'platform', 'both');
	end if;
end $CREATE_REPORT_QUEUE$;

do $CREATE_REPORT_STATUS$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_status' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_status AS ENUM ('open', 'in_review', 'resolved', 'dismissed', 'escalated');
	end if;
end $CREATE_REPORT_STATUS$;

do $CREATE_REPORT_RESOLUTION_ACTION$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_resolution_action' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_resolution_action AS ENUM (
			'hide_content',
			'delete_content',
			'warn',
			'ban_from_event',
			'cancel_event',
			'dismiss',
			'escalate',
			'other'
		);
	end if;
end $CREATE_REPORT_RESOLUTION_ACTION$;

do $CREATE_REPORT_ACTOR_CONTEXT$
BEGIN
	if not exists (
		select 1 from pg_type t
		join pg_namespace n on n.oid = t.typnamespace
		where t.typname = 'report_actor_context' and n.nspname = 'public'
	)
	then
		CREATE TYPE public.report_actor_context AS ENUM ('reporter', 'organizer', 'platform_moderator', 'system');
	end if;
end $CREATE_REPORT_ACTOR_CONTEXT$;

-- Роли площадки: нет записи = обычный пользователь
CREATE TABLE IF NOT EXISTS public.account_platform_roles (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	account_id uuid NOT NULL,
	"role" public.platform_role NOT NULL,
	active bool NOT NULL DEFAULT true,
	assigned_at timestamptz NOT NULL DEFAULT now(),
	assigned_by uuid NULL,
	CONSTRAINT account_platform_roles_pk PRIMARY KEY (id),
	CONSTRAINT account_platform_roles_account_unique UNIQUE (account_id),
	CONSTRAINT account_platform_roles_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT account_platform_roles_assigned_by_fk FOREIGN KEY (assigned_by) REFERENCES public.accounts(id)
);

CREATE INDEX IF NOT EXISTS account_platform_roles_role_idx ON public.account_platform_roles ("role");
CREATE INDEX IF NOT EXISTS account_platform_roles_active_idx ON public.account_platform_roles (active);

-- Справочник причин жалобы
CREATE TABLE IF NOT EXISTS public.report_reasons (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	code varchar(64) NOT NULL,
	"name" varchar(255) NOT NULL,
	description varchar(512) NULL,
	target_scope public.report_target_scope NOT NULL DEFAULT 'both',
	severity public.report_severity NOT NULL,
	primary_queue public.report_queue NOT NULL,
	sort_order int NOT NULL DEFAULT 0,
	active bool NOT NULL DEFAULT true,
	create_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT report_reasons_pk PRIMARY KEY (id),
	CONSTRAINT report_reasons_code_unique UNIQUE (code)
);

CREATE INDEX IF NOT EXISTS report_reasons_active_sort_idx ON public.report_reasons (active, sort_order);

-- Жалобы на контент (событие / сообщение)
CREATE TABLE IF NOT EXISTS public.content_reports (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	reporter_account_id uuid NOT NULL,
	target_type public.report_target_type NOT NULL,
	target_id uuid NOT NULL,
	event_id uuid NULL,
	message_id uuid NULL,
	conversation_id uuid NULL,
	reason_id uuid NOT NULL,
	"comment" text NULL,
	target_snapshot jsonb NULL,
	status public.report_status NOT NULL DEFAULT 'open',
	organizer_status public.report_status NULL,
	platform_status public.report_status NULL,
	assigned_to uuid NULL,
	resolution_action public.report_resolution_action NULL,
	resolution_comment text NULL,
	resolved_by uuid NULL,
	resolved_at timestamptz NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	updated_at timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT content_reports_pk PRIMARY KEY (id),
	CONSTRAINT content_reports_reporter_fk FOREIGN KEY (reporter_account_id) REFERENCES public.accounts(id),
	CONSTRAINT content_reports_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT content_reports_message_fk FOREIGN KEY (message_id) REFERENCES public.message(id) ON DELETE SET NULL,
	CONSTRAINT content_reports_conversation_fk FOREIGN KEY (conversation_id) REFERENCES public.conversation(id) ON DELETE SET NULL,
	CONSTRAINT content_reports_reason_fk FOREIGN KEY (reason_id) REFERENCES public.report_reasons(id),
	CONSTRAINT content_reports_assigned_to_fk FOREIGN KEY (assigned_to) REFERENCES public.accounts(id),
	CONSTRAINT content_reports_resolved_by_fk FOREIGN KEY (resolved_by) REFERENCES public.accounts(id),
	CONSTRAINT content_reports_message_target_chk CHECK (
		(target_type = 'event' AND message_id IS NULL AND conversation_id IS NULL)
		OR (target_type = 'message')
	)
);

CREATE INDEX IF NOT EXISTS content_reports_status_created_idx ON public.content_reports (status, created_at DESC);
CREATE INDEX IF NOT EXISTS content_reports_event_status_idx ON public.content_reports (event_id, status);
CREATE INDEX IF NOT EXISTS content_reports_target_idx ON public.content_reports (target_type, target_id);
CREATE INDEX IF NOT EXISTS content_reports_platform_status_idx ON public.content_reports (platform_status, created_at DESC);
CREATE INDEX IF NOT EXISTS content_reports_organizer_status_idx ON public.content_reports (organizer_status, created_at DESC);
CREATE INDEX IF NOT EXISTS content_reports_reporter_idx ON public.content_reports (reporter_account_id);
CREATE INDEX IF NOT EXISTS content_reports_reason_idx ON public.content_reports (reason_id);

-- Одна активная жалоба от пользователя на один объект
CREATE UNIQUE INDEX IF NOT EXISTS content_reports_open_target_reporter_uidx
	ON public.content_reports (reporter_account_id, target_type, target_id)
	WHERE status IN ('open', 'in_review', 'escalated');

-- Аудит действий по жалобе
CREATE TABLE IF NOT EXISTS public.content_report_actions (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	report_id uuid NOT NULL,
	actor_account_id uuid NULL,
	actor_context public.report_actor_context NOT NULL,
	"action" varchar(64) NOT NULL,
	details jsonb NULL,
	created_at timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT content_report_actions_pk PRIMARY KEY (id),
	CONSTRAINT content_report_actions_report_fk FOREIGN KEY (report_id) REFERENCES public.content_reports(id) ON DELETE CASCADE,
	CONSTRAINT content_report_actions_actor_fk FOREIGN KEY (actor_account_id) REFERENCES public.accounts(id)
);

CREATE INDEX IF NOT EXISTS content_report_actions_report_id_idx ON public.content_report_actions (report_id, created_at DESC);

-- Состояние модерации сообщения (hide вместо физического удаления для legal)
ALTER TABLE public.message
	ADD COLUMN IF NOT EXISTS hidden bool NOT NULL DEFAULT false,
	ADD COLUMN IF NOT EXISTS hidden_at timestamptz NULL,
	ADD COLUMN IF NOT EXISTS hidden_by uuid NULL;

DO $MESSAGE_HIDDEN_BY_FK$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'message_hidden_by_fk'
	)
	THEN
		ALTER TABLE public.message
			ADD CONSTRAINT message_hidden_by_fk FOREIGN KEY (hidden_by) REFERENCES public.accounts(id);
	END IF;
END $MESSAGE_HIDDEN_BY_FK$;

CREATE INDEX IF NOT EXISTS message_hidden_idx ON public.message (hidden) WHERE hidden = true;

INSERT INTO public.report_reasons (code, name, description, target_scope, severity, primary_queue, sort_order)
SELECT v.code, v.name, v.description, v.target_scope::public.report_target_scope, v.severity::public.report_severity, v.primary_queue::public.report_queue, v.sort_order
FROM (VALUES
	('spam', 'Спам', 'Реклама, флуд, повторяющиеся сообщения', 'both', 'community', 'organizers', 10),
	('harassment', 'Оскорбления / травля', 'Оскорбительное поведение в обсуждении', 'message', 'community', 'organizers', 20),
	('off_topic', 'Оффтоп', 'Сообщение не относится к мероприятию', 'message', 'community', 'organizers', 30),
	('inappropriate_event', 'Неуместное мероприятие', 'Событие нарушает правила площадки', 'event', 'community', 'platform', 40),
	('other', 'Другое', 'Иная причина (community)', 'both', 'community', 'organizers', 90),
	('illegal_content', 'Неправомерный контент', 'Контент, нарушающий закон', 'both', 'safety', 'both', 100),
	('threats', 'Угрозы / насилие', 'Угрозы или призывы к насилию', 'both', 'safety', 'both', 110),
	('fraud', 'Мошенничество', 'Обман, скамы, фишинг', 'both', 'safety', 'both', 120),
	('hate', 'Разжигание ненависти', 'Ненависть / экстремизм', 'both', 'safety', 'both', 130),
	('sexual_exploitation', 'Сексуальная эксплуатация', 'Недопустимый сексуальный контент', 'both', 'safety', 'both', 140)
) AS v(code, name, description, target_scope, severity, primary_queue, sort_order)
WHERE NOT EXISTS (
	SELECT 1 FROM public.report_reasons r WHERE r.code = v.code
);


-- =============================================================================
-- Content reports: photo / account / organization / event_organizator
-- =============================================================================

do $ADD_REPORT_TARGET_TYPE_VALUES$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_type' AND e.enumlabel = 'photo') THEN
		ALTER TYPE public.report_target_type ADD VALUE 'photo';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_type' AND e.enumlabel = 'account') THEN
		ALTER TYPE public.report_target_type ADD VALUE 'account';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_type' AND e.enumlabel = 'organization') THEN
		ALTER TYPE public.report_target_type ADD VALUE 'organization';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_type' AND e.enumlabel = 'event_organizator') THEN
		ALTER TYPE public.report_target_type ADD VALUE 'event_organizator';
	END IF;
END $ADD_REPORT_TARGET_TYPE_VALUES$;

do $ADD_REPORT_TARGET_SCOPE_VALUES$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_scope' AND e.enumlabel = 'photo') THEN
		ALTER TYPE public.report_target_scope ADD VALUE 'photo';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_scope' AND e.enumlabel = 'account') THEN
		ALTER TYPE public.report_target_scope ADD VALUE 'account';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_scope' AND e.enumlabel = 'organization') THEN
		ALTER TYPE public.report_target_scope ADD VALUE 'organization';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_scope' AND e.enumlabel = 'event_organizator') THEN
		ALTER TYPE public.report_target_scope ADD VALUE 'event_organizator';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_target_scope' AND e.enumlabel = 'all') THEN
		ALTER TYPE public.report_target_scope ADD VALUE 'all';
	END IF;
END $ADD_REPORT_TARGET_SCOPE_VALUES$;

do $ADD_REPORT_RESOLUTION_ACTION_VALUES$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_resolution_action' AND e.enumlabel = 'suspend_account') THEN
		ALTER TYPE public.report_resolution_action ADD VALUE 'suspend_account';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_resolution_action' AND e.enumlabel = 'suspend_organization') THEN
		ALTER TYPE public.report_resolution_action ADD VALUE 'suspend_organization';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_resolution_action' AND e.enumlabel = 'remove_organizator') THEN
		ALTER TYPE public.report_resolution_action ADD VALUE 'remove_organizator';
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_resolution_action' AND e.enumlabel = 'reset_avatar') THEN
		ALTER TYPE public.report_resolution_action ADD VALUE 'reset_avatar';
	END IF;
END $ADD_REPORT_RESOLUTION_ACTION_VALUES$;

ALTER TABLE public.content_reports DROP CONSTRAINT IF EXISTS content_reports_message_target_chk;

ALTER TABLE public.content_reports
	ADD COLUMN IF NOT EXISTS file_id uuid NULL,
	ADD COLUMN IF NOT EXISTS album_id uuid NULL,
	ADD COLUMN IF NOT EXISTS reported_account_id uuid NULL,
	ADD COLUMN IF NOT EXISTS organization_id uuid NULL,
	ADD COLUMN IF NOT EXISTS event_organizator_id uuid NULL;

DO $CONTENT_REPORTS_NEW_FKS$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'content_reports_album_fk') THEN
		ALTER TABLE public.content_reports
			ADD CONSTRAINT content_reports_album_fk FOREIGN KEY (album_id) REFERENCES public.media_albums(id) ON DELETE SET NULL;
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'content_reports_reported_account_fk') THEN
		ALTER TABLE public.content_reports
			ADD CONSTRAINT content_reports_reported_account_fk FOREIGN KEY (reported_account_id) REFERENCES public.accounts(id);
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'content_reports_organization_fk') THEN
		ALTER TABLE public.content_reports
			ADD CONSTRAINT content_reports_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id);
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'content_reports_event_organizator_fk') THEN
		ALTER TABLE public.content_reports
			ADD CONSTRAINT content_reports_event_organizator_fk FOREIGN KEY (event_organizator_id) REFERENCES public.event_organizators(id) ON DELETE SET NULL;
	END IF;
END $CONTENT_REPORTS_NEW_FKS$;

CREATE INDEX IF NOT EXISTS content_reports_file_id_idx ON public.content_reports (file_id);
CREATE INDEX IF NOT EXISTS content_reports_reported_account_idx ON public.content_reports (reported_account_id);
CREATE INDEX IF NOT EXISTS content_reports_organization_idx ON public.content_reports (organization_id);

ALTER TABLE public.file_album_rls
	ADD COLUMN IF NOT EXISTS hidden bool NOT NULL DEFAULT false,
	ADD COLUMN IF NOT EXISTS hidden_at timestamptz NULL,
	ADD COLUMN IF NOT EXISTS hidden_by uuid NULL;

DO $FILE_ALBUM_HIDDEN_BY_FK$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'file_album_rls_hidden_by_fk') THEN
		ALTER TABLE public.file_album_rls
			ADD CONSTRAINT file_album_rls_hidden_by_fk FOREIGN KEY (hidden_by) REFERENCES public.accounts(id);
	END IF;
END $FILE_ALBUM_HIDDEN_BY_FK$;

CREATE INDEX IF NOT EXISTS file_album_rls_hidden_idx ON public.file_album_rls (hidden) WHERE hidden = true;

UPDATE public.report_reasons
SET target_scope = 'all'::public.report_target_scope
WHERE code IN ('spam', 'other', 'illegal_content', 'threats', 'fraud', 'hate', 'sexual_exploitation');

INSERT INTO public.report_reasons (code, name, description, target_scope, severity, primary_queue, sort_order)
SELECT v.code, v.name, v.description, v.target_scope::public.report_target_scope, v.severity::public.report_severity, v.primary_queue::public.report_queue, v.sort_order
FROM (VALUES
	('inappropriate_photo', 'Недопустимое фото', 'Фото нарушает правила площадки', 'photo', 'community', 'organizers', 50),
	('fake_account', 'Поддельный / чужой профиль', 'Фейковый аккаунт или выдача себя за другого', 'account', 'community', 'platform', 60),
	('organizer_misconduct', 'Нарушения организатора', 'Организатор события нарушает правила', 'event_organizator', 'community', 'platform', 70),
	('inappropriate_organization', 'Нарушения организации', 'Организация нарушает правила площадки', 'organization', 'community', 'platform', 80)
) AS v(code, name, description, target_scope, severity, primary_queue, sort_order)
WHERE NOT EXISTS (
	SELECT 1 FROM public.report_reasons r WHERE r.code = v.code
);

do $ADD_APPLY_PENALTY_RESOLUTION$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_enum e JOIN pg_type t ON t.oid = e.enumtypid JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'report_resolution_action' AND e.enumlabel = 'apply_penalty') THEN
		ALTER TYPE public.report_resolution_action ADD VALUE 'apply_penalty';
	END IF;
END $ADD_APPLY_PENALTY_RESOLUTION$;

ALTER TABLE public.events
	ADD COLUMN IF NOT EXISTS cancelled_at timestamptz NULL,
	ADD COLUMN IF NOT EXISTS cancelled_by_account_id uuid NULL,
	ADD COLUMN IF NOT EXISTS cancel_source varchar(32) NULL,
	ADD COLUMN IF NOT EXISTS cancel_report_id uuid NULL;

DO $EVENTS_CANCEL_FKS$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'events_cancelled_by_fk') THEN
		ALTER TABLE public.events
			ADD CONSTRAINT events_cancelled_by_fk FOREIGN KEY (cancelled_by_account_id) REFERENCES public.accounts(id);
	END IF;
	IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'events_cancel_report_fk') THEN
		ALTER TABLE public.events
			ADD CONSTRAINT events_cancel_report_fk FOREIGN KEY (cancel_report_id) REFERENCES public.content_reports(id) ON DELETE SET NULL;
	END IF;
END $EVENTS_CANCEL_FKS$;

CREATE INDEX IF NOT EXISTS events_cancel_source_idx ON public.events (cancel_source) WHERE cancel_source IS NOT NULL;

DO $CREATE_MODERATION_PENALTY_TYPE$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM pg_type t JOIN pg_namespace n ON n.oid = t.typnamespace WHERE n.nspname = 'public' AND t.typname = 'moderation_penalty_type') THEN
		CREATE TYPE public.moderation_penalty_type AS ENUM (
			'suspend_account',
			'suspend_organization',
			'ban_event_create',
			'ban_event_participate',
			'ban_messaging',
			'ban_organize',
			'ban_from_event'
		);
	END IF;
END $CREATE_MODERATION_PENALTY_TYPE$;

CREATE TABLE IF NOT EXISTS public.moderation_penalties (
	id uuid DEFAULT uuid_generate_v4() NOT NULL,
	account_id uuid NULL,
	organization_id uuid NULL,
	event_id uuid NULL,
	report_id uuid NULL,
	penalty_type public.moderation_penalty_type NOT NULL,
	reason varchar(500) NULL,
	starts_at timestamptz DEFAULT now() NOT NULL,
	ends_at timestamptz NULL,
	revoked_at timestamptz NULL,
	revoked_by uuid NULL,
	lifted_at timestamptz NULL,
	created_by uuid NOT NULL,
	created_at timestamptz DEFAULT now() NOT NULL,
	CONSTRAINT moderation_penalties_pk PRIMARY KEY (id),
	CONSTRAINT moderation_penalties_account_fk FOREIGN KEY (account_id) REFERENCES public.accounts(id),
	CONSTRAINT moderation_penalties_organization_fk FOREIGN KEY (organization_id) REFERENCES public.organizations(id),
	CONSTRAINT moderation_penalties_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT moderation_penalties_report_fk FOREIGN KEY (report_id) REFERENCES public.content_reports(id) ON DELETE SET NULL,
	CONSTRAINT moderation_penalties_revoked_by_fk FOREIGN KEY (revoked_by) REFERENCES public.accounts(id),
	CONSTRAINT moderation_penalties_created_by_fk FOREIGN KEY (created_by) REFERENCES public.accounts(id),
	CONSTRAINT moderation_penalties_subject_chk CHECK (account_id IS NOT NULL OR organization_id IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS moderation_penalties_account_idx ON public.moderation_penalties (account_id);
CREATE INDEX IF NOT EXISTS moderation_penalties_organization_idx ON public.moderation_penalties (organization_id);
CREATE INDEX IF NOT EXISTS moderation_penalties_event_idx ON public.moderation_penalties (event_id);
CREATE INDEX IF NOT EXISTS moderation_penalties_active_idx ON public.moderation_penalties (penalty_type, ends_at)
	WHERE revoked_at IS NULL AND lifted_at IS NULL;
