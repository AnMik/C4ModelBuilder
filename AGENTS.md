# C4ModelBuilder — контекст агента

## Что это за инструмент
C4ModelBuilder генерирует PlantUML-диаграмму C4 Component (уровень 3) из
C#-кода, размеченного атрибутами C4Context (L1), C4Container (L2),
C4Component (L3), C4Code (L4). Реализован только уровень 3.

## Архитектура и границы (обязательны)
- Пайплайн из 3 стадий: Models → Analyzer → PlantUmlCreator.
- Ссылки идут только в одну сторону и ациклично:
  `C4ModelBuilder.Models` ← (`C4ModelBuilder.Analyzer`, `C4ModelBuilder.PlantUmlCreator`).
- `Analyzer` и `PlantUmlCreator` НЕ ссылаются друг на друга.
- Данные между слоями передаются только через модели в
  `C4ModelBuilder.Models/Analysis` (`InvocationTree`). Логику анализа и рендера
  не смешивать.
- Атрибуты `C4*` — стабильный публичный контракт: имена, `AttributeUsage` и
  семантику без breaking-изменений не менять.
- Публичный API — двухэтапный: `SolutionAnalyzer.Create(ILogger, Solution, maxDepth,
  targetProject = null, excludeMask = null, ct = default)` → `SolutionAnalyzer`,
  затем `AnalyzeComponents(ct)` → `IAsyncEnumerable<InvocationTree>` (по дереву вызовов
  на каждый root-класс с `[C4Component]`), затем
  `PlantUmlGenerator.Generate(InvocationTree)` → текст PlantUML (единственная
  публичная точка входа генератора).
- Анализатор принимает Roslyn-`Solution` и не выполняет I/O; открытие `.sln`
  (`MSBuildWorkspace`) — на стороне вызывающего кода (`C4ModelBuilder.Cli`).
- Анализатор (`SolutionAnalyzer`/`MethodAnalyzer`) строит и возвращает ПОЛНОЕ
  дерево вызовов (`InvocationTree`) на каждый root-класс и НЕ занимается
  рендером.
- Внутри `C4ModelBuilder.PlantUmlCreator` этапы разделены на internal-классы:
  `InvocationTreeMerger.MergeToComponentDiagram` (схлопывание дерева в модель
  `C4ComponentDiagram`) и `PlantUmlRenderer` (формирование текста). Модель
  `C4ComponentDiagram` — internal-деталь генератора, не публичный контракт.
- `PlantUmlGenerator.Generate` — чистая функция (дерево → текст), без I/O и
  глобального состояния; одинаковый вход → одинаковый выход.

