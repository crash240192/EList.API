# Постановка: жалобы на контент (UI)

Документ для мобильного/веб-клиента. Описывает продукт и привязку к уже реализованному API EList 3.0.1. Дизайн-макеты не входят в объём — это функциональная постановка.

Базовый путь API: **`/eList/api/...`**.  
Заголовки: `Authorization-jwt` (всегда, любая непустая строка) и `Authorization` (UUID токена сессии).  
Обёртка ответа: `{ success, errorCode, message, result }`. Enum в JSON — **строки PascalCase** (`"Photo"`, `"HideContent"`, `"Open"`), не snake_case.  
Поля JSON — camelCase (`targetType`, `reasonId`).

---

## 1. Зачем это в продукте

Пользователь может пожаловаться на контент. Жалоба попадает в одну или две очереди:

| Кто обрабатывает | Что видит |
|---|---|
| **Организаторы мероприятия** | Сообщения в чате события и фото события (альбом / обложка). Не видят жалобы на само мероприятие, профиль, организацию. |
| **Модераторы площадки** (`moderator` / `admin` / `superuser`) | Всё, что ушло на площадку: события, профили, организации, организаторы, фото профиля/аватарки, safety-кейсы, эскалации. |

Обычный пользователь (нет строки в `account_platform_roles`) видит кнопку «Пожаловаться», **«Мои жалобы»** (исходящие) и **«Жалобы на меня» / входящие замечания**.

**Правило:** предмет жалобы не модерирует её. Организатор не обрабатывает жалобу на своё событие/своё сообщение/своё фото профиля. Участник организации не обрабатывает жалобу на свою организацию.

---

## 2. Роли и как их узнать на клиенте

| Роль | Как определить | Что открыть в UI |
|---|---|---|
| Пользователь | `GET /platformRoles/my` → `result == null` или `active == false` | Подача жалобы, «Мои жалобы», «Жалобы на меня», инбокс замечаний |
| Организатор события | `GET /EventOrganizators/isOrganizator/{eventId}` → `true` (прямой организатор **или** активный участник организации-соорганизатора) | Бейдж + очередь **этого** события |
| Moderator / Admin / Superuser | `GET /platformRoles/my` → `role` | Кабинет площадки. Admin/Superuser ещё и справочник причин + назначение ролей |

Роль площадки кэшировать на сессию и обновлять при старте приложения.

---

## 3. Что на что жалуемся

| Цель | `targetType` | `targetId` | Куда уходит |
|---|---|---|---|
| Мероприятие | `Event` | id события | Только площадка |
| Сообщение в чате события | `Message` | id сообщения | Организаторы; если причина safety — ещё и площадка |
| Фото альбома/обложки события | `Photo` | id файла, опционально `albumId` | Как сообщение |
| Фото профиля / аватар аккаунта или организации | `Photo` | id файла | Только площадка |
| Профиль пользователя | `Account` | id аккаунта | Только площадка |
| Организация | `Organization` | id организации | Только площадка |
| Запись организатора события | `EventOrganizator` | id строки `event_organizators` | Только площадка |

Жалобы на личные диалоги (чат без `eventId`) API не принимает.

---

## 4. Точки входа «Пожаловаться»

Показывать **только авторизованному** и **не на свой объект**.

| Экран | Элемент | `targetType` / `targetId` |
|---|---|---|
| Карточка / страница события | меню «⋯» → Пожаловаться на мероприятие | `Event` / `eventId` |
| Чат события | долгий тап / «⋯» у чужого сообщения | `Message` / `messageId` |
| Альбом события, обложка события | «⋯» у фото | `Photo` / `fileId` + `albumId` если есть |
| Альбом профиля, аватар пользователя | «⋯» у фото | `Photo` / `fileId` |
| Аватар организации | «⋯» у фото | `Photo` / `fileId` |
| Профиль пользователя | «⋯» → Пожаловаться на профиль | `Account` / `accountId` |
| Страница организации | «⋯» → Пожаловаться на организацию | `Organization` / `organizationId` |
| Блок организаторов на событии | «⋯» у конкретного организатора (аккаунт или организация) | `EventOrganizator` / id записи из `GET /EventOrganizators/getByEventId/{eventId}` |

