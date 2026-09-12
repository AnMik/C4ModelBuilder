param(
    [string] $Configuration = "Debug",
    [string] $OutputDirectory = "output/sample-target"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Resolve-Path "$PSScriptRoot/.."
$projectPath = Join-Path $repositoryRoot "C4ModelBuilder/C4ModelBuilder.Sample.Target/C4ModelBuilder.Sample.Target.csproj"
$cliProjectPath = Join-Path $repositoryRoot "C4ModelBuilder/C4ModelBuilder.Cli/C4ModelBuilder.Cli.csproj"
$resolvedOutputDirectory = Join-Path $repositoryRoot $OutputDirectory

& dotnet run `
    --project $cliProjectPath `
    --configuration $Configuration `
    -- `
    --solution $projectPath `
    --output $resolvedOutputDirectory `
    --max-depth 15

exit $LASTEXITCODE
