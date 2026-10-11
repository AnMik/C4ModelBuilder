## 1. Workflow сборки и тестов

- [x] 1.1 Создать `.github/workflows/ci.yml`: имя `Build and test`, триггеры `push` (ветка
      `main`), `pull_request`, `workflow_dispatch`, блок `concurrency` с
      `cancel-in-progress: true` (группа `${{ github.workflow }}-${{ github.ref }}`), одна
      джоба на `ubuntu-latest`; проверка — файл разбирается как валидный YAML и соответствует
      `.editorconfig` (UTF-8 с BOM, CRLF, отступ 2 пробела, без завершающих пробелов, ≤140
      символов)
- [x] 1.2 В джобе добавить `actions/checkout@v4` и `actions/setup-dotnet@v4`
      (`dotnet-version: 8.0.x`); проверка — версии экшенов совпадают с используемыми в
      существующих workflow
- [x] 1.3 Добавить шаги `dotnet restore`, `dotnet build --configuration Release --no-restore`,
      `dotnet test --configuration Release --no-build` — все по пути
      `C4ModelBuilder/C4ModelBuilder.sln` (прямые слеши); проверка — те же три команды
      локально дают 0 ошибок сборки и 42/42 теста

## 2. Гейт публикации

- [x] 2.1 В `publish-attributes-nuget.yml` добавить джобу тестов (`checkout` → `setup-dotnet`
      → `restore` → `build --configuration Release` → `test --configuration Release
      --no-build` по решению) и `needs` на неё у джобы `publish`; проверка — YAML валиден,
      `needs` ссылается на существующую джобу, `permissions` (`id-token: write`,
      `contents: read`) и шаги `pack` / `nuget push` не изменены
- [x] 2.2 То же в `publish-cli-docker.yml`: джоба тестов и `needs` у `build-and-push`;
      проверка — YAML валиден, `permissions` (`contents: read`, `packages: write`) и шаги
      `docker/*` не изменены
- [x] 2.3 Сверить, что шаги тестов в `ci.yml` и в обоих публикующих workflow совпадают (те же
      команды, конфигурация Release, тот же путь решения); проверка — построчное сравнение
      блоков `run` не находит расхождений

## 3. Документация

- [x] 3.1 Обновить `README.md`: в описании `.github/workflows/` указать, что `ci.yml`
      собирает и тестирует решение, а публикующие workflow прогоняют тесты перед публикацией;
      проверка — описание соответствует созданным и изменённым workflow
- [x] 3.2 Обновить `AGENTS.md`: убрать утверждение, что ни одна джоба не запускает
      `dotnet test`, и указать `ci.yml` и гейт публикации; проверка — поиск по `dotnet test`
      в `AGENTS.md` показывает упоминание `ci.yml`

## 4. Проверка

- [x] 4.1 Прогнать `dotnet build C4ModelBuilder/C4ModelBuilder.sln --configuration Release` и
      `dotnet test C4ModelBuilder/C4ModelBuilder.sln --configuration Release --no-build`;
      проверка — 0 ошибок и 42/42 теста
- [x] 4.2 Проверить изменение: `openspec validate "ci-tests-in-github-pipelines" --type
      change --strict`; проверка — валидно, артефакт `specs` имеет статус `skipped`
- [x] 4.3 Проверить формат новых и изменённых файлов (BOM, CRLF, отсутствие завершающих
      пробелов, ≤140 символов); проверка — все файлы соответствуют `.editorconfig`
- [ ] 4.4 После пуша ветки убедиться, что `ci.yml` завершился успешно в GitHub Actions;
      проверка — зелёный прогон в списке Actions репозитория
- [ ] 4.5 Убедиться в порядке джоб гейта на первом намеренном релизе; проверка — в графе
      прогона публикующего workflow джоба тестов идёт раньше публикующей, при падении тестов
      публикующая не запускается. Холостой диспатч публикующего workflow как проверку не
      использовать: он реально публикует пакет в NuGet и тег `latest` в `ghcr.io`
