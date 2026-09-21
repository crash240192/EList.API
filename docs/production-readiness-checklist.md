# EList 3.0.1 — Чеклист готовности к продакшн-релизу

> Первичный аудит: 31 августа 2026  
> Предыдущая актуализация: 3 сентября 2026  
> **Пересборка: 14 сентября 2026** (`develop` + tickets API + исходники `Agreements/`)  
> Продуктовое описание: [SERVICE.md](./SERVICE.md)  
> Юр. ревью билетов: [legal-ticketing-review.md](./legal-ticketing-review.md)

### Scope soft launch (рекомендуемый)

**Социальная платформа событий** с выключенной продажей билетов (`features.ticketSalesEnabled=false`).  
Билетный контур можно включать на стенде; в prod — только после закрытия юр. развилок и реального ЮKassa split.

---

## 1. Обзор готовности (14.09.2026)

| Категория | 03.09 | 14.09 | Комментарий |
|-----------|-------|-------|-------------|
| Ядро (аккаунты, auth, события, подписки) | 🟢 ~85% | 🟢 ~90% | Регистрация: consent + person ≥14 в create |
| Социальное (участие, приглашения, чаты) | 🟢 ~80% | 🟢 ~80% | Event-чаты в Conversations — TODO |
| Организации + модерация | 🟢 ~85% | 🟢 ~85% | Payout encryption; verified → `CanSellTickets` |
| Медиа | 🟢 ~85% | 🟢 ~85% | Album ACL |
| Платежи / билеты | 🔴 ~5% | 🟡 ~55% | Stub API есть; split/прод-ЮKassa нет; флаг выкл. |
| Юридика / compliance | 🟢 ~80% | 🟢 ~85% | Исходники в `Agreements/`; runtime = БД; Policy без галочки |
| Production hardening | 🟢 ~75% | 🟢 ~75% | CORS/errors/CI/health; secrets из `.env` |
| Автотесты | 🔴 0% | 🔴 0% | Нет test projects |

---

## 2. Карта модулей

### Готово к soft launch (без билетов)

| Модуль | Статус | Примечание |
|--------|--------|-----------|
| Accounts | ✅ | Consent flags на create; delete/export |
| Authorization | ✅ | |
| Events / Templates | ✅ | Soft-delete справочников; geo |
| Subscriptions / Rating | ✅ | |
| Organizations | ✅ | Verification + payout crypto |
| ContentReports / BugReports / PlatformRoles | ✅ | |
| Notifications (+ antiflood) | ✅ | |
| Agreements | ✅ | Policy информационна; Consent+Agreement enforced |
| Media | ✅ | |
| Participations / Invitations | ✅ | BW + visibility |
| Wallets / Tariffs | ⚠️ | **Рудимент тарифа**, не билетные деньги; deposit UX слабый |

### Билеты (в коде, вне prod soft launch)

| Модуль | Статус |
|--------|--------|
| Orders / Tickets / Refunds / Webhooks (stub) | ✅ API + UI stub |
| Check-in / Transfer | ✅ API; UI слабо |
| Real YooKassa + split на реквизиты организатора | ❌ |
| Gate `TicketingAgreement` на `CanSellTickets` | ❌ только Verified |
| Типы/тарифы билетов | ❌ одна цена `Cost` |
| 54-ФЗ / чеки | ❌ модель не закрыта в договоре |

---

## 3. Зафиксированные продуктовые правила (новые / уточнённые)

- [x] **Policy без обязательной галочки** — информационный документ; `AcceptPolicy` игнорируется; re-consent не включает Policy
- [x] **Обязательны Consent + Agreement** при регистрации и при обновлении версий
- [x] **Кошелёк ≠ билеты** — только баланс/списание тарифа платформы; не P2P, не оплата билетов
- [x] **Продавец билета = организатор**; сервис = площадка; целевой расчёт = ЮKassa split
- [ ] Закрыть развилки в Агентском договоре (§2.1 режим агента, §6.2 фискализация) — см. legal-review
- [ ] Enforced accept `TicketingAgreement` перед `CanSellTickets=true`
- [ ] Реальный provider + split до включения флага в prod

---

## 4. P0 — блокеры / срочный дебаг

