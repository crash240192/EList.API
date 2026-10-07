# Типы билетов — план реализации (MVP)

> Дата: 7 октября 2026  
> Статус: план, код не начат  
> Зависит от: W1–W3 (заказы / gift / QR) ✅; W4 (54-ФЗ) — независимо, можно параллелить после ТП  
> Связано: [tickets-scenarios.md](./tickets-scenarios.md), [SERVICE.md](./SERVICE.md) §7

## 1. Продуктовые решения (зафиксировано)

| Решение | Выбор |
|---------|--------|
| Типов на заказ | **Один** `ticketTypeId` + `quantity` |
| Цена события | **Не задаётся вручную**. Считается из активных типов |
| Карточки / витрина | Диапазон: «Бесплатно» / «от X ₽» / «X–Y ₽» |
| Поиск по цене | Фильтр по **ценам типов билетов**, не по `event_parameters.cost` |
| Смешение free/paid типов | Разрешено на одном событии |
| PDF / Wallet pass | Вне MVP (бэклог G2 rest) |
| Несколько типов в одном заказе | Вне MVP |

### Семантика фильтра `price` (как сейчас в UI)

UI сегодня: `price=0` → «Бесплатно»; `price=N` → «до N ₽».

После типов:

| Фильтр | Условие попадания в выборку |
|--------|------------------------------|
| `price = 0` | Есть **хотя бы один** активный тип с `price = 0`, **или** билеты выкл. и участие бесплатное (`ticketsEnabled=false`, участие без оплаты) |
| `price = N > 0` | Есть активный тип с `price ≤ N`, **или** (без билетов) legacy/`cost` ≤ N для «на месте» |
| `price` не задан | без ценового фильтра |

Для событий **с** `ticketsEnabled=true` цена участия = цены типов; `event_parameters.cost` больше не источник истины (см. §4 миграция).

---

## 2. Модель данных

### Новая таблица `event_ticket_types`

| Колонка | Тип | Описание |
|---------|-----|----------|
| `id` | uuid PK | |
| `event_id` | uuid FK → events | |
| `name` | varchar(120) | «Стандарт», «VIP» |
| `description` | text null | кратко |
| `price` | numeric(12,2) ≥ 0 | цена за 1 билет |
| `currency` | char(3) default RUB | как у orders |
| `capacity` | int null | лимит мест **этого** типа; null = без лимита типа |
| `sort_order` | int | порядок в UI |
| `active` | bool default true | скрыть тип с продажи |
| `create_date` / `update_date` | timestamptz | |

Индексы: `(event_id)`, `(event_id, active)`, `(event_id, price)` для поиска.

### Изменения существующих таблиц

| Таблица | Изменение |
|--------|-----------|
| `orders` | `ticket_type_id uuid NOT NULL` FK → `event_ticket_types` (после backfill) |
| `tickets` | `ticket_type_id uuid NOT NULL` FK (денормализация для UI / check-in) |
| `event_parameters.cost` | **Deprecated** как источник цены при `tickets_enabled`. Оставить колонку: синхронизировать = `min(active types.price)` для совместимости API/карточек на переходный период; либо хранить null и отдавать derived в DTO |

**Рекомендация MVP:** при сохранении типов писать в `event_parameters.cost = MIN(active.price)` (и при 0 типов с tickets — валидация ошибки). В ответах API дополнительно отдавать `priceMin` / `priceMax` / `ticketTypes[]`, чтобы UI мог показать диапазон без догадок.

---

## 3. Derived price на событии

```
activeTypes = ticket_types WHERE event_id AND active
priceMin = MIN(price)
priceMax = MAX(price)

display:
  !ticketsEnabled        → старое поведение («на месте» / cost / Бесплатно)
  ticketsEnabled && empty types → ошибка конфигурации (не публиковать)
  priceMin == 0 && priceMax == 0 → «Бесплатно»
  priceMin == priceMax          → «X ₽»
  else                          → «от priceMin ₽» или «priceMin–priceMax ₽»
```

