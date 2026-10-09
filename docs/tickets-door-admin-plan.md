# Билеты на входе + админка + PDF (W6)

> Дата: 9 октября 2026 (сценарии: [tickets-door-scenarios.md](./tickets-door-scenarios.md))  
> Статус: **W6a готово**; **W6b в реализации** (stats + org summary); W6c–e дальше  
> База: `cursor/tickets-door-0b40` ← `tickets-integration-0b40` ← `develop`  
> Связано: [tickets-scenarios.md](./tickets-scenarios.md), [ticket-types-plan.md](./ticket-types-plan.md)  
> Вне scope сейчас: W4 фискализация (касса / API чеков ещё не готовы)

## 1. Зачем

Сейчас check-in уже есть точечно:

- API: `POST /api/orders/tickets/validate`, `POST /api/orders/tickets/check-in`
- Доступ: только **организатор события** (или platform moderator)
- UI: `TicketCheckInPanel` на `EventPage` (код + QR)

Не хватает:

- роли **билетёра** в организации (не полный менеджер/владелец);
- отдельной рабочей поверхности «на входе» (не карточка события);
- сводной статистики по мероприятию / типу (продано, погашено, осталось, pending…);
- печатной формы / PDF билета (G2b).

## 2. Роли организации

Текущий enum: `Owner` | `Manager`.

### Предложение

| Роль | Код | Права по билетам |
|------|-----|------------------|
| Owner | как сейчас | всё |
| Manager | как сейчас | орг. админка + события + билеты |
| **TicketTaker** (билетёр) | новый | только validate/check-in + live-счётчики по **назначенным** событиям; без payout/юр/удаления орг |

Правила:

1. Билетёр — член `organization_accounts` с ролью `TicketTaker`.
2. Доступ к check-in события: org события содержит аккаунт как Owner/Manager **или** TicketTaker **и** событие в allow-list билетёра (см. §3).
3. Platform moderator — по-прежнему полный обход для саппорта.

Альтернатива (если не хотим трогать enum сразу): отдельная таблица `event_ticket_staff (event_id, account_id, can_check_in)` без новой org-роли.  
**Рекомендация MVP W6:** роль org `TicketTaker` + назначение на события — проще объяснить в UI настроек организации.

## 3. Модель доступа к событию

Новая таблица (черновик):

```text
event_ticket_staff
  id uuid PK
  event_id uuid FK → events
  account_id uuid FK → accounts
  can_check_in bool DEFAULT true
  can_view_stats bool DEFAULT true
  create_date / update_date
  UNIQUE (event_id, account_id)
```

- Owner/Manager org события имеют implicit доступ без строки.
- TicketTaker — только при явной строке (или «все будущие события org» флагом на членстве — phase 2).

Расширить `AssertOrganizerCanManageTicketsAsync` → `AssertCanCheckInTicketsAsync` с этой логикой.

## 4. Новая страница UI — «Билеты» / Door desk

Маршрут (черновик): `/tickets/desk` или `/org/:orgId/tickets`.

### Экраны MVP

1. **Список мероприятий org** (активные / сегодня / архив) с бейджами:
   - продано / погашено / осталось capacity (если задана) / pending оплаты.
2. **Desk события** (основной экран на входе):
   - крупный ввод кода + QR-сканер (вынести/переиспользовать `TicketCheckInPanel`);
   - результат: статус, тип, holder (если не private), время check-in;
   - live counters: Issued / Used / Refund* / Pending orders;
   - разбивка по `ticketTypes`.
3. **Назначение билетёров** (только Owner/Manager): pick из членов org с ролью TicketTaker или promote + assign.

Не тащить payout / редактирование события на desk — одна задача: вход.

### Навигация

Пункт сайдбара «Билеты» виден, если у пользователя есть org с `CanSellTickets` **или** роль TicketTaker хотя бы в одной org.

## 5. API для статистики и staff

| Метод | Назначение |
|-------|------------|
| `GET /api/events/{eventId}/tickets/stats` | агрегаты: byStatus, byType, capacity remaining |
| `GET /api/organizations/{orgId}/events/ticket-summary` | список событий org с краткими счётчиками |
| `GET/PUT /api/events/{eventId}/ticket-staff` | назначение билетёров (Owner/Manager) |
| существующие validate / check-in | расширить auth (§3); в ответе уже есть `ticketTypeName` |

Агрегаты считать SQL `GROUP BY status, ticket_type_id` по `tickets` (+ optional join `orders` для Pending).

## 6. PDF / печатная форма (G2b в этой же волне)

### Содержание билета (одна сторона A6/A7 или экран print)

- Название события, дата/время, адрес  
- Тип билета, цена (0 → «Бесплатно»)  
- Код + QR (тот же payload, что в MyTickets)  
- Holder / «Электронный билет EList»  
- Order id (короткий) опционально  

### Реализация

| Вариант | Плюсы | Минусы |
|---------|-------|--------|
| **A. Client print** (`window.print` + CSS `@media print` / html2canvas→pdf) | быстро, без сервиса | слабее «официальный» PDF |
| **B. Server PDF** (QuestPDF / similar на API) | стабильный файл, ссылка «скачать» | пакет + нагрузка |

**MVP:** A на MyTickets («Печать / PDF») + общий layout-компонент `TicketPrintLayout`.  
**Phase 2:** B `GET /api/orders/tickets/{id}/pdf` для шаринга/e-mail.

QR payload не менять — тот же `parseTicketCodeFromText` / code string.

## 7. Декомпозиция PR

Полная матрица сценариев, юзкейсы и срезы **W6a–W6e**: [tickets-door-scenarios.md](./tickets-door-scenarios.md) §6–7.

| Срез | Содержимое | База |
|------|------------|------|
| API/UI-I | уже: merge W5 → `tickets-integration*` | develop |
| **W6a** | `TicketTaker` + `event_ticket_staff` + auth check-in + UI ролей/assign | door ← integration |
| **W6b** | stats + org ticket-summary | W6a |
| **W6c** | Hub + Desk UI + sidebar | W6a+W6b |
| **W6d** | Print / client PDF (параллельно) | door-ui |
| **W6e** | Undo, race-safe check-in, polish smoke | W6a–c |

W4 (чеки) не блокирует W6.

## 8. Критерии готовности W6 MVP

- [ ] Owner назначает билетёра на событие; билетёр гасит билет без прав менеджера
- [ ] Сайдбар «Билеты» → hub; desk глубже (скан → карточка → confirm Used)
- [ ] Undo для Owner/Manager (карточка / повторный скан / история)
- [ ] Org summary: продано / использовано / осталось по событиям
- [ ] `features:ticketDeskRevealHolder` default false
- [ ] MyTickets: «Печать» (системный print / Save as PDF)
- [ ] EventPage — CTA на desk

## 9. Решения продукта — CLOSED

См. [tickets-door-scenarios.md](./tickets-door-scenarios.md) §5 и §10.  
Старт реализации: **W6a**.
