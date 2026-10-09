-- Типы билетов на мероприятие (W5 Phase A)
-- orders/tickets.ticket_type_id nullable до Phase B (CreateOrder всегда пишет тип)

CREATE TABLE IF NOT EXISTS public.event_ticket_types (
	id uuid NOT NULL DEFAULT public.uuid_generate_v4(),
	event_id uuid NOT NULL,
	name varchar(120) NOT NULL,
	description text NULL,
	price numeric(12, 2) NOT NULL DEFAULT 0,
	currency char(3) NOT NULL DEFAULT 'RUB',
	capacity int NULL,
	sort_order int NOT NULL DEFAULT 0,
	active bool NOT NULL DEFAULT true,
	create_date timestamptz NOT NULL DEFAULT now(),
	update_date timestamptz NOT NULL DEFAULT now(),
	CONSTRAINT event_ticket_types_pk PRIMARY KEY (id),
	CONSTRAINT event_ticket_types_event_fk FOREIGN KEY (event_id) REFERENCES public.events(id),
	CONSTRAINT event_ticket_types_price_chk CHECK (price >= 0),
	CONSTRAINT event_ticket_types_capacity_chk CHECK (capacity IS NULL OR capacity > 0)
);

CREATE INDEX IF NOT EXISTS event_ticket_types_event_id_idx
	ON public.event_ticket_types (event_id);
CREATE INDEX IF NOT EXISTS event_ticket_types_event_active_idx
	ON public.event_ticket_types (event_id, active);
CREATE INDEX IF NOT EXISTS event_ticket_types_event_price_idx
	ON public.event_ticket_types (event_id, price);

ALTER TABLE public.orders
	ADD COLUMN IF NOT EXISTS ticket_type_id uuid NULL;
ALTER TABLE public.tickets
	ADD COLUMN IF NOT EXISTS ticket_type_id uuid NULL;

DO $$
BEGIN
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'orders_ticket_type_fk'
	) THEN
		ALTER TABLE public.orders
			ADD CONSTRAINT orders_ticket_type_fk
			FOREIGN KEY (ticket_type_id) REFERENCES public.event_ticket_types(id);
	END IF;
	IF NOT EXISTS (
		SELECT 1 FROM pg_constraint WHERE conname = 'tickets_ticket_type_fk'
	) THEN
		ALTER TABLE public.tickets
			ADD CONSTRAINT tickets_ticket_type_fk
			FOREIGN KEY (ticket_type_id) REFERENCES public.event_ticket_types(id);
	END IF;
END $$;

CREATE INDEX IF NOT EXISTS orders_ticket_type_id_idx ON public.orders (ticket_type_id);
CREATE INDEX IF NOT EXISTS tickets_ticket_type_id_idx ON public.tickets (ticket_type_id);

-- Backfill: один тип «Стандарт» на каждое tickets_enabled событие без типов
INSERT INTO public.event_ticket_types (event_id, name, description, price, currency, capacity, sort_order, active)
SELECT
	e.id,
	'Стандарт',
	NULL,
	GREATEST(COALESCE(ep.cost, 0)::numeric(12, 2), 0),
	'RUB',
	NULL,
	0,
	true
FROM public.events e
INNER JOIN public.event_parameters ep ON ep.id = e.event_parameters_id
WHERE ep.tickets_enabled = true
	AND NOT EXISTS (
		SELECT 1 FROM public.event_ticket_types t WHERE t.event_id = e.id
	);

-- Проставить FK на заказах/билетах (первый/единственный тип события)
UPDATE public.orders o
SET ticket_type_id = t.id
FROM public.event_ticket_types t
WHERE o.ticket_type_id IS NULL
	AND t.event_id = o.event_id
	AND t.id = (
		SELECT t2.id
		FROM public.event_ticket_types t2
		WHERE t2.event_id = o.event_id
		ORDER BY t2.sort_order, t2.create_date
		LIMIT 1
	);

UPDATE public.tickets tk
SET ticket_type_id = t.id
FROM public.event_ticket_types t
WHERE tk.ticket_type_id IS NULL
	AND t.event_id = tk.event_id
	AND t.id = (
		SELECT t2.id
		FROM public.event_ticket_types t2
		WHERE t2.event_id = tk.event_id
		ORDER BY t2.sort_order, t2.create_date
		LIMIT 1
	);

-- Синхронизация cost = min(active price) для tickets_enabled
UPDATE public.event_parameters ep
SET cost = sub.min_price
FROM (
	SELECT e.event_parameters_id AS parameters_id,
		MIN(t.price)::double precision AS min_price
	FROM public.events e
	INNER JOIN public.event_ticket_types t ON t.event_id = e.id AND t.active = true
	INNER JOIN public.event_parameters ep2 ON ep2.id = e.event_parameters_id
	WHERE ep2.tickets_enabled = true
		AND e.event_parameters_id IS NOT NULL
	GROUP BY e.event_parameters_id
) sub
WHERE ep.id = sub.parameters_id;
