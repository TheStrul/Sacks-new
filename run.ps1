# Simple launcher script for SacksConsoleApp
# Usage: .\run.ps1 [arguments...]

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

$projectPath = "SacksApp\SacksApp.csproj"

if ($Arguments) {
    dotnet run --project $projectPath -- $Arguments
} else {
    dotnet run --project $projectPath
}
