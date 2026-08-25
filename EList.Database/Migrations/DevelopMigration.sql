-- =============================================================================
-- EList DevelopMigration.sql - Migration 2 (incremental upgrade)
--
-- Apply AFTER InitialDatabase.sql on existing production/develop databases that
-- already ran the legacy combined schema. Fresh installs only need migration 1.
-- =============================================================================

-- =============================================================================
-- Organizations extension + legal + payments
-- =============================================================================

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


-- =============================================================================
-- Content moderation + events cancel + penalties
-- =============================================================================

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

-- =============================================================================
-- Field-level encryption (column widen + hash indexes)
-- =============================================================================

-- ---------------------------------------------------------------------------
-- Field-level encryption for personal data (ciphertext in-place + blind hash)
-- ---------------------------------------------------------------------------
ALTER TABLE public.contact_data
	ALTER COLUMN value TYPE text;

ALTER TABLE public.contact_data
	ADD COLUMN IF NOT EXISTS value_hash varchar(128) NULL;

CREATE INDEX IF NOT EXISTS contact_data_value_hash_idx ON public.contact_data (value_hash);

ALTER TABLE public.person_info
	ALTER COLUMN first_name TYPE text,
	ALTER COLUMN last_name TYPE text,
	ALTER COLUMN patronymic TYPE text;

ALTER TABLE public.person_info
	ALTER COLUMN birthdate TYPE text USING birthdate::text;

ALTER TABLE public.organization_legal
	ALTER COLUMN inn TYPE text,
	ALTER COLUMN ogrn TYPE text,
	ALTER COLUMN kpp TYPE text,
	ALTER COLUMN legal_address TYPE text,
	ALTER COLUMN head_name TYPE text,
	ALTER COLUMN head_basis TYPE text;

ALTER TABLE public.organization_legal
	ADD COLUMN IF NOT EXISTS inn_hash varchar(128) NULL;

CREATE INDEX IF NOT EXISTS organization_legal_inn_hash_idx ON public.organization_legal (inn_hash);
