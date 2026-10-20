# C4ModelBuilder.Attributes

Атрибуты для разметки целевого репозитория.
На основании разметки строится диаграмма с помощью консольного приложения [C4ModelBuilder.Cli](https://github.com/AnMik/C4ModelBuilder/tree/main/C4ModelBuilder/C4ModelBuilder.Cli).

Разметка проекта осуществляется атрибутами:
1. [C4Component](C4ComponentAttribute.cs) - атрибут на классе, интерфейсе или методе; единственный поддерживаемый.
2. [C4Context](C4ContextAttribute.cs), [C4Container](C4ContainerAttribute.cs),
   [C4Code](C4CodeAttribute.cs) - объявлены, но не обрабатываются.

Контракт разметки - `openspec/specs/c4-annotation-attributes/spec.md`.

Пример разметки:

```csharp
[C4Component("Отправка промо-кампании", technology: "RabbitMQ")]
public sealed class CampaignSender
{
}

[C4Component("Главная страница", isRoot: true, technology: "ASP.NET Core")]
public sealed class HomeController
{
}
```

Описание обязательно: разметка без параметра описания не компилируется.
Технология необязательна: если она не задана, диаграмма выводится без строки технологии.
Миграция с версии 2.x: `[C4Component(Description = "…")]` → `[C4Component("…")]`,
`[C4Component(IsRoot = true, Description = "…")]` → `[C4Component("…", isRoot: true)]`.
Миграция с версии 3.x: версия 3.1.0 добавляет необязательный параметр `technology`; существующая разметка
компилируется без правок.

