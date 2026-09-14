# eList 3.0.1 (elist.api)

Backend REST API агрегатора городских мероприятий **EList**.

## Документация

| Документ | Содержание |
|----------|------------|
| **[docs/SERVICE.md](docs/SERVICE.md)** | Описание сервиса, доменная модель, согласия, кошелёк, билеты |
| **[docs/production-readiness-checklist.md](docs/production-readiness-checklist.md)** | Актуальный чеклист готовности к релизу |
| **[docs/legal-ticketing-review.md](docs/legal-ticketing-review.md)** | Сверка юр. модели продажи билетов с кодом |
| **[docs/ui-handoff-checklist-and-tickets.md](docs/ui-handoff-checklist-and-tickets.md)** | Handoff для UI |
| **[Agreements/](Agreements/)** | Исходники юридических текстов (runtime — в БД) |
| **[AGENTS.md](AGENTS.md)** | Сборка, path base, типичные ловушки для агентов |

> Документация **не** обновляется автоматически при каждом коммите — поддерживается вручную вместе с эпиками.

## Стек

- .NET 6 / ASP.NET Core
- PostgreSQL 16 + PostGIS
- linq2db, FluentMigrator
- Sibling: [elist.common](https://github.com/crash240192/elist.common)

## Быстрый старт

```bash
# sibling common рядом с репо (см. AGENTS.md)
dotnet restore EList.sln
dotnet build EList.sln --no-restore
cd EList.Api
ASPNETCORE_URLS="http://localhost:5131" dotnet run
```

- API: `http://localhost:5131/eList/api/...`
- Swagger: `http://localhost:5131/eList/swagger`

## Связанные клиенты

- UI: `elist.ui`
- Filestorage: `elist.filestorage.api`