Карточки списка: достаточно `priceMin` (+ опционально `priceMax`).  
Страница события: полный список типов.

---

## 4. Миграция существующих данных

События с `tickets_enabled = true` и текущим `cost`:

1. Создать один тип «Стандарт» (или «Билет») с `price = cost` (0 → бесплатный тип).
2. Проставить `orders.ticket_type_id` / `tickets.ticket_type_id` на этот тип по `event_id`.
3. События без билетов — типов нет; поиск по `cost` как сейчас.

События с `tickets_enabled = false` не трогаем.

---

## 5. Матрица сценариев (MVP)

| # | Сценарий | Ожидание |
|---|----------|----------|
| T1 | Org включает билеты, добавляет 1 тип price=0 | Публикация ок; покупка → PaidImmediately |
| T2 | Два типа: 0 ₽ и 500 ₽ | Карточка «от 0 ₽» / «0–500 ₽»; buy — выбор типа |
| T3 | Три типа 300/500/1000 | Карточка «от 300 ₽» или «300–1000 ₽» |
| T4 | Заказ: тип VIP × 2 | amount = 2 × VIP.price; билеты с `ticket_type_id` |
| T5 | Заказ без `ticketTypeId` | 400; если ровно один активный тип — можно default (опционально) |
| T6 | Тип inactive | Не в buy UI; старые билеты валидны |
| T7 | Capacity типа 10, уже 10 sold/held | Create order → EventIsFull (по типу) |
| T8 | Общий maxPersons и лимит типа | Оба проверяются (AND) |
| T9 | Изменение цены типа после продаж | Новые заказы — новая цена; старые orders не пересчитываются |
| T10 | Удаление типа с выданными билетами | Запрет hard-delete; только `active=false` |
| T11 | Поиск price=0 | События с типом 0 ₽ **или** free join без билетов |
| T12 | Поиск price≤500 | Событие, у которого есть активный тип ≤ 500 |
| T13 | Refund одного билета из заказа VIP×3 | Как сейчас по ticketIds; тип не мешает |
| T14 | Transfer / check-in / QR | Без изменений UX; тип можно показать лейблом |
| T15 | Gift buy (уже participant) | Create order с выбранным типом |
| T16 | Публикация ticketsEnabled без типов | Валидация: нужен ≥1 активный тип |
| T17 | Редактирование: добавить/убрать тип | Sync `cost = min`; invalidate кэш списка |
| T18 | Событие без билетов, cost «на месте» | Типов нет; фильтр по `parameters.cost` |

Вне MVP: корзина из нескольких типов; early-bird окна; персональные квоты; PDF.

---

## 6. API (черновик контрактов)

### Типы на событии

- `GET /api/events/{eventId}/ticket-types` — публично/участникам список активных (org видит и inactive при edit).
- Запись через существующий assign parameters / create event payload:

```json
"ticketTypes": [
  { "id": null, "name": "Стандарт", "price": 0, "capacity": null, "sortOrder": 0, "active": true },
  { "id": "...", "name": "VIP", "price": 1500, "capacity": 20, "sortOrder": 1, "active": true }
]
```

Replace-by-event (diff: create / update / soft-deactivate missing).

### Заказ

```json
POST /api/orders
{
  "eventId": "...",
  "ticketTypeId": "...",
  "quantity": 2,
  "idempotencyKey": "..."
}
```

Цена **только** с сервера из типа (игнор цены с клиента).

### Ответ события / list item

```json
"parameters": {
  "ticketsEnabled": true,
  "cost": 300,
  "priceMin": 300,
  "priceMax": 1000
},
"ticketTypes": [ /* optional on full get; list может только min/max */ ]
```

`cost` = `priceMin` для обратной совместимости старых клиентов.

### Поиск

`EventsSearchRequest.Price` — логика §1; SQL/linq2db:

```
ticketsEnabled && EXISTS type active AND price <= :price
OR !ticketsEnabled && (cost IS NULL OR cost <= :price)
```