### Регистрация и согласия

- [x] **UI: передавать `AcceptConsent` / `AcceptAgreement` в `POST /accounts/create`**
- [x] Профиль (ФИО + ДР ≥14) создаётся в той же TX `create` — нет аккаунта без возраста
- [x] Серверный age gate (`PersonValidator` + `UserUnderMinimumAge=4003`) на create и `persons/set`
- [x] UI: обязательные имя/фамилия/ДР; Policy — ссылка без галочки
- [x] Fallback дублирующие `agree` после логина (мягкий; бэк пишет согласия в TX create)
- [x] Глобальный UI-обработчик 403 + `AgreementNotFound` + `missingDocuments` (Consent/Agreement only)
- [x] Коды в UI `errorCodes`: `AgreementNotFound=18001`, `UserUnderMinimumAge=4003`, …

### Билеты на стенде (не prod)

- [ ] Стендовый `ticketSalesEnabled=true` + smoke stub checkout
- [ ] CTA: `TicketsEnabled` → «Купить/получить билет»; иначе Participate; `Cost` без билетов → «Платно на месте»
- [ ] Не смешивать wallet top-up с оплатой билета
- [ ] Скрывать ticketing UX при выключенном флаге

### Ops soft launch

- [x] Health / version, CI build, CORS, safe errors, PII redaction
- [x] Prod secrets из host/`.env`
- [x] Backup на сервере (`pg_dump` / restore известен)
- [x] HTTPS на инфраструктуре
- [ ] HSTS (HTTP Strict Transport Security) — заголовок/`max-age` на reverse proxy; опционально при уже работающем HTTPS
- [ ] Uptime-check + алерт
- [ ] (опц.) чистка plaintext секретов из git history / ротация

---

## 5. P1 — второй порядок

### Билеты → prod-ready

- [ ] Закрыть текст Агентского договора (режим + фискализация) и залить в БД
- [ ] Gate `TicketingAgreement` + verified + payout реквизиты
- [ ] Реальный `IPaymentProvider` ЮKassa со split / marketplace transfers
- [ ] Return URL / `confirmationUrl` в UI вместо (или вместе с) локальной заглушкой
- [ ] Org check-in UI; transfer/gift UI; refund UI на существующие API
- [ ] Capacity reservation / гонка мест
- [ ] Adult gate на покупке при необходимости Policy

### Прочее

- [x] Углубить account delete (media/messages/subscriptions/geo/password)
- [x] Notify об исключении из участников / BW *(уже на develop)*
- [x] Event-чаты в Conversations (`personalOnly=false`); ACL списка организаторов события
- [x] Age gate ≥14 на регистрации (UI + API create/`persons/set`)
- [ ] Shared rate limiter при multi-instance — **отложено**: in-memory на инстанс ок при 2–3 репликах

### Функциональность (остатки кода)

- [ ] Person `PUT update` (если ещё закомментирован)
- [ ] Media album `setParameters`
- [ ] Invitations: заполнить `result.Event`
- [ ] Premium-параметры событий по тарифу
- [x] Wallets Deposit API + stub payment (тарифный контур, не билеты) / DebtCollector
- [ ] Auto-invitations
- [ ] Локализация (`localization.enabled: false`)
- [ ] Swagger v3
- [ ] Route conflict `WalletsController.GetWalletAsync` (`[HttpGet("/{walletId}")]`)
- [ ] Порядок таблиц в `InitialDatabase.sql` для fresh install
- [ ] Unit/integration smoke tests (auth, events, agreements)

### Платежи (v1.1)

- [x] OrdersService + PaymentsController (stub + complete; split/webhook — дальше)
- [ ] Webhook controller (YooKassa/TBank) + idempotency
- [x] Tickets API (issue / validate / used / transfer) — runtime stub
- [x] Refunds API (stub provider)
- [x] TicketingAgreement gate на `CanSellTickets` + capacity с учётом pending заказов
- [ ] 54-ФЗ / онлайн-касса (разделённая фискализация зафиксирована в черновике агентского договора)
- [ ] Organization payment-provider onboarding / реальный YooKassa split

### Compliance (P1)

