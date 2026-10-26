# Мультирасчёты Deal NN — ops / smoke чеклист стенда

> Актуально: 26 октября 2026 (Europe/Moscow)  
> Код: P1–P4 (онбординг acqapi, Init Deal, A2C, DealMaintenance).  
> План: [tbank-multisplit-plan.md](./tbank-multisplit-plan.md).  
> Runtime env: [tbank-payments.md](./tbank-payments.md).

Чеки 54‑ФЗ (CloudKassir) — **отдельный** контур; см. §6 и [tickets-scenarios.md](./tickets-scenarios.md) W4/G1.

---

## 1. Доступы банка (до первого живого Init)

| # | Что | Кто | Статус |
|---|-----|-----|--------|
| 1.1 | WL IP API-сервера на `rest-api-test` / `securepay` (Init/GetState/Cancel/closeSpDeal) | Банк | ☐ (сервер уже в WL — подтвердить) |
| 1.2 | WL IP на `acqapi-test` / `acqapi` | Банк | ☐ |
| 1.3 | OAuth username/password для acqapi (password grant; Basic `partner:partner`) | Банк | ☐ позже |
| 1.4 | mTLS-сертификат на prod `acqapi` (если требуют) | Банк + ops | ☐ позже |
| 1.5 | TerminalKey + Password эквайринга (test / prod) | Банк | ☐ |
| 1.6 | Тот же или отдельный TerminalKey для **e2c** — уточнить у менеджера | Банк | ☐ |
| 1.7 | `dealTermDays` по договору / доп. соглашению | Юр. + банк | ☐ (в коде default 7) |

---

## 2. Env / конфигурация API

Не коммитить прод-секреты.

| Ключ | Назначение |
|------|------------|
| `payments__provider=tbank` | Включить Т‑Банк |
| `payments__tbank__terminalKey` / `password` | Эквайринг + подпись Token |
| `payments__tbank__apiBaseUrl` | test: `https://rest-api-test.tinkoff.ru/v2` |
| `payments__tbank__notificationUrl` | Публичный `…/eList/api/payments/tbank/webhook` |
| `payments__tbank__successUrl` / `failUrl` | UI `/payments/return` |
| `payments__tbank__acqApi__*` | Онбординг точек (username/password + опц. cert) |
| `payments__tbank__e2cApiBaseUrl` | Пусто = вывести из apiBaseUrl → `…/e2c/v2` |
| `payments__tbank__autoPayoutOnPaid` | `true` — A2C сразу после CONFIRMED |
| `payments__tbank__dealTermDays` | TTL для closeSpDeal |
| `dealMaintenance__active` | Воркер sync/retry/close |
| `features__organizationsEnabled` / `ticketsEnabled` / `ticketSalesEnabled` | Продуктовые флаги |

---

## 3. Smoke end-to-end (одностадийка + Deal NN)

| # | Шаг | Ожидание |
|---|-----|----------|
| 3.1 | Org: legal + payout → `provider-onboarding/start` | `shopCode` в `provider_seller_id`, Active (или DEMO `manualShopCode`) |
| 3.2 | Org verified + TicketingAgreement + CanSellTickets | Gate проходит |
| 3.3 | Покупка билета → Init | `CreateDealWithType=NN`, `PayType=O`, **без** Shops; PaymentURL |
| 3.4 | Оплата DEMO-картой | Webhook / GetState → `CONFIRMED` + `SpAccumulationId` → `orders.provider_deal_id` |
| 3.5 | Fulfill | Билеты Issued; строка `order_payouts` (seller) |
| 3.6 | A2C | e2c Init+Payment на `amount_seller`, `FinalPayout=true` → Succeeded; `provider_deal_closed_at` |
| 3.7 | Кошелёк | Init **без** Deal; кредит после CONFIRMED |
| 3.8 | Pending TTL | `PendingOrdersPurgeWorker` отменяет только Pending/Authorized |
| 3.9 | Maintenance | Зависший Initiated → GetState; просроченный Deal без payout → closeSpDeal |

Тестовые карты — из кабинета Т‑Бизнеса / PDF EACQ (не светить в git).

---

## 4. Webhook / сеть

- [ ] `NotificationURL` без HTTPS-redirect, ломающего тело (или корректный TLS end-to-end)
- [ ] Reverse proxy не режет POST body
- [ ] Ответ webhook телом `OK` (как сейчас)
- [ ] UI poll `/payments/return` работает без manual complete

---

## 5. Отказы и возвраты (ops-памятка)

| Ситуация | Поведение |
|----------|-----------|
| Покупатель не оплатил | Cancel / PendingOrdersPurge |
| CONFIRMED, до A2C | Cancel у эквайринга + revoke билетов по политике |
| После FinalPayout / closeSpDeal | Cancel по сделке ограничен банком — ручной разбор |
| A2C Failed | `DealMaintenanceWorker` ретраит; после `dealTermDays` — closeSpDeal |

---

## 6. CloudKassir (W4) — параллельно, не блокер сплита

| # | Что | Статус |
|---|-----|--------|
| 6.1 | Креды CloudKassir (если прямой контур) + `cloudKassir__directFiscalization__inn` | ☐ запас |
| 6.2 | `cloudKassir__directFiscalization__enabled=true` | код готов, **default off**; на проде — Init.Receipt |
| 6.3 | Чек Income после Paid (platform Inn + PurveyorData) | ✅ + журнал |
| 6.4 | Чек возврата IncomeReturn | ✅ |
| 6.5 | Webhook Receipt URL в ЛК → `/eList/api/payments/cloudkassir/receipt` | ☐ ops |
| 6.6 | §6.2 агентского договора = разделённая фискализация | ✅ юр.; dual-чек в коде — later |

Деньги (Deal/A2C) и чеки (касса) **независимы**: касса не заменяет PartnerId/выплату.
