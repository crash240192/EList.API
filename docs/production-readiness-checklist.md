# EList 3.0.1 — Чеклист готовности к продакшн-релизу

> Первичный аудит: 31 августа 2026  
> Актуализация: 3 сентября 2026 (`origin/develop` @ `0f57cf8`)  
> Рекомендуемый scope MVP v1: **бесплатная социальная платформа событий** (продажа билетов через API выключена)

---

## Что изменилось с 31 августа

С момента первого чеклиста закрыт большой блок P0 по hardening, ACL и compliance. Ключевые коммиты:

| Коммит | Суть |
|--------|------|
| `581a9dc` … `219a012` | Person/contact/album/event/invitation validators + ACL |
| `f3a45ed` | CORS whitelist, safe errors, admin roles, payout crypto, `/health` |
| `80a4bc4` | Soft-delete каталогов, consent при регистрации, delete/export аккаунта, `ticketSalesEnabled` |
| `6102148` | `ReConsentMiddleware` |
| `566d96d` | Configurable age TTL, PII redaction в логах, GitHub Actions CI |
| `902616b` / `0f57cf8` | Host configuration + CI sibling layout для EList.Common |

---

## Обзор готовности (актуально)

| Категория | Было (31.08) | Сейчас (03.09) | Комментарий |
|-----------|--------------|----------------|-------------|
| Ядро (аккаунты, auth, события, подписки) | 🟢 ~75% | 🟢 ~85% | Consent, delete/export, validators |
| Социальное (участие, приглашения, чаты) | 🟡 ~60% | 🟢 ~80% | BW/access validators; чаты event — TODO |
| Организации + модерация | 🟢 ~80% | 🟢 ~85% | Payout encryption |
| Медиа | 🟡 ~50% | 🟢 ~85% | AlbumAccessValidator |
| Платежи/билеты | 🔴 ~5% | 🔴 ~5% | Schema only; `ticketSalesEnabled=false` |
| Юридика / compliance | 🟡 ~30% | 🟡 ~65% | Enforce + re-consent + export; нет текстов документов |
| Production hardening | 🔴 ~20% | 🟡 ~60% | CORS/errors/CI/health; секреты всё ещё в git |

---

## Карта модулей (актуально)

### ✅ Готово к MVP

| Модуль | Статус | Примечание |
|--------|--------|------------|
| **Accounts** | ✅ | + `DELETE /me`, `GET /me/export`, consent flags на create |
| **Authorization** | ✅ | |
| **Events** | ✅ | Soft-delete categories/types; geo search |
| **EventTemplates** | ✅ | |
| **Subscriptions** | ✅ | Access validators |
| **Rating** | ✅ | |
| **Organizations** | ✅ | Payout encrypted; verification via DaData |
| **ContentReports / BugReports / PlatformRoles** | ✅ | |
| **Notifications** | ✅ | Admin-only send/broadcast |
| **SystemNotifications** | ✅ | |
| **Agreements** | ✅/⚠️ | Enforcement есть; **текстов документов в репо/БД нет** |
| **Media** | ✅ | ACL через AlbumAccessValidator |
| **Participations / Invitations** | ✅ | BW + visibility; нет notify об исключении |
| **Wallets/Tariffs** | ⚠️ | CRUD есть; Deposite API нет; DebtCollector выкл. |

### 🔴 Вне MVP v1

| Модуль | Статус |
|--------|--------|
| Payments / Orders / Tickets / Refunds / Webhooks | Schema + `IOrdersDataProvider`, нет API |
| Auto-invitations | Только таблицы |
| Automated tests | Нет test projects |

---

## P0 — Блокеры prod (первый порядок)

### 🔒 Инфраструктура и безопасность

