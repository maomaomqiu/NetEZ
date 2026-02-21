# NetEZ 快速性能测试脚本
# PowerShell 脚本

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "    NetEZ 快速性能基准测试" -ForegroundColor Cyan
Write-Host "======================================`n" -ForegroundColor Cyan

# 检查是否已编译
$projectPath = Join-Path $PSScriptRoot "..\NetEZ.PerformanceTest"
$dllPath = Join-Path $projectPath "bin\Debug\netcoreapp3.1\NetEZ.PerformanceTest.dll"

if (-not (Test-Path $dllPath)) {
    Write-Host "首次运行，正在编译项目..." -ForegroundColor Yellow
    Set-Location $projectPath
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "编译失败！请检查错误信息。" -ForegroundColor Red
        exit 1
    }
}

# 启动性能监控（后台）
Write-Host "启动性能监控..." -ForegroundColor Green
$monitorJob = Start-Job -ScriptBlock {
    $process = $null
    while ($true) {
        Start-Sleep -Seconds 2
        if ($null -eq $process -or $process.HasExited) {
            $process = Get-Process -Name "NetEZ.PerformanceTest" -ErrorAction SilentlyContinue | Select-Object -First 1
        }
        if ($null -ne $process) {
            $cpu = [math]::Round($process.CPU, 2)
            $mem = [math]::Round($process.WorkingSet64 / 1MB, 2)
            Write-Host "[Monitor] CPU: $cpu% | 内存: $mem MB" -ForegroundColor DarkGray
        }
    }
}

# 运行测试
Write-Host "`n运行基准测试...`n" -ForegroundColor Green
Set-Location $projectPath
echo "3" | dotnet run --no-build

# 停止监控
Stop-Job $monitorJob
Remove-Job $monitorJob

Write-Host "`n测试完成！" -ForegroundColor Green
Write-Host "详细文档: PERFORMANCE_TESTING.md" -ForegroundColor Cyan
