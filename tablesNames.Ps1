# This script generates a report of all source code files and their line counts.

# Get the directory where the script is located.
$rootDir = $PSScriptRoot

# Define the file extensions to include in the report.
$includeExtensions = @("*.cs", "*.ps1")

# Define directories to exclude from the search.
$excludeDirs = @("bin", "obj", ".vs", ".git")
$excludePattern = ($excludeDirs | ForEach-Object { [Regex]::Escape($_) }) -join "|"
$excludeRegex = "\\({0})\\" -f $excludePattern

# Find all relevant files recursively.
Write-Host "Scanning for files in $rootDir..."
$files = Get-ChildItem -Path $rootDir -Recurse -Include $includeExtensions | Where-Object { $_.FullName -notmatch $excludeRegex }

$reportData = @()

foreach ($file in $files) {
    try {
        $lineCount = (Get-Content -Path $file.FullName).Count
        
        # Only include files with more than 300 lines
        if ($lineCount -gt 300) {
            $relativePath = $file.FullName.Substring($rootDir.Length + 1)
            
            $reportData += [PSCustomObject]@{
                FileName = $relativePath
                Lines    = $lineCount
            }
        }
    }
    catch {
        Write-Warning "Could not process file: $($file.FullName)"
    }
}

# Sort the data by file name
$sortedReport = $reportData | Sort-Object FileName

# Display the report as a Markdown table
Write-Host ""
Write-Host "| File Name | Lines |"
Write-Host "| :--- | :--- |"
foreach ($entry in $sortedReport) {
    Write-Host "| $($entry.FileName) | $($entry.Lines) |"
}
Write-Host ""
Write-Host "Report complete. Found $($sortedReport.Count) files with more than 300 lines."