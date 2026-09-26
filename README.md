# C4ModelBuilder

Инструмент для автоматической генерации PlantUML C4 диаграммы компонентов на основе разметки специальными атрибутами c# репозитория.

## Разметка

На первом этапе нужно разметить атрибутами целевой проект.
Разметка проекта осуществляется атрибутами из проекта [C4ModelBuilder.Attributes](C4ModelBuilder/C4ModelBuilder.Attributes/README.md).

Сейчас поддерживается только C4-уровень 3 (Component) — атрибут `C4Component`;
остальные атрибуты (`C4Context`, `C4Container`, `C4Code`) объявлены, но не обрабатываются.

## Построение схемы

На втором этапе нужно запустить анализ целевого проекта с помощью консольного приложения.
Пример вызова CLI для примера целевого проекта:

```powershell
./scripts/Invoke-Sample.ps1
```

### Как элементы попадают на диаграмму

1. Точка входа — **класс** с `[C4Component(IsRoot = true)]`.
2. Root-классов может быть несколько, интерфейсы как root не поддерживаются.
3. У каждого root-класса анализируются только **публичные методы**.
4. Поддерево вызовов строится из текущего метода по вызовам методов **полей** текущего класса.
5. Компонентом диаграммы становится узел дерева, у которого атрибут `[C4Component]` имеет **непустой `Description`**.
6. Описание берётся от **класса**, а не от интерфейса.
7. Описание от интерфейса берётся только если класс-реализация не найден в решении.
8. Узлы без `Description` на диаграмму не попадают — они используются только для построения промежуточного дерева вызовов.
9. Вызовы CQRS через `Rds.Cqrs.Queries.IQueryService` / `Rds.Cqrs.Commands.ICommandProcessor` резолвятся в обработчики запросов.
10. Поля типов `IMapper` и `ITaggableCache` игнорируются.

## Параметры командной строки (CLI)

При запуске `C4ModelBuilder.Cli` поддерживаются следующие аргументы:

| Аргумент | Псевдоним | Тип | Обязательный | По умолчанию | Описание |
| - | - | - | - | - | - |
| `--solution` | `-s` | `FileInfo` | Да | — | Путь к целевому файлу решения (`.sln`). |
| `--output` | `-o` | `DirectoryInfo` | Да | — | Путь к директории для сохранения сгенерированных PlantUML-диаграмм. |
| `--max-depth` | `-d` | `int` | Нет | `15` | Максимальный уровень глубины рекурсии при анализе дерева вызовов. |
| `--output-type` | `-t` | `OutputType` | Нет | `Puml` | Тип выходного файла (`puml`). |
| `--project` | `-p` | `string` | Нет | Все проекты | Имя конкретного проекта решения для анализа. |
| `--exclude` | `-e` | `string` | Нет | — | Маска исключаемых из анализа проектов (например, `*tests` или `*Sample*`). |
| `--log-level` | `-l` | `LogLevel` | Нет | `Warning` | Минимальный уровень логирования (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`). |

### Пример прямого запуска через `dotnet run`

```bash
dotnet run --project C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj -- -s "C4ModelBuilder/C4ModelBuilder.sln" -o "output" -d 16 -t puml -l Information
```

## Структура репозитория

- `C4ModelBuilder.Attributes` - атрибуты для разметки целевого решения.
- `C4ModelBuilder.Cli` - консольный инструмент построения с4 диаграмм целевого решения.
- `C4ModelBuilder.Models` - общие модели.
- `C4ModelBuilder.Analyzer` - анализатор классов и связей целевого проекта.
- `C4ModelBuilder.PlantUmlCreator` - построитель диаграмм в формате plantuml на основе результатов анализа.
- `C4ModelBuilder.Sample.Target` - пример целевого проекта, размеченного атрибутами.
- `C4ModelBuilder.PlantUmlCreator.Tests` - тесты построителя диаграмм.
- `C4ModelBuilder.Analyzer.Tests` - тесты анализатора: собирают in-memory `Solution` (`AdhocWorkspace`).
- `scripts/Invoke-Sample.ps1` - запуск CLI на примере целевого проекта.
- `openspec/` - спеки (`specs/`), изменения (`changes/`) и конфигурация (`config.yaml`).
- `AGENTS.md` - контекст и правила для ИИ-агентов.
- `.github/workflows/` - публикация `C4ModelBuilder.Attributes` в NuGet и CLI-образа в `ghcr.io`.
- `Dockerfile` - образ CLI: внутри доступна команда `c4builder`, базовый образ на .NET 8 SDK.

### Разработка

- NuGet-пакет `C4ModelBuilder.Attributes` собран под `netstandard2.1`.

Требования:

- .NET 8 **SDK**: анализ решения выполняется через Roslyn `MSBuildWorkspace`.
- `npm` (опционально): только для OpenSpec-воркфлоу.

Файлы интеграции (`.cline/skills/`, `.clinerules/workflows/`) в git не хранятся, поэтому локально нужно выполнить:

```bash
npm i -g @fission-ai/openspec@1.13.0
openspec init --tools cline
```

Конвенции кодирования заданы в [.editorconfig](.editorconfig). Архитектурные границы и дисциплина тестов описаны в `AGENTS.md`.
