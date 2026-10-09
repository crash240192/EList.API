-- Phase B: ticket_type_id обязателен на orders/tickets после backfill и CreateOrder по типу

-- Добить оставшиеся NULL (на случай заказов после Phase A без типа)
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

-- Если остались «сироты» без типа события — создать fallback-тип и привязать
INSERT INTO public.event_ticket_types (event_id, name, description, price, currency, capacity, sort_order, active)
SELECT DISTINCT o.event_id, 'Стандарт', NULL, 0, 'RUB', NULL, 0, true
FROM public.orders o
WHERE o.ticket_type_id IS NULL
	AND NOT EXISTS (
		SELECT 1 FROM public.event_ticket_types t WHERE t.event_id = o.event_id
	);

INSERT INTO public.event_ticket_types (event_id, name, description, price, currency, capacity, sort_order, active)
SELECT DISTINCT tk.event_id, 'Стандарт', NULL, 0, 'RUB', NULL, 0, true
FROM public.tickets tk
WHERE tk.ticket_type_id IS NULL
	AND NOT EXISTS (
		SELECT 1 FROM public.event_ticket_types t WHERE t.event_id = tk.event_id
	);

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

-- Удалить невозможные сироты (нет event_id / событие без типов после fallback — не должно остаться)
-- Перед NOT NULL: только если NULL нет
DO $$
BEGIN
	IF EXISTS (SELECT 1 FROM public.orders WHERE ticket_type_id IS NULL) THEN
		RAISE EXCEPTION 'orders.ticket_type_id still has NULL rows; cannot set NOT NULL';
	END IF;
	IF EXISTS (SELECT 1 FROM public.tickets WHERE ticket_type_id IS NULL) THEN
		RAISE EXCEPTION 'tickets.ticket_type_id still has NULL rows; cannot set NOT NULL';
	END IF;

	ALTER TABLE public.orders
		ALTER COLUMN ticket_type_id SET NOT NULL;
	ALTER TABLE public.tickets
		ALTER COLUMN ticket_type_id SET NOT NULL;
END $$;
