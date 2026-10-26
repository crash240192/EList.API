# Т-Банк: эквайринг и пополнение кошелька

> Актуально: 26 октября 2026  
> Код: API ветки Мультирасчётов P1–P5 (`cursor/tbank-multisplit-*-0318`), UI return-poll  
> Продуктовая модель: [SERVICE.md §7](./SERVICE.md)

Документ для **сборки / staging / prod**: что **сейчас** в runtime-коде, какие env нужны, как устроен флоу.

> **Важно (26.10.2026):** целевой продукт сплита — **Мультирасчёты Deal NN**  
> ([`docs/мультирасчеты/`](./мультирасчеты/), план: [tbank-multisplit-plan.md](./tbank-multisplit-plan.md),  
> ops: [tbank-multisplit-ops-checklist.md](./tbank-multisplit-ops-checklist.md)).  
> Билеты: Init `CreateDealWithType=NN` + A2C; кошелёк — Init без Deal.

---

## 1. Модель (кратко) — runtime Deal NN

| | Билеты (Deal NN) | Кошелёк (тариф) |
|--|--------|-----------------|
| Провайдер | `payments.provider=tbank` | тот же |
| Init | `PayType=O`, `CreateDealWithType=NN` (без `Shops[]`) | без Deal (деньги площадки) |
| После CONFIRMED | fulfill билетов → A2C продавцу (`PartnerId`=shopCode, `FinalPayout`) | кредит кошелька |
| OrderId в банке | GUID заказа | `wallet:{depositId}` |
| Успех | webhook CONFIRMED → Paid + Issued + payout | webhook CONFIRMED → кредит |
| Fallback | GET order → GetState (~2s gate) | GET deposit → GetState |
| UI return | `/payments/return?orderId=…` | `/payments/return?depositId=…&walletId=…` |

Stub (`yookassaStub`) остаётся для локалки без банка: return с `?stub=1` → `complete` / `completeWalletDeposit`.

`SupportsManualComplete=false` у T-Bank — UI **не** зовёт complete после реального PaymentURL.

---

## 2. End-to-end флоу

```
Клиент                API                         Т-Банк
  │  POST order / deposit                          │
  │──────────────────►│  Init (PayType=O)           │
  │◄─ confirmationUrl │────────────────────────────►│
  │  redirect PaymentURL                            │
  │─────────────────────────────────────────────────►│
  │  SuccessURL / FailURL (+ query)                 │
  │◄────────────────────────────────────────────────│
  │  poll GET order|deposit                         │
  │──────────────────►│  (если Pending) GetState    │
  │                   │◄────────────────────────────│
  │                   │  webhook NotificationURL    │
  │                   │◄── CONFIRMED / … ───────────│
  │◄─ Paid/Succeeded  │  fulfill (идемпотентно)     │
```

- **Webhook** — основной путь зачисления. Ответ телом `OK`.  
  `POST /eList/api/payments/tbank/webhook` (без JWT / без re-consent).
- **GetState (P4)** — если NotificationURL недоступен (localhost без туннеля): при poll UI бэкенд сам спрашивает банк и fulfill’ит.
- **Success/Fail URL** — только UX возврата; зачисление **не** делается на SuccessURL.

---

## 3. Конфиг для сборки

### Обязательно (prod / staging с банком)

| Ключ | Пример / смысл |
|------|----------------|
| `payments__provider` | `tbank` |
| `payments__tbank__terminalKey` | **prod** TerminalKey (не DEMO из репо) |
| `payments__tbank__password` | пароль терминала (только env / secret store) |
| `payments__tbank__apiBaseUrl` | prod: `https://securepay.tinkoff.ru/v2` |
| `payments__tbank__notificationUrl` | **публичный** HTTPS, напр. `https://tvoy-spot.ru/eList/api/payments/tbank/webhook` |
| `payments__tbank__successUrl` | `https://…/payments/return` |
| `payments__tbank__failUrl` | тот же или отдельный fail |
| `payments__returnUrl` | fallback base для return (если success/fail пусты) |
| `payments__commissionPercent` | комиссия площадки в Init Fee / учёте заказа |

### Онбординг продавца (билеты)

| Ключ | Смысл |
|------|--------|
| `payments__tbank__acqApi__username` / `password` | **Целевой** онбординг точки (acqapi OAuth → shopCode) |
| `payments__tbank__acqApi__baseUrl` | Default test `https://acqapi-test.tinkoff.ru`; prod `https://acqapi.tinkoff.ru` |
| `payments__tbank__acqApi__clientCertificatePath` | Опционально mTLS (.pfx) на prod |
| `payments__tbank__e2cApiBaseUrl` | A2C base (`…/e2c/v2`); пусто = из `apiBaseUrl` |
| `payments__tbank__autoPayoutOnPaid` | После CONFIRMED сразу A2C продавцу (default true) |
| `payments__tbank__dealTermDays` | TTL сделки для `DealMaintenanceWorker` / closeSpDeal (default 7) |
| `payments__tbank__receipt__enabled` | Init.Receipt (default **true**). Без него при кассе на терминале → **309** `expected.receipt` |
| `payments__tbank__receipt__taxation` / `tax` | СНО / НДС чека (`usn_income` / `none` …) |
| `payments__tbank__receipt__defaultEmail` | Fallback email, если login покупателя не email |
| `dealMaintenance:*` | Воркер: sync Initiated, retry payout, closeSpDeal |
| `payments__tbank__smRegister__username` / `password` | Legacy fallback → ShopCode |
| `payments__tbank__manualShopCode` | DEMO/стенд: ShopCode без bank API |
| `payments__tbank__allowTicketsWithoutShopCode` | Init **без** `Shops[]` (сумма на терминал площадки). По умолчанию **true** на `rest-api-test`, **false** на `securepay` |
| `payments__provider` | `tbank` \| `yookassaStub` — stub не требует ShopCode |
| `features__ticketSalesEnabled` | kill-switch билетов (`true` на стенде) |

