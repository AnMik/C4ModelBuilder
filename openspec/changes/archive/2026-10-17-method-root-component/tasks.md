# Tasks

## 1. Регрессионные тесты на корни-методы

- [x] 1.1 Добавить в `CreateSampleSolution` класс с публичным методом, помеченным `C4Component` с
  `isRoot: true`, и тест: корневое дерево называется `{Class}.{Method}`, несёт описание из атрибута и
  содержит вызовы из тела метода. Проверка: `dotnet test C4ModelBuilder/C4ModelBuilder.sln` — новый тест
  падает (корня-метода в результате нет).
- [x] 1.2 Добавить тест: приватный метод с `isRoot: true` даёт корневое дерево, а метод с атрибутом без
  `isRoot` отдельного корня не создаёт. Проверка: тест падает до реализации.
- [x] 1.3 Добавить тест: при `isRoot: true` и на классе, и на его методе в результате два независимых
  корня; состав дерева класса не меняется, корни не смешиваются. Проверка: тест падает до реализации.
- [x] 1.4 Добавить тест на `maxDepth` от корня-метода: при `maxDepth: 0` дерево содержит только корневой
  метод, при `maxDepth: 1` — вызовы первого уровня. Проверка: тест падает до реализации.
- [x] 1.5 Добавить тест: корневой метод, вызывающий другой метод своего класса, даёт дерево без узла
  класса-владельца, а вызванный метод — прямой дочерний узел корня. Проверка: тест падает до исправления
  (в дереве есть узел `{класс}`, и на диаграмме появляется второй корень).

## 2. Реализация в анализаторе

- [x] 2.1 В `SolutionAnalyzer.AnalyzeComponents` вынести построение дерева корневого класса в общий
  приватный метод и добавить перечисление корней-методов по `Members.OfType<MethodDeclarationSyntax>()`
  с проверкой `IsRootC4Component()`; корневое дерево метода получать через
  `MethodAnalyzer.AnalyzeMethod(..., currentDepth: 0)`. Проверка: `dotnet test C4ModelBuilder/C4ModelBuilder.sln`
  — тесты 1.1–1.4 и все существующие зелёные.
- [x] 2.2 Убедиться, что границы слоёв не нарушены: `C4ModelBuilder.Models`, `C4ModelBuilder.PlantUmlCreator`
  и `C4ModelBuilder.Attributes` не изменены. Проверка: `git status --short` показывает правки только в
  `C4ModelBuilder.Analyzer` и его тестах.
- [x] 2.3 Передавать в `MethodAnalyzer.AnalyzeMethod` символ класса-владельца корня-метода и для вызовов
  его методов добавлять узел метода напрямую, без узла-класса; для классовых корней поведение не меняется.
  Проверка: тест 1.5 зелёный, `AdminController.puml` после `scripts/Invoke-Sample.ps1` не изменился,
  а `AdminController.SendPromoAsync.puml` не содержит компонента `AdminController`.

## 3. Пример и документация

- [x] 3.1 Пометить `AdminController.SendPromoAsync` в `C4ModelBuilder.Sample.Target/AdminController.cs`
  атрибутом с `isRoot: true`; прогнать `scripts/Invoke-Sample.ps1` и убедиться, что в каталоге вывода
  рядом с `AdminController.puml` появился `AdminController.SendPromoAsync.puml`, а состав компонентов и
  связей `AdminController.puml` не изменился относительно вывода до правки.
- [x] 3.2 Обновить README («Как элементы попадают на диаграмму»): правила корней класса и метода, имя
  файла `{класс}.{метод}.puml`, появление обоих файлов при обоих корнях, поведение при совпадении имён.
  Проверка: `Select-String -Path README.md -Pattern 'isRoot'` находит описание обоих видов корней.
- [x] 3.3 Обновить AGENTS.md: корни классов и методов в описании `SolutionAnalyzer` и известная точка
  внимания о методах интерфейсов, которые корнями не становятся. Проверка: `Select-String -Path AGENTS.md
  -Pattern 'isRoot'` подтверждает формулировки.

## 4. Версия и интеграционная проверка

- [x] 4.1 Поднять `<Version>` в `C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj` с `1.1.0`
  до `1.2.0`. Проверка: `dotnet run --project C4ModelBuilder/C4ModelBuilder.Cli -- --version` печатает
  `1.2.0`.
- [x] 4.2 Финальный прогон: `dotnet build C4ModelBuilder/C4ModelBuilder.sln` и `dotnet test
  C4ModelBuilder/C4ModelBuilder.sln` завершаются успешно.

## Workflow follow-up

- Заархивировать change (`/opsx-archive`) в этой же ветке до мерджа PR.
- Проверить, что в `openspec/specs/c4-annotation-attributes/spec.md` появилось требование о корне-методе,
  а в `openspec/specs/cli-tool/spec.md` — правило имени файла `{класс}.{метод}.{расширение}`.
- Выпуск CLI (тег `cli-v1.2.0` и диспатч `publish-cli-docker.yml`) выполнить после мерджа по порядку из
  AGENTS.md.
