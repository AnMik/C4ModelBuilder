## 1. Атрибут и версия пакета

- [x] 1.1 Переписать `C4ComponentAttribute` на конструктор `(string description, bool isRoot = false)`
      со свойствами только для чтения и XML-комментарием; проверка — `dotnet build` решения без ошибок
- [x] 1.2 Поднять `<Version>` в `C4ModelBuilder.Attributes.csproj` с 2.0.1 до 3.0.0; проверка —
      `dotnet pack` создаёт пакет версии 3.0.0

## 2. Анализатор

- [x] 2.1 Перевести `AttributeDataExtensions` на `ConstructorArguments` (описание — первый аргумент,
      `isRoot` — второй при наличии) и удалить обработку `NamedArguments`; проверка — тесты
      `C4ModelBuilder.Analyzer.Tests` зелёные

## 3. Разметка и конвенции в репозитории

- [x] 3.1 Обновить 26 использований атрибута в `SolutionAnalyzerTests.cs` на форму
      `[C4Component("…")]` и `[C4Component("…", isRoot: true)]`; проверка — тесты анализатора зелёные
- [x] 3.2 Обновить 12 использований в `C4ModelBuilder.Sample.Target`; проверка — сборка решения без ошибок
- [x] 3.3 Поднять `dotnet_diagnostic.cs8625.severity` до `error` в `.editorconfig`; проверка —
      `dotnet build` без новых ошибок и предупреждений
- [x] 3.4 Обновить `C4ModelBuilder.Attributes/README.md`: новый синтаксис и пример миграции; проверка —
      синтаксис в README совпадает с фактическим конструктором атрибута

## 4. Итоговая проверка

- [x] 4.1 Временно добавить `[C4Component(null)]` в `Sample.Target` и убедиться, что сборка падает с
      ошибкой `cs8625`, затем строку удалить; проверка — сборка сначала красная, затем зелёная
- [x] 4.2 Прогнать `scripts/Invoke-Sample.ps1` и убедиться, что `.puml` непустой; проверка — в тексте
      есть `Component(` и `@enduml`
- [x] 4.3 Прогнать `dotnet test C4ModelBuilder/C4ModelBuilder.sln` (42/42) и `openspec validate --strict`;
      проверка — оба зелёные
