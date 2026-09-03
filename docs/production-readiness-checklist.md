# EList 3.0.1 — Чеклист готовности к продакшн-релизу

> Первичный аудит: 31 августа 2026  
> Актуализация: 3 сентября 2026 (`origin/develop` @ `0f57cf8`)  
> Уточнения от владельца: 3 сентября 2026 (секреты через `.env` на проде; юр. документы в БД, не в git; rate limiter → P1)  
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
| Юридика / compliance | 🟡 ~30% | 🟢 ~80% | Enforce + re-consent + export; документы в prod БД (не в git) |
| Production hardening | 🔴 ~20% | 🟢 ~75% | CORS/errors/CI/health; prod secrets из `.env` |

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
| **Agreements** | ✅ | Enforcement есть; тексты загружены в prod БД (в git не хранятся — ок) |
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
- [x] **Prod secrets из `.env` / host config** — на проде секретное убрано из appsettings; подтягивается при деплое
- [ ] **(опционально) Почистить секреты в git-истории / dev appsettings** — в репозитории всё ещё лежат plaintext значения; для prod не блокер, но риск утечки через историю/форки. Ротация ключей, если они когда-либо светились публично.
- [ ] **HTTPS / reverse proxy / HSTS** — операционный деплой (если ещё не закрыто инфраструктурой)

### ⚖️ Юридика

- [x] **Enforce consent при регистрации** — `AcceptPolicy/Consent/Agreement`
- [x] **Re-consent middleware** — `features.reConsentEnforcementEnabled`
- [x] **Data export API** — `GET /api/accounts/me/export`
- [x] **Account deletion API** — `DELETE /api/accounts/me` (анонимизация + deactivate)
- [x] **`documents/add` admin-only**
- [x] **Баги AgreementsController** — agree не anonymous, возвращает результат сервиса
- [x] **Юридические тексты Policy / Consent / Agreement** — загружены в prod БД; в git не дублируем (source of truth = `documents`)
- [ ] **Договоры поручения с процессорами** — DaData, GreenSMS, Yandex SMTP, filestorage (бумажная/договорная работа, не код)
- [ ] **Контакт оператора / DPO** в Policy (если ещё не указан в загруженном тексте)
- [ ] **Углубить delete** — cascade/anonymize messages, media, agreements (сейчас soft anonymize person/contacts) → можно P1

### 🛡️ Возраст

- [x] **TTL anonymous age configurable** — `agreements.anonymousAgeTtlHours` (default 24)
- [ ] **Исправить `Age > 18` → `>= 18`** в `AccountDataHolder.AdultConfirmed` — в работе
- [ ] **Age gate при регистрации** — сейчас только self-declaration для 18+/платных событий (P1, если Policy это покрывает)
- [ ] **Зафиксировать в Policy**, что age = self-declaration (если ещё не зафиксировано)

### 🔧 Функциональные блокеры

- [x] **Soft-delete eventCategories / eventTypes / contactTypes**
- [x] **Валидация телефона/email** — `ContactValidator`
- [x] **BW-листы + visibility** в invitations/participations
- [x] **Шифрование organization_payout**
- [x] **Privacy ACL persons** — BirthDate/Gender/Patronymic скрыты; ФИО всё ещё видны всем
- [x] **`ticketSalesEnabled: false`** — продажа билетов через API заблокирована
- [ ] **Уведомления об исключении** из участников / BW (TODO в `ParticipationsService`) → P1
- [ ] **Список организаторов события** — TODO доступа в `EventOrganizatorsService.GetByEventIdAsync` → P1
- [ ] **Event-чаты в Conversations** — TODO byAccount/byEvent → P1

### 📋 Операционка

