# W6 — вход, билетёр, админка, PDF: сценарии и декомпозиция

> Дата: 9 октября 2026  
> Статус: анализ / готов к реализации  
> База веток: `cursor/tickets-door-0b40` ← `tickets-integration-0b40` ← `develop`  
> UI: `cursor/tickets-door-ui-0b40` ← `tickets-integration-ui-0b40`  
> Краткий техплан: [tickets-door-admin-plan.md](./tickets-door-admin-plan.md)  
> Вне scope: W4 фискализация (касса не готова)

---

## 0. As-is (что уже есть)

| Область | Сейчас | Ограничение |
|---------|--------|-------------|
| Validate | `POST /api/orders/tickets/validate` | Только организатор события / platform mod |
| Check-in | `POST /api/orders/tickets/check-in` Issued→Used | То же; отказ на Used / RefundPending / ≠Issued |
| UI входа | `TicketCheckInPanel` на `EventPage` | Зарыто в карточке события; нет отдельного «рабочего» экрана |
| Роли org | `Owner` \| `Manager` | Нет билетёра; UI не меняет роль (add member → Manager) |
| Типы | `ticketTypeId` / `ticketTypeName` в ответах | На входе тип есть в API, UX desk слабый |
| Capacity | soft-hold при CreateOrder (pending+paid по типу / событию) | Нет публичного stats API для org |
| Счётчики | `GetTicketsByEventAsync` есть в репо | Нет агрегатов для UI |
| PDF / печать | QR на MyTickets | Нет печатной формы / PDF |
| Продажа | только org с `CanSellTickets` | Desk/staff проектируем **вокруг org**, не personal host |

Статусы билета: `Issued` → `Used` | `RefundPending` → `Refunded` | `Void`.  
Статусы заказа (для «продано / в ожидании»): `Pending` / `Authorized` / `Paid` / `Canceled` / …

---

## 1. Акторы

| Актор | Кто | Цель |
|-------|-----|------|
| **A1 Buyer / Holder** | купил / получил gift/transfer | Показать QR/код, распечатать PDF, попасть на событие |
| **A2 Org Owner** | `OrganizationMemberRole.Owner` | Настроить билетёров, видеть полную аналитику, undo при необходимости |
| **A3 Org Manager** | `Manager` | То же по операционке (без смены owner / критичных юр-настроек) |
| **A4 TicketTaker** | новая роль | Быстро проверять и гасить на входе; видеть live-счётчики **своих** событий |
| **A5 Event organizator (account)** | личный организатор без org | Сегодня билеты не продаёт (`CanSellTickets` у org) — **вне W6** |
| **A6 Platform moderator** | саппорт | Break-glass: validate/check-in / stats любого события |
| **A7 Door guest** | человек с бумажным/экранным билетом | Не логинится в desk; только показывает код/QR билетёру |

---

## 2. Юзкейсы (полный объём W6)

### UC-R — роли и доступ

| ID | Юзкейс | Актор | Предусловие | Результат |
|----|--------|-------|-------------|-----------|
| UC-R1 | Добавить члена org как билетёра | Owner/Manager | Аккаунт существует | Роль `TicketTaker`, без прав payout/юр |
| UC-R2 | Повысить/понизить роль (Manager ↔ TicketTaker) | Owner (Manager→TT ок; TT→Manager — Owner) | Член active | Роль обновлена; доступы пересчитаны |
| UC-R3 | Назначить билетёра на событие | Owner/Manager | Событие org, билетёр в org | Строка `event_ticket_staff` |
| UC-R4 | Снять билетёра с события | Owner/Manager | Есть назначение | Нет доступа к desk этого события |
| UC-R5 | Билетёр открывает список «мои входы» | TicketTaker | ≥1 назначение | Только назначенные события |
| UC-R6 | Owner/Manager открывает org ticket hub | Owner/Manager | Org can sell / есть ticket-события | Все ticket-события org |
| UC-R7 | Билетёр без назначения пытается check-in | TicketTaker | Нет staff-строки | 403 AccessError |
| UC-R8 | Бывший менеджер, ставший TT, теряет орг-админку | — | Роль сменена | Нет settings/payout; desk только по allow-list |

