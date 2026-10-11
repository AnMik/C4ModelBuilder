## Why

Тесты решения не запускаются ни одной джобой в `.github/workflows`: оба существующих
workflow — ручные публикаторы (`on: workflow_dispatch`) пакета `C4ModelBuilder.Attributes`
и образа CLI. Поэтому регрессия в тестах доходит до `main` незамеченной: тринадцать красных
тестов анализатора были найдены вручную при выполнении изменения
`establish-baseline-contract-specs`, а не автоматикой. Добавление джобы CI, запускающей
тесты, тогда же было явно вынесено из объёма изменения
`fix-analyzer-test-scenarios-namespace`. Сейчас набор тестов зелёный (42 теста: 15
анализатора и 27 рендера), то есть гейт есть что защищать. Публикация устроена ещё хуже:
оба публикующих workflow запускаются только вручную и собирают лишь свои проекты
(`C4ModelBuilder.Attributes.csproj` и `C4ModelBuilder.Cli.csproj`), тестовых проектов в их
графах нет, поэтому ни пакет, ни образ сейчас не проходят через тесты вообще.

## What Changes

- Добавляется workflow `.github/workflows/ci.yml`: сборка и тесты решения
  `C4ModelBuilder/C4ModelBuilder.sln` при `push` в `main`, при `pull_request` и вручную
  (`workflow_dispatch`).
- Один job на `ubuntu-latest` с SDK `8.0.x`: `dotnet restore` →
  `dotnet build --configuration Release --no-restore` →
  `dotnet test --configuration Release --no-build`.
- Оба публикующих workflow дополняются джобой прогона тестов, а публикующие джобы
  (`publish` в `publish-attributes-nuget.yml` и `build-and-push` в
  `publish-cli-docker.yml`) получают `needs` на неё: релиз не выпускается при красных тестах.
- Шаги тестов (`restore` → `build --configuration Release` → `test --configuration Release
  --no-build` по `C4ModelBuilder/C4ModelBuilder.sln`) дублируются в публикующих workflow, а не
  выносятся в переиспользуемый workflow.
- Документация приводится в соответствие: `README.md` (состав `.github/workflows/`) и
  `AGENTS.md` (утверждение, что ни одна джоба не запускает `dotnet test`).
- **BREAKING**: нет. Публичный контракт — атрибуты `C4*`, параметры и коды возврата CLI,
  содержимое текста `.puml` — и структура проектов решения не меняются; новый файл workflow
  наблюдаемого поведения инструмента не затрагивает.

## Не входит в объём

- `openspec validate` в CI (в том числе `--strict`): отдельное решение. Сейчас строгий режим
  красный на `spec/c4-annotation-attributes` (`requirements[0]` длиннее 500 символов).
- Обход гейта: опубликовать пакет или образ при красных тестах нельзя, отдельной ветки
  «принудительной публикации» не предусматривается.
- Содержимое публикуемых артефактов: версия пакета, теги образа, шаги `dotnet pack` и
  `docker/build-push-action` не меняются.
- Перевод предупреждений в ошибки (`-warnaserror` / `TreatWarningsAsErrors`): сборка Release
  даёт три предупреждения (`CA1502`, `CA1506` в анализаторе, `CA1812` в CLI), их чистка —
  отдельное изменение.
- Пин версии SDK через `global.json`; сбор и публикация покрытия; смоук-прогон CLI на
  `C4ModelBuilder.Sample.Target`; `openspec validate --archived` как pre-commit линт.
- Включение обязательной проверки статуса (required status check) в настройках репозитория
  на GitHub: это настройка вне репозитория, в диффе не фиксируется.

## Capabilities

### New Capabilities

Нет: изменение относится к инфраструктуре CI, наблюдаемое поведение инструмента не меняется.

### Modified Capabilities

Нет: требования `c4-annotation-attributes`, `cli-tool` и `component-diagram-generation`
не меняются. В метаданных изменения установлено `skip_specs: true`.

## Impact

- Новый файл: `.github/workflows/ci.yml`.
- Правки: `.github/workflows/publish-attributes-nuget.yml` и
  `.github/workflows/publish-cli-docker.yml` (новая джоба тестов и `needs` у публикующей
  джобы).
- Документация: `README.md`, `AGENTS.md`.
- Код решения, публичный контракт, спеки: не затрагиваются.
- Наблюдаемый эффект: на каждый `push` в `main` и на каждый `pull_request` запускается
  сборка и прогон тестов на раннере `ubuntu-latest`; каждый запуск публикующего workflow
  сначала прогоняет тесты и удлиняется на их время, а при красных тестах публикующая джоба
  не запускается.