- [x] Отзыв согласия (withdraw) → `POST /api/agreements/withdraw` (= углублённый delete)
- [x] Retention / purge (anonymous age + inactive tokens) — `RetentionPurgeWorker`
- [ ] Appeal workflow для sanctions
- [ ] Audit log staff-доступа к PII
- [x] Cookie policy (web-клиент) — страница `/cookies`

---

## 6. P2 / бэклог

- Типы билетов, 54-ФЗ, Wallet pass / PDF
- Auto-invitations
- Локализация, Swagger polish
- Автотесты (unit + e2e: register, re-consent, free join, ticket stub, org enable tickets)
- Product UX: «мои события», reminders, push, deep links, карта «рядом» — см. исторический бэклог ниже при необходимости

---

## 7. P0/P1 из аудита 03.09 — статус

### Закрыто ранее

- [x] CORS, safe errors, media ACL, role checks, health, CI
- [x] Consent enforce + ReConsentMiddleware
- [x] Account delete/export
- [x] Soft-delete справочников; contact validation; BW/visibility
- [x] Org payout encryption; `ticketSalesEnabled` kill-switch
- [x] AdultConfirmed `>= 18`; anonymous age TTL
- [x] Notifications P0–P2 + antiflood
- [x] Tickets stub API (orders/complete/webhook/check-in/transfer/refund) — **после 03.09**

### Остаётся открытым

- [ ] Processor DPAs (DaData, SMS, SMTP, filestorage) — вне кода
- [ ] Контакт оператора ПДн / DPO в Policy (юр. лицо, email/адрес для запросов субъектов) — проверить загруженный текст
- [ ] HSTS на reverse proxy (HTTPS уже есть)
- [ ] Uptime monitoring / алерты

---

## 8. Матрица воркфлоу (integrity)

| Воркфлоу | Backend | UI | Целостность |
|----------|---------|----|-------------|
| Регистрация + Consent/Agreement + person | ✅ | ⚠️ | **Сломан** — флаги create |
| Login / activate / password | ✅ | ✅ | OK |
| Re-consent | ✅ middleware | ⚠️ Gate | Частично |
| Каталог / карта / событие | ✅ | ✅ | OK |
| Free participate | ✅ | ✅ | OK |
| Cost без TicketsEnabled | ✅ | ⚠️ badge | Проверить copy |
| Ticket purchase | ✅ stub | ✅ stub | E2E stub; не prod |
| My tickets / gift / refund | ✅ API | ⚠️ | UI «скоро» |
| Org verify + CanSellTickets | ✅ | ✅ | Нет gate TicketingAgreement |
| Wallet / tariff | ⚠️ | ⚠️ | Рудимент тарифа |
| Notifications WS | ✅ | ✅ | OK |
| Media | ✅ | ✅ | OK |
| Delete / export | ✅ | ⚠️ | Сверить UI |

---

## 9. Рекомендуемый порядок работ

```
1. ~~Fix registration AcceptConsent/AcceptAgreement + person sync~~ ✅
2. ~~403 re-consent handler + error codes на UI~~ ✅
3. Закрыть текст Агентского договора (§2.1 / §6.2) + заливка в БД
4. Ticket CTA/badge + stub E2E на стенде (флаг только на stage)
5. TicketingAgreement gate + реальный ЮKassa split
6. Soft launch без ticketSalesEnabled в prod
7. Включение билетов в prod после п.3–5
```

---

## 10. Связанные артефакты

| Артефакт | Путь |
|----------|------|
| Описание сервиса | [SERVICE.md](./SERVICE.md) |
| Docker / Staging / correlation id | [docker-environment.md](./docker-environment.md) |
| Legal ticketing review | [legal-ticketing-review.md](./legal-ticketing-review.md) |
| UI handoff | [ui-handoff-checklist-and-tickets.md](./ui-handoff-checklist-and-tickets.md) |
| Tickets workflow | [tickets workflow.txt](./tickets%20workflow.txt) |
| Content reports UI | [content-reports-ui.md](./content-reports-ui.md) |
| Исходники документов | [`../Agreements/`](../Agreements/) |
| Cloud/dev notes | [`../AGENTS.md`](../AGENTS.md) |