**ShopCode** сейчас для legacy `Init.Shops[]`; после P2/P3 — `PartnerId` для A2C из Deal.  
Бесплатные билеты — без онбординга. Stub (`yookassaStub`) — достаточно `OnboardingStatus=Active` (без `ProviderSellerId`).  
На test-host T-Bank платный заказ без ShopCode допускается (Init без сплита), если не выключить `allowTicketsWithoutShopCode`.

### Не коммитить в prod-образ

- DEMO `terminalKey` / `password` из `appsettings.json` ветки разработки — переопределить env.
- Локальные `http://localhost:…` для notification/success/fail.

---

## 4. Чеклист перед первой сборкой со стендом банка

- [ ] Секреты терминала только в env / vault
- [ ] `apiBaseUrl` = test или prod host осознанно
- [ ] `notificationUrl` открыт с интернета (или туннель на staging)
- [ ] UI задеплоен с `/payments/return` и poll (ветка return-poll)
- [ ] CORS / reverse proxy пропускают webhook без редиректа, ломающего тело
- [ ] Для билетов: org verified + TicketingAgreement + ShopCode
- [ ] Smoke: Init → оплата DEMO-картой → webhook или GetState → Paid / Succeeded

---

## 5. API-точки (оператору)

| Метод | Путь | Назначение |
|-------|------|------------|
| POST | `/eList/api/orders` | заказ + Init → `confirmationUrl` |
| GET | `/eList/api/orders/{id}` | статус (+ GetState sync) |
| POST | `/eList/api/Wallets/{id}/deposits` | пополнение + Init |
| GET | `/eList/api/Wallets/deposits/{id}` | статус депозита (+ GetState) |
| POST | `/eList/api/payments/tbank/webhook` | NotificationURL |
| POST | `/eList/api/orders/payments/complete` | **только stub** |

---

## 5.1 CloudKassir (фискализация) ≠ сплит

Документация [API кассы CloudKassir](https://developers.cloudkassir.ru/#api-kassy) описывает **только фискализацию** (`kkt/receipt`, AgentSign / SupplierInfo для 54‑ФЗ).  
Там **нет** сплита денег — это зона **эквайринга Т‑Банка (Мультирасчёты Deal NN / A2C)**.

Сплит в чеке (агент/поставщик) и сплит денег по сделке — разные механизмы; касса не заменяет Deal/выплаты. См. [tbank-multisplit-plan.md](./tbank-multisplit-plan.md).

### Env: прямой CloudKassir (запасной контур, default off)

Сейчас на проде чеки идут через **Init.Receipt** (касса на терминале).  
Прямой `kkt/receipt` — код готов, выключен; включать только если уйдёте с кассы банка.

| Ключ | Смысл |
|------|--------|
| `cloudKassir__directFiscalization__enabled` | `true` — слать чеки сами в CloudKassir API |
| `cloudKassir__directFiscalization__publicId` / `apiSecret` | Basic auth (не коммитить) |
| `cloudKassir__directFiscalization__inn` | ИНН владельца ККТ |
| `cloudKassir__directFiscalization__apiBaseUrl` | Default `https://api.cloudpayments.ru` |
| `cloudKassir__directFiscalization__taxationSystem` / `vat` / `agentSign` | Параметры чека |

Флоу (если enabled): Paid → `kkt/receipt` → журнал → webhook `/eList/api/payments/cloudkassir/receipt`.  
Дока: [CloudKassir HTML](./Тестирование%20онлайн-чека%20–%20CloudKassir%20for%20Developers.html).

---

## 6. Troubleshooting

### ErrorCode 309 — `{request.validate.expected.receipt}`

К терминалу подключена онлайн-касса, а в `Init` не был передан `Receipt`.  
Код шлёт `Receipt` при `payments__tbank__receipt__enabled=true` (default). Нужен Email или Phone покупателя (login с `@` или `defaultEmail`).

**Не включать одновременно** `payments__tbank__receipt__enabled` и `cloudKassir__directFiscalization__enabled` — два чека на одну оплату.

### HTTP 403 Forbidden на `rest-api-test.tinkoff.ru/v2/Init`

Это **не** ошибка `Token` / TerminalKey. Банк отвечает JSON `Success=false` + `ErrorCode` при неверной подписи; **HTTP 403 от nginx** = IP сервера (или вашего ноутбука) **не в White List тестовой среды**.

Что сделать:

1. Узнать внешний IP, с которого идёт Init (сервер API / NAT).
2. Написать в поддержку эквайринга (`acq_help@tinkoff.ru` или чат Т‑Бизнеса):
   - ИНН и наименование организации;
   - IP (или пул), с которого будут запросы;
   - тестовый URL: `rest-api-test.tinkoff.ru`.
3. Дождаться добавления в WL и повторить Init.

Пока WL нет — Init/GetState на test host будут 403 с любого клиента (curl, наш `HttpRestClient2`, SDK).

Prod-хост `securepay.tinkoff.ru` WL тестовой среды не использует, но нужны **боевые** TerminalKey/Password (не DEMO).

### Иные ограничения

- Cloud Agent / sandbox иногда не резолвит `rest-api-test` (egress/DNS) — отдельная проблема от 403.
- 54-ФЗ / онлайн-касса — вне этого контура (см. checklist).
- ЮKassa split в prod не целевой путь; целевой провайдер — Т-Банк marketplace.
