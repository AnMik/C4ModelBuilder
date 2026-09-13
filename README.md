# C4ModelBuilder

Инструмент для автоматической генерации PlantUML диаграммы на основе разметки c# кода.

### Разметка

На первом этапе нужно разметить атрибутами целевой проект, на основании которого планируется строить диаграммы.

Разметка проекта осуществляется атрибутами:
1. [C4Context](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ContextAttribute.cs) - не поддерживается.
2. [C4Container](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ContainerAttribute.cs) - не поддерживается.
3. [C4Component](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4ComponentAttribute.cs) - атрибут на классе, интерфейсе или методе.
4. [C4Code](C4ModelBuilder/C4ModelBuilder.Attributes/Attributes/C4CodeAttribute.cs) - не поддерживается.

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

### Структура решения

Решение состоит из следующих проектов:
- `C4ModelBuilder.Attributes` - атрибуты для разметки целевого решения.
- `C4ModelBuilder.Cli` - консольный инструмент построения с4 диаграмм целевого решения.
- `C4ModelBuilder.Models` - общие модели.
- `C4ModelBuilder.Analyzer` - анализатор классов и связей целевого проекта.
- `C4ModelBuilder.PlantUmlCreator` - построитель диаграмм в формате plantuml на основе результатов анализа.
- `C4ModelBuilder.Sample.Target` - пример целевого проекта, размеченного атрибутами.
- `C4ModelBuilder.PlantUmlCreator.Tests` - тесты построителя диаграмм.
- `C4ModelBuilder.Analyzer.Tests` - тесты анализатора: разбирают собственный солюшен инструмента.