### UC-D — desk на входе

| ID | Юзкейс | Актор | Результат |
|----|--------|-------|-----------|
| UC-D1 | Открыть desk события (крупный UI) | A2–A4, A6 | Экран: код, QR, counters, last result |
| UC-D2 | Скан / ввод кода → **карточка билета** (ещё не Used) | A2–A4 | Показать тип/статус; кнопки «Отметить вход» / закрыть (validate без гашения) |
| UC-D3 | Подтвердить вход на карточке | A2–A4 | Issued→Used; counters++; audit who/when; на карточке появляется «Отменить вход» (только A2–A3) |
| UC-D4 | Скан QR (камера) → та же карточка | A2–A4 | Не гасить молча: сначала карточка, затем confirm (или явный one-tap confirm в UX) |
| UC-D5 | Повторный скан уже Used | A2–A4 | Карточка «уже на площадке» + время/кто; Owner/Manager — «Отменить вход» |
| UC-D6 | Скан чужого события | A2–A4 | Отказ «другой event» |
| UC-D7 | Скан RefundPending / Refunded / Void | A2–A4 | Отказ с понятным текстом |
| UC-D8 | Два сканера одновременно на один код | A2–A4 | Один success, второй «уже использован» (гонка) |
| UC-D9 | Offline / нет сети | A2–A4 | MVP: явная ошибка сети; **offline queue — phase 2** |
| UC-D10 | Смена события на desk (мульти-вход) | A4 с несколькими назначениями | Быстрый switch без ухода в EventPage |

### UC-S — статистика и админка

| ID | Юзкейс | Актор | Метрики |
|----|--------|-------|---------|
| UC-S1 | Сводка события | A2–A4* | См. §3 словарь метрик |
| UC-S2 | Сводка по типам | A2–A4* | per `ticketType`: sold, used, remaining capacity |
| UC-S3 | Список событий org | A2–A3 | Карточки с sold/used/pending |
| UC-S4 | Live refresh на desk | A2–A4 | Poll 5–15s или refresh после check-in |
| UC-S5 | Экспорт CSV (phase 2) | A2–A3 | Список билетов события |
| UC-S6 | История операций с билетами события | A2–A3 | Лента/таблица: validate/check-in/undo; открыть карточку билета → undo |

\*TicketTaker: stats только если `can_view_stats` на staff (default true); без PII holder — см. §5.

### UC-P — печать / PDF

| ID | Юзкейс | Актор | Результат |
|----|--------|-------|-----------|
| UC-P1 | Печать одного билета из MyTickets | A1 | Print layout: event, type, code, QR → `window.print` |
| UC-P2 | «PDF» через системный диалог | A1 | В print-dialog выбрать «Сохранить как PDF» (отдельной lib в MVP нет) |
| UC-P3 | Печать после gift/transfer | A1 (новый holder) | Актуальный holder + тот же code |
| UC-P4 | Печать из desk (дубликат для гостя) | A2–A3 | Phase 2; MVP не обязателен |
| UC-P5 | Wallet pass / Apple/Google | A1 | **Вне W6** (после PDF) |

### UC-X — краевые / админ

| ID | Юзкейс | Решение MVP |
|----|--------|-------------|
| UC-X1 | Undo check-in (Used→Issued) | Owner/Manager: с открытой карточки / повторный скан / из истории; audit; не TT |
| UC-X2 | Событие отменено / tickets выключены | Desk read-only validate; check-in запрещён |
| UC-X3 | Тип soft-deleted, билеты уже выданы | Check-in по коду работает; тип в ответе с именем |
| UC-X4 | Capacity типа исчерпан, но Issued ещё не Used | Counters: remaining seats = capacity − reserved; «на площадке» = Used |
| UC-X5 | Privacy holder | Пока `ticketDeskRevealHolder=false` — desk API без login/ФИО holder |

---

## 3. Словарь метрик (единый)

Чтобы UI и API не разъехались:

