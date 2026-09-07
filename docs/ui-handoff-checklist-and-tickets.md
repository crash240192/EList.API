# UI handoff: изменения бэкенда (чеклист) и постановка по билетам

> Актуально: сентябрь 2026  
> Backend: `develop` (чеклист, уведомления, antiflood) + PR билетов `#40` (`cursor/tickets-payments-stub-1699`)  
> Назначение: передача фронтенду изменений флоу и задач на UI

---

## A. Что изменилось в логике (чеклист → soft launch)

### 1. Юридика / доступ

| Было | Стало |
|------|--------|
| Согласия опциональны | При регистрации обязательны `AcceptPolicy` / `AcceptConsent` / `AcceptAgreement` |
| Обновление документов не мешало API | `ReConsentMiddleware`: без актуальных Policy/Consent/Agreement → **403**, `errorCode=AgreementNotFound`, поле `missingDocuments[]` |
| — | Push `AgreementUpdateRequired` **отдельно на каждый** тип документа (не схлопывать) |
| — | `DELETE /api/accounts/me`, `GET /api/accounts/me/export` |

Whitelist при re-consent (запросы не блокируются): `/api/agreements`, `/api/authorization`, `/api/accounts/create`, `/api/accounts/me`, health/swagger (с учётом path base `/eList`).

### 2. Возраст

- Анонимное «мне 18+»: TTL `agreements.anonymousAgeTtlHours` (default 24).
- `AdultConfirmed`: возраст **`>= 18`** (раньше было `> 18`).
- Платные анонсы (`Cost > 0`) по-прежнему требуют adult-подтверждения на бэке.

### 3. Участие / приватность / ACL

- BW/WL и visibility жёстче на participate/invite.
- Person: BirthDate/Gender/Patronymic скрыты по ACL; ФИО пока видны.
- Media: доступ к альбомам через `AlbumAccessValidator` + видимость события.
- Soft-delete категорий/типов событий и contact types (UI не должен ломаться на «удалённых» справочниках).

### 4. Организации

- Верификация + payout (зашифрован на бэке).
- `can_sell_tickets` только у verified-организации.
- Уведомления: member add/remove/deactivate, transfer ownership, verification approved/rejected.

### 5. Уведомления (P0–P2 + antiflood)

Новые / важные типы для ленты:

| Type | Когда |
|------|--------|
| `InvitationAccepted` / `Declined` / `Cancelled` | accept / decline / cancel |
| `RemovedFromEvent`, `NotInWhiteList`, BL/WL add/remove | исключение / листы |
| `EventRestored`; update/cancel также **соорганизаторам** | lifecycle |
| `EventOrganizatorAssigned` / `Removed` | назначение |
| Org member / ownership / verification | организация |
| `EventRatingChanged` vs `NewEventRating`, `EventRatingDeleted` | рейтинг |
| Digests: `EventRatingDigest`, `ParticipatedDigest`, `EventLeftDigest`, `RelatedPersonActivityDigest` | antiflood |
| `AgreementUpdateRequired` | новый юр. документ (по одному типу) |
| `MessageReplied` | **только reply** |
| `NewMessage` | **больше не шлётся** (broadcast отключён до адресации сообщений) |

Правила antiflood (сервер, `appsettings.notificationFlood`):

- рейтинги / join-leave для организаторов: first-K + digest;
- RelatedPerson*: по умолчанию 1:1, можно `mode=digest`;
- `EventUpdated`: пуш только при смене времени / места / названия / active / coords (не description/cover).

### 6. Цена события (без продажи билетов)

- `Cost > 0` + `TicketsEnabled=false` = **анонс «оплата на месте»**, Participate **свободный**.
- Продажа через API гейтится `features.ticketSalesEnabled` (в prod сейчас `false`).

---

## B. Постановка UI — основная функциональность (после чеклиста)

### B1. Регистрация и re-consent (P0)

1. Чекбоксы трёх документов + ссылки на `GET /api/agreements/documents/last/{type}`.
2. Без галочек — не отправлять create.
3. Глобальный обработчик **403** + `errorCode=AgreementNotFound` + `missingDocuments`:
   - экран «Обновите соглашения»;
   - для каждого типа — текст документа + `GET /api/agreements/agree/{documentType}`;
   - после принятия — повторить исходный запрос / вернуться в приложение.
4. По пушу `AgreementUpdateRequired` в ленте — тот же флоу (**по одному документу**, не «все сразу одной кнопкой»).

### B2. Возраст

1. Анонимный gate: `GET /api/agreements/age/anonymous/agree` + `.../get`; учитывать TTL 24ч (после истечения — снова спросить).
2. Везде, где UI считал «взрослый с 19», поправить на **18+**.
3. Для событий с `Cost > 0` — не пускать в чувствительные действия без adult (как на бэке).

### B3. Профиль / данные

1. Настройки: «Скачать мои данные» → `GET /api/accounts/me/export`.
2. «Удалить аккаунт» → `DELETE /api/accounts/me` + logout + понятный copy (анонимизация).
3. Чужие профили: не ждать BirthDate/Gender/Patronymic; не падать, если полей нет.

### B4. События и участие

1. Карточка события:
   - `Cost > 0` и `!TicketsEnabled` → бейдж вроде «Платно на месте», кнопка **«Участвовать»** как сейчас;
   - `TicketsEnabled` → см. раздел C (кнопка покупки, не Participate).
2. Ошибки BW/WL/private — показывать `message` с API, не общий «ошибка».
3. Справочники категорий/типов — переживать soft-delete (скрывать неактивные, не кэшировать навсегда).

### B5. Лента уведомлений