Не показывать пункт, если это своё мероприятие (пользователь организатор), своё сообщение, свой профиль, своё фото профиля, себя в списке организаторов. API всё равно отклонит — UI должен не давать нажать.

---

## 5. Экран подачи жалобы

Один универсальный шит / модалка.

**Заголовок** зависит от типа: «Жалоба на мероприятие / сообщение / фото / профиль / организацию / организатора».

**Поля**

1. Причина — обязательный список.  
   `GET /contentReports/reasons?onlyActive=true&forTargetType={TargetType}`  
   Показывать `name`, подписью `description`. Сортировка уже с сервера (`sortOrder`).  
   Причины с `severity = Safety` визуально выделить (иконка щита / подпись «Серьёзное нарушение») — они уходят ещё и на площадку.
2. Комментарий — необязательный, многострочный, до ~2000 символов на клиенте.
3. Кнопка «Отправить».

**Запрос**

```http
POST /eList/api/contentReports/create
```

```json
{
  "targetType": "Photo",
  "targetId": "<guid>",
  "albumId": "<guid или null>",
  "reasonId": "<guid>",
  "comment": "текст или null"
}
```

**После успеха:** тост «Жалоба отправлена», закрыть шит, пункт меню у этого объекта заменить на «Жалоба уже отправлена» (неактивно). Повтор открытой жалобы на тот же объект API отклонит (`errorCode` 20003).

**Ошибки (показывать `message` с сервера, запасные тексты ниже)**

| errorCode | Когда | Текст на клиенте |
|---|---|---|
| 6 | не авторизован / нет прав | Войдите в аккаунт |
| 5 | причина не подходит / своё содержимое / чат без события | Нельзя отправить эту жалобу |
| 20001 | причина не найдена | Выберите другую причину |
| 20003 | уже есть открытая жалоба | Вы уже жаловались на это |
| 20005 | resolve/escalate без take | Сначала возьмите жалобу в работу |
| 20006 | действует штраф | текст с сервера (`message` уже содержит срок) |
| 20007 | restore не модерационной отмены | Это мероприятие отменил организатор |
| 20008 | штраф не найден | Ограничение не найдено |
| 2002 / 6002 / 11001 / 14001 / 17003 | цель не найдена | Объект больше недоступен |

---

## 6. Справочник причин (для подписей в UI)

Клиент **не хардкодит** список — всегда грузит с API. Ниже — сиды, чтобы дизайнеру было понятно наполнение.

Community:

| code | Название | Где доступна |
|---|---|---|
| spam | Спам | все типы |
| harassment | Оскорбления / травля | сообщение |
| off_topic | Оффтоп | сообщение |
| inappropriate_event | Неуместное мероприятие | событие |
| inappropriate_photo | Недопустимое фото | фото |
| fake_account | Поддельный / чужой профиль | аккаунт |
| organizer_misconduct | Нарушения организатора | организатор |
| inappropriate_organization | Нарушения организации | организация |
| other | Другое | все типы |

Safety (всегда ещё и на площадку):

| code | Название |
|---|---|
| illegal_content | Неправомерный контент |
| threats | Угрозы / насилие |
| fraud | Мошенничество |
| hate | Разжигание ненависти |
| sexual_exploitation | Сексуальная эксплуатация |

---

## 7. «Мои жалобы» (все авторизованные)

Пункт в профиле / настройках.

`GET /contentReports/my?pageIndex=0&pageSize=20`

Карточка списка:

- тип цели + название причины;
- дата;
- статус (`status`): Открыта / В работе / Решена / Отклонена / Передана на площадку;
- короткий превью объекта из `targetSnapshot` (см. §12).

Тап → деталка **без** кнопок модерации. Репортёр видит свой комментарий, статус и итог (`resolutionAction` + `resolutionComment`), если жалоба закрыта. Историю действий можно не показывать репортёру в первой версии.

---

## 7.1. «Жалобы на меня» и замечания модерации

Две связанные страницы в профиле. Личность жалобщика адресату **не отдаём** и не делаем экран «кто пожаловался».

