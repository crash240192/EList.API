# Т-Банк Мультирасчёты (Deal NN) — план внедрения

> Актуально: 25 октября 2026  
> Решение: целевая модель сплита — **Мультирасчёты Deal NN** по PDF в [`docs/мультирасчеты/`](./мультирасчеты/) (подтверждено менеджером Т‑Банка).  
> Источники банка:
>
> - `multisplit.pdf` — описание продукта v1.5  
> - `Протокол EACQ Мультирасчеты (Оплаты).pdf` v1.15  
> - `Протокол A2C_V2 Мультирасчеты (Выплаты).pdf` v1.17  
> - `Регистрация и обновление точек партнеров.pdf` v1.11  

Связано: [tbank-payments.md](./tbank-payments.md) (текущий runtime), [SERVICE.md §7](./SERVICE.md), [tickets-scenarios.md](./tickets-scenarios.md) W4.

---

## 1. Решение и отказ от старой гипотезы

| Было в коде / SERVICE (ошибочно для нашего терминала) | Стало (по PDF + менеджер) |
|-------------------------------------------------------|---------------------------|
| `Init` + `Shops[{ShopCode, Amount, Fee}]`, `PayType=O` | `Init` + `CreateDealWithType=NN` → **Сделка** (`DealId` / `SpAccumulationId`) |
| Сплит «сразу при CONFIRMED» | Холд → **Confirm** (списание) → отдельно **A2C-выплата** продавцу → закрытие сделки |
| Онбординг только `register.tinkoff.ru/v1` | Регистрация точек через **`acqapi(.tinkoff).ru`** (OAuth, часто mTLS + WL IP) |

`yookassaStub` остаётся для локалки без банка.

Кошелёк площадки (тариф) — **вне** сделок Мультирасчётов: обычный Init без Deal (деньги площадки), как и сейчас без `Shops`.

---

## 2. Целевой флоу (NOT PCI DSS, форма банка)

Канон из `multisplit.pdf` §6.1:

```
1. Init (/v2/Init) + CreateDealWithType=NN  → PaymentURL
2. Покупатель платит на форме банка        → холд; webhook со SpAccumulationId (= DealId)
3. Confirm (/v2/Confirm)                   → списание с покупателя (после «оказания услуги»)
4. e2c Init (/e2c/v2/Init) + DealId + сумма продавца
   [+ FinalPayout=true если последняя выплата]
5. e2c Payment (/e2c/v2/Payment) + PaymentId из п.4
6. Повтор 4–5 при частичных выплатах (пока нет FinalPayout / closeSpDeal)
7. closeSpDeal ИЛИ FinalPayout=true        → сделка закрыта
```

Ограничения банка (кратко):

- Выплата только по существующей сделке и только после списания; сумма ≤ остаток сделки.
- После `FinalPayout=true` или `closeSpDeal` сделка закрыта; возвраты покупателю после закрытия — нельзя.
- Срок жизни сделки (договор с банком): без выплат → отмена оплат; была ≥1 выплата → остаток уходит **площадке**.
- Банк может **авто-Confirm холда через ~4 суток** (учесть в политике Confirm).

---

## 3. Маппинг на домен EList (билеты)

| Сущность банка | Сущность EList |
|----------------|----------------|
| Сделка (`DealId` / `SpAccumulationId`) | Поле на `orders` (новое), 1 заказ ↔ 1 сделка (MVP) |
| `PaymentId` (EACQ) | `orders.provider_payment_id` (как сейчас) |
| Покупатель | `buyer_account_id` |
| Продавец / точка | org + `ProviderSellerId` / `PaymentRecipientId` после регистрации точки |
| Сумма списания | `amount_total` |
| Выплата продавцу | `amount_seller` |
| Комиссия площадки | `amount_commission` = остаток сделки после выплаты продавцу (через FinalPayout / close / истечение) |

### Продуктовые точки (зафиксировать до P2)

| # | Вопрос | Варианты | Черновая рекомендация для билетов |
|---|--------|----------|-----------------------------------|
| D1 | Когда **выдавать билеты**? | На холде (AUTHORIZED) / только после Confirm | После успешного холда (AUTHORIZED): UX как «оплачено»; Confirm — отдельный бэкенд-шаг |
| D2 | Когда звать **Confirm**? | Сразу после холда / в день события / после check-in / вручную | **Сразу после AUTHORIZED** (или чуть с задержкой), иначе банк сам подтвердит через ~4 дня; «эскроу до события» — только если банк/юристы явно ок с двухстадийкой и сроком сделки |
| D3 | Когда **выплата org**? | Сразу после Confirm / после события / после check-in / по кнопке | После Confirm + успешно выданные билеты; либо отложенный job «T+N после start_time» — выбрать с юр. |
| D4 | Как забирать **комиссию**? | `FinalPayout` остатка площадке / `closeSpDeal` | После выплаты `amount_seller` — `FinalPayout=true` на остаток **или** closeSpDeal с политикой «остаток площадке» |
| D5 | Возврат билета | Cancel до Confirm / Cancel после Confirm до payout / после payout | Матрица статусов сделки + запрет Cancel после close |

---

## 4. Волны внедрения

### P0 — Контракт и docs (эта волна)