1. Добавить рендеры для типов из §A5 (invites, BL/WL, org, organizator, rating, digests, agreement).
2. Digests: UI по `Type` + `Data.Count` («Ещё N оценок…»), не как одиночное действие человека.
3. Чат события: **не ждать** push на каждое сообщение; опираться на polling/WS чата; push только на **ответ вам** (`MessageReplied`).
4. `EventUpdated`: не ожидать пуш на смену описания/обложки.

### B6. Организации (организаторский UI)

1. Статусы верификации + тексты из notification types.
2. Включение «продажа билетов» — только после verified + `can_sell_tickets` (ошибка API → понятный UX «нужна верификация / реквизиты»).

---

## C. Постановка UI — билеты и оплата

**Статус бэка:** API в PR [#40](https://github.com/crash240192/EList.API/pull/40) (`cursor/tickets-payments-stub-1699`).  
Глобальный флаг `features.ticketSalesEnabled` по умолчанию `false`. UI можно разрабатывать сразу; на стенде для тестов флаг включают в `appsettings`.

### Продуктовые правила

| Режим события | UI |
|---------------|-----|
| `TicketsEnabled=false`, любой Cost | Участие как сейчас; Cost — информационный («на месте») |
| `TicketsEnabled=true`, `Cost=0` | Бесплатный билет: заказ → сразу Paid + участие |
| `TicketsEnabled=true`, `Cost>0` | Покупка → stub/ЮKassa → после оплаты билет + участие |
| Participate / accept invite при `TicketsEnabled` | **Запрещены** (`OrganizationPaymentRequired`) → вести на покупку |

- Одна квота `MaxPersonsCount`; билет = auto-Participate.
- Типов билетов пока нет (только `quantity`).
- Чек-ин / refund / PDF / Wallet pass — вне этого этапа.
- Кошельки/тарифы платформы — **другой** денежный контур, не смешивать с билетами.

### Экраны и флоу

#### C1. Карточка события (участник)

- Если `TicketsEnabled`: первичная CTA **«Получить билет» / «Купить билет»**, не «Участвовать».
- Показать цену (`Cost` или «Бесплатно»), остаток мест при лимите.
- Если уже участник / есть билет — «Вы идёте» + вход в «Мои билеты».

#### C2. Checkout

1. `POST /eList/api/orders` — body: `{ eventId, quantity, idempotencyKey? }`.
2. Если `paidImmediately=true` → success + билеты + participation.
3. Если есть `confirmationUrl` → открыть оплату (сейчас stub-URL; позже ЮKassa).
4. После возврата: `POST /eList/api/orders/payments/complete` — `{ orderId }` или `{ providerPaymentId }`  
   (stub; для реальной ЮKassa оплату подтвердит webhook, complete может стать «проверить статус»).
5. Ошибки: места кончились, нет права продажи, продажи выкл., уже участник — показывать `message` с API.

#### C3. Мои заказы / билеты

- `GET /api/orders/my`, `GET /api/orders/{id}`
- `GET /api/orders/tickets/my?eventId=`
- Карточка билета: код (`Code`), статус `issued | used | refunded | void`, событие, дата выдачи.
- QR из `Code` (чек-ин API позже — пока «показать на входе»).

#### C4. Организатор: параметры события

Разделить в форме:

- **Стоимость анонса** (`Cost`) — всегда можно;
- **Продажа билетов** (`TicketsEnabled`) — только если глобальный флаг + org `can_sell_tickets`.

Copy: «Стоимость без продажи билетов = оплата на месте».  
При `TicketsEnabled` список участников пополняется после оплаты; отдельный Participate для гостей не нужен.

#### C5. Организатор: продажи (минимум v1)

- Отдельный список заказов по событию на UI можно отложить, если нет удобного org-эндпоинта — достаточно «участники = купившие».
- Позже: чек-ин по `GET /api/orders/tickets/byCode/{code}` (доступен организатору события).

#### C6. Feature flags на клиенте

- Скрыть ticketing UX, пока `ticketSalesEnabled` выкл. (или ловить ошибку API).
- Не смешивать с wallet/тарифами платформы.

### API (шпаргалка)

```
POST   /eList/api/orders
POST   /eList/api/orders/payments/complete
GET    /eList/api/orders/my
GET    /eList/api/orders/{orderId}
GET    /eList/api/orders/tickets/my?eventId=
GET    /eList/api/orders/tickets/byCode/{code}
```

Ответ create (`CreateOrderResponse`):

- `order` — заказ;
- `confirmationUrl?` — URL оплаты;
- `providerPaymentId?`;
- `paidImmediately` — true для бесплатного билета.

Конфиг бэка (для стенда):

```json
"payments": {
  "provider": "yookassaStub",
  "commissionPercent": 10,
  "currency": "RUB",
  "returnUrl": "https://tvoy-spot.ru/payments/return"
},
"features": {
  "ticketSalesEnabled": true
}
```

### Вне scope UI на этом этапе

- Реальный webhook ЮKassa
- Refunds
- Типы/тарифы билетов
- 54-ФЗ / чек
- Apple/Google Wallet
- Лист ожидания

---

## Приоритет внедрения на UI

1. Re-consent + регистрация согласий + age `>= 18`
2. Лента: новые типы + digests + reply-only чат
3. Карточка события: Cost «на месте» vs будущие билеты
4. Delete / export аккаунта
5. Ticketing UX (после мержа #40 и включения флага на стенде)

---

## Связанные артефакты

| Артефакт | Ссылка / путь |
|----------|----------------|
| Production checklist | `docs/production-readiness-checklist.md` |
| Notifications P0 | PR #36 (merged) |
| Adult `>= 18` | PR #35 (merged) |
| Antiflood | влит в `develop` (`notificationFlood`) |
| Tickets T0+T1 + YooKassaStub | PR #40 |
