# Билеты — сценарии и пробелы

> Актуально: 7 октября 2026  
> Код: API orders/tickets + UI BuyTicket / MyTickets / return / check-in  
> Связано: [tickets workflow.txt](./tickets%20workflow.txt), [tbank-payments.md](./tbank-payments.md), [SERVICE.md](./SERVICE.md) §7

## Матрица сценариев

| # | Сценарий | API | UI | Статус | Пробел |
|---|----------|-----|----|--------|--------|
| A1 | Покупка платного (1 билет) | ✅ | ✅ | Готово* | *нужен WL/securepay + flag |
| A2 | Покупка пачки (N≤20) | ✅ | ✅ qty | Готово | buyer уже participant → нельзя |
| A3 | Бесплатный билет Cost=0 | ✅ | ✅ | Готово | W3: create-event Cost=0 + ticketsEnabled |
| A4 | Оплата T-Bank → webhook/GetState | ✅ | ✅ poll | Готово* | *test host WL |
| A5 | Оплата stub → complete | ✅ | ✅ | Готово | |
| A6 | Отказ/истечение оплаты у банка | ✅ | poll fail | Готово | |
| A7 | Отмена неоплаченного Pending покупателем | ✅ | ✅ | Готово | W1 |
| A8 | TTL / авто-отмена брошенных Pending | ✅ | — | Готово | W1 purge worker |
| B1 | Возврат одного билета из пачки | ✅ | ✅ | Готово | |
| B2 | Возврат всех оставшихся | ✅ | ✅ | Готово | |
| B3 | Отмена заявки на возврат | ✅ | ✅ | Готово | |
| B4 | Возврат после check-in (Used) | ✅ block | ✅ | Готово | |
| C1 | Подарок/transfer | ✅ | ✅ | Готово | W2: подписчики+подписки+login lookup |
| C2 | Покупка при уже participant (подарок) | ✅ | ✅ | Готово | W2: create + CTA «Купить в подарок» |
| D1 | Check-in по коду / QR | ✅ | ✅ | Готово | W3: камера + QR на билете |
| D2 | Validate без изменения статуса | ✅ | ✅ | Готово | |
| E1 | My tickets | ✅ | ✅ | Готово | + pending cancel |
| E2 | My orders (pending/history) | ✅ | ✅ | Готово | W3: pending + история на My tickets |
| F1 | Org CanSellTickets + agreement | ✅ | ✅ | Готово | |
| F2 | Global `ticketSalesEnabled` | ✅ | ✅ | Готово | `GET /api/features` + EventPage |
| F3 | Invite при TicketsEnabled | ✅ | ✅ | Готово | |
| G1 | 54-ФЗ Receipt / AgentSign | ❌ | — | Отложено | после ТП Т-Банк/касса |
| G2 | Типы билетов | 🟡 Phase A | ❌ | В работе | схема+CRUD; заказ/поиск — B/C; UI — D |
| G2b | PDF / Wallet pass | ❌ | — | Бэклог | после типов |

## Волны доработки

### W1 — незавершённая оплата ✅
1. `POST /orders/{id}/cancel` + UI pending на My tickets.
2. `PendingOrdersPurgeWorker` (TTL).
3. `GET /api/features` + скрытие buy CTA при `ticketSalesEnabled=false`.

### W2 — покупка для других / gift UX ✅
1. Create order разрешён при уже participant; CTA «Купить в подарок».
2. Gift: подписчики + подписки + `GET /api/accounts/lookup?q=`.

### W3 — продукт polish ✅
1. Free tickets в create-event UI (`Cost=0` + `ticketsEnabled`).
2. My orders: pending + история на My tickets; QR билета.
3. QR check-in камерой у организатора (`TicketCheckInPanel` + `QrScanner`).
4. Adult gate на покупку — отложено (ageLimit уже на доступе к событию).

### W4 — фискализация (после ТП)
1. Receipt в Init (AgentSign / SupplierInfo).
2. Обязательные поля org для чека (телефон и т.д.).
3. Закрыть §6.2 агентского договора.

### W5 — типы билетов (план)
См. [ticket-types-plan.md](./ticket-types-plan.md).  
Коротко: один тип на заказ; цена события = min/max активных типов; поиск `price` по типам.

## State machine (кратко)

```
Create → Pending ──pay──► Paid + Issued tickets
         │ cancel/TTL/bank
         ▼
      Canceled

Issued ──check-in──► Used
Issued ──transfer──► Issued (new holder)
Issued ──refund──► RefundPending ──► Refunded | back to Issued
```
