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
| Ядро (аккаунты, auth, события, подписки) | 🟢 ~85% | 🟢 ~85% | **Баг:** UI create без `AcceptConsent`/`AcceptAgreement` |
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

### Регистрация и согласия (сломанный флоу)

- [ ] **UI: передавать `AcceptConsent` / `AcceptAgreement` в `POST /accounts/create`**
- [ ] Убрать или сделать fallback дублирующие `agree` после логина (бэк уже пишет согласия в TX create)
- [ ] Не глотать ошибки `POST /persons/set`; дожать person после activate
- [ ] Глобальный UI-обработчик 403 + `AgreementNotFound` + `missingDocuments` (Consent/Agreement only)
- [ ] Коды в UI `errorCodes`: `AgreementNotFound=18001`, `OrganizationPaymentRequired=11006`, …

### Билеты на стенде (не prod)

- [ ] Стендовый `ticketSalesEnabled=true` + smoke stub checkout
- [ ] CTA: `TicketsEnabled` → «Купить/получить билет»; иначе Participate; `Cost` без билетов → «Платно на месте»
- [ ] Не смешивать wallet top-up с оплатой билета
- [ ] Скрывать ticketing UX при выключенном флаге

### Ops soft launch

- [x] Health / version, CI build, CORS, safe errors, PII redaction
- [x] Prod secrets из host/`.env`
- [ ] Uptime-check + алерт
- [ ] Ежедневный `pg_dump` off-host + понимание restore
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

- [ ] Углубить account delete (media/messages/agreements)
- [ ] Notify об исключении из участников / BW
- [ ] Event-чаты в Conversations; ACL списка организаторов события
- [ ] Shared rate limiter при multi-instance
- [ ] Wallet deposit UX (всё ещё **тарифный** контур)
- [ ] Age gate ≥14 на регистрации (если требует Policy)

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
- [ ] Контакт оператора/DPO в Policy (проверить загруженный текст)
- [ ] HTTPS/HSTS на инфраструктуре
- [ ] Monitoring / backup runbooks

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
1. Fix registration AcceptConsent/AcceptAgreement + person sync
2. 403 re-consent handler + error codes на UI
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
| Legal ticketing review | [legal-ticketing-review.md](./legal-ticketing-review.md) |
| UI handoff | [ui-handoff-checklist-and-tickets.md](./ui-handoff-checklist-and-tickets.md) |
| Tickets workflow | [tickets workflow.txt](./tickets%20workflow.txt) |
| Content reports UI | [content-reports-ui.md](./content-reports-ui.md) |
| Исходники документов | [`../Agreements/`](../Agreements/) |
| Cloud/dev notes | [`../AGENTS.md`](../AGENTS.md) |