| Ключ | Определение |
|------|-------------|
| `ordersPending` | Σ quantity заказов Pending+Authorized по событию (и по типу) |
| `sold` / `issued` | число билетов Status=Issued **+** Used (ещё «куплены», не refunded) — **уточнение ниже** |
| `issuedOpen` | Status=Issued (ещё не на площадке) |
| `used` | Status=Used |
| `refundPending` | Status=RefundPending |
| `refunded` | Status=Refunded |
| `void` | Status=Void |
| `reserved` | как CreateOrder: Σ qty Pending+Authorized+Paid по типу (soft-hold) |
| `capacity` | `event_ticket_types.capacity` или null (=∞) |
| `remaining` | если capacity set: `max(0, capacity − reserved)`, иначе null |
| `checkedInToday` | Used с `checkedInAt` в локальных сутках события (nice-to-have) |

**Рекомендация для бейджей «Продано» на hub:**  
`sold = count(tickets where status ∈ {Issued, Used, RefundPending})`  
(Refunded/Void не считаем проданными.)  
«На площадке» = `used`. «Ещё войдут» = `issuedOpen`.

---

## 4. Матрица сценариев приёмки

### Роли (R)

| # | Сценарий | Ожидание |
|---|----------|----------|
| R1 | Owner добавляет TT в org | Роль TicketTaker; нет доступа к payout |
| R2 | Owner назначает TT на Event E | TT видит E в desk list |
| R3 | TT check-in на E | Success |
| R4 | TT check-in на событие без назначения | 403 |
| R5 | Manager check-in без staff-строки | Success (implicit) |
| R6 | Снятие назначения | TT теряет E |
| R7 | Platform mod check-in любого E | Success |

### Desk (D)

| # | Сценарий | Ожидание |
|---|----------|----------|
| D1 | Validate Issued | OK, status Issued, type name |
| D2 | Скан Issued → confirm → Used | Used + checkedInAt/By; на карточке Undo (Owner/Manager) |
| D3 | Повторный скан Used | Карточка «уже использован» + Undo для Owner/Manager |
| D4 | QR → карточка → confirm | Без тихого auto-check-in |
| D5 | Код другого event | Ошибка |
| D6 | RefundPending | Ошибка |
| D7 | Гонка двух клиентов | Ровно один Used |
| D8 | После check-in counters used++ | UI обновлён |

### Stats (S)

| # | Сценарий | Ожидание |
|---|----------|----------|
| S1 | 10 Issued, 3 Used, capacity 20 | issuedOpen=10, used=3, remaining=7 (если reserved=13) |
| S2 | Pending×2 qty | ordersPending учитывается в remaining |
| S3 | Два типа VIP/Std | Разбивка byType корректна |
| S4 | TT без can_view_stats | Desk без цифр / 403 на stats |

### PDF (P)

| # | Сценарий | Ожидание |
|---|----------|----------|
| P1 | Print Issued билета | QR сканируется тем же парсером |
| P2 | Save as PDF из print-dialog | Файл/превью читаемы, код/QR рабочие |
| P3 | После transfer печатает новый holder | Код тот же, UI «мой билет» |

### Undo (U) — если включаем в MVP

| # | Сценарий | Ожидание |
|---|----------|----------|
| U1 | Owner undo Used | → Issued, checkedIn* cleared |
| U2 | TT undo | 403 |

---

## 5. Продуктовые решения (зафиксировано 9.10.2026)

| Тема | Решение |
|------|---------|
| Модель доступа | Org role `TicketTaker` **+** `event_ticket_staff` allow-list |
| Implicit check-in | Owner/Manager org события — всегда; TT — только allow-list |
| **PII holder на desk** | **Серверный флаг** `features:ticketDeskRevealHolder` в `appsettings` (и/или env). **Default `false`**: validate/check-in/desk ticket DTO **без** login/ФИО. При `true` API начинает отдавать holder-поля (для стенда/org-политики). Не per-user UI-toggle в MVP. |
| **Undo check-in** | **Да.** Поток desk: скан QR → карточка подтверждения → «Отметить вход» (Issued→Used). Пока карточка открыта — «Отменить вход»; повторный скан того же Used-билета открывает карточку с «Отменить вход»; плюс **история операций** по событию → открыть билет → undo. Кто может: Owner/Manager (и mod); **TicketTaker — нет**. |
| Offline | Нет в MVP; явный offline banner |
| **Печать / PDF** | MVP: кнопка **«Печать»** → системный диалог браузера (принтер или «Сохранить как PDF»). Отдельная lib / server PDF — phase 2. |
| Wallet | Вне W6 |
| Personal events | Вне W6 (нет CanSellTickets) |
| **Навигация** | Сайдбар: **«Билеты»** → hub (события, счётчики, назначения). **Контроль входа (desk)** — глубже: `/tickets/desk?eventId=…` из карточки события hub / CTA на EventPage. |
| EventPage panel | Thin entry «Рабочее место / контроль входа» → desk; не дублировать полный UX |
| Экспорт CSV | Phase 2 |
| Мульти-тип в одном QR | Нет (1 билет = 1 код) |

