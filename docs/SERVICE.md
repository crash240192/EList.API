# EList — описание сервиса

> Актуально: 14 сентября 2026  
> Репозитории: `elist.api` (этот), `elist.ui`, `elist.common`, `elist.filestorage.api`  
> Этот документ — **источник правды по продуктовой и доменной модели**.  
> Чеклист готовности: [production-readiness-checklist.md](./production-readiness-checklist.md).

---

## 1. Что это

**EList** — веб-агрегатор городских мероприятий: поиск и карта событий, участие, организации-организаторы, социальный слой (подписки, приглашения, обсуждения), модерация и (в развитии) продажа билетов.

Платформа — **информационный посредник и инфраструктура**. Сервис **не является организатором** размещённых мероприятий и **не является продавцом билетов**. Продавец билета и сторона договора на посещение — **организатор** (организация).

- Клиент: `elist.ui`
- Backend API: `elist.api` (path base `/eList`)
- Медиа: `elist.filestorage.api`
- Общие контракты/утилиты: `elist.common`

---

## 2. Обновляется ли документация при каждом коммите?

**Нет.** `docs/`, `AGENTS.md`, `Agreements/` и UI-`PLAN.md` / `CLAUDE.md` **не генерируются и не актуализируются автоматически** при коммите. Хуков/CI на обновление описания сервиса нет.

| Артефакт | Как живёт |
|----------|-----------|
| `docs/SERVICE.md` | Вручную при смене доменной модели / крупных фичах |
| `docs/production-readiness-checklist.md` | Вручную перед релизными итерациями |
| `Agreements/*.txt` | Исходники юр. текстов (черновики). **Runtime source of truth = таблица документов в БД** |
| `AGENTS.md` | Подсказки для Cloud Agent / локальной разработки |
| UI `PLAN.md` / `CLAUDE.md` | Архитектура фронта; тоже вручную |

Правило: при мерже эпика (auth/consent, tickets, org verification и т.п.) обновлять как минимум `SERVICE.md` + чеклист **в том же PR** или сразу следом.

---

## 3. Экосистема репозиториев

```
elist.ui  ──REST──►  elist.api  ──► PostgreSQL (+ PostGIS)
              │         │
              │         ├── elist.common (CommandResult, ErrorCode, config, SMS/SMTP, crypto)
              │         └── filestorage client ──► elist.filestorage.api
              │
              └── Yandex Maps (клиент)
```

---

## 4. Роли и основные сущности

| Роль | Описание |
|------|----------|
| Пользователь (account) | Регистрация, профиль (person), участие, покупка билетов |
| Организатор | Организация + роли; события от имени org или аккаунта |
| Модератор / platform role | Жалобы, санкции, админ-справочники |
| Аноним | Каталог; age-gate 18+ с TTL |

Ключевые сущности: Account, Person, Contact, Event (+ parameters), Participation, Invitation, Organization (+ legal, payout), Subscription, Conversation, Media Album, Wallet/Tariff, Order/Ticket/Refund, Document/Agreement, Notification.

---

## 5. Юридические документы

Исходники (HTML-текст): каталог [`../Agreements/`](../Agreements/).  
В рантайме версии живут в БД и отдаются через `/api/agreements/...`.

| `DocumentType` | Файл-исходник | Кто принимает | Галочка |
|----------------|---------------|---------------|---------|
| `Policy` (0) | Политика обработки персональных данных | Все (ознакомление) | **Не требуется** — информационный документ |
| `Consent` (1) | Согласие на обработку персональных данных | Пользователь при регистрации / re-consent | **Обязательна** |
| `Agreement` (2) | Пользовательское соглашение | Пользователь при регистрации / re-consent | **Обязательна** |
| `OrganizationAgreement` (3) | Соглашение для организаций | Организатор | Обязательна для org-флоу |
| `TicketingAgreement` (4) | Агентский договор на продажу билетов | Организатор при подключении продажи билетов | Должна быть обязательна (см. legal-review) |

### Зафиксированные правила продукта

