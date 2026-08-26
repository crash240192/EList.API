-- =============================================================================
-- Manual cleanup: duplicate/spam events at the same lat/lng
-- =============================================================================
-- Keeps ONE event (oldest by create_date), deletes the rest and related rows.
--
-- Usage:
--   1. Run the discovery query below, copy lat/lng of the spam cluster.
--   2. Set v_lat / v_lng in the DO block.
--   3. Run inside a transaction; check NOTICE / counts; then COMMIT or ROLLBACK.
--
-- Does NOT delete files from filestorage (cover_image_id / album file_id).
-- =============================================================================


-- ---------------------------------------------------------------------------
-- Discovery: largest coordinate clusters
-- ---------------------------------------------------------------------------
/*
SELECT latitude, longitude, count(*) AS cnt,
       min(create_date) AS first_at,
       max(create_date) AS last_at,
       min(id::text) AS sample_id
FROM public.events
GROUP BY latitude, longitude
HAVING count(*) > 100
ORDER BY cnt DESC
LIMIT 20;
*/


BEGIN;

DO $$
DECLARE
	-- >>> Set the spam point coordinates
	v_lat numeric := 55.755800;   -- TODO
	v_lng numeric := 37.617300;   -- TODO
	v_eps numeric := 0.000001;    -- coordinate equality tolerance

	v_keep_id uuid;
	v_deleted_events int;
	v_total int;
