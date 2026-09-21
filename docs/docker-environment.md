# Docker / окружения: Development, Staging, Production

## Ключевое правило

| Рычаг | Когда | Что делает |
|--------|--------|------------|
| `dotnet publish -c Release` | **сборка** образа | оптимизация кода |
| `ASPNETCORE_ENVIRONMENT` | **запуск** контейнера | какой `appsettings.*.json` и режим ошибок |

В image лежат **и** `appsettings.json` (база), **и** `appsettings.{Environment}.json` (оверлей). Это нормально: ASP.NET Core всегда мержит базу + оверлей + переменные окружения.

Не удаляйте `appsettings.json` из контейнера. Секреты перекрывайте env / volume, не зашивайте в образ.

---

## Значения `ASPNETCORE_ENVIRONMENT`

| Env | Ошибки 500 клиенту | Типичное применение |
|-----|--------------------|---------------------|
| `Development` | полная цепочка + stack | локально |
| `Staging` | полная цепочка + stack (как Dev) | стенд |
| `Production` | безопасный текст + `correlationId` | прод |

Дополнительно (любой env):

```json
"features": {
  "exposeDetailedErrors": true
}
```

или env: `features__exposeDetailedErrors=true`  
На **Production** флаг не включать. На Staging детали и так включены по имени окружения.

Файлы оверлея:

- API: `EList.Api/appsettings.Staging.json`
- Filestorage: `EList.Filestorage.Api/appsettings.Staging.json`

---

## Как переключить на стенде

```bash
# docker run
docker run -e ASPNETCORE_ENVIRONMENT=Staging ...

# docker compose (filestorage)
ASPNETCORE_ENVIRONMENT=Staging docker compose up -d

# или в .env рядом с compose
ASPNETCORE_ENVIRONMENT=Staging
```

Пересборка образа **не нужна** — меняется только runtime env (если оверлей уже в publish output).

---

## Correlation id

При необработанном 500 API/filestorage возвращают:

- JSON: `correlationId`
- заголовок: `X-Correlation-Id`

UI показывает id в тосте. Ищите ту же строку в логах контейнера:

```bash
docker logs <container> 2>&1 | grep '<correlation-id>'
```

NLog (API) пишет в stdout (`jsonConsole`) и в `/app/logs/yyyy-MM-dd.log` внутри контейнера. Смонтируйте volume на `/app/logs`, если нужен доступ с хоста.

---

## Пример compose (API — набросок)

```yaml
services:
  elist-api:
    image: crash240192/elist-api:latest
    environment:
      ASPNETCORE_ENVIRONMENT: Staging   # или Production
      # Секреты лучше так, а не в appsettings.json:
      # connectionStrings__elist_main_db__connectionString: Host=...
    volumes:
      - ./logs/api:/app/logs
```

Filestorage: см. `docker-compose.yml` в репозитории filestorage (`ASPNETCORE_ENVIRONMENT` через env).

---

## UI

Сборка Vite — отдельно (`VITE_*` build-args). Режим API (Staging/Production) на UI не влияет: UI всегда показывает `message` и `correlationId` из ответа бэкенда.