---

## 6. Список доработок (backlog)

### B1. Домен и доступ (API)

1. Enum `OrganizationMemberRole.TicketTaker` (+ Db enum / миграция CHECK или int).  
2. API смены роли члена (`PUT .../members/role`) — сейчас фактически только add→Manager.  
3. Таблица `event_ticket_staff` + CRUD.  
4. `AssertCanCheckInTicketsAsync` / `AssertCanViewTicketStatsAsync` вместо «только organizator».  
5. Учесть org organizator события vs account organizator (tickets → org path).  
6. Опционально: `UndoCheckInAsync`.  
7. Усилить check-in против гонки (conditional update `WHERE status=Issued`).

### B2. Статистика (API)

8. `GET /api/events/{eventId}/tickets/stats` — totals + byType + byStatus.  
9. `GET /api/organizations/{orgId}/events/ticket-summary` — список для hub.  
10. `GET /api/events/{eventId}/tickets` (paged, filters) — для Owner/Manager admin table.  
11. Reuse reserved/capacity логики CreateOrder (не дублировать формулы).

### B3. Desk UI

12. Маршрут `/tickets/desk` (+ `?eventId=`).  
13. Hub: выбор org (если несколько) → список событий с бейджами.  
14. Desk screen: код, QR, last result, counters, byType.  
15. Назначение staff (модалка на событии / org settings).  
16. Управление ролью TT в `OrganizationsSettingsPanel` / members.  
17. Пункт сайдбара «Билеты» по capability.  
18. EventPage: CTA «Рабочее место на входе» вместо/рядом с панелью.  
19. Состояния ошибок (уже used / wrong event / network) — крупные, для шумного входа.  
20. Accessibility: крупный шрифт, контраст, вибро/звук success/fail (nice).

### B4. PDF / печать (UI ± API)

21. `TicketPrintLayout` (event, type, when, where, code, QR).  
22. MyTickets: «Печать» (`window.print` + `@media print`; PDF = системный Save as PDF).  
23. ~~Отдельная PDF-lib~~ — не в MVP.  
24. Единый QR payload с текущим `parseTicketCodeFromText`.  
25. Phase 2: server `GET /api/orders/tickets/{id}/pdf` / one-click download.

### B5. Документация / ops

26. Обновить SERVICE / tickets-scenarios (H1/H2/G2b).  
27. Swagger-описания новых endpoints.  
28. Чеклист приёмки §4 в CI/smoke script (хотя бы API).

### B6. Phase 2 (не блокируют MVP, но «полный объём» продукта)

29. Offline queue + sync.  
30. CSV export.  
31. Server PDF + e-mail ticket.  
32. Wallet pass.  
33. «Все будущие события org» для TT без per-event assign.  
34. Desk duplicate print for guest.  
35. WebSocket live counters.

---

## 7. Декомпозиция на поставку

### Принцип

Каждый вертикальный срез: **миграция/API → UI → smoke**.  
База всегда `tickets-door-*` ← integration ← develop.

```text
W6a Roles+Auth ──┐
                 ├── W6c Desk UI ── W6e Polish/smoke
W6b Stats API  ──┤
                 │
W6d PDF/Print ───┘ (параллельно с W6a–c)
```

### Срезы (PR)

