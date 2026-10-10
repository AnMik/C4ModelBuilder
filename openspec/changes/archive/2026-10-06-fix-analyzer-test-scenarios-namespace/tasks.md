## 1. Починка сценарных констант

- [x] 1.1 Заменить `C4ModelBuilder.Models.Attributes` на `C4ModelBuilder.Attributes` в семи
  сценарных константах `SolutionAnalyzerTests.cs`. Проверка: `dotnet test` для проекта
  `C4ModelBuilder.Analyzer.Tests` — 15 из 15 тестов зелёные.

- [x] 1.2 Если после замены остались падения — разобрать остаток и устранить его, починку
  не считать завершённой до зелёного прогона. Проверка: список падений пуст.

## 2. Подтверждение неизменности поведения

- [x] 2.1 Убедиться, что продакшн-код не тронут. Проверка: `git diff` показывает только
  `C4ModelBuilder/C4ModelBuilder.Analyzer.Tests/SolutionAnalyzerTests.cs`.

- [x] 2.2 Проверить, что дельт спек нет. Проверка: `openspec.cmd status` показывает артефакт
  `specs` как `skipped`, а `openspec.cmd validate` проходит.

## 3. Сборка и тесты

- [x] 3.1 Собрать решение. Проверка: `dotnet build C4ModelBuilder/C4ModelBuilder.sln` без
  ошибок.

- [x] 3.2 Прогнать тесты. Проверка: `dotnet test C4ModelBuilder/C4ModelBuilder.sln` — все
  тесты зелёные.
