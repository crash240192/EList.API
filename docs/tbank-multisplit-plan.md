# Т-Банк Мультирасчёты (Deal NN) — план внедрения

> Актуально: 25 октября 2026  
> Решение: целевая модель сплита — **Мультирасчёты Deal NN** по PDF в [`docs/мультирасчеты/`](./мультирасчеты/) (подтверждено менеджером Т‑Банка).  
> Оплата покупателя: **одностадийка** (`PayType=O` / настройка терминала) — без холда/`Confirm`; Deal + A2C для сплита.  

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
| `Init` + `Shops[{ShopCode, Amount, Fee}]`, `PayType=O` | `Init` + `CreateDealWithType=NN` + одностадийка → **Сделка** (`DealId` / `SpAccumulationId`), статус `CONFIRMED` |
| Сплит «сразу при CONFIRMED» через Shops | Деньги в Deal у банка → **A2C-выплата** продавцу → закрытие / FinalPayout (комиссия площадке). **Не** через р/с сайта |
| Онбординг только `register.tinkoff.ru/v1` | Регистрация точек через **`acqapi(.tinkoff).ru`** (OAuth, часто mTLS + WL IP) |

`yookassaStub` остаётся для локалки без банка.

Кошелёк площадки (тариф) — **вне** сделок Мультирасчётов: обычный Init без Deal (деньги площадки), как и сейчас без `Shops`.

---

## 2. Целевой флоу (NOT PCI DSS, форма банка)

Канон из `multisplit.pdf` §6.1:

```
1. Init (/v2/Init) + CreateDealWithType=NN + PayType=O  → PaymentURL
2. Покупатель платит на форме банка  → CONFIRMED; webhook со SpAccumulationId (= DealId)
3. e2c Init (/e2c/v2/Init) + DealId + сумма продавца
   [+ FinalPayout=true если последняя выплата]
4. e2c Payment (/e2c/v2/Payment) + PaymentId из п.3
5. Повтор 3–4 при частичных выплатах (пока нет FinalPayout / closeSpDeal)
6. closeSpDeal ИЛИ FinalPayout=true        → сделка закрыта
```

Ограничения банка (кратко):

- Выплата только по существующей сделке и только после списания; сумма ≤ остаток сделки.
- После `FinalPayout=true` или `closeSpDeal` сделка закрыта; возвраты покупателю после закрытия — нельзя.
- Срок жизни сделки (договор с банком): без выплат → отмена оплат; была ≥1 выплата → остаток уходит **площадке**.
- Двухстадийка (`PayType=T` / Confirm) **не используем** для билетов, пока продукт не потребует эскроу до события.

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
| D1 | Когда **выдавать билеты**? | После `CONFIRMED` | **После `CONFIRMED`** (одностадийка) |
| D2 | Двухстадийка / Confirm? | `PayType=T` vs `O` | **`O` (одностадийка)** — Confirm не зовём |
| D3 | Когда **выплата org**? | Сразу после оплаты / после события / после check-in / по кнопке | После `CONFIRMED` + выданные билеты; либо отложенный job «T+N после start_time» — выбрать с юр. |
| D4 | Как забирать **комиссию**? | `FinalPayout` остатка площадке / `closeSpDeal` | После выплаты `amount_seller` — `FinalPayout=true` на остаток **или** closeSpDeal с политикой «остаток площадке» |
| D5 | Возврат билета | Cancel до payout / после payout / после close | Матрица статусов сделки + запрет Cancel после close |

---

## 4. Волны внедрения

### P0 — Контракт и docs

- [x] Решение: строим под PDF Мультирасчёты Deal NN  
- [x] Одностадийка (`PayType=O`); D1/D2 зафиксированы  
- [ ] D3–D5 с юр. (когда payout / комиссия / возвраты)  
- [ ] Узнать у банка: max deal term, нужен ли mTLS на acqapi test, e2c TerminalKey тот же или отдельный  
- [x] SERVICE / tbank-payments: `Shops/Fee` = legacy до P2  

### P1 — Онбординг точки (acqapi) — **в коде**

| Шаг | API |
|-----|-----|
| OAuth | `POST …/oauth/token` (Basic `partner:partner` + password grant) |
| Register | `POST …/sm-register/register` |
| Get / Update | `GET …/sm-register/register/shop/{shopCode}`, `PATCH …/sm-register/register/{shopCode}` |

- [x] `TBankAcqApiClient` + настройки `payments:tbank:acqApi`  
- [x] Provider: acqapi → legacy SM-Register → `manualShopCode`  
- [x] Маппинг org → register payload (ИНН/ОГРН/КПП/CEO/bankAccount); email с login владельца при наличии  
- [ ] Ops: WL IP + mTLS cert на стенде/prod (конфиг `clientCertificatePath` готов)  
- DEMO: `manualShopCode` / stub  

**Выход:** org с Active onboarding даёт `shopCode` (= PartnerId для A2C).

### P2 — Оплата EACQ Deal (замена Shops) — **в коде (MVP)**

- [x] Убрать `Shops[]` / `Fee` из Init билетов  
- [x] `CreateDealWithType = "NN"` + `PayType=O` при наличии ShopCode org  
- [x] Миграция: `orders.provider_deal_id`  
- [x] Webhook / GetState: `SpAccumulationId` → `provider_deal_id`; успех = `CONFIRMED` → билеты  
- [ ] Отдельная таблица/enum статусов deal (`Opened` / …) — позже с P3  
- [ ] Cancel-матрица D5 — уточнить с юр.  
- [x] Кошелёк: Init **без** Deal  

**Выход:** покупка билета создаёт сделку; DealId сохраняется; билеты после `CONFIRMED`. A2C — P3.

### P3 — Выплаты A2C_V2 — **в коде (MVP)**

- [x] `TBankA2cClient` (`/e2c/v2/Init`, `/Payment`, `/GetState`)  
- [x] После `CONFIRMED` + fulfill: A2C на `amount_seller`, `PartnerId` = shopCode, **`FinalPayout=true`** (остаток = комиссия площадке)  
- [x] Журнал `order_payouts` (unique order+kind, статусы)  
- [x] `payments:tbank:autoPayoutOnPaid` (default true); soft-fail без кредов/DealId  
- [ ] Ретраи / GetState worker для `Initiated` — P4  
- [ ] CancelPayment / GetSbpMembers — по необходимости  

**Выход:** после оплаты на стенде с e2c-доступом org получает долю; комиссия остаётся площадке при закрытии сделки.

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
| `TBankModels` / `TBankAcquiringClient` | Init fields Deal + PayType=O; − Shops для билетов |
| `TBankPaymentProvider` | Deal lifecycle вместо Shops/Fee |
| `TBankAcqApiClient` (+ legacy SmRegister) | ✅ P1: OAuth + register/get/patch + mTLS hook |
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

1. План + SERVICE / tbank-payments — ✅  
2. Ветка `cursor/tbank-multisplit-onboarding-0318` — P1 (**эта**).  
3. `cursor/tbank-multisplit-eacq-deal-0318` — P2.  
4. `cursor/tbank-multisplit-a2c-payout-0318` — P3.  
5. P4/P5 по мере готовности стенда и юр. D3–D5.