- [x] **CORS whitelist** — `AllowedOrigins` → `tvoy-spot.ru` (`Program.cs`)
- [x] **Stack trace в ответах** — только Development (`ErrorHandlingMiddleware`)
- [x] **Media ACL** — `AlbumAccessValidator` + event visibility
- [x] **Role checks** — notifications send/broadcast; `documents/add`
- [x] **ErrorCode.AgreementNotFound** — добавлен в EList.Common
- [x] **PII redaction в API-логах** — `LoggerHandlerWebApiFilter.RedactJson`
- [x] **ConfigurationManager из host config** — env vars / `appsettings.{env}.json`
- [x] **Health check** — `GET /health`, `GET /version`
- [x] **CI build** — `.github/workflows/build.yml`
- [ ] **Убрать секреты из `appsettings.json` + ротация** — БД, SMTP, SMS, DaData, filestorage, encryption salt всё ещё в репо
- [ ] **Production encryption keys** — salt `"per rectum ad astra"`; задать `fieldKey`/`indexKey`, ротация
- [ ] **Rate limiter multi-instance** — `EventCreateRateLimiter` всё ещё in-memory
- [ ] **HTTPS / reverse proxy / HSTS** — операционный деплой

### ⚖️ Юридика

- [x] **Enforce consent при регистрации** — `AcceptPolicy/Consent/Agreement`
- [x] **Re-consent middleware** — `features.reConsentEnforcementEnabled`
- [x] **Data export API** — `GET /api/accounts/me/export`
- [x] **Account deletion API** — `DELETE /api/accounts/me` (анонимизация + deactivate)
- [x] **`documents/add` admin-only**
- [x] **Баги AgreementsController** — agree не anonymous, возвращает результат сервиса
- [ ] **Юридические тексты Policy / Consent / Agreement** — подготовить юристом и загрузить в `documents` (seed или admin)
- [ ] **Договоры поручения с процессорами** — DaData, GreenSMS, Yandex SMTP, filestorage
- [ ] **Контакт оператора / DPO** в Policy
- [ ] **Углубить delete** — cascade/anonymize messages, media, agreements (сейчас soft anonymize person/contacts)

### 🛡️ Возраст

- [x] **TTL anonymous age configurable** — `agreements.anonymousAgeTtlHours` (default 24)
- [ ] **Исправить `Age > 18` → `>= 18`** в `AccountDataHolder.AdultConfirmed`
- [ ] **Age gate при регистрации** — сейчас только self-declaration для 18+/платных событий
- [ ] **Зафиксировать в Policy**, что age = self-declaration

### 🔧 Функциональные блокеры

- [x] **Soft-delete eventCategories / eventTypes / contactTypes**
- [x] **Валидация телефона/email** — `ContactValidator`
- [x] **BW-листы + visibility** в invitations/participations
- [x] **Шифрование organization_payout**
- [x] **Privacy ACL persons** — BirthDate/Gender/Patronymic скрыты; ФИО всё ещё видны всем
- [x] **`ticketSalesEnabled: false`** — продажа билетов через API заблокирована
- [ ] **Уведомления об исключении** из участников / BW (TODO в `ParticipationsService`)
- [ ] **Список организаторов события** — TODO доступа в `EventOrganizatorsService.GetByEventIdAsync`
- [ ] **Event-чаты в Conversations** — TODO byAccount/byEvent

### 📋 Операционка

- [x] Health endpoint
- [x] CI restore/build
- [ ] **LICENSE**
- [ ] **Deploy pipeline** (сейчас только build)
- [ ] **Мониторинг / алерты** (5xx, DB, SMTP/SMS)
- [ ] **Backup strategy** (PostgreSQL + filestorage)
- [ ] **README** вместо GitLab template

---

## P1 — Второй порядок

### Функциональность (остатки кода)

