#!/bin/bash
# NetEZ 快速性能测试脚本
# Bash 脚本 (Linux/MacOS)

echo "======================================"
echo "    NetEZ 快速性能基准测试"
echo "======================================
"

# 项目路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
PROJECT_PATH="$SCRIPT_DIR/../NetEZ.PerformanceTest"
DLL_PATH="$PROJECT_PATH/bin/Debug/netcoreapp3.1/NetEZ.PerformanceTest.dll"

# 检查是否已编译
if [ ! -f "$DLL_PATH" ]; then
    echo "首次运行，正在编译项目..."
    cd "$PROJECT_PATH"
    dotnet build
    if [ $? -ne 0 ]; then
        echo "编译失败！请检查错误信息。"
        exit 1
    fi
fi

# 启动性能监控（后台）
echo "启动性能监控..."
(
    while true; do
        sleep 2
        PID=$(pgrep -f "NetEZ.PerformanceTest" | head -1)
        if [ ! -z "$PID" ]; then
            CPU=$(ps -p $PID -o %cpu | tail -1)
            MEM=$(ps -p $PID -o rss | tail -1)
            MEM_MB=$(echo "scale=2; $MEM/1024" | bc)
            echo "[Monitor] CPU: ${CPU}% | 内存: ${MEM_MB} MB"
        fi
    done
) &
MONITOR_PID=$!

# 运行测试
echo "
运行基准测试...
"
cd "$PROJECT_PATH"
echo "3" | dotnet run --no-build

# 停止监控
kill $MONITOR_PID 2>/dev/null

echo "
测试完成！"
echo "详细文档: PERFORMANCE_TESTING.md"
