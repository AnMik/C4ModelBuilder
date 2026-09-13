# Этап 1: Сборка
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем проекты для оптимизации кэширования слоев
COPY ["C4ModelBuilder/C4ModelBuilder.sln", "C4ModelBuilder/"]
COPY ["C4ModelBuilder/C4ModelBuilder.Models/C4ModelBuilder.Models.csproj", "C4ModelBuilder/C4ModelBuilder.Models/"]
COPY ["C4ModelBuilder/C4ModelBuilder.Attributes/C4ModelBuilder.Attributes.csproj", "C4ModelBuilder/C4ModelBuilder.Attributes/"]
COPY ["C4ModelBuilder/C4ModelBuilder.Analyzer/C4ModelBuilder.Analyzer.csproj", "C4ModelBuilder/C4ModelBuilder.Analyzer/"]
COPY ["C4ModelBuilder/C4ModelBuilder.PlantUmlCreator/C4ModelBuilder.PlantUmlCreator.csproj", "C4ModelBuilder/C4ModelBuilder.PlantUmlCreator/"]
COPY ["C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj", "C4ModelBuilder/C4ModelBuilder.Cli/"]

RUN dotnet restore "C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj"

# Копируем исходники и собираем проект
COPY . .
WORKDIR "/src/C4ModelBuilder/C4ModelBuilder.Cli"
RUN dotnet publish "C4ModelBuilder.Cli.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Этап 2: Финальный образ (требуется dotnet SDK для работы Roslyn MSBuildWorkspace)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Делаем символическую ссылку, чтобы утилиту можно было вызывать как 'c4builder' из любого места
RUN printf '#!/bin/sh\ndotnet /app/C4ModelBuilder.Cli.dll "$@"\n' > /usr/local/bin/c4builder && chmod +x /usr/local/bin/c4builder

ENTRYPOINT ["dotnet", "/app/C4ModelBuilder.Cli.dll"]