Страницу можно собрать **только на REST**. WebSocket — опциональный live-push; при открытии экрана всегда грузить историю с API, не полагаться на то, что сокет уже что-то прислал.

### Входящие замечания (инбокс)

Общий инбокс всех уведомлений аккаунта (подписки, приглашения, модерация).

```
GET /eList/api/notifications/my?pageIndex=0&pageSize=20&unreadOnly=false
GET /eList/api/notifications/my?type=ContentReportWarningIssued
GET /eList/api/notifications/my/count?unreadOnly=true
GET /eList/api/notifications/read/{id}
GET /eList/api/notifications/read/all
```

В REST `type` — **строка PascalCase** (`ContentReportWarningIssued`), не число. Число `73` — только в WebSocket (см. §18).

Поля карточки (camelCase): `id`, `title`, `message`, `type`, `createdAt`, `readAt`, `eventId`, `data`.  
В `data` для жалоб: `reportId`, `targetType`, `targetId`, `eventId`, `resolutionAction`, `reasonName`.

Фильтр «только модерация» на клиенте: `type` в диапазоне ContentReport* (70–79) либо отдельные вкладки «Предупреждения» (`ContentReportWarningIssued`) / «Все».

Тап по карточке с `data.reportId`:
- автор жалобы (`ContentReportReviewed`) → `GET /contentReports/get/{reportId}` или «Мои жалобы»;
- адресат / предупреждение / скрытие (`FiledAgainstYou`, `WarningIssued`, `ContentModerated`, блокировки) → `GET /contentReports/againstMe/{reportId}`;
- очередь организатора (`NewInOrganizerQueue`) → вкладка жалоб события;
- очередь площадки (`NewInPlatformQueue`) → кабинет площадки.

### Жалобы, которые касаются меня

```
GET /eList/api/notifications/my   — не заменяет этот список
GET /eList/api/contentReports/againstMe?pageIndex=0&pageSize=20
GET /eList/api/contentReports/againstMe/{reportId}
```

В выборку: ваш профиль; сообщения/фото, где заполнен `reportedAccountId`; организации, где вы активный участник; вы как организатор-человек.  
**Не входит:** чужой контент в чате/альбоме события (это очередь организатора) и жалоба на само мероприятие (её видит площадка; вам придёт уведомление, только если вынесут `Warn` или отменят событие).

`GET /contentReports/get/{id}` и `GET /contentReports/actions/{id}` адресату дают **403**. Не использовать их с инбокса.

Ответ `ContentReportSubjectView`:

| Поле | Смысл |
|---|---|
| `id` | id жалобы |
| `targetType` / `targetId` | что затронуто |
| `eventId`, `messageId`, `fileId`, `albumId`, `organizationId`, `eventOrganizatorId` | связи для превью и навигации |
| `targetSnapshot` | JSON превью объекта (§12) |
| `reason` | причина (без очередей модерации в UI) |
| `status` | Open / InReview / Resolved / Dismissed / Escalated |
| `resolutionAction` | что сделали, если уже решили |
| `moderatorRemark` | текст замечания; **только** для `Warn` и `Other` |
| `resolvedAt`, `createdAt`, `updatedAt` | даты |

Нет полей: `reporter`, `reporterAccountId`, `comment` жалобщика, `assignedTo`, `resolvedBy`, `actions`, `organizerStatus`, `platformStatus`.

Подписи статуса на этой странице лучше человеческие: «На рассмотрении» / «Предупреждение» / «Контент скрыт» / «Отклонено» — по `status` + `resolutionAction`, без слова «жалоба от пользователя X».

---

## 8. Кабинет организатора (контекст события)

### Где живёт

На странице события, которым пользователь управляет: вкладка **«Жалобы»** рядом с участниками / чатом. Видимость: `isOrganizator == true`.

Бейдж на иконке: `GET /contentReports/organizer/{eventId}/count?onlyActive=true`.

На шапке события (организатор **и** staff) показать сводку:

`GET /contentReports/stats/Event/{eventId}`

