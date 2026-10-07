# Билеты — сценарии и пробелы

> Актуально: 7 октября 2026  
> Код: API orders/tickets + UI BuyTicket / MyTickets / return / check-in  
> Связано: [tickets workflow.txt](./tickets%20workflow.txt), [tbank-payments.md](./tbank-payments.md), [SERVICE.md](./SERVICE.md) §7

## Матрица сценариев

| # | Сценарий | API | UI | Статус | Пробел |
|---|----------|-----|----|--------|--------|
| A1 | Покупка платного (1 билет) | ✅ | ✅ | Готово* | *нужен WL/securepay + flag |
| A2 | Покупка пачки (N≤20) | ✅ | ✅ qty | Готово | buyer уже participant → нельзя |
| A3 | Бесплатный билет Cost=0 | ✅ | ⚠️ | Частично | create-event UI блокирует Cost=0+tickets |
| A4 | Оплата T-Bank → webhook/GetState | ✅ | ✅ poll | Готово* | *test host WL |
| A5 | Оплата stub → complete | ✅ | ✅ | Готово | |
| A6 | Отказ/истечение оплаты у банка | ✅ | poll fail | Готово | |
| A7 | **Отмена неоплаченного Pending покупателем** | ❌→W1 | ❌→W1 | **В работе** | места держатся без TTL |
| A8 | **TTL / авто-отмена брошенных Pending** | ❌→W1 | — | **В работе** | |
| B1 | Возврат одного билета из пачки | ✅ | ✅ | Готово | |
| B2 | Возврат всех оставшихся | ✅ | ✅ | Готово | |
| B3 | Отмена заявки на возврат | ✅ | ✅ | Готово | |
| B4 | Возврат после check-in (Used) | ✅ block | ✅ | Готово | |
| C1 | Подарок/transfer | ✅ | ✅ | Готово | UI: только подписчики |
| C2 | Покупка «для других» (buyer ≠ holder) | ❌ | ❌ | Нет | create блокирует participant |
| D1 | Check-in по коду | ✅ | ✅ | Готово | нет QR-сканера |
| D2 | Validate без изменения статуса | ✅ | ✅ | Готово | |
| E1 | My tickets | ✅ | ✅ | Готово | |
| E2 | My orders (pending/paid) | ✅ | ❌ | UI gap | |
| F1 | Org CanSellTickets + agreement | ✅ | ✅ | Готово | |
| F2 | Global `ticketSalesEnabled` | ✅ API | ❌ UI | UI gap | CTA может показать buy |
| F3 | Invite при TicketsEnabled | ✅ | ✅ | Готово | |
| G1 | 54-ФЗ Receipt / AgentSign | ❌ | — | Отложено | после ТП Т-Банк/касса |
| G2 | Типы билетов / PDF / Wallet pass | ❌ | — | Бэклог | |

## Волны доработки

### W1 — незавершённая оплата (сейчас)
1. `POST /orders/{id}/cancel` — покупатель отменяет Pending/Authorized; Cancel у провайдера; статус Canceled; места освобождаются.
2. Фоновый purge Pending старше N минут (config) + Cancel у провайдера.
3. UI: показать pending-заказы и кнопку «Отменить» (My tickets / event).
4. UI: скрыть buy CTA если global flag выключен (если API отдаёт флаг или отдельный endpoint).

### W2 — покупка для других / gift UX
1. Разрешить create при уже participant, если qty идёт «в подарок» (или отдельный flow).
2. Transfer: поиск получателя шире подписчиков (по логину/контакту).

### W3 — продукт polish
1. Free tickets в create-event UI.
2. My orders страница / блок.
3. QR check-in (камера).
4. Adult gate на покупку (если нужно по политике).

### W4 — фискализация (после ТП)
1. Receipt в Init (AgentSign / SupplierInfo).
2. Обязательные поля org для чека (телефон и т.д.).
3. Закрыть §6.2 агентского договора.

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
