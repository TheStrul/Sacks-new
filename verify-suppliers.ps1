$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

$results = @()

# Lux - Row 2 is data row (row 1 is title)
try {
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Lux 31.8.25.xlsx")
    $ws = $wb.Worksheets.Item(1)
    # Find non-empty cells in row 2
    $c1 = ($ws.Range("A2:Z2").Cells | Where-Object { $_.Text } | Measure-Object).Count
    $wb.Close($false)
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Lux 25.11.25.xlsx")
    $ws = $wb.Worksheets.Item(1)
    $c2 = ($ws.Range("A2:Z2").Cells | Where-Object { $_.Text } | Measure-Object).Count
    $wb.Close($false)
    $results += "Lux: Old=$c1, New=$c2, Expected=4 - $(if ($c1 -eq 4 -and $c2 -eq 4) { '✅ MATCH' } else { '❌ MISMATCH' })"
} catch {
    $results += "Lux: ❌ ERROR - $_"
}

# Jizan - Note: old file is JIZ 31.08.25.xlsx, row 4 is header
try {
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\JIZ 31.08.25.xlsx")
    $c1 = $wb.Worksheets.Item(1).Cells.Item(4, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Jizan 25.11.25.xlsx")
    $c2 = $wb.Worksheets.Item(1).Cells.Item(4, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $results += "Jizan: Old=$c1, New=$c2, Expected=8 - $(if ($c1 -eq 8 -and $c2 -eq 8) { '✅ MATCH' } else { '❌ MISMATCH' })"
} catch {
    $results += "Jizan: ❌ ERROR - $_"
}

# Ace - Row 1 is header
try {
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Ace 31.8.25.xlsx")
    $c1 = $wb.Worksheets.Item(1).Cells.Item(1, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Ace 25.11.25.xlsx")
    $c2 = $wb.Worksheets.Item(1).Cells.Item(1, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $results += "Ace: Old=$c1, New=$c2, Expected=8 - $(if ($c1 -eq 8 -and $c2 -eq 8) { '✅ MATCH' } else { '❌ MISMATCH' })"
} catch {
    $results += "Ace: ❌ ERROR - $_"
}

# CHK - Row 1 is header
try {
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\Chk 31.8.25.xlsx")
    $c1 = $wb.Worksheets.Item(1).Cells.Item(1, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $wb = $excel.Workbooks.Open("$PWD\AllInputs\CHK 25.11.25.xlsx")
    $c2 = $wb.Worksheets.Item(1).Cells.Item(1, $wb.Worksheets.Item(1).Columns.Count).End(-4159).Column
    $wb.Close($false)
    $results += "CHK: Old=$c1, New=$c2, Expected=13 - $(if ($c1 -eq 13 -and $c2 -eq 13) { '✅ MATCH' } else { '❌ MISMATCH' })"
} catch {
    $results += "CHK: ❌ ERROR - $_"
}

$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null

Write-Host "`n=== Supplier Format Verification Results ===`n"
$results | ForEach-Object { Write-Host $_ }