| Поле | Что показать |
|---|---|
| `openReports` | открытые жалобы **на само мероприятие** |
| `warningCount` | сколько предупреждений уже вынесено по событию |
| `relatedOpenReports` | открытые жалобы на чат/фото/организаторов **этого** события |
| `relatedWarningCount` | предупреждения по связанному контенту |
| `activePenalties` | действующие баны на этом событии |

Тот же эндпоинт универсален: `Account/{id}`, `Organization/{id}`, `Photo/{fileId}`, `Message/{id}`, `EventOrganizator/{id}`.  
Доступ: staff — всё; организатор — своё событие и его контент; пользователь — свой аккаунт; участник организации — свою орг.

На карточке профиля / организации в кабинете площадки вызывать stats и рисовать бейджи «жалоб: N · предупреждений: M».

### Важно для UX

Очередь **пустая для жалоб на само мероприятие** — так задумано. Организатор обрабатывает только чат и фото события. Жалобы на ивент, профили и организации идут на площадку.

Если пользователь одновременно организатор и модератор площадки, вкладка события всё равно показывает только organizer-очередь. Площадочные тикеты — в отдельном кабинете.

### Список

`POST /contentReports/organizer/{eventId}/search`

```json
{
  "onlyActive": true,
  "pageIndex": 0,
  "pageSize": 20,
  "targetType": null,
  "severity": null,
  "status": null
}
```

Фильтры (клиент):

- активные / все;
- тип: сообщение / фото;
- серьёзность: community / safety;
- статус организатора: открыта / в работе / решена / отклонена / эскалирована.

Сортировка серверная: новые сверху.

Карточка:

- тип + причина (бейдж Safety, если `reason.severity == Safety`);
- превью: текст сообщения **или** миниатюра фото (`fileId`);
- автор цели (`reportedAccountId` / `reporter`);
- `organizerStatus`.

Тап → карточка жалобы (§10).

---

## 9. Кабинет площадки

Отдельный раздел для staff (после `GET /platformRoles/my`).

Бейдж: `GET /contentReports/platform/count?onlyActive=true`.

Список: `POST /contentReports/platform/search` — то же тело, плюс фильтры `targetType`, `reportedAccountId`, `organizationId`, `eventId`, `reasonId`.

Группировка/табы по типу цели полезны: События / Сообщения / Фото / Профили / Организации / Организаторы / Safety.

Safety (`severity = Safety`) — отдельный таб или пин сверху: параллельно обрабатываются организаторами, площадка не должна «потерять» кейс, если организатор уже скрыл сообщение.

Если `GET /events/get/{id}` вернул `active: false` и `cancelSource: "moderation"` — плашка «Отменено модерацией» и кнопка «Восстановить» (`POST /contentReports/restoreEvent/{eventId}` + комментарий). Организаторскую отмену (`cancelSource: "organizer"`) этим методом не восстанавливать.

---

## 10. Карточка жалобы (модератор)

`GET /contentReports/get/{reportId}`  
История: `GET /contentReports/actions/{reportId}` (опционально аккордеон).

### Блок «объект»

Собрать из полей ответа + `targetSnapshot` (JSON-строка):

| Тип | Что показать | Куда вести |
|---|---|---|
| Event | имя события, описание | страница события |
| Message | текст, автор, дата; если `message.hidden` — плашка «Скрыто модерацией» | чат события (скролл к сообщению) |
| Photo | картинка по `fileId`; подпись kind из snapshot (`event_album`, `event_cover`, `account_album`, `account_avatar`, `organization_avatar`) | альбом / профиль / событие |
| Account | логин, аватар | профиль |
| Organization | название | страница организации |
| EventOrganizator | аккаунт и/или организация + событие | событие → блок организаторов |

Плюс: жалобщик (`reporter`), комментарий, причина, даты, `organizerStatus` / `platformStatus`.

Если `GET` вернул 6 — «Нет доступа» (организатор не должен открыть платформенный тикет на своё событие по прямой ссылке).

### Действия

**Обязательный порядок:** сначала **Взять в работу**, затем действие. `resolve` и `escalate` без take вернут `errorCode` **20005** (`ContentReportNotInReview`).

