# EList 3.0.1 — Чеклист готовности к продакшн-релизу

> Дата аудита: 31 августа 2026  
> Ветка: `develop`  
> Рекомендуемый scope MVP v1: **бесплатная социальная платформа событий** (без платёжного контура)

---

## Обзор готовности

| Категория | Оценка | Комментарий |
|-----------|--------|-------------|
| Ядро (аккаунты, auth, события, подписки) | 🟢 ~75% | Рабочий happy path |
| Социальное (участие, приглашения, чаты) | 🟡 ~60% | TODO по правам и BW-листам |
| Организации + модерация | 🟢 ~80% | Сильная сторона |
| Медиа | 🟡 ~50% | ~10 TODO по ACL |
| Платежи/билеты | 🔴 ~5% | Только схема БД + repository |
| Юридика / compliance | 🟡 ~30% | Инфраструктура есть, enforcement нет |
| Production hardening | 🔴 ~20% | Секреты в репо, CORS, stack trace |

---

## Карта модулей

### ✅ Реализовано (пригодно для MVP с доработками)

| Модуль | Эндпоинты | Статус |
|--------|-----------|--------|
| **Accounts** | create, getData, updateLocation | ✅ |
| **Authorization** | login, activate, check, сброс пароля | ✅ |
| **Events** | CRUD, search, categories/types, cancel | ✅ (delete категорий/типов — crash) |
| **EventTemplates** | CRUD + search | ✅ |
| **Subscriptions** | subscribe/unsubscribe, counts | ✅ |
| **Rating** | vote, get, delete | ✅ |
| **Organizations** | CRUD, members, legal, payout, verification, INN lookup | ✅ |
| **ContentReports** | жалобы, очереди, penalties, resolve | ✅ |
| **BugReports** | категории + отчёты | ✅ |
| **PlatformRoles** | moderator/admin/superuser | ✅ |
| **Notifications** | WebSocket + REST (my, read, send) | ✅ |
| **SystemNotifications** | admin CRUD шаблонов, SMTP/SMS | ✅ |
| **Agreements** | документы, age gate, user/org consent | ⚠️ инфраструктура без enforcement |

### 🟡 Частично — нужно закрыть до prod

| Модуль | Проблемы |
|--------|----------|
| **Media** | Нет ACL: любой может редактировать/смотреть чужие альбомы |
| **Participations** | BW-листы не проверяются; нет уведомлений об исключении |
| **Invitations** | BW-листы и права организации — TODO |
| **Conversations** | Event-чаты не попадают в списки byAccount/byEvent |
| **Persons** | `PUT update` закомментирован; валидация отключена |
| **Contacts** | Валидация телефона — `NotImplementedException` |
| **Wallets** | Deposite/Charge есть в сервисе, но не в API |
| **Events** | DELETE categories/types → `NotImplementedException` |

### 🔴 Не реализовано (можно отложить для MVP v1)

| Модуль | Что есть в коде |
|--------|-----------------|
| **Payments/Orders/Tickets** | Таблицы + `IOrdersDataProvider`, модели, enum провайдеров — **нет контроллера/сервиса** |
| **Payment webhooks** | Таблица `payment_webhook_events` — нет обработчиков |
| **Auto-invitations** | Только таблицы в InitialDatabase.sql |
| **DebtCollectorWorker** | Реализован, но `active: false` |
| **Account deletion / data export** | Полностью отсутствует |
| **Automated tests** | Нет test projects |

---

## Scope первой итерации (MVP v1)

### В scope v1

- [ ] Регистрация → активация → профиль → поиск/создание **бесплатных** событий
- [ ] Участие, приглашения, подписки, рейтинг
- [ ] Медиа (фото событий, аватары)
- [ ] Чаты и push-уведомления
- [ ] Организации (информационные, без `can_sell_tickets`)
- [ ] Модерация + bug reports
- [ ] Юридические документы + обязательное согласие при регистрации

### За пределами v1 (v1.1+)

- [ ] Платные события и билеты (YooKassa/TBank)
- [ ] Кошельки с пополнением
- [ ] Auto-invitations
- [ ] Premium-тарифы и DebtCollector

---

## P0 — Блокеры prod (первый порядок)

### 🔒 Инфраструктура и безопасность

