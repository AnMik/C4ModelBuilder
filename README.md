# C4ModelBuilder

Инструмент для автоматической генерации PlantUML C4 диаграммы компонентов на основе разметки специальными атрибутами c# репозитория.

### Разметка

Чтобы построение диаграммы работало, на первом этапе нужно разметить атрибутами целевой проект.
Разметка проекта осуществляется атрибутами из проекта [C4ModelBuilder](C4ModelBuilder/C4ModelBuilder.Attributes/README.md).

### Построение схемы

На втором этапе нужно запустить анализ целевого проекта с помощью консольного приложения.
Пример вызова CLI для примера целевого проекта:
```powershell
./scripts/Invoke-Sample.ps1
```

### Параметры командной строки (CLI)

При запуске `C4ModelBuilder.Cli` поддерживаются следующие аргументы:

| Аргумент | Псевдоним | Тип | Обязательный | По умолчанию | Описание |
|---|---|---|---|---|---|
| `--solution` | `-s` | `FileInfo` | Да | — | Путь к целевому файлу решения (`.sln`). |
| `--output` | `-o` | `DirectoryInfo` | Да | — | Путь к директории для сохранения сгенерированных PlantUML-диаграмм. |
| `--max-depth` | `-d` | `int` | Нет | `15` | Максимальный уровень глубины рекурсии при анализе дерева вызовов. |
| `--output-type` | `-t` | `OutputType` | Нет | `Puml` | Тип выходного файла (`puml`). |
| `--project` | `-p` | `string` | Нет | Все проекты | Имя конкретного проекта решения для анализа. |
| `--exclude` | `-e` | `string` | Нет | `*tests*` | Маска исключаемых из анализа проектов (например, `*tests` или `*Sample*`). |
| `--log-level` | `-l` | `LogLevel` | Нет | `Warning` | Минимальный уровень логирования (`Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`, `None`). |

#### Пример прямого запуска через `dotnet run`:

```bash
dotnet run --project C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj -- -s "C4ModelBuilder/C4ModelBuilder.sln" -o "output" -d 16 -t puml -l Information
```

### Структура репозитория

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
-
### Разработка

Требования:
- .NET 8 **SDK**: анализ решения выполняется через Roslyn `MSBuildWorkspace`.
- `npm` (опционально): только для OpenSpec-воркфлоу.

Файлы интеграции (`.cline/skills/`, `.clinerules/workflows/`) в git не хранятся, поэтому локально нужно выполнить:
```bash
npm i -g @fission-ai/openspec@1.13.0
openspec init --tools cline
```

Конвенции кодирования заданы в [.editorconfig](.editorconfig). Архитектурные границы и дисциплина тестов описаны в `AGENTS.md`.