Кнопки решения на карточке **неактивны**, пока `assignedTo` — не текущий пользователь и статус очереди не `InReview`. Если тикет взял другой модератор — кнопка «Перехватить» = повторный `take` (переназначает на себя).

`POST /contentReports/take/{reportId}` — без тела. Нельзя взять уже закрытую очередь (`Resolved` / `Dismissed`).

`POST /contentReports/resolve/{reportId}`

```json
{
  "resolutionAction": "HideContent",
  "resolutionComment": "необязательный комментарий для аудита",
  "targetAccountId": null,
  "penaltyType": null,
  "durationHours": null
}
```

`targetAccountId` передавать только если UI явно выбрал аккаунт (бан / блокировка) и его нет в `reportedAccountId`.

Для `ApplyPenalty` обязателен `penaltyType`. `durationHours` — срок в часах (`24`, `168`, `720`…); `null` = бессрочно до ручного снятия. Тот же `durationHours` можно передать с `SuspendAccount` / `SuspendOrganization` / `BanFromEvent`.

`POST /contentReports/escalate/{reportId}` `{ "comment": "..." }` — **только организатор**, очередь должна быть `InReview`.

После успеха — обновить карточку, счётчик очереди и блок статистики цели.

---

## 11. Матрица действий на карточке

Показывать только применимые кнопки. Подтверждение (confirm) — на разрушающих действиях.

### Организатор события

| Действие | `resolutionAction` | Когда | Confirm |
|---|---|---|---|
| Отклонить | `Dismiss` | всегда | нет |
| Предупреждение | `Warn` | всегда; комментарий желателен | нет |
| Скрыть | `HideContent` | сообщение или фото | да |
| Удалить | `DeleteContent` | сообщение или фото | да |
| Забанить на событии | `BanFromEvent` | есть `eventId` и аккаунт автора; можно указать `durationHours` | да, показать кого и срок |
| Временное ограничение | `ApplyPenalty` | организатору доступен только `penaltyType = BanFromEvent` | да |
| Сбросить обложку/аватар | `ResetAvatar` | фото обложки события | да |
| Другое | `Other` | нужен комментарий | нет |
| Передать на площадку | отдельный `escalate` | не финальный статус | да, комментарий |

Не показывать организатору: отмену события, блокировку аккаунта/организации, снятие организатора.

### Площадка (дополнительно)

| Действие | Когда |
|---|---|
| `CancelEvent` | есть `eventId` |
| `SuspendAccount` | есть аккаунт (профиль, автор сообщения, организатор-человек, владелец фото профиля) |
| `SuspendOrganization` | есть `organizationId` или тип Organization |
| `RemoveOrganizator` | тип EventOrganizator |
| `ResetAvatar` | фото аватарки / обложки **или** жалоба на аккаунт/организацию |
| `ApplyPenalty` | выбрать тип и срок (см. §11.1) |
| Восстановить событие | не resolve, а `POST /contentReports/restoreEvent/{eventId}` — если событие отменено модерацией (`cancelSource = moderation`) |

Эскалация площадке не нужна.

Если API вернул «Нельзя модерировать жалобу, предметом которой вы являетесь» — скрыть кнопки и показать пояснение.

### Что происходит в продукте (чтобы подписать кнопки)

| Действие | Эффект |
|---|---|
| Скрыть сообщение | `hidden=true`; в чате API по-прежнему отдаёт сообщение — клиент показывает заглушку «Сообщение скрыто модерацией» |
| Удалить сообщение | скрытие + удаление |
| Скрыть фото альбома | файл пропадает из `GET /media/albums/filesByAlbumId/{albumId}` |
| Удалить фото альбома | связь файл–альбом удаляется |
| Скрыть/удалить обложку или аватар | обложка события сбрасывается / запись аватарки удаляется |
| Бан на событии | в чёрный список + снятие с участия и приглашений; при `durationHours` — автоснятие по истечении |
| Отменить событие | `active=false`, `cancelSource=moderation`; приглашения отменяются |
| Восстановить событие | `active=true`, метаданные отмены сбрасываются; участники получают `EventRestored` |
| Заблокировать аккаунт / организацию | `active=false` + запись в `moderation_penalties`; при сроке — вход/орг. вернутся сами |
| Снять организатора | удаляется запись `event_organizators` |
| Сбросить аватар | текущая аватарка/обложка убирается |
| ApplyPenalty | см. таблицу ограничений ниже |