для `price=0` — `price = 0` у типа / cost.

---

## 7. UI

| Экран | Изменение |
|-------|-----------|
| Create / edit event | При «Билеты (онлайн)»: редактор типов (имя, цена, capacity, порядок, вкл/выкл). Поле «Стоимость» скрыть или read-only «от типов» |
| Event card / list | `formatEventListItemPrice(min, max)` |
| FilterBar | Без смены UX; бэкенд меняет семантику |
| BuyTicketModal | Выбор типа (radio) → qty → оплата |
| My tickets | Лейбл типа рядом с кодом/QR |
| Check-in | Опционально показать имя типа в результате validate |

---

## 8. Декомпозиция задач

### Phase A — схема и домен (API) ✅

1. ✅ Миграция FluentMigrator: `event_ticket_types` + FK на orders/tickets (**nullable** до Phase B).
2. ✅ DTO / repository / data provider CRUD типов.
3. ✅ Backfill: 1 тип «Стандарт» на tickets_enabled; проставить FK на orders/tickets.
4. ✅ Sync `parameters.cost = min(active)` при save типов; `priceMin`/`priceMax` в get parameters.
5. ✅ Валидация: ticketsEnabled ⇒ ≥1 active type; price ≥ 0; capacity > 0 if set.
6. ✅ `GET /api/events/{eventId}/ticket-types`; replace через `EventParametersRequest.TicketTypes`.

### Phase B — заказ и capacity (API)

6. `CreateOrderRequest.TicketTypeId`; цена и описание платежа из типа.
7. `CountReserved…` по типу (+ общий maxPersons).
8. Fulfill: писать `ticket_type_id` на билеты.
9. Refund / transfer / check-in — прокинуть тип в ответы (без смены логики).

### Phase C — поиск и ответы (API)

10. `priceMin` / `priceMax` в event get + short search.
11. Переписать фильтр `Price` в `EventsDataProvider` (§1).
12. Документация SERVICE / tickets-scenarios (G2 → в работе).

### Phase D — UI

13. Редактор типов в CreateEventPage (+ шаблоны, если templates хранят parameters).
14. Карточки / EventPage: диапазон цены.
15. BuyTicketModal: выбор типа.
16. MyTickets: имя типа.
17. Прогон сценариев T1–T18 (smoke).

### Порядок поставки PR

| PR | Содержимое | База |
|----|------------|------|
| API-A | Phase A+B (схема + order) | develop / tickets stack |
| API-B | Phase C (search + DTO) | API-A |
| UI-A | Phase D | API-B + UI tickets stack |

Можно объединить API-A+B в один PR, если объём терпимый.

---

## 9. Риски и краевые случаи

- **Шаблоны событий** (`event_templates`) — если сериализуют `cost`, добавить `ticketTypes` в snapshot.
- **Старые клиенты** смотрят только `cost` — sync min-price обязателен на переходный период.
- **Фильтр «до N»** при типах 100 и 5000: событие попадёт при N=200 (есть тип ≤200) — это ок для «есть доступный билет в бюджете»; альтернатива «все типы ≤ N» хуже для витрины. **Зафиксировать: EXISTS тип ≤ N.**
- **Организатор без тарифа** с costLimit=0: сейчас только бесплатные события — типы тоже только price=0 (валидация по tariff costLimit).
- **Гонки capacity** — как сейчас soft-hold; отдельная задача из checklist.

---

## 10. Критерии готовности MVP

- [ ] Org может завести ≥2 типа и опубликовать
- [ ] Покупка только с выбранным типом; сумма верная
- [ ] Карточка показывает диапазон / «от»
- [ ] Поиск «Бесплатно» / «до N» учитывает типы
- [ ] Backfill старых ticket-событий не ломает заказы
- [ ] Soft-delete типа с проданными билетами
- [ ] `npm run build` + `dotnet build` + smoke T1, T4, T11, T12
