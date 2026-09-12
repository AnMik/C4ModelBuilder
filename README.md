# C4ModelBuilder

Инструмент для автоматической генерации PlantUML диаграммы на основе разметки c# кода.

Разметка осуществляется атрибутами:
1. [C4Context](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ContextAttribute.cs)
2. [C4Container](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ContainerAttribute.cs)
3. [C4Component](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ComponentAttribute.cs)
4. [C4Code](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4CodeAttribute.cs)

(на данный момент поддерживается только уровень 3 - components).

Солюшен состоит из следующих проектов:
- C4ModelBuilder.Attributes - атрибуты для разметки целевого проекта.
- C4ModelBuilder.Models - общие модели билдера.
- C4ModelBuilder.Analyzer - анализатор классов и связей целевого проекта.
- C4ModelBuilder.PlantUmlCreator - построитель диаграмм в формате plantuml на основе результатов анализа.
- C4ModelBuilder.Cli - консольный инструмент; также содержит заглушки типов `Rds.Cqrs` и размечаемый `[C4Component]` сценарий? который используется тестами анализатора.
- C4ModelBuilder.PlantUmlCreator.Tests - тесты построителя диаграмм.
- C4ModelBuilder.Analyzer.Tests - тесты анализатора: разбирают собственный солюшен инструмента.
