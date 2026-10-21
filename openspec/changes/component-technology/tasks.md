# Tasks

## 1. Атрибут разметки

- [x] 1.1 Добавить в `C4ComponentAttribute` третий необязательный параметр конструктора
  `string? technology = null` и свойство `Technology { get; }`; сохранить блочные namespace и
  C# 8-стиль. Проверка: `dotnet build C4ModelBuilder/C4ModelBuilder.sln` зелёный, `[C4Component("x")]`,
  `[C4Component("x", isRoot: true)]` и `[C4Component("x", isRoot: true, technology: "T")]` компилируются
  в `C4ModelBuilder.Sample.Target`
- [x] 1.2 Обновить XML-пример в `C4ComponentAttribute.cs` и примеры в `C4ModelBuilder.Attributes/README.md`
  так, чтобы они показывали технологию. Проверка: примеры в README компилируются как есть
- [x] 1.3 Поднять `<Version>` до `3.1.0` в `C4ModelBuilder.Attributes.csproj`. Проверка:
  `dotnet pack` даёт пакет версии 3.1.0

## 2. Модель дерева вызовов

- [x] 2.1 Добавить в `InvocationTree` третий необязательный параметр конструктора
  `string? c4ComponentTechnology = null` и свойство `C4ComponentTechnology`. Проверка:
  `dotnet build` зелёный, существующие вызовы `InvocationTree(name, c4ComponentDescription: …)` в тестах
  компилируются без правок

## 3. Чтение технологии в анализаторе

- [x] 3.1 Добавить `GetC4ComponentTechnology()` в `AttributeDataExtensions` — чтение
  `ConstructorArguments[2]`; `IsRootC4Component` и `GetC4ComponentDescription` не менять. Проверка:
  тест на `SolutionAnalyzerTests`, где технология класса и метода читается из разметки
- [x] 3.2 Добавить `ClassTechnology`/`MethodTechnology` в `InvokedMethod` по образцу пары описаний.
  Проверка: `SolutionAnalyzerTests` — технология попадает в узел через вызов поля
- [x] 3.3 Передавать технологию в `MethodAnalyzer` (узел метода — своя, узел класса — своя) и в
  `SolutionAnalyzer` (корневой узел класса и корневой узел метода). Проверка: тесты анализатора на
  корень-класс, корень-метод и на отсутствие наследования от класса к методу

## 4. Схлопывание и рендер

- [ ] 4.1 Добавить `Technology` в `C4ComponentDiagram.C4Component` после `Type`. Проверка: `dotnet build`
  зелёный, существующие позиционные конструкции в тестах компилируются
- [ ] 4.2 Перенести технологию в `InvocationTreeMerger`, не меняя дедупликацию по `NodeName`. Проверка:
  тесты `InvocationTreeMergerTests` — технология доезжает до модели, отсутствующая остаётся `null`,
  состав компонентов и связей не меняется
- [ ] 4.3 Перевести `ResolveComponentType` на выбор тега по технологии: подстрока `Repository` без учёта
  регистра раньше `Gateway`, иначе тег не выводится; имя компонента больше не учитывается. Проверка:
  тесты `InvocationTreeMergerTests` — `OrderRepository` без технологии получает `Normal`, а компонент с
  технологией `"EF Core Repository"` — `Repository`
- [ ] 4.4 Перевести `PlantUmlRenderer` на `Component(alias, "Name", "Tech", "Desc"[, $tags="…"])` с
  пустой строкой вместо отсутствующей технологии. Проверка: тесты `PlantUmlRendererTests` на компонент с
  технологией, без технологии (описание остаётся четвёртым аргументом) и на технологию вместе с цветовым
  тегом
- [ ] 4.5 Обновить существующие проверки формы макроса в `PlantUmlRendererTests` и `PlantUmlGeneratorTests`
  под четыре аргумента. Проверка: `dotnet test` зелёный

## 5. Документация и версия CLI

- [ ] 5.1 Описать технологию в разделе «Разметка» `README.md` и в пункте про попадание элементов на
  диаграмму, включая правило выбора цвета по технологии. Проверка: README не противоречит дельта-спекам,
  пример разметки компилируется
- [ ] 5.2 Зафиксировать в `README.md` соглашение о парных версиях: CLI `2.0.0` поставляется вместе с
  атрибутами `3.1.0`, пояснить, что пакетной зависимости между ними нет. Проверка: утверждение в README
  совпадает с `<Version>` в обоих `.csproj`
- [ ] 5.3 Обновить `AGENTS.md`: правило выбора цветового тега теперь по технологии, а не по подстроке в
  имени; шкалы версий — атрибуты `3.x`, CLI `2.x`. Проверка: в `AGENTS.md` не осталось упоминаний типизации
  по имени компонента
- [ ] 5.4 Поднять `<Version>` до `2.0.0` в `C4ModelBuilder.Cli.csproj`. Проверка:
  `dotnet run --project C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj -- --version`
  печатает 2.0.0

## 6. Интеграционная проверка

- [ ] 6.1 Прогнать `./scripts/Invoke-Sample.ps1` и убедиться, что в `.puml` описание стоит четвёртым
  аргументом, технология — третьим, а компоненты без технологии дают `""`. Проверка: сгенерированный
  файл содержит ожидаемую форму макроса и открывается в PlantUML без ошибок
- [ ] 6.2 Прогнать `dotnet build C4ModelBuilder/C4ModelBuilder.sln` и
  `dotnet test C4ModelBuilder/C4ModelBuilder.sln` в конфигурации Release. Проверка: обе команды зелёные

## Workflow follow-up

- Синхронизировать дельта-спеки в `openspec/specs/` (`/opsx-sync`) и заархивировать change до мерджа
  в той же ветке (`/opsx-archive`).
- Проверить, что опубликованные теги образа CLI появляются только после зелёных тестов; живой прогон
  публикации возможен лишь на намеренном релизе.