- [x] Решение: строим под PDF Мультирасчёты Deal NN  
- [ ] Закрыть D1–D5 письменно (продукт + юр.)  
- [ ] Узнать у банка: max deal term, нужен ли mTLS на acqapi, test TerminalKey именно Мультирасчётов, e2c TerminalKey тот же или отдельный  
- [ ] Пометить в [tbank-payments.md](./tbank-payments.md) / SERVICE: `Shops/Fee` = устаревшая гипотеза; runtime пока legacy до P2  

### P1 — Онбординг точки (acqapi)

Заменить/дополнить `TBankSmRegisterClient` под PDF «Регистрация…»:

| Шаг | API |
|-----|-----|
| OAuth | `POST https://acqapi-test.tinkoff.ru/oauth/token` (prod: `acqapi.tinkoff.ru`) |
| Register | `POST …/sm-register/register` |
| Get / Update | `GET/PUT …/sm-register/register/shop/{shopCode}` и update partner |

- mTLS + WL IP (ops)  
- Маппинг полей org (ИНН, ОГРН, CEO, bankAccount.*) строго по обязательности PDF v1.11  
- Хранить: `shopCode`, статус точки, при необходимости `PaymentRecipientId` / телефон для BasicFieldKey  
- DEMO: сохранить `manualShopCode` / stub  

**Выход:** org с Active onboarding даёт валидный получатель для A2C.

### P2 — Оплата EACQ Deal (замена Shops)

Переписать `TBankPaymentProvider` / `TBankInitRequest`:

- Убрать `Shops[]` / `Fee` из Init билетов  
- Добавить `CreateDealWithType = "NN"` (без `DealId` при новой сделке)  
- Двухстадийность под холд→Confirm (уточнить `PayType` с банком; не оставлять слепой `O`, если Confirm обязателен)  
- Миграция БД: `orders.provider_deal_id` (string/number as string), статусы deal (`Opened` / `Authorized` / `Captured` / `PartiallyPaidOut` / `Closed` / `Expired`)  
- Webhook: парсить `SpAccumulationId`, сохранять DealId; маппинг AUTHORIZED vs CONFIRMED  
- `Confirm` API-клиент + вызов по политике D2  
- `Cancel` до/после Confirm по матрице D5  
- Кошелёк: Init **без** Deal (как сейчас без Shops)  

**Выход:** покупка билета создаёт сделку; poll/webhook стабильны; билеты по D1.

### P3 — Выплаты A2C_V2

Новый клиент `TBankA2cClient` (`/e2c/v2/Init`, `/e2c/v2/Payment`, GetState, CancelPayment, GetSbpMembers при СБП):

- После D3: Init e2c на `amount_seller` + `DealId`  
- Payment e2c  
- Таблица/журнал `order_payouts` (idempotent, status, provider ids)  
- Комиссия: FinalPayout / close на остаток `amount_commission`  
- Ретраи, алерты при fail  

**Выход:** деньги org и комиссия площадки проходят через сделку end-to-end на стенде.

### P4 — Закрытие, TTL, воркеры

- `closeSpDeal` / контроль FinalPayout  
- Worker: сделки без Confirm / без payout к дедлайну deal term  
- Согласовать с `PendingOrdersPurgeWorker` и refunds  

### P5 — Ops + фискализация

- Чеклист стенда: WL, NotificationURL, test cards, e2c  
- W4 CloudKassir параллельно (чеки ≠ сплит; см. tickets-scenarios G1)  
- Обновить агентский договор при смене момента выплаты vs «удержание при расчёте»  

---

## 5. Изменения по слоям кода (ориентир)

| Компонент | Действие |
|-----------|----------|
| `TBankModels` / `TBankAcquiringClient` | + Confirm; Init fields Deal; − Shops для билетов |
| `TBankPaymentProvider` | Deal lifecycle вместо Shops/Fee |
| `TBankSmRegisterClient` → `TBankAcqApiClient` | OAuth + acqapi URLs + mTLS hook |
| `TBankA2cClient` (новый) | e2c Init/Payment |
| `OrdersService` + webhook | DealId, Confirm trigger, payout trigger |
| DB migration | `provider_deal_id`, payout journal, deal status |
| UI | минимально: статусы «ожидает списания / выплата продавцу» при необходимости; return poll уже есть |
| `yookassaStub` | без изменений для локалки |

---

## 6. Критерии приёмки (smoke стенд)

1. Register точки org (test acqapi) → shopCode сохранён.  
2. Init билета с `CreateDealWithType=NN` → PaymentURL → оплата DEMO-картой → webhook с `SpAccumulationId`.  
3. Confirm → статус сделки Captured.  
4. e2c Init+Payment на `amount_seller` → успех.  
5. FinalPayout/close → сделка Closed; повторная выплата отклонена.  
6. Cancel до Confirm → покупателю возврат; билеты не Issued (или revoked по политике).  
7. Кошелёк по-прежнему без Deal.

---

## 7. Порядок работ в git

1. Этот документ + правка ссылок в SERVICE / tbank-payments (**сейчас**).  
2. Ветка `cursor/tbank-multisplit-onboarding-0318` — P1.  
3. `cursor/tbank-multisplit-eacq-deal-0318` — P2.  
4. `cursor/tbank-multisplit-a2c-payout-0318` — P3.  
5. P4/P5 по мере готовности стенда и юр. D1–D5.