- [ ] **Убрать все секреты из `appsettings.json`** — сейчас в репозитории лежат пароли БД, SMTP, SMS API, DaData, filestorage token, encryption salt
- [ ] **Secrets manager / env vars** — доработать `ConfigurationManager` (сейчас читает только `appsettings.json`)
- [ ] **CORS** — заменить `SetIsOriginAllowed(origin => true)` на whitelist доменов клиентов
- [ ] **Stack trace в ответах** — убрать из `ErrorHandlingMiddleware` для prod (сейчас отдаётся клиенту)
- [ ] **Media ACL** — закрыть ~10 TODO в `MediaService` (утечка/редактирование чужих альбомов)
- [ ] **Role checks** — `notifications/send`, `notifications/broadcast`, `agreements/documents/add` доступны любому авторизованному пользователю
- [ ] **Rate limiter** — `EventCreateRateLimiter` in-memory; для multi-instance нужен Redis/DB
- [ ] **Исправить `ErrorCode.AgreementNotFound`** — используется в `AgreementService`, но отсутствует в enum
- [ ] **HTTPS + reverse proxy** — cert management, HSTS
- [ ] **Ротация encryption keys** — заменить salt `"per rectum ad astra"` на production-grade ключи

### ⚖️ Юридика (обязательно для публичного запуска в РФ)

- [ ] **Подготовить и опубликовать юридические тексты** (юрист):
  - [ ] Политика обработки ПДн (`Policy`)
  - [ ] Согласие на обработку ПДн (`Consent`)
  - [ ] Пользовательское соглашение (`Agreement`)
- [ ] **Seed migration или admin upload** — загрузить документы v1.0.0 в таблицу `documents`
- [ ] **Enforce consent при регистрации** — блокировать `POST /accounts/create` без принятия Policy + Consent + Agreement
- [ ] **Middleware re-consent** — при новой версии документа блокировать API до повторного согласия
- [ ] **Account deletion API** — право на удаление (152-ФЗ ст. 14, GDPR Art. 17): cascade/anonymization person, contacts, messages, media, agreements
- [ ] **Data export API** — machine-readable dump данных пользователя (GDPR Art. 20)
- [ ] **Защитить `POST documents/add`** — только admin/superuser
- [ ] **Исправить баги AgreementsController** — `SaveUserAgreementAsync` с `[AllowAnonymous]` но требует AccountId; контроллер игнорирует результат сервиса
- [ ] **Privacy notice для процессоров** — DaData, GreenSMS, Yandex SMTP, filestorage (договоры поручения обработки ПДн)
- [ ] **Контакт оператора / DPO** — email в Policy и в API/docs

### 🛡️ Возрастные ограничения

- [ ] **Исправить `Age > 18` → `>= 18`** в `EventsService` (пользователь ровно 18 лет блокируется)
- [ ] **Обязательный age gate** перед регистрацией или созданием события
- [ ] **Решить TTL anonymous agreement (1 час)** — слишком короткий для UX; сделать persistent или привязать к сессии
- [ ] **Документировать self-declaration** — юридически зафиксировать, что верификация возраста = галочка, не документ

### 🔧 Функциональные блокеры

- [ ] **DELETE eventCategories/eventTypes** — реализовать или убрать endpoint (сейчас `NotImplementedException`)
- [ ] **Валидация телефона** — `ContactDataValidator.ValidatePhoneNumber` бросает exception
- [ ] **BW-листы в Invitations/Participations** — enforce при invite/participate
- [ ] **Проверки прав организатора** — TODO в ParticipationsService, EventOrganizatorsService
- [ ] **Шифрование payout-данных** — `organization_payout` (bank_account, BIK) хранится в plaintext
- [ ] **Privacy ACL на `GET /persons/get/{accountId}`** — доступ к чужим PII без проверки
- [ ] **Отключить платные события в v1** — feature flag `paidEventsEnabled: false`

### 📋 Операционка

- [ ] **LICENSE** — файл отсутствует
- [ ] **CI/CD pipeline** — нет GitLab CI config в репо
- [ ] **Health check endpoint** — для k8s/load balancer
- [ ] **Мониторинг** — алерты на 5xx, DB connection, SMS/SMTP failures
- [ ] **Backup strategy** — PostgreSQL + filestorage
- [ ] **Логирование PII** — audit: не логировать plaintext email/phone в NLog

---

## P1 — Второй порядок (после v1 / параллельно)

### Функциональность

- [ ] **Person update endpoint** — раскомментировать `PUT /persons/update`
- [ ] **Media: `setParameters` для альбомов** — закомментированный endpoint
- [ ] **Event-чаты в Conversations** — TODO в byAccount/byEvent
- [ ] **Уведомления об исключении** из BW-листа / участников
- [ ] **Invitations: заполнить `result.Event`** — TODO в repository
- [ ] **Premium-параметры событий** — валидация по тарифу
- [ ] **Wallets Deposite API** — если нужен ручной billing до платёжного провайдера
- [ ] **DebtCollectorWorker** — включить после тестирования тарифной логики
- [ ] **Auto-invitations** — реализовать поверх существующих таблиц
- [ ] **Локализация** — `localization.enabled: false`, подготовить i18n

