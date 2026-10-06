# Т-Банк: эквайринг и пополнение кошелька

> Актуально: 6 октября 2026  
> Код: API `cursor/tbank-marketplace-acquiring-0b40`, UI `cursor/tbank-payment-return-poll-0b40`  
> Продуктовая модель: [SERVICE.md §7](./SERVICE.md)

Документ для **сборки / staging / prod**: что уже в коде, какие env нужны, как устроен флоу.  
**P5 = только документация** — runtime-поведение платежей закрыто в P1–P4.

---

## 1. Модель (кратко)

| | Билеты | Кошелёк (тариф) |
|--|--------|-----------------|
| Провайдер | `payments.provider=tbank` | тот же |
| Init | `PayType=O`, `Shops[]` + `Fee` при ShopCode org | без `Shops` (деньги площадки) |
| OrderId в банке | GUID заказа | `wallet:{depositId}` |
| Успех | webhook CONFIRMED → билеты + Participate | webhook CONFIRMED → кредит кошелька |
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
| `payments__tbank__smRegister__username` / `password` | SM-Register → ShopCode |
| `payments__tbank__manualShopCode` | только DEMO/стенд без SM-Register |
| `features__ticketSalesEnabled` | kill-switch билетов (`true` на стенде с банком) |

Без Active ShopCode у org заказ с билетами не создастся (`CanSellTickets`).

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

## 6. Troubleshooting

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