| Срез | API | UI | Критерий готово |
|------|-----|-----|-----------------|
| **W6a** | TicketTaker + staff table + role API + check-in auth | Members: роль TT; assign staff на событие | R1–R7 |
| **W6b** | stats + org summary (+ optional tickets list) | — (можно stub JSON) | S1–S3 API |
| **W6c** | — (consumes a+b) | Сайдбар «Билеты» → hub; desk глубже; карточка confirm; EventPage CTA | D1–D8, S1 UI |
| **W6d** | — | Print layout + «Печать» на MyTickets | P1–P3 |
| **W6e** | Undo API + история операций + race-safe check-in; флаг `ticketDeskRevealHolder` | Undo на карточке/истории; poll counters | U1–U2, D3, D7 |

### Оценка сложности (техн., не календарь)

| Срез | Инвазивность | Зависимости |
|------|--------------|-------------|
| W6a | Средняя (enum+migration+auth path) | integration |
| W6b | Низкая–средняя (агрегации SQL) | W6a для auth на stats |
| W6c | Средняя–высокая (новая страница IA) | W6a + W6b |
| W6d | Низкая (UI-only MVP) | integration; не ждёт W6a |
| W6e | Низкая | W6a–c |

**Параллель:** W6d можно вести сразу на `tickets-door-ui` без ожидания ролей.  
**Критический путь полного объёма:** W6a → W6b → W6c → W6e; W6d сбоку.

### Порядок файлов / зон (ориентир)

**W6a API:**  
`OrganizationMemberRole` (Models+Db) → migration → OrganizationsService role update → `event_ticket_staff` provider/repo → OrdersService assert → Controllers.

**W6a UI:**  
`organization/types` + api → OrganizationsSettingsPanel members → event staff modal → capability helper.

**W6b API:**  
OrdersDataProvider aggregates → OrdersService/EventsController or OrganizationsController.

**W6c UI:**  
`pages/tickets-desk/*` → router → sidebar → reuse QrScanner/TicketCheckInPanel (refactor to shared).

**W6d UI:**  
`features/tickets/TicketPrintLayout` → MyTicketsPage actions → print CSS.

---

## 8. Негативные / злоупотребления

| Риск | Митигация |
|------|-----------|
| TT получает Manager по ошибке | Явный confirm при смене роли; audit |
| Утечка PII holder на входе | Default hide для TT |
| Перебор кодов | Rate limit check-in/validate per account+event (phase 2 / существующий antiflood) |
| Staff на чужую org | FK + проверка membership той же org, что organizator события |
| PDF с чужим билетом | Только holder или org Owner/Manager |

---

## 9. Критерии «полный объём W6» (done)

- [ ] Роль TicketTaker в org + назначение на события  
- [ ] Desk `/tickets/desk`: validate + check-in + QR + counters + byType  
- [ ] Hub org: sold / used / remaining / pending по событиям  
- [ ] Auth: TT только allow-list; Owner/Manager implicit; mod break-glass  
- [ ] Undo check-in для Owner/Manager  
- [ ] Race-safe check-in  
- [ ] MyTickets: «Печать» (системный print / Save as PDF), QR рабочий  
- [ ] Сайдбар «Билеты» → hub; desk глубже по eventId  
- [ ] EventPage ведёт на desk  
- [ ] Docs + smoke R/D/S/P  

Phase 2 (offline, CSV, server PDF, wallet, auto-assign all events) — отдельный backlog после приёмки.

---

## 10. Решения продукта — CLOSED

| # | Вопрос | Решение (9.10.2026) |
|---|--------|---------------------|
| 1 | Undo | **Да.** Карточка после скана → confirm Used → Undo на открытой карточке / повторный скан / история. Owner/Manager only. |
| 2 | Holder PII | Флаг API `features:ticketDeskRevealHolder`, **default false** (данные holder не отдаются). |
| 3 | PDF | Системный **«Печать»** / Save as PDF; без отдельной lib в MVP. |
| 4 | Меню | Пункт **«Билеты»**; контроль входа — внутри (desk). |

**W6a в работе** на `tickets-door-*`: миграция M13, `TicketTaker`, `event_ticket_staff`, role/staff API, check-in auth, UI ролей/assign.