### Платежи (v1.1 — отдельный релиз)

- [ ] **OrdersService + PaymentsController** — create order, initiate payment
- [ ] **Webhook controller** — YooKassa/TBank, idempotency через `payment_webhook_events`
- [ ] **Tickets API** — issue, validate, mark used
- [ ] **Refunds** — partial/full
- [ ] **TicketingAgreement** — юридический документ для организаций с `can_sell_tickets`
- [ ] **54-ФЗ / онлайн-касса** — интеграция с фискализацией
- [ ] **Organization onboarding** — payment provider seller ID flow

### Юридика / compliance (P1)

- [ ] **Отзыв согласия** — механизм withdraw consent + последствия
- [ ] **Retention policy** — автоочистка: inactive tokens, anonymous_age_agreements, logs
- [ ] **Appeal workflow** — user-facing обжалование модерационных санкций
- [ ] **Audit log staff-доступа к PII**
- [ ] **Cookie policy** — на стороне web-клиента (backend N/A для mobile API)
- [ ] **Transparency report** — если целевой рынок EU (DSA)

### Качество и DX

- [ ] **Unit/integration tests** — хотя бы smoke для auth, events, agreements
- [ ] **Swagger v3** — сейчас SerializeAsV2
- [ ] **README** — заменить GitLab template на документацию проекта
- [ ] **Исправить route conflict** — `WalletsController.GetWalletAsync` с абсолютным `[HttpGet("/{walletId}")]`
- [ ] **Миграции** — исправить порядок таблиц в InitialDatabase.sql для fresh install

---

## Сводная матрица по волнам

| Область | P0 (до prod) | P1 (после prod) | v1.1+ |
|---------|-------------|-----------------|-------|
| Регистрация + auth | ✅ + enforce consent | person update | — |
| Бесплатные события | ✅ + fix deletes | premium params | — |
| Участие/приглашения | ✅ + BW enforce | notifications | auto-invite |
| Медиа | ✅ + ACL | album params | — |
| Организации | ✅ (info only) | — | verified + tickets |
| Модерация | ✅ | appeal flow | AI moderation |
| Платежи | ❌ disable | — | full stack |
| Юридика | ✅ docs + delete/export | retention, withdraw | ticketing agreement |
| Infra security | ✅ all items | monitoring | multi-region |

---

## Критические находки в коде

| # | Проблема | Файл / место | Приоритет |
|---|----------|--------------|-----------|
| 1 | Секреты в git (БД, SMTP, SMS, DaData, filestorage) | `EList.Api/appsettings.json` | 🔴 P0 |
| 2 | Media ACL — ~10 TODO без проверки владельца/доступа | `EList.Services.Impl/MediaService.cs` | 🔴 P0 |
| 3 | `ErrorCode.AgreementNotFound` не в enum | `AgreementService.cs` vs `ErrorCodes.cs` | 🔴 P0 |
| 4 | CORS allow-all + credentials | `EList.Api/Program.cs` | 🔴 P0 |
| 5 | Stack trace в HTTP-ответах | `EList.Api/Middleware/ErrorHandlingMiddleware.cs` | 🔴 P0 |
| 6 | DELETE categories/types → NotImplementedException | `EventsMetadataDataProvider.cs` | 🟡 P0 |
| 7 | Валидация телефона → NotImplementedException | `ContactDataValidator.cs` | 🟡 P0 |
| 8 | Платежи — только schema, нет API | `DevelopMigration.sql`, `IOrdersDataProvider` | ⚪ v1.1 |

---

## Рекомендуемый порядок работ

```
Фаза 1 — Блокеры безопасности:
  Secrets → CORS → stack trace → Media ACL → AgreementNotFound fix

Фаза 2 — Compliance:
  Legal docs (юрист) → seed documents → enforce consent middleware
  → Account deletion → Data export → Age logic fix

Фаза 3 — Функциональные дыры:
  BW-lists enforce → DELETE categories fix → Phone validation
  → Organizer permission checks → Role checks на admin endpoints

Фаза 4 — Операционка:
  Health check → CI/CD → Monitoring → Backup strategy

После soft launch:
  P1 items → Payments v1.1 (отдельный epic)
```

---

## Связанные документы

- [content-reports-ui.md](./content-reports-ui.md) — спецификация UI/API модерации
- [AGENTS.md](../AGENTS.md) — техническая документация для разработки