См. подробную расшифровку в разделе [Monitoring / backup / LICENSE](#ops-monitoring-backup-license) ниже.

- [x] Health endpoint
- [x] CI restore/build
- [ ] **Мониторинг / алерты** — см. детали ниже
- [ ] **Backup & restore** — см. детали ниже
- [ ] **LICENSE** — см. детали ниже
- [ ] **README** вместо GitLab template (желательно, не блокер)

---

## P1 — Второй порядок

### Инфра (перенесено из P0)

- [ ] **Rate limiter multi-instance** — `EventCreateRateLimiter` in-memory; на старте при одном инстансе достаточно; при scale-out → Redis/DB shared store
- [ ] Углубить account delete (media/messages/agreements)
- [ ] Notify об исключении / event-чаты в Conversations / GetByEventId ACL

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
| Secrets (prod `.env`) | ✅ закрыто | опционально: чистка git history | — |
| Legal texts в БД | ✅ закрыто | DPO/процессоры если не в тексте | ticketing agreement |
| Age `>= 18` | ❗ в работе | age gate на signup | — |
| Monitoring / backup / LICENSE | желательно до soft launch | — | — |
| Rate limiter shared | — | ❗ при multi-instance | — |
| Conversations event-chats | — | желательно | — |
| Платежи | выкл. флагом | — | full stack |
| Product UX (ниже) | — | discovery + reminders | tickets UX |

---

<a id="ops-monitoring-backup-license"></a>

## Monitoring / backup / LICENSE — что имеется в виду

Это не одна задача в коде, а **три операционных пакета**. Для MVP достаточно минимального набора; полный — по мере роста трафика.

### 1. Monitoring (наблюдаемость + алерты)

**Цель:** узнать о проблеме раньше пользователей.

| Слой | Минимум для soft launch | Дальше (P1) |
|------|-------------------------|-------------|
| **Liveness** | Уже есть `GET /health` — дергать из LB / Docker / k8s probe каждые 10–30с | Readiness: проверка соединения с PostgreSQL (+ опционально filestorage) отдельным `/ready` |
| **Метрики** | Счётчик 5xx / latency на reverse proxy (nginx/Caddy/Traefik access log) или простой uptime-check (UptimeRobot / Better Stack / Grafana Cloud free) на `/health` и на `https://tvoy-spot.ru` | Prometheus + `/metrics` (request duration, DB pool, WS connections) |
| **Логи** | Централизованный сбор NLog (файл → stdout уже есть) в одно место: Docker logs / Loki / CloudWatch / journald | Алерт по паттернам: `Failed to call`, SMTP/SMS errors, spike `InternalError` |
| **Алерты «болит»** | 1–2 канала (Telegram/email): API down > 2 мин; error rate > N%/5 мин | Отдельно: SMS-провайдер 4xx/5xx, DaData timeout, disk > 80%, PG connections |
| **Бизнес-сигналы (опционально)** | — | Регистрации/день, create event fail, activation code fail rate |

**Конкретные работы (чеклист):**
- [ ] Uptime-check на `/eList/health` (или `/health` с учётом pathBase) + уведомление в Telegram/почту
- [ ] Reverse proxy логирует status/latency; раз в день глазами или простой dashboard
- [ ] Понимание, куда пишутся `logs/yyyy-MM-dd.log` на проде и сколько места занимают
- [ ] (желательно) алерт на рост 5xx

**Не требуется для MVP:** полный APM (AppInsights/Jaeger), distributed tracing, SLO dashboard.

### 2. Backup (резервное копирование и восстановление)

**Цель:** не потерять ПДн и контент при падении диска / ошибке миграции / ransomware.

| Что бэкапить | Минимум | Проверка |
|--------------|---------|----------|
| **PostgreSQL** | Ежедневный logical dump (`pg_dump`) или snapshot тома; хранение ≥ 7 дней off-host (S3/другой сервер) | Раз в месяц: restore на staging и `SELECT count(*)` по `accounts`/`events` |
| **Filestorage** | Копия object storage / volume с медиа (аватары, альбомы) с той же периодичностью | Выборочно открыть 2–3 файла после restore |
| **Секреты / `.env`** | Отдельный сейф (1Password/Bitwarden/sealed secret), не только на сервере | Доступ у ≥ 2 человек |
| **Точка отката миграций** | Перед каждой schema-migration — ручной dump | Документированный rollback |

**Конкретные работы (чеклист):**
- [ ] Cron/скрипт ночного `pg_dump` (custom или plain) → удалённое хранилище
- [ ] Retention policy: например 7 daily + 4 weekly
- [ ] Бэкап filestorage (rsync/S3 sync)
- [ ] Письменный runbook: «как восстановить за ≤ 1–2 часа»
- [ ] Один успешный drill restore до публичного анонса (или сразу после soft launch)

**152-ФЗ / здравый смысл:** ПДн в бэкапах тоже ПДн — шифрование at-rest бэкапов и ограничение доступа.

### 3. LICENSE (правовой статус кода репозитория)

**Цель:** явно зафиксировать, **кому принадлежит код** и можно ли его копировать/форкать.

| Вариант | Когда выбирать |
|---------|----------------|
| **Нет публичного LICENSE + private repo** | Коммерческий продукт: код не open source. Тогда LICENSE в git **не обязателен**; важнее NDA/договоры с подрядчиками и © в Policy |
| **Proprietary LICENSE / «All rights reserved»** | Если репо когда-либо станет видимым: короткий файл «© … Все права защищены. Использование без согласия запрещено» |
| **Open source (MIT/Apache-2.0)** | Только если сознательно открываете код |

**Конкретные работы:**
- [ ] Решить: репо остаётся private commercial → достаточно © в пользовательском соглашении / Policy; файл `LICENSE` опционален
- [ ] Если репо public или есть внешние контрибьюторы → добавить явный `LICENSE` + CLA/условия вклада
- [ ] Проверить, что сторонние NuGet-пакеты совместимы с выбранной моделью (обычно ок для proprietary backend)

**Итог по LICENSE для EList:** скорее всего достаточно private repo + формулировки в Agreement/Policy; отдельный MIT/Apache не нужен, если не планируете open source.

### Приоритет ops для soft launch

```
Must:   uptime-check /health + алерт «сервис лежит»
Must:   ежедневный pg_dump off-host + понимание, как restore
Should: бэкап filestorage + retention
Nice:   LICENSE/© формулировка; README; deploy pipeline в CI
Later:  Prometheus, ready-probe с DB, drill restore по расписанию
```

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
| 1 | `Age > 18` вместо `>= 18` | `AccountDataHolder.cs:66` | 🟡 P0 (в работе) |
| 2 | Monitoring / backup (минимум) | ops | 🟡 желательно до soft launch |
| 3 | Секреты plaintext в git-истории / dev appsettings | репозиторий | ⚪ не блокер prod (prod на `.env`) |
| 4 | Rate limiter in-memory | `EventCreateRateLimiter` | ⚪ P1 (при multi-instance) |
| 5 | Delete аккаунта не чистит media/messages | `AccountsService` | ⚪ P1 |
| 6 | Платежи — только schema | `IOrdersDataProvider` | ⚪ v1.1 |

---

## Рекомендуемый порядок до soft launch

```
1. Fix Age >= 18
2. Минимум ops: uptime на /health + ежедневный pg_dump off-host
3. (по желанию) бэкап filestorage, ©/LICENSE формулировка
4. Soft launch на tvoy-spot.ru
5. Итерация 2: shared rate limiter, event-чаты, notify об исключении, product UX
```

---

## Связанные документы

- [content-reports-ui.md](./content-reports-ui.md) — спецификация UI/API модерации
- [AGENTS.md](../AGENTS.md) — техническая документация для разработки