BEGIN
	CREATE TEMP TABLE tmp_spam_events ON COMMIT DROP AS
	SELECT e.id, e.create_date, e.event_parameters_id
	FROM public.events e
	WHERE abs(e.latitude - v_lat) < v_eps
	  AND abs(e.longitude - v_lng) < v_eps;

	-- Optional: also require same name
	-- DELETE FROM tmp_spam_events t
	-- USING public.events e
	-- WHERE t.id = e.id AND e.name <> 'EXACT_SPAM_NAME';

	SELECT count(*) INTO v_total FROM tmp_spam_events;
	IF v_total < 2 THEN
		RAISE NOTICE 'Found % event(s) at point — nothing to delete.', v_total;
		RETURN;
	END IF;

	-- Keep the oldest event
	SELECT id INTO v_keep_id
	FROM tmp_spam_events
	ORDER BY create_date ASC, id ASC
	LIMIT 1;

	CREATE TEMP TABLE tmp_delete_events ON COMMIT DROP AS
	SELECT id, event_parameters_id
	FROM tmp_spam_events
	WHERE id <> v_keep_id;

	RAISE NOTICE 'Keep event %; will delete % of % events at (%, %)',
		v_keep_id,
		(SELECT count(*) FROM tmp_delete_events),
		v_total,
		v_lat,
		v_lng;

	-- -------------------------------------------------------------------------
	-- Break FK cycles: events.cancel_report_id <-> content_reports
	-- -------------------------------------------------------------------------
	UPDATE public.events e
	SET cancel_report_id = NULL
	WHERE e.id IN (SELECT id FROM tmp_delete_events)
	   OR e.cancel_report_id IN (
			SELECT r.id FROM public.content_reports r
			WHERE r.event_id IN (SELECT id FROM tmp_delete_events)
	   );

	-- -------------------------------------------------------------------------
	-- Moderation
	-- -------------------------------------------------------------------------
	DELETE FROM public.content_report_actions a
	WHERE a.report_id IN (
		SELECT r.id FROM public.content_reports r
		WHERE r.event_id IN (SELECT id FROM tmp_delete_events)
	);

	UPDATE public.moderation_penalties
	SET report_id = NULL
	WHERE report_id IN (
		SELECT r.id FROM public.content_reports r
		WHERE r.event_id IN (SELECT id FROM tmp_delete_events)
	);

	DELETE FROM public.moderation_penalties
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.content_reports
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	-- -------------------------------------------------------------------------
	-- Payments / tickets
	-- -------------------------------------------------------------------------
	DELETE FROM public.payment_webhook_events w
	WHERE w.order_id IN (
		SELECT o.id FROM public.orders o
		WHERE o.event_id IN (SELECT id FROM tmp_delete_events)
	);

	DELETE FROM public.refunds rf
	WHERE rf.order_id IN (
		SELECT o.id FROM public.orders o
		WHERE o.event_id IN (SELECT id FROM tmp_delete_events)
	);

	DELETE FROM public.tickets t
	WHERE t.event_id IN (SELECT id FROM tmp_delete_events)
	   OR t.order_id IN (
			SELECT o.id FROM public.orders o
			WHERE o.event_id IN (SELECT id FROM tmp_delete_events)
	   );

	DELETE FROM public.orders
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	-- -------------------------------------------------------------------------
	-- Chats: message -> conversation
	-- -------------------------------------------------------------------------
	UPDATE public.message m
	SET reply_to = NULL
	WHERE m.conversation_id IN (
		SELECT c.id FROM public.conversation c
		WHERE c.event_id IN (SELECT id FROM tmp_delete_events)
	);

	DELETE FROM public.message m
	WHERE m.conversation_id IN (
		SELECT c.id FROM public.conversation c
		WHERE c.event_id IN (SELECT id FROM tmp_delete_events)
	);

	DELETE FROM public.conversation
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	-- -------------------------------------------------------------------------
	-- Event albums (+ orphaned media_albums)
	-- -------------------------------------------------------------------------
	CREATE TEMP TABLE tmp_albums ON COMMIT DROP AS
	SELECT DISTINCT ear.album_id
	FROM public.event_album_rls ear
	WHERE ear.event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.file_album_rls
	WHERE album_id IN (SELECT album_id FROM tmp_albums);

	DELETE FROM public.event_album_parameters
	WHERE album_id IN (SELECT album_id FROM tmp_albums);

	DELETE FROM public.account_album_rls
	WHERE album_id IN (SELECT album_id FROM tmp_albums);

	DELETE FROM public.event_album_rls
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.media_albums ma
	WHERE ma.id IN (SELECT album_id FROM tmp_albums)
	  AND NOT EXISTS (SELECT 1 FROM public.event_album_rls x WHERE x.album_id = ma.id)
	  AND NOT EXISTS (SELECT 1 FROM public.account_album_rls x WHERE x.album_id = ma.id);

	-- -------------------------------------------------------------------------
	-- Simple event_id dependents
	-- -------------------------------------------------------------------------
	DELETE FROM public.notifications
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.participations
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.invitations
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.participants_white_list
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.participants_black_list
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.events_rating
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.persons_rating
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.event_type_rls
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	DELETE FROM public.event_organizators
	WHERE event_id IN (SELECT id FROM tmp_delete_events);

	-- -------------------------------------------------------------------------
	-- Events + orphaned event_parameters
	-- -------------------------------------------------------------------------
	CREATE TEMP TABLE tmp_params ON COMMIT DROP AS
	SELECT DISTINCT event_parameters_id AS id
	FROM tmp_delete_events
	WHERE event_parameters_id IS NOT NULL;

	DELETE FROM public.events
	WHERE id IN (SELECT id FROM tmp_delete_events);

	GET DIAGNOSTICS v_deleted_events = ROW_COUNT;

	DELETE FROM public.event_parameters ep
	WHERE ep.id IN (SELECT id FROM tmp_params)
	  AND NOT EXISTS (
			SELECT 1 FROM public.events e WHERE e.event_parameters_id = ep.id
	  );

	RAISE NOTICE 'Done. Deleted events: %; kept: %', v_deleted_events, v_keep_id;
END $$;

-- Spot-check before commit:
-- SELECT latitude, longitude, count(*) FROM public.events
-- GROUP BY 1, 2 HAVING count(*) > 100 ORDER BY 3 DESC LIMIT 20;

-- COMMIT;
ROLLBACK;
