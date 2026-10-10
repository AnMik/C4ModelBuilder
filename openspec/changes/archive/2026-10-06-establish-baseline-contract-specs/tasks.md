## 1. Сверка требований с кодом

- [x] 1.1 Сверить требования `c4-annotation-attributes` с `C4ModelBuilder.Attributes/*.cs` и `.csproj`:
  состав атрибутов, `AttributeUsage`, свойства, параметры поставки. Проверка: каждое требование
  сопоставлено с конкретным файлом, расхождения исправлены в спеке.

- [x] 1.2 Сверить требования `cli-tool` с `CliArgumentsParser`, `C4Builder`, `Program`,
  `IServiceCollectionExtensions` и `Dockerfile`. Проверка: каждое требование сопоставлено с кодом,
  а для требований без тестов в спеке явно указан источник подтверждения (инспекция кода).

- [x] 1.3 Сверить требования `component-diagram-generation` с тестами
  `C4ModelBuilder.PlantUmlCreator.Tests`. Проверка: каждое требование сопоставлено хотя бы с одним
  существующим тестом либо помечено как подтверждаемое инспекцией.

- [x] 1.4 Подтвердить осознанный пропуск `design.md`: условие из инструкции артефакта не
  выполняется, так как изменение документационное и технических решений не вносит. Проверка:
  файла `design.md` в каталоге change нет, причина указана в `proposal.md`.

## 2. Проверка состава изменения

- [x] 2.1 Убедиться, что изменение не затронуло код и документацию. Проверка: `git status --short`
  показывает только файлы внутри `openspec/changes/establish-baseline-contract-specs/`.

- [x] 2.2 Убедиться, что каждая capability из `proposal.md` имеет дельту. Проверка: `openspec.cmd
  list --json` показывает изменение, а каталог `specs/` содержит три `spec.md` с заголовками
  `## ADDED Requirements`.

## 3. Сборка и тесты

- [x] 3.1 Собрать решение. Проверка: `dotnet build C4ModelBuilder/C4ModelBuilder.sln` завершается
  без ошибок.

- [x] 3.2 Прогнать тесты. Проверка: `dotnet test C4ModelBuilder/C4ModelBuilder.sln` —
  PlantUmlCreator.Tests 27/27 зелёные; в Analyzer.Tests 13 из 15 красных
  по предсуществующей причине (находка 9, вынесена отдельным change), к baseline не относится.

## 4. Проверка артефактов

- [x] 4.1 Проверить дельты валидатором. Проверка: `openspec.cmd validate
  "establish-baseline-contract-specs" --type change --strict --no-interactive` сообщает, что
  изменение валидно.

- [x] 4.2 Проверить формат файлов артефактов по `.editorconfig`. Проверка: у всех файлов UTF-8 с
  BOM, CRLF без одиночных LF, нет завершающих пробелов, ни одна строка не длиннее 140 символов.
