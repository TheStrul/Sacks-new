# Test MCP Server Connection
$mcpExe = ".\SacksMcp\bin\Debug\net10.0\SacksMcp.exe"

Write-Host "Starting MCP Server..." -ForegroundColor Cyan
$process = Start-Process -FilePath $mcpExe -NoNewWindow -PassThru -RedirectStandardInput "stdin.txt" -RedirectStandardOutput "stdout.txt" -RedirectStandardError "stderr.txt"

Start-Sleep -Seconds 2

# Send initialize request
$initRequest = @{
    jsonrpc = "2.0"
    id = 1
    method = "initialize"
    params = @{
        protocolVersion = "2024-11-05"
        capabilities = @{}
        clientInfo = @{
            name = "test-client"
            version = "1.0.0"
        }
    }
} | ConvertTo-Json -Depth 10

Write-Host "Sending initialize request..." -ForegroundColor Yellow
$initRequest | Out-File -FilePath "stdin.txt" -Encoding UTF8
Start-Sleep -Seconds 1

# Send tools/list request
$listRequest = @{
    jsonrpc = "2.0"
    id = 2
    method = "tools/list"
} | ConvertTo-Json

Write-Host "Sending tools/list request..." -ForegroundColor Yellow
$listRequest | Out-File -FilePath "stdin.txt" -Append -Encoding UTF8
Start-Sleep -Seconds 2

# Read output
if (Test-Path "stdout.txt") {
    Write-Host "`n=== STDOUT ===" -ForegroundColor Green
    Get-Content "stdout.txt"
}

if (Test-Path "stderr.txt") {
    Write-Host "`n=== STDERR ===" -ForegroundColor Red
    Get-Content "stderr.txt"
}

# Clean up
Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
Remove-Item "stdin.txt" -ErrorAction SilentlyContinue
Remove-Item "stdout.txt" -ErrorAction SilentlyContinue
Remove-Item "stderr.txt" -ErrorAction SilentlyContinue

Write-Host "`nTest complete." -ForegroundColor Cyan
