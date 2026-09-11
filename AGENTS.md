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
  `C4ModelBuilder.Models/Analysis` (`InvocationTree`, `C4ComponentDiagram`,
  `C4Component`, `C4Relation`). Логику анализа и рендера не смешивать.
- Атрибуты `C4*` — стабильный публичный контракт: имена, `AttributeUsage` и
  семантику без breaking-изменений не менять.
- Анализатор (`SolutionAnalyzer`/`MethodAnalyzer`) строит и возвращает ПОЛНОЕ
  дерево вызовов (`InvocationTree`) на каждый root-класс и НЕ занимается
  рендером. Схлопывание узлов без атрибута в связи и дедупликацию
  компонентов/связей выполняет `PlantUmlGenerator.BuildComponentDiagram`.
- `PlantUmlGenerator.Generate` — чистая функция (текст → текст), без I/O и
  глобального состояния; одинаковый вход → одинаковый выход.

## Ключевые файлы
- Атрибуты: `C4ModelBuilder.Models/Attributes/*.cs`
- Модели: `C4ModelBuilder.Models/Analysis/{InvocationTree,C4ComponentDiagram}.cs`
- Анализ: `C4ModelBuilder.Analyzer/{SolutionAnalyzer,SolutionParser,MethodAnalyzer,RdsCqrsRequestsAnalyzer}.cs`
- Инфраструктура: `C4ModelBuilder.Analyzer/Infrastructure/*.cs`
- Рендер: `C4ModelBuilder.PlantUmlCreator/PlantUmlGenerator.cs`
- Примеры/заглушки: `C4ModelBuilder.Examples/Sample/*.cs` и `RdsCqrsStubs.cs`
- Тесты: `C4ModelBuilder.Analyzer.Tests/SolutionAnalyzerIntegrationTests.cs`,
  `C4ModelBuilder.PlantUmlCreator.Tests/PlantUmlGeneratorTests.cs`
- Конституция: `.specify/memory/constitution.md` (локальный, не в git)

## Технические ограничения
- .NET 8, современный C#; `Nullable` и `ImplicitUsings` во всех проектах.
- `.editorconfig`: UTF-8 с BOM, CRLF, без trailing whitespace, максимум
  140 символов в строке.
- Пакеты — только из nuget.org (`nuget.config`); новые зависимости обосновывать.
- Никаких machine-specific абсолютных путей в коде.

## Сборка и тесты
- Сборка: `dotnet build C4ModelBuilder/C4ModelBuilder.sln`
- Тесты: `dotnet test C4ModelBuilder/C4ModelBuilder.sln`
- NUnit. Интеграционные тесты анализатора используют `MSBuildWorkspace` и
  разбирают сам солюшен; не зависят от путей/сети.

## Дисциплина тестирования и нейминг
- Новое/изменённое поведение покрывать NUnit-тестами на наблюдаемый вывод
  (текст PlantUML или `C4ComponentDiagram`). Сначала тест падает, потом
  реализация; перед мерджем всё зелёное.
- Тесты рендера проверяют точный текст; тесты анализа — точную модель.
- Нейминг тестов (стиль Хорикова): человекочитаемое предложение без слова
  «Test» и без жаргона, части через `_`, формат
  `<Что_тестируем>_<Сценарий>_<Ожидаемый_результат>`. Примеры:
  `Generator_renders_two_components_and_one_relation`,
  `Build_non_component_nodes_are_collapsed_into_relations_between_components`,
  `Analysis_depth_limits_how_deep_a_call_tree_is_expanded`.

## Порядок работы при доработке
1. Понять задачу и найти затронутый слой (Models/Analyzer/PlantUmlCreator).
2. Прочитать конституцию и соответствующие тесты/модели.
3. Реализовать, соблюдая границы слоёв и конвенции.
4. Добавить/обновить NUnit-тесты на наблюдаемый вывод.
5. Собрать и прогнать тесты.
6. Обновить README, если изменился набор поддерживаемых C4-уровней или
   структура проекта.

## Известные точки внимания
- Магические строки (имена CQRS-интерфейсов, `HandleAsync`, исключения
  `IMapper`/`ITaggableCache`) захардкожены в `MethodAnalyzer` и
  `RdsCqrsRequestsAnalyzer`; запланирована изоляция/документирование.
- Реализован только C4-уровень 3 (Component); остальные уровни объявлены
  атрибутами, но не обрабатываются.