- [ ] Person `PUT update` (если ещё закомментирован)
- [ ] Media album `setParameters`
- [ ] Invitations: заполнить `result.Event`
- [ ] Premium-параметры событий по тарифу
- [ ] Wallets Deposite API / DebtCollector
- [ ] Auto-invitations
- [ ] Локализация (`localization.enabled: false`)
- [ ] Swagger v3
- [ ] Route conflict `WalletsController.GetWalletAsync` (`[HttpGet("/{walletId}")]`)
- [ ] Порядок таблиц в `InitialDatabase.sql` для fresh install
- [ ] Unit/integration smoke tests (auth, events, agreements)

### Платежи (v1.1)

- [ ] OrdersService + PaymentsController
- [ ] Webhook controller (YooKassa/TBank) + idempotency
- [ ] Tickets API (issue / validate / used)
- [ ] Refunds
- [ ] TicketingAgreement
- [ ] 54-ФЗ / онлайн-касса
- [ ] Organization payment-provider onboarding

### Compliance (P1)

- [ ] Отзыв согласия (withdraw)
- [ ] Retention / purge (tokens, anonymous age, logs)
- [ ] Appeal workflow для sanctions
- [ ] Audit log staff-доступа к PII
- [ ] Cookie policy (web-клиент)

---

## Сводная матрица по волнам

| Область | P0 остаток | P1 | v1.1+ |
|---------|------------|----|-------|
| Secrets / encryption keys | ❗ открыто | — | — |
| Legal texts + DPO | ❗ открыто | — | ticketing agreement |
| Age `>= 18` | ❗ открыто | age gate на signup | — |
| Conversations event-chats | желательно | — | — |
| Monitoring / backup / LICENSE | ❗ открыто | — | — |
| Платежи | выкл. флагом | — | full stack |
| Product UX (ниже) | — | discovery + reminders | tickets UX |

---

## Продуктовый бэклог: чего нет, но было бы полезно пользователю

Ниже — не блокеры релиза, а **ценность для пользователя** относительно текущего API. Сгруппировано по приоритету для роста продукта после soft launch.

### 🔥 Высокая ценность (быстро закрывает «дыры» в опыте)

| Фича | Зачем пользователю | База в коде |
|------|--------------------|-------------|
| **Лента / «мои события»** (upcoming / past / organizing / invited) | Сейчас есть search + participation, но нет удобного personal calendar feed | participations, invitations, events |
| **Напоминания о событии** (T−24h / T−1h) | Снижает no-show; критично для офлайн-встреч | SystemNotifications + NotificationsService |
| **Mobile push (FCM/APNs)** | In-app + WebSocket работают только при открытом клиенте | Notification hub; нет device tokens |
| **Избранное / «хочу пойти»** (wishlist без commit) | Ниже порог, чем participate; помогает организатору видеть интерес | нет сущности |
| **Шаринг / deep links** (`tvoy-spot.ru/e/{id}`) | Виральность; без этого рост только изнутри приложения | публичный get/search уже есть |
| **Профиль организатора + история прошедших событий** | Доверие до участия (рейтинг уже есть) | Rating + Events + Organizations |
| **Блокировка пользователя (user-level block)** | Сейчас только event BW-листы и модерация; нет «не видеть этого человека» | BW на уровне события |

### 🗺️ Discovery и гео

| Фича | Зачем | База |
|------|-------|------|
| **Карта событий / clusters** | Geo search (`Latitude/Longitude/LocationRange`) уже есть — нужен UX слой | EventsSearchRequest + PostGIS |
| **«Рядом со мной» + фильтр «сегодня / выходные»** | Главный entrypoint для casual user | search + `updateLocation` |
| **Рекомендации** (по подпискам, прошлым категориям, geo) | Retention после первой недели | subscriptions, participations, categories |
| **Тренды / подборки редакции** | Холодный старт без графа друзей | system notifications / admin tools |

### 👥 Социальный слой