1. **Policy ≠ согласие.** Политика публикуется для ознакомления (152-ФЗ). Отдельная галочка «согласен с Политикой» **не нужна** и **не блокирует** API. Поле `AcceptPolicy` на create сохранено для совместимости клиентов и **игнорируется** сервером.
2. При регистрации обязательны **Consent + Agreement** (`AcceptConsent`, `AcceptAgreement`) и профиль: **имя, фамилия, дата рождения** (возраст ≥ **14**, иначе `UserUnderMinimumAge=4003`). Профиль пишется в той же TX, что и аккаунт. Consent/Agreement же проверяются в `ReConsentMiddleware`.
3. Обновление Policy **не** требует re-consent и **не** шлёт push о необходимости повторного согласия.
4. Подтверждение **18+** (`AdultConfirmed`) — отдельная ось (контент 18+ / анонимное age-agreement); не путать с порогом регистрации 14+.

---

## 6. Кошелёк (wallet) — рудимент тарифа

Кошелёк — **баланс для списания оплаты по тарифу платформы** (лимиты/возможности аккаунта или организации). Не платёжный инструмент маркетплейса.

| Делает | Не делает |
|--------|-----------|
| Хранит баланс и привязку тарифа | Не принимает оплату за билеты |
| Списание по `NextChargeAt` при достаточном балансе | Не уходит в минус |
| Пополнение (`wallet_deposits`) без сдвига периода | Не P2P и не переводы |
| Автоактивация при депозите, если due и хватает средств | Не escrow / не холд билетных денег |
| Ledger списаний `wallet_tariff_charges` | Не замена сплита пользователь→продавец |

Период тарифа начинается с момента **успешного** списания (`LastChargeDate` / `NextChargeAt = now + Period`). Депозит только увеличивает баланс; если период истёк или ещё не активирован и денег хватает — списание сразу.

**Выбранный vs действующий тариф**

- `TariffId` — выбранный пользователем тариф (сохраняется даже без оплаты).
- Если выбранный платный тариф не активен (`NextChargeAt` в прошлом / null и баланс &lt; cost) — для лимитов событий применяется **бесплатный тариф по умолчанию** (`cost = 0` в том же контуре личный/организация).
- При пополнении / DebtCollector / открытии кошелька: если денег хватает — выбранный тариф снова списывается и активируется.
- В каждом контуре (`forOrganization` true/false) допускается **только один** тариф с `cost = 0` (ошибка `DefaultFreeTariffAlreadyExists=10007`).
- Новый кошелёк получает free default сразу. `debtCollector.active = true`.

**Два денежных контура разделены:**

```
Тариф платформы  →  Wallet / Tariffs / wallet_deposits   (внутренний баланс сервиса)
Билеты события   →  Orders + ЮKassa split                 (пользователь → продавец)
```

В UI баланс кошелька и пополнение тарифа не смешивать с оплатой билета.  
История операций на странице кошелька может показывать **оба** контура (тариф + билеты) для прозрачности — это лента активности, а не движение баланса кошелька.

Пока реальная ЮKassa не подключена, пополнение идёт через тот же `IPaymentProvider` stub, что и билеты (`CreatePayment` → return URL → `deposits/complete`).

---

## 7. Модель продажи билетов (целевая)

### Продуктовая / юридическая модель

- **Продавец билета** — организация-организатор.
- **Площадка** — EList: витрина, заказ, билет, check-in, отчётность.
- **Оплата** — через ЮKassa: сумма организатору на его реквизиты, сервису — **процент через split** при проведении платежа.
- Договор на посещение — между **участником и организатором**.

Согласуется с Пользовательским соглашением (§6) и Соглашением для организаций (§2.4): сервис не продавец.

### Технический статус (14.09.2026)

| Слой | Статус |
|------|--------|
| Orders / tickets / stub payment / webhook path / check-in / transfer / refunds | API есть |
| Учёт `AmountTotal` / `AmountSeller` / `AmountCommission` в заказе | Есть |
| Реальный split ЮKassa на реквизиты организатора | **Нет** (stub = один платёж на полную сумму) |
| `features.ticketSalesEnabled` | По умолчанию **`false`** |
| Принятие `TicketingAgreement` как жёсткий gate на `CanSellTickets` | **Не enforced** (тип документа есть; `SetCanSellTickets` проверяет только верификацию) |