## Ключевые файлы
- Атрибуты: `C4ModelBuilder.Attributes/*.cs` (проект собран под `netstandard2.1`, дефолтный C# 8: блочные namespace, без `ImplicitUsings`)
- Модели: `C4ModelBuilder.Models/Analysis/InvocationTree.cs`
- Анализ: `C4ModelBuilder.Analyzer/{SolutionAnalyzer,SolutionParser,MethodAnalyzer,RdsCqrsRequestsAnalyzer}.cs`
- Инфраструктура: `C4ModelBuilder.Analyzer/Infrastructure/*.cs`
- Рендер (публичный фасад + internal-этапы):
  `C4ModelBuilder.PlantUmlCreator/{PlantUmlGenerator,PlantUmlRenderer,InvocationTreeMerger,C4ComponentDiagram}.cs`
- CLI: `C4ModelBuilder.Cli/Program.cs`, `C4ModelBuilder.Cli/Infrastructure/*.cs`
  (открытие `.sln` через `MSBuildWorkspace`, DI, разбор аргументов)
- Запуск CLI на примере: `scripts/Invoke-Sample.ps1`
- Пример target-проекта/заглушки: `C4ModelBuilder.Sample.Target/*.cs` и `RdsCqrsStubs.cs`
- Тесты: `C4ModelBuilder.Analyzer.Tests/SolutionAnalyzerTests.cs`,
  `C4ModelBuilder.PlantUmlCreator.Tests/{InvocationTreeMergerTests,PlantUmlRendererTests,PlantUmlGeneratorTests}.cs`
- Спеки и процесс изменений: `openspec/` (`specs/`, `changes/`, `config.yaml`)

## Технические ограничения
- .NET 8, современный C#; `Nullable` и `ImplicitUsings` во всех проектах, кроме
  `C4ModelBuilder.Attributes` (netstandard2.1, дефолтный C# 8: блочные namespace
  и явные using).
- `.editorconfig`: UTF-8 с BOM, CRLF, без trailing whitespace, максимум
  140 символов в строке.
- Пакеты — только из nuget.org (`nuget.config`); новые зависимости обосновывать.
- CLI и её Docker-образ требуют .NET SDK, а не только runtime: анализ решения
  выполняется через Roslyn `MSBuildWorkspace`.
- Никаких machine-specific абсолютных путей в коде.

## Сборка и тесты
- Сборка: `dotnet build C4ModelBuilder/C4ModelBuilder.sln`
- Тесты: `dotnet test C4ModelBuilder/C4ModelBuilder.sln`
- NUnit. Юнит-тесты анализатора собирают in-memory `Solution` (`AdhocWorkspace`)
  и вызывают `SolutionAnalyzer.Create(Solution, ...)`; без `MSBuildWorkspace`,
  путей и сети.

## Дисциплина тестирования и нейминг
- Новое/изменённое поведение покрывать NUnit-тестами на наблюдаемый вывод
  (текст PlantUML или `C4ComponentDiagram`). Сначала тест падает, потом
  реализация; перед мерджем всё зелёное.
- Тесты рендера проверяют точный текст; тесты анализа — точную модель.
- Нейминг тестов (стиль Хорикова): человекочитаемое предложение без слова
  «Test» и без жаргона, части через `_`, формат
  `<Что_тестируем>_<Сценарий>_<Ожидаемый_результат>`. Примеры:
  `Render_renders_two_components_and_one_relation`,
  `Merge_non_component_nodes_are_collapsed_into_relations_between_components`,
  `Analysis_depth_limits_how_deep_a_call_tree_is_expanded`.

## Процесс изменений (OpenSpec)

- Нетривиальные изменения (новая функциональность, смена публичного контракта, правки в
  нескольких слоях) начинаются с change-предложения, а не с правки кода.
- Воркфлоу в Cline: `/opsx-explore` — обсудить замысел, `/opsx-propose <имя>` — создать
  change с артефактами (proposal, спеки-дельта, design, tasks), `/opsx-apply` — реализовать
  задачи, `/opsx-sync` — перенести дельта-спеки в основные без архивации,
  `/opsx-update` — поправить артефакты активного change (код не трогает),
  `/opsx-archive` — заархивировать change после мерджа.
- Source of truth по поведению — спеки: `openspec/specs/<capability>/spec.md`.
  Активные изменения — `openspec/changes/<change>/`, завершённые —
  `openspec/changes/archive/<дата>-<change>/`.
- Каркас change создаёт только CLI: `openspec new change "<имя>"`; папки в
  `openspec/changes/` вручную не создавать.
- Проектный контекст и правила для воркфлоу — `openspec/config.yaml`
  (секции `context`, `rules`, `operations`); при смене конвенций обновлять и его.
- Артефакты интеграции (`.cline/skills/`, `.clinerules/workflows/`) в git не хранятся:
  локально нужны `npm i -g @fission-ai/openspec@1.13.0` и `openspec init --tools cline`.
- Состояние проверять командой `openspec list` (активные изменения) и `openspec doctor`.

## Порядок работы при доработке
1. Понять задачу и найти затронутый слой (Models/Analyzer/PlantUmlCreator);
   нетривиальные изменения начинать с change-предложения (см. раздел «Процесс изменений (OpenSpec)»).
2. Прочитать проектный контекст (`openspec/config.yaml`) и соответствующие тесты/модели.
3. Реализовать, соблюдая границы слоёв и конвенции.
4. Добавить/обновить NUnit-тесты на наблюдаемый вывод.
5. Собрать и прогнать тесты.
6. Обновить README/AGENTS.md, если изменились CLI-опции, набор поддерживаемых
   C4-уровней, структура проектов или способ подключения OpenSpec.

## Известные точки внимания
- Магические строки (имена CQRS-интерфейсов, `HandleAsync`, исключения
  `IMapper`/`ITaggableCache`) захардкожены в `MethodAnalyzer` и
  `RdsCqrsRequestsAnalyzer`; запланирована изоляция/документирование.
- Реализован только C4-уровень 3 (Component); остальные уровни объявлены
  атрибутами, но не обрабатываются.
- Семантика попадания элементов на диаграмму неявная: дерево вызовов строится
  от root-классов (`IsRoot = true`) по вызовам на полях; компонентом становится
  только узел с непустым `Description`; при вызове через интерфейс атрибуты
  берутся с класса-реализации, разметка интерфейса игнорируется (если
  реализация найдена в решении). Правила описаны в README («Как элементы
  попадают на диаграмму») — при изменении поведения анализатора обновлять их.