| Фича | Зачем | База |
|------|-------|------|
| **Друзья / адресная книга контактов платформы** | Приглашения сейчас по accountId — без discovery «кого позвать» | invitations, contacts |
| **Совместные друзья на событии** («идут 3 ваших подписки») | Сильный конверсионный сигнал | subscriptions + participations |
| **Публичный / приватный профиль** (гранулярнее Show-флагов) | Privacy + social proof | PersonAccessValidator |
| **Реакции / RSVP статусы** (going / maybe / interested) | Гибче, чем binary participate | participations |
| **Event-чат в списках бесед** | Уже TODO — без этого чат события «теряется» | ConversationsController |

### 🗓️ Организатору

| Фича | Зачем | База |
|------|-------|------|
| **Чек-ин участников (QR / код)** | Контроль входа на офлайн-ивент | tickets schema почти готова |
| **Аналитика события** (views → invites → participates → no-show) | Понятно, что работает | частично notifications/participations |
| **Повтор события из шаблона в 1 тап** | Templates уже есть — нужен UX «duplicate last» | EventTemplates |
| **Co-hosts права** (уже assign organizators) + делегирование модерации чата | Масштаб команд | EventOrganizators |
| **Рассылка участникам** (email/push) с лимитами | Сейчас broadcast только platform admin | NotificationsService |
| **Лист ожидания** при лимите мест | Нет waitlist при MaxParticipants | participations |

### 🎫 Монетизация (после v1.1)

| Фича | Зачем |
|------|-------|
| Покупка билета + PDF/Wallet pass | Core paid UX |
| Промокоды / early bird | Конверсия |
| Донаты / «поддержка организатора» | Для бесплатных ивентов |
| Подписка организатора (тарифы уже в wallets) | B2B revenue без ticketing |

### ♿ Доверие и качество

| Фича | Зачем |
|------|-------|
| Верифицированный организатор badge (уже есть org verification) — показать в UI | Trust |
| Отзывы текстом к рейтингу (сейчас vote без review body?) | Качество сигналов |
| Appeal для пользователя после бана | Fairness + support load |
| Онбординг: интересы → первые 5 событий рядом | Activation |

### Рекомендуемый product-порядок после soft launch

```
1. Мои события + напоминания + push        → retention
2. Deep links + карта «рядом»              → acquisition
3. Wishlist / RSVP maybe + social proof    → conversion
4. Event-чаты в inbox + user block         → safety & UX polish
5. Организаторская аналитика + waitlist    → supply-side
6. Ticketing v1.1                          → monetization
```

---

## Критические находки (оставшиеся)

| # | Проблема | Где | Приоритет |
|---|----------|-----|-----------|
| 1 | Секреты в git | `appsettings.json` | 🔴 P0 |
| 2 | Шуточный encryption salt | `encryption.salt` | 🔴 P0 |
| 3 | Нет загруженных Policy/Consent/Agreement | таблица `documents` | 🔴 P0 |
| 4 | `Age > 18` вместо `>= 18` | `AccountDataHolder.cs:66` | 🟡 P0 |
| 5 | Rate limiter in-memory | `EventCreateRateLimiter` | 🟡 P0 (multi-node) |
| 6 | Нет monitoring/backup/LICENSE | ops | 🟡 P0 |
| 7 | Delete аккаунта не чистит media/messages | `AccountsService.DeleteMyAccountAsync` | 🟡 P0/P1 |
| 8 | Платежи — только schema | `IOrdersDataProvider` | ⚪ v1.1 |

---

## Рекомендуемый порядок до закрытия P0

```
1. Ротация всех секретов + вынос в secrets/env; новые encryption keys
2. Юр. тексты → upload documents → smoke re-consent
3. Fix Age >= 18; DPO contact в Policy
4. Monitoring + backups + LICENSE
5. (желательно) Event-чаты в Conversations + notify об исключении
6. Soft launch на tvoy-spot.ru
```

---

## Связанные документы

- [content-reports-ui.md](./content-reports-ui.md) — спецификация UI/API модерации
- [AGENTS.md](../AGENTS.md) — техническая документация для разработки