Подробный разбор: [legal-ticketing-review.md](./legal-ticketing-review.md).  
Пошаговый UI↔API флоу: [tickets workflow.txt](./tickets%20workflow.txt).

### Режимы события

| `TicketsEnabled` | `Cost` | Поведение |
|------------------|-------|-----------|
| `false` | любой | Participate; Cost = «оплата на месте» (информативно) |
| `true` | `0` | Бесплатный билет: заказ → сразу Paid + Participate |
| `true` | `> 0` | Заказ → оплата (stub/ЮKassa) → билет + Participate |
| `true` | — | Обычный Participate / accept invite **запрещены** (`OrganizationPaymentRequired`) |

---

## 8. Основные воркфлоу (целостность)

| Воркфлоу | Статус | Комментарий |
|----------|--------|-------------|
| Регистрация (create + Consent/Agreement + person ≥14 + wallet) | ✅ | `AcceptConsent`/`AcceptAgreement` + ФИО/ДР в `create`; профиль в той же TX; age gate `UserUnderMinimumAge` |
| Person info после регистрации | ✅ | Создаётся в `create`; `POST /persons/set` — для правок профиля (ДР ≥14) |
| Логин / активация / смена пароля | ✅ | |
| Re-consent Consent/Agreement | ✅ | Middleware + UI Gate + обработчик 403/`AgreementNotFound` |
| Каталог / карта / карточка события | ✅ | |
| Участие без билетов | ✅ | |
| Покупка билета (stub) | ⚠️ | UI+API stub; реальной ЮKassa и split нет |
| Организации, верификация, payout | ✅ / ⚠️ | Верификация нужна для `CanSellTickets`; payout ещё не связан с реальным split |
| Кошелёк / тариф | ⚠️ | NextChargeAt + ledger; пополнение stub; не билетный контур |
| Ошибки / Staging | ✅ | `ASPNETCORE_ENVIRONMENT` + `features:exposeDetailedErrors`; UI показывает `correlationId` — см. [docker-environment.md](./docker-environment.md) |
| Уведомления (WS + antiflood) | ✅ | |
| Медиа / альбомы | ✅ | |
| Жалобы / модерация / admin | ✅ | |
| Delete / export аккаунта | ✅ API | UI — сверить с handoff |

---

## 9. Аутентификация (кратко)

- Заголовок клиента: `Authorization-jwt` (хеш клиента).
- Сессия пользователя: `Authorization` = UUID токена.
- Path base: `/eList` → `/eList/api/...`.
- Swagger: `/eList/swagger`.

---

## 10. Конфигурация, важная для продукта

```json
"payments": {
  "provider": "yookassaStub",
  "commissionPercent": 10,
  "currency": "RUB",
  "returnUrl": "https://tvoy-spot.ru/payments/return"
},
"features": {
  "ticketSalesEnabled": false,
  "reConsentEnforcementEnabled": true
},
"agreements": {
  "anonymousAgeTtlHours": 24
}
```

---

## 11. Связанные документы

| Документ | Назначение |
|----------|------------|
| [production-readiness-checklist.md](./production-readiness-checklist.md) | Статус готовности и бэклог |
| [legal-ticketing-review.md](./legal-ticketing-review.md) | Юр. модель билетов vs код |
| [ui-handoff-checklist-and-tickets.md](./ui-handoff-checklist-and-tickets.md) | Handoff для фронта |
| [tickets workflow.txt](./tickets%20workflow.txt) | Пошаговый ticket checkout |
| [content-reports-ui.md](./content-reports-ui.md) | Жалобы / модерация UI |
| [`../Agreements/`](../Agreements/) | Исходники юр. текстов |
| [`../AGENTS.md`](../AGENTS.md) | Сборка и cloud-dev notes |
