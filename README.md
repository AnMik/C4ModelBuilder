# C4ModelBuilder

Инструмент для автоматической генерации PlantUML диаграммы на основе разметки c# кода.

Разметка осуществляется атрибутами:
1. [C4Context](C4ModelBuilder/C4ModelBuilder.Models/Attributes/C4ContextAttribute.cs)
2. [C4Container](C4ModelBuilder/C4ModelBuilder.Models/Attributes/C4ContainerAttribute.cs)
3. [C4Component](C4ModelBuilder/C4ModelBuilder.Models/Attributes/C4ComponentAttribute.cs)
4. [C4Code](C4ModelBuilder/C4ModelBuilder.Models/Attributes/C4CodeAttribute.cs)

(на данный момент поддерживается только уровень 3 - components).

Солюшен состоит из следующих проектов:
- C4ModelBuilder.Models - атрибуты для разметки и общие модели билдера.
- C4ModelBuilder.Analyzer - анализатор классов и связей целевого проекта.
- C4ModelBuilder.PlantUmlCreator - построитель диаграмм в формате plantuml на основе результатов анализа.
- C4ModelBuilder.Examples - консольный проект с примером использования инструмента,
- C4ModelBuilder.PlantUmlCreator.Tests - тесты построителя диаграмм.