### 11.1 Временные ограничения (`ApplyPenalty` / `durationHours`)

Пресеты в UI: 24 ч, 7 дней, 30 дней, 90 дней, бессрочно. Можно свой срок в часах (1…43800).

| `penaltyType` | Кто может | Эффект |
|---|---|---|
| `BanFromEvent` | организатор и площадка | чёрный список этого события |
| `BanEventCreate` | площадка | нельзя создавать мероприятия |
| `BanEventParticipate` | площадка | нельзя вступать / принимать приглашения |
| `BanMessaging` | площадка | нельзя писать в чаты мероприятий |
| `BanOrganize` | площадка | нельзя быть назначенным организатором |
| `SuspendAccount` | площадка | аккаунт не входит в систему |
| `SuspendOrganization` | площадка | организация неактивна |

Активные ограничения пользователя: `GET /contentReports/penalties/my`.  
Досрочное снятие (staff): `POST /contentReports/penalties/revoke/{penaltyId}`.

---

## 12. `targetSnapshot` — как разобрать превью

Поле — **строка JSON**. Парсить на клиенте.

```json
{ "type": "message", "messageId": "...", "eventId": "...", "accountId": "...", "messageText": "...", "createDate": "..." }
```

```json
{ "type": "photo", "kind": "event_album", "fileId": "...", "albumId": "...", "eventId": "...", "accountId": null, "organizationId": null }
```

`kind` фото: `event_album` | `event_cover` | `account_album` | `account_avatar` | `organization_avatar` | `album`.

Событие: `name`, `description`, `active`, `coverImageId`.  
Аккаунт: `login`, `active`, `avatarId`.  
Организация: `name`, `active`.  
Организатор: `eventOrganizatorId`, `eventId`, `accountId`, `organizationId`.

---

## 13. Скрытый контент в существующих экранах

| Место | Поведение API | Что сделать в UI |
|---|---|---|
| Чат события | скрытые сообщения **приходят**, `hidden: true` | не показывать текст; заглушка; без «пожаловаться» и без «изменить» |
| Альбом | скрытые файлы **не приходят** | ничего; фото просто исчезает |
| Обложка / аватар | id сброшен или запись удалена | стандартный placeholder |

Организатору в карточке жалобы текст/фото всё равно видны через snapshot и `fileId` — это нормально для разбора.

---

## 14. Админка площадки (admin / superuser)

Не обязательно в первом релизе клиента, но API готово.

**Причины** — `GET/POST/PUT /contentReports/reasons...`, деактивация предпочтительнее удаления (удаление запрещено, если уже есть жалобы).

**Роли площадки** — `GET /platformRoles/all`, `POST /platformRoles/assign` `{ accountId, role }`, `PUT .../setActive/{accountId}?active=`, `DELETE .../delete/{accountId}`. Moderator не назначает роли.

---

## 15. Статусы — подписи

| Значение | Подпись |
|---|---|
| Open | Открыта |
| InReview | В работе |
| Resolved | Решена |
| Dismissed | Отклонена |
| Escalated | На площадке |

На карточке для staff показывать две колонки, если обе не null: «Организаторы» и «Площадка».  
Кейс safety: организатор закрыл свою очередь, `platformStatus` ещё Open — общий `status` остаётся открытым. В кабинете площадки тикет должен оставаться в активных.

---

## 16. Навигация (предложение)

```
Профиль
  ├ Мои жалобы              ← исходящие  GET /contentReports/my
  ├ Жалобы на меня          ← входящие   GET /contentReports/againstMe
  └ Уведомления             ← инбокс     GET /notifications/my
       (бейдж: GET /notifications/my/count?unreadOnly=true)

Событие (я организатор)
  └ Жалобы   ← счётчик GET /contentReports/organizer/{eventId}/count

Меню staff (роль площадки)
  └ Модерация
       ├ Очередь
       ├ Причины        (admin+)
       └ Роли площадки  (admin+)
```

