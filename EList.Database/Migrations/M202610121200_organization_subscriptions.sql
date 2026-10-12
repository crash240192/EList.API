-- Подписка пользователя на организацию (односторонняя: org → никто не подписывается)

CREATE TABLE IF NOT EXISTS public.organization_subscriptions (
	id uuid NOT NULL DEFAULT uuid_generate_v4(),
	subscriber_id uuid NOT NULL,
	organization_id uuid NOT NULL,
	notify_event_created boolean NOT NULL DEFAULT true,
	CONSTRAINT organization_subscriptions_pk PRIMARY KEY (id),
	CONSTRAINT organization_subscriptions_subscriber_fk
		FOREIGN KEY (subscriber_id) REFERENCES public.accounts(id),
	CONSTRAINT organization_subscriptions_organization_fk
		FOREIGN KEY (organization_id) REFERENCES public.organizations(id) ON DELETE CASCADE,
	CONSTRAINT organization_subscriptions_pair_uidx UNIQUE (subscriber_id, organization_id)
);

CREATE INDEX IF NOT EXISTS organization_subscriptions_subscriber_id_idx
	ON public.organization_subscriptions (subscriber_id);
CREATE INDEX IF NOT EXISTS organization_subscriptions_organization_id_idx
	ON public.organization_subscriptions (organization_id);
CREATE INDEX IF NOT EXISTS organization_subscriptions_org_notify_event_idx
	ON public.organization_subscriptions (organization_id)
	WHERE notify_event_created = true;
