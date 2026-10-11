# Tasks

## 1. Версия сборки и разбор аргументов

- [ ] 1.1 Добавить `<Version>1.1.0</Version>` в `C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj`;
  проверка: `dotnet msbuild C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj -getProperty:Version`
  печатает `1.1.0`, а `C4ModelBuilder.Attributes` остаётся `3.0.0`
- [ ] 1.2 Перевести точку входа на действие команды (design D3/D4): опции и `SetAction` собираются в
  `CliArgumentsParser`, `Program` вызывает `parseResult.InvokeAsync(...)` и возвращает его код,
  значения читаются из `ParseResult` внутри действия, ветки `130`/`1` сохраняются; проверка:
  `dotnet build C4ModelBuilder/C4ModelBuilder.sln` без ошибок и предупреждений
- [ ] 1.3 Проверить поведение новых флагов на сборке Release:
  `dotnet <bin>/Release/net8.0/C4ModelBuilder.Cli.dll --version` → в stdout есть `1.1.0` и код `0`;
  `--help` и `-h` → в stdout перечень параметров и код `0`; `--version -s <любой>.sln -o <каталог>` →
  сообщение об ошибке и код `1`; анализ при этих вызовах не запускается
- [ ] 1.4 Регресс-прогон существующих сценариев разбора после правки 1.2: запуск без параметров,
  несуществующий файл `--solution` и файл с расширением не `.sln` дают сообщение об ошибке и код `1`;
  `./scripts/Invoke-Sample.ps1` даёт код `0` и тот же состав `.puml`, что и до изменения
- [ ] 1.5 Описать новые флаги в README (раздел CLI): `--version`, `--help`, `-h`, невозможность
  сочетать `--version` с другими параметрами, команда проверки версии; проверка: команда из README
  выполняется как записано и печатает версию

## 2. Поставка контейнерного образа

- [ ] 2.1 В `Dockerfile` принять `ARG VERSION` (дефолт для локальной сборки), объявить
  `org.opencontainers.image.version` / `revision` / `source` и передать `/p:Version=$VERSION` в
  `dotnet publish`; проверка: `docker build --build-arg VERSION=1.1.0 -t c4builder-cli:local .`,
  затем `docker inspect --format '{{index .Config.Labels "org.opencontainers.image.version"}}'
  c4builder-cli:local` → `1.1.0` и `docker run --rm c4builder-cli:local --version` → `1.1.0`
- [ ] 2.2 В `.github/workflows/publish-cli-docker.yml` читать версию из
  `C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj` (`dotnet msbuild -getProperty:Version`),
  передавать её как `build-args` и публиковать теги `:X.Y.Z` и `:latest`; проверка: та же команда
  чтения версии, скопированная из шага, локально печатает `1.1.0`, а значения `build-args` и тега
  `:X.Y.Z` совпадают с версией из `.csproj`
- [ ] 2.3 Обновить `AGENTS.md` (две шкалы версий, порядок релиза CLI: bump в `.csproj` → тег
  `cli-vX.Y.Z` → диспатч workflow, политика «существующий тег не перезаписываем») и README
  (теги образа, `docker inspect`); проверка: команды и имена тегов в документации совпадают
  со значениями из workflow

## 3. Интеграционная проверка

- [ ] 3.1 `dotnet test C4ModelBuilder/C4ModelBuilder.sln --configuration Release` зелёный и
  `dotnet build C4ModelBuilder/C4ModelBuilder.sln --configuration Release` без ошибок
- [ ] 3.2 Прогон `./scripts/Invoke-Sample.ps1` на Release-сборке: код возврата `0`, а в `.puml`
  нет строки версии (`.puml` содержит только диаграмму) — версия не просочилась в контракт генератора
- [ ] 3.3 Проверить, что номера совпадают по всей цепочке: `-getProperty:Version` из `.csproj`,
  `--version` на Release-сборке, `VERSION` в `build-args` workflow и тег `:X.Y.Z` в workflow дают
  одно значение `1.1.0`

## Workflow follow-up

- Проверка, недоступная до релиза: живой прогон `publish-cli-docker.yml` (диспатч реально публикует
  теги `:1.1.0` и `:latest` в `ghcr.io`), а также проверка, что образ по версионному тегу соответствует
  опубликованному коду, — выполняется на первом намеренном релизе CLI; локально подтверждаются
  только сборка образа и его метаданные (задачи 2.1, 2.2).
- После выполнения задач — `/opsx-archive` в той же ветке: перенос change в
  `openspec/changes/archive/<дата>-cli-versioning/` и синк дельта-спеки `cli-tool` в
  `openspec/specs/cli-tool/spec.md` входят в тот же PR.