«Уведомления» можно совместить с уже существующим инбоксом платформы (подписки, приглашения) — не заводить второй колокольчик. Пункт «Пожаловаться» — контекстное меню объекта.

---

## 17. Приёмка (чеклист)

- [ ] На чужом событии есть жалоба на ивент; на своём — нет.
- [ ] В чате события можно пожаловаться на чужое сообщение; в личке — пункта нет.
- [ ] После отправки повтор на тот же объект недоступен / ошибка 20003.
- [ ] Safety-причина: тикет есть и у организатора события, и в кабинете площадки.
- [ ] Вкладка «Жалобы» у организатора **не** показывает жалобы на само мероприятие (очередь пустая, если жаловались только на ивент).
- [ ] Скрытое сообщение в чате — заглушка, не сырой текст.
- [ ] Скрытое фото пропадает из альбома.
- [ ] Бан с карточки сообщения добавляет автора в чёрный список события.
- [ ] Модератор-организатор не может взять в работу жалобу, где он предмет (кнопок нет / ошибка 6).
- [ ] Пользователь без роли площадки не видит кабинет модерации (API тоже не пустит).
- [ ] Счётчики очередей совпадают с числом активных в списке (`onlyActive=true`).
- [ ] Жалоба на профиль: владелец профиля получает WS `ContentReportFiledAgainstYou` (без имени жалобщика).
- [ ] Жалоба на сообщение/фото события: организаторы получают `ContentReportNewInOrganizerQueue`.
- [ ] Предупреждение: адресат получает `ContentReportWarningIssued`.
- [ ] Автор жалобы после решения получает `ContentReportReviewed`.
- [ ] Оффлайн: после reconnect непрочитанные приходят тем же JSON (как остальные уведомления).
- [ ] Профиль содержит «Мои жалобы» и «Жалобы на меня» как разные списки.
- [ ] `againstMe` не показывает, кто пожаловался; `get/{id}` с этого экрана не вызывать.
- [ ] Предупреждение видно и в инбоксе (`notifications/my?type=ContentReportWarningIssued`), и в карточке `againstMe/{id}.moderatorRemark`.
- [ ] Инбокс открывается по REST без активного WebSocket; сокет только добавляет новые карточки сверху.

---

## 18. WebSocket-уведомления

Канал тот же, что у подписок, приглашений и чёрного списка: `ws(s)://{host}/eList/ws/notifications?authorization={token}&authorization-jwt={jwt}`.

Тело — объект `Notification` (Newtonsoft, **PascalCase**, `Type` — **число** enum, как у остальных уведомлений). Непрочитанные уходят сразу при подключении.

**REST и WS — разная сериализация.**  
`GET /notifications/my` идёт через System.Text.Json: поля camelCase, `type` = `"ContentReportWarningIssued"`.  
Сокет: поля PascalCase, `Type` = `73`. Клиент должен понимать оба формата (или нормализовать в одном слое).

`Data` — объект:

```json
{
  "ReportId": "...",
  "TargetType": 3,
  "TargetId": "...",
  "EventId": "...",
  "OrganizationId": null,
  "ReasonCode": "fake_account",
  "ReasonName": "Поддельный / чужой профиль",
  "ResolutionAction": null,
  "Queue": "platform"
}
```

`TargetType` / `ResolutionAction` в `Data` тоже сериализуются числом (Newtonsoft). Соответствие см. enum в API: Account=3, Warn=2, …

