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
- Публичный контракт — атрибуты `C4*` (имена, `AttributeUsage`, семантика), публичный API
  библиотеки (текущая структура проектов решения) и CLI (параметры, коды возврата, состав
  файлов, контейнерный образ). Несовместимое изменение контракта допустимо только вместе с
  мажорным подъёмом версии пакета и дельтой спеки (прецедент: 3.0.0). Спеки покрывают
  атрибуты, CLI и структуру текста диаграммы (`openspec/specs/`); публичный API библиотеки
  спеками не покрыт.
- Пайплайн библиотеки — двухэтапный: `SolutionAnalyzer.Create(ILogger, Solution, maxDepth,
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
- CI: `.github/workflows/ci.yml` — `restore` → `build --configuration Release --no-restore` →
  `test --configuration Release --no-build` по тому же solution, на `ubuntu-latest` с SDK
  `8.0.x`. Джоба `tests` в `publish-attributes-nuget.yml` и `publish-cli-docker.yml` выполняет
  те же шаги, а публикующая джоба зависит от неё через `needs`.
- NUnit. Юнит-тесты анализатора собирают in-memory `Solution` (`AdhocWorkspace`)
  и вызывают `SolutionAnalyzer.Create(NullLogger.Instance, Solution, maxDepth: …)`; без
  `MSBuildWorkspace`, путей и сети.

## Дисциплина тестирования и нейминг
- Новое/изменённое поведение покрывать NUnit-тестами на наблюдаемый вывод
  (текст PlantUML или `C4ComponentDiagram`). Сначала тест падает, потом
  реализация; перед мерджем всё зелёное.
- Тесты рендера проверяют структуру текста, а не текст целиком: наличие макросов
  (`Does.Contain`), количество вхождений (`Rel(`, `Component(`), формулировку отдельных
  строк. Порядок строк и пробелы контрактом не являются
  (`openspec/specs/component-diagram-generation/spec.md`).
- Тесты анализа и слияния проверяют модель: значения полей (`Is.EqualTo`), состав
  наборов (`CollectionAssert.AreEquivalent`).
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
  `/opsx-archive` — заархивировать change до мерджа, в той же ветке.
- Source of truth по поведению — спеки: `openspec/specs/<capability>/spec.md`.
  Активные изменения — `openspec/changes/<change>/`, завершённые —
  `openspec/changes/archive/<дата>-<change>/`.
- Архивация идёт до мерджа, в той же ветке: перенос в
  `openspec/changes/archive/<дата>-<change>/` и синк дельта-спеков входят в тот же PR,
  поэтому на `main` изменение попадает уже архивным; догонять архивацию отдельным коммитом
  после мерджа не нужно.
- Следствие: проверку, невозможную до мерджа или релиза, помечай прямо в задаче (образец —
  задача 4.5 в `2026-10-11-ci-tests-in-github-pipelines`); правку артефактов
  заархивированного change вноси в `openspec/changes/archive/<дата>-<change>/`.
- Каркас change создаёт только CLI: `openspec new change "<имя>"`; папки в
  `openspec/changes/` вручную не создавать.
- Проектный контекст и правила для воркфлоу — `openspec/config.yaml`
  (секции `context`, `rules`, `operations`); при смене конвенций обновлять и его.
- Артефакты интеграции (`.cline/skills/`, `.clinerules/workflows/`) в git не хранятся;
  команды установки — в README («Разработка»).
- Состояние проверять командой `openspec list` (активные изменения) и `openspec doctor`.
- Команды OpenSpec вызывать как `openspec …`; если политика ExecutionPolicy блокирует
  `.ps1` (например, `Restricted` или `AllSigned`), использовать `openspec.cmd …`.
- `.openspec.yaml` (метаданные change) CLI создаёт в собственном формате — LF без BOM;
  это исключение зафиксировано в `.editorconfig` секцией `[**/.openspec.yaml]`.

## Порядок работы при доработке
1. Понять задачу и найти затронутый слой (Models/Analyzer/PlantUmlCreator);
   нетривиальные изменения начинать с change-предложения (см. раздел «Процесс изменений (OpenSpec)»).
2. Прочитать проектный контекст (`openspec/config.yaml`) и соответствующие тесты/модели.
3. Реализовать, соблюдая границы слоёв и конвенции.
4. Добавить/обновить NUnit-тесты на наблюдаемый вывод.
5. Собрать и прогнать тесты.
6. Обновить README/AGENTS.md, если изменились CLI-опции, набор поддерживаемых
   C4-уровней, структура проектов или способ подключения OpenSpec.
7. Заархивировать завершённый change (`/opsx-archive`) в той же ветке — до мерджа PR.

## Известные точки внимания
- Магические строки (имена CQRS-интерфейсов, `HandleAsync`, исключения
  `IMapper`/`ITaggableCache`) захардкожены в `MethodAnalyzer` и
  `RdsCqrsRequestsAnalyzer`; типизация компонента — по подстроке в имени
  (`Repository`/`Gateway`). Запланирована изоляция/документирование.
- Атрибуты распознаются по простому имени типа (`Infrastructure/ISymbolExtensions.cs`), а не
  по символу: одноимённый тип из чужого пространства имён даст ложное срабатывание.
- Семантика попадания элементов на диаграмму неявная: правила (root-классы, обход по
  вызовам полей, роль `Description`, разрешение интерфейс → класс) описаны только в README
  («Как элементы попадают на диаграмму») и спеками не зафиксированы; контракт содержимого —
  `openspec/specs/component-diagram-generation/`. При изменении поведения анализатора
  обновлять README и, при необходимости, дельту спеки.
- Порядок компонентов и связей в `.puml` определяется обходом `HashSet`
  (`InvocationTreeMerger`) и случайным хешированием строк — между запусками порядок строк
  может отличаться. Контрактом порядок не считается, поэтому побайтовые проверки недопустимы.
- Проекта `C4ModelBuilder.Cli.Tests` не существует: поведение CLI подтверждается инспекцией
  кода, а не тестами. Тесты запускает `ci.yml` (push в `main`, pull request, ручной запуск),
  а в публикующих workflow их прогоняет джоба `tests`, от которой зависит публикующая джоба
  (`needs`): релиз не выпускается при красных тестах.
- Шаги тестов продублированы в `.github/workflows/ci.yml` и в двух публикующих workflow:
  при изменении способа запуска тестов править все три файла.
- Публичный API библиотеки объявлен контрактом, но не покрыт спеками: изменение публичных
  сигнатур или структуры проектов решения требует change и дельты спеки.
