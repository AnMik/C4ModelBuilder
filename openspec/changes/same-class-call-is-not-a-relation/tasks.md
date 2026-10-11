## 1. Признак возврата в дереве вызовов

- [ ] 1.1 Написать в `C4ModelBuilder.Analyzer.Tests/SolutionAnalyzerTests.cs` тест
      `Analysis_marks_a_call_to_a_method_of_the_same_class_as_a_return_to_the_same_class`: узел-обёртка
      класса, созданный для вызова метода своего класса, помечен признаком; проверка — тест
      компилируется и падает по утверждению о признаке (до реализации его нет)
- [ ] 1.2 Добавить в `C4ModelBuilder.Models/Analysis/InvocationTree.cs` аддитивное `init`-свойство
      признака (имя уточняется при реализации); проверка — `dotnet build
      C4ModelBuilder/C4ModelBuilder.sln` зелёная, существующие вызовы
      `new InvocationTree(name, description)` компилируются без правок
- [ ] 1.3 В `C4ModelBuilder.Analyzer/MethodAnalyzer.cs` ставить признак при создании узла-обёртки,
      если класс вызываемого метода совпадает с классом анализируемого метода; проверка — тест 1.1
      зелёный
- [ ] 1.4 Написать тест `Analysis_does_not_mark_a_return_through_another_component`: возврат в класс
      через другой компонент признака не получает; проверка — тест зелёный, узкое правило
      подтверждено

## 2. Прозрачная обёртка в схлопывании

- [ ] 2.1 Написать в `C4ModelBuilder.PlantUmlCreator.Tests/InvocationTreeMergerTests.cs` тест
      `Merge_return_to_the_same_class_relates_the_calling_method_to_the_called_one`: дерево
      `ClassX → ClassX.MethodA → ClassX (помечен) → ClassX.MethodB` со всеми описаниями даёт
      компоненты `ClassX`, `ClassX.MethodA`, `ClassX.MethodB` и связи `ClassX → ClassX.MethodA`,
      `ClassX → ClassX.MethodB`, `ClassX.MethodA → ClassX.MethodB`, без связи
      `ClassX.MethodA → ClassX`; проверка — тест падает (сейчас есть именно цикл)
- [ ] 2.2 В `C4ModelBuilder.PlantUmlCreator/InvocationTreeMerger.cs` для помеченного узла не
      добавлять связь с ним, связывать ближайший компонент-предок с вызванным методом и сохранять
      переустановку ближайшего компонента-предка; проверка — тест 2.1 зелёный, существующие тесты
      `Merge_internal_call_to_the_same_component_*` остаются зелёными
- [ ] 2.3 Написать в `C4ModelBuilder.PlantUmlCreator.Tests/PlantUmlGeneratorTests.cs` тест текста:
      в результате есть `Rel(ClassX.MethodA, ClassX.MethodB, "")` и `Rel(ClassX, ClassX.MethodB,
      "")`, нет `Rel(ClassX.MethodA, ClassX, "")`; проверка — тест зелёный
- [ ] 2.4 Написать тест `Merge_return_to_the_same_class_without_component_target_keeps_only_
      ownership_relation`: вызванный метод без описания; проверка — связи нет, а связь от класса к
      вызвавшему методу сохранена (поведение методов без описания не изменилось)

## 3. Пример целевого проекта и документация

- [ ] 3.1 Добавить в `C4ModelBuilder.Sample.Target/AdminController.cs` вызов приватного метода
      своего класса (например, `SendOneAsync`) из `SendPromoAsync`; проверка — проект
      `C4ModelBuilder.Sample.Target` собирается
- [ ] 3.2 Обновить ожидания тестов `C4ModelBuilder.Analyzer.Tests/SolutionAnalyzerTests.cs` для
      `AdminController`; проверка — `dotnet test C4ModelBuilder/C4ModelBuilder.sln` зелёный
- [ ] 3.3 Обновить `README.md` («Как элементы попадают на диаграмму»): вызов метода своего класса
      даёт связь между вызывающим и вызванным методами, а класс остаётся их компонентом-предком;
      проверка — формулировка совпадает с дельтой спеки `component-diagram-generation`

## 4. Проверка

- [ ] 4.1 Прогнать `dotnet build C4ModelBuilder/C4ModelBuilder.sln` и `dotnet test
      C4ModelBuilder/C4ModelBuilder.sln`; проверка — 0 ошибок, все тесты зелёные (включая 1.1, 1.4,
      2.1, 2.3, 2.4)
- [ ] 4.2 Проверить изменение: `openspec validate "same-class-call-is-not-a-relation" --type change
      --strict`; проверка — валидно, артефакты `proposal`, `specs`, `design`, `tasks` без замечаний
- [ ] 4.3 Проверить формат новых и изменённых файлов (UTF-8 с BOM, CRLF, отсутствие завершающих
      пробелов, ≤140 символов); проверка — все файлы соответствуют `.editorconfig`
- [ ] 4.4 Выполнить `scripts/Invoke-Sample.ps1` и посмотреть сгенерированный `.puml`: для
      `AdminController` есть `Rel(AdminController, AdminController.SendPromoAsync, "")`,
      `Rel(AdminController, AdminController.SendOneAsync, "")`,
      `Rel(AdminController.SendPromoAsync, AdminController.SendOneAsync, "")` и нет
      `Rel(AdminController.SendPromoAsync, AdminController, "")`; проверка — текст `.puml`
      соответствует дельте спеки

## Workflow follow-up

- Заархивировать change в той же ветке до мерджа (`/opsx-archive`): перенос в
  `openspec/changes/archive/<дата>-same-class-call-is-not-a-relation/` и синк дельты в
  `openspec/specs/component-diagram-generation/spec.md` входят в тот же PR.
- После архивации убедиться, что `openspec list` не показывает изменение, а `openspec doctor` не
  находит проблем.