| Type | Значение | Кому | Когда | Куда вести в UI |
|---|---|---|---|---|
| ContentReportFiledAgainstYou | 70 | владелец профиля / автор сообщения / участники организации / организатор-человек | создана жалоба на них | `GET /contentReports/againstMe/{reportId}` |
| ContentReportNewInOrganizerQueue | 71 | организаторы события (включая участников организаций-соорганизаторов), кроме жалобщика и предмета | жалоба попала в очередь события | вкладка «Жалобы» события (`EventId`) |
| ContentReportNewInPlatformQueue | 72 | moderator/admin/superuser | новая площадочная жалоба или эскалация | кабинет площадки, карточка `ReportId` |
| ContentReportWarningIssued | 73 | предмет жалобы (профиль, автор, организаторы события при Warn на ивент, участники орг.) | действие `Warn` | `againstMe/{reportId}`; текст в `Message` и в `moderatorRemark` |
| ContentReportContentModerated | 74 | автор контента | скрытие / удаление | объект из `TargetType`/`TargetId` |
| ContentReportReviewed | 75 | автор жалобы | любое решение, включая отклонение | «Мои жалобы» → `ReportId` |
| ContentReportAccountSuspended | 76 | заблокированный аккаунт | `SuspendAccount` | экран блокировки |
| ContentReportOrganizationSuspended | 77 | активные участники организации | `SuspendOrganization` | страница организации |
| ContentReportOrganizatorRemoved | 78 | снятый организатор / участники его организации | `RemoveOrganizator` | страница события |
| ContentReportAvatarReset | 79 | владелец аватарки / участники орг. | `ResetAvatar` | профиль / организация / событие |
| ContentReportPenaltyIssued | 80 | ограниченный аккаунт / участники орг. | `ApplyPenalty` и прочие timed-меры | настройки / `penalties/my` |
| EventRestored | 4 | участники события | восстановление после модерационной отмены | страница события |

Уже существующие типы **переиспользуются**:

- бан на событии → `AddedToBlackList` (41);
- отмена события → `EventCancelled` (2) участникам;
- восстановление → `EventRestored` (4).

Жалоба на **само мероприятие** при создании **не** шлётся организаторам как «на вас пожаловались» — только staff в очередь площадки. Организаторы узнают, если площадка вынесет `Warn` / отменит событие.

---

## 19. Вне первой версии UI

- Фильтрация скрытых сообщений на бэкенде чата (сейчас это обязанность клиента).
- Жалобы на личные сообщения и комментарии вне события.
- Массовые действия в очереди.
- Отдельный экран «кто меня пожаловался» — не делаем (личность жалобщика адресату не показываем).

---

## 20. Эндпоинты (шпаргалка)

Префикс `/eList`.

| Метод | Путь | Кто |
|---|---|---|
| GET | `/api/contentReports/reasons?onlyActive=true&forTargetType=Photo` | все |
| POST | `/api/contentReports/create` | все |
| GET | `/api/contentReports/my` | все (я отправитель) |
| GET | `/api/contentReports/againstMe` | все (жалобы на меня / мои орг.) |
| GET | `/api/contentReports/againstMe/{id}` | адресат жалобы (без личности жалобщика) |
| GET | `/api/contentReports/get/{id}` | автор / организатор очереди / staff |
| GET | `/api/notifications/my` | все (история входящих) |
| GET | `/api/notifications/my/count` | все |
| GET | `/api/notifications/read/{id}` | все |
| GET | `/api/notifications/read/all` | все |
| GET | `/api/contentReports/actions/{id}` | те же |
| POST | `/api/contentReports/organizer/{eventId}/search` | организатор / staff |
| GET | `/api/contentReports/organizer/{eventId}/count` | организатор / staff |
| POST | `/api/contentReports/platform/search` | staff |
| GET | `/api/contentReports/platform/count` | staff |
| POST | `/api/contentReports/take/{id}` | организатор очереди / staff |
| POST | `/api/contentReports/resolve/{id}` | те же, **только после take** (иначе 20005) |
| POST | `/api/contentReports/escalate/{id}` | организатор, после take |
| GET | `/api/contentReports/stats/{targetType}/{targetId}` | организатор события / staff / владелец цели |
| GET | `/api/contentReports/penalties/my` | все |
| POST | `/api/contentReports/penalties/revoke/{id}` | staff |
| POST | `/api/contentReports/restoreEvent/{eventId}` | staff, только `cancelSource=moderation` |
| GET | `/api/platformRoles/my` | все |
| GET | `/api/EventOrganizators/isOrganizator/{eventId}` | все |
| GET | `/api/EventOrganizators/getByEventId/{eventId}` | для жалобы на организатора |

CRUD причин и назначение ролей — admin/superuser, см. Swagger `/eList/swagger/index.html`.
