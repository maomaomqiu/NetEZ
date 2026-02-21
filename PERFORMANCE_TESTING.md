# NetEZ 性能测试指南

本文档提供全面的性能测试方法、工具和示例代码。

## 📊 关键性能指标 (KPI)

### 1. 吞吐量指标
- **QPS (Queries Per Second)** - 每秒处理的请求数
- **TPS (Transactions Per Second)** - 每秒完成的事务数
- **带宽利用率** - 网络带宽使用情况 (MB/s)

### 2. 延迟指标
- **平均响应时间** (Average RT)
- **P50/P95/P99 延迟** - 百分位延迟
- **最大/最小延迟**

### 3. 资源占用
- **CPU 使用率**
- **内存占用** (工作集/私有字节)
- **GC 频率和耗时**
- **线程数**

### 4. 连接管理
- **并发连接数**
- **连接建立速度**
- **连接复用率** (使用连接池时)

### 5. 稳定性指标
- **错误率** - 失败请求占比
- **连接断开率**
- **长时间运行稳定性** (Soak Test)

---

## 🧪 测试场景

### 场景 1: 基准性能测试
测试单个客户端和服务器的基本性能。

### 场景 2: 并发连接测试
测试服务器支持的最大并发连接数。

### 场景 3: 高负载压力测试
测试在高负载下的性能表现和稳定性。

### 场景 4: 长连接稳定性测试
测试长时间运行的稳定性 (24小时+)。

### 场景 5: 不同消息大小测试
测试小消息(100B)、中等消息(1KB)、大消息(100KB)的性能差异。

### 场景 6: 连接池性能测试
测试 TcpClientPool 的连接复用效率。

---

## 🔧 测试工具推荐

### 1. 内置性能测试工具
参见下方提供的测试代码。

### 2. 第三方工具

**网络压测工具**:
- **wrk** - HTTP 压测，可改造用于 TCP
- **iperf3** - 网络带宽测试
- **netperf** - 网络性能测试
- **tcpkali** - TCP/WebSocket 压测工具

**监控工具**:
- **Windows Performance Monitor** (perfmon) - CPU/内存/网络监控
- **dotMemory** - .NET 内存分析
- **dotTrace** - .NET 性能分析
- **PerfView** - 微软官方性能分析工具

**压测平台**:
- **JMeter** - 可编写自定义 TCP 采样器
- **Gatling** - Scala 编写的压测工具
- **Locust** - Python 编写的压测工具

---

## 💻 性能测试代码

### 测试项目结构

```
NetEZ.PerformanceTest/
├── Server/
│   └── PerformanceTestServer.cs    # 性能测试服务器
├── Client/
│   └── PerformanceTestClient.cs    # 性能测试客户端
├── Benchmark/
│   └── BenchmarkRunner.cs          # 基准测试执行器
└── Program.cs                      # 主程序
```

### 1. 性能测试服务器

创建文件: `NetEZ.PerformanceTest/Server/PerformanceTestServer.cs`

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using NetEZ.Core.Server;
using NetEZ.Core.Protocol.JMP;
using NetEZ.Core.Event;

namespace NetEZ.PerformanceTest.Server
{
    public class PerformanceTestServer
    {
        private TcpServiceBase _server;
        private long _totalRequests = 0;
        private long _totalBytes = 0;
        private DateTime _startTime;
        private Timer _statsTimer;

        public void Start(string ip, int port)
        {
            _server = new TcpServiceBase("PerfTestServer", ip, port);

            // 注册协议解析器
            _server.RegisterMessageParser(new JMPParser());

            // 注册事件
            _server.RegisterOnClientConnectedCallback(OnClientConnected);
            _server.RegisterOnClientReceivedCallback(OnClientReceived);
            _server.RegisterOnClientDisconnectedCallback(OnClientDisconnected);

            // 启动服务器
            if (_server.Start())
            {
                Console.WriteLine($"[Server] 启动成功: {ip}:{port}");
                _startTime = DateTime.Now;

                // 启动统计定时器 (每秒输出一次统计)
                _statsTimer = new Timer(PrintStats, null, 1000, 1000);
            }
            else
            {
                Console.WriteLine("[Server] 启动失败");
            }
        }

        private void OnClientConnected(IClientManager client)
        {
            Console.WriteLine($"[Server] 客户端连接: {client.ClientId}");
        }

        private void OnClientReceived(IClientManager client, IMessage msg)
        {
            Interlocked.Increment(ref _totalRequests);

            var jmpMsg = msg as IJMPMessage;
            if (jmpMsg != null)
            {
                Interlocked.Add(ref _totalBytes, jmpMsg.Body.Length);

                // Echo 模式：直接返回收到的消息
                _server.SendMessageToClient(client.ClientId, msg);
            }
        }

        private void OnClientDisconnected(IClientManager client)
        {
            Console.WriteLine($"[Server] 客户端断开: {client.ClientId}");
        }

        private void PrintStats(object state)
        {
            var elapsed = (DateTime.Now - _startTime).TotalSeconds;
            var qps = _totalRequests / elapsed;
            var throughput = (_totalBytes / elapsed) / (1024 * 1024); // MB/s

            Console.WriteLine($"[Stats] QPS: {qps:F0} | 吞吐量: {throughput:F2} MB/s | 总请求: {_totalRequests}");
        }

        public void Stop()
        {
            _statsTimer?.Dispose();
            _server?.Stop();
            Console.WriteLine("[Server] 已停止");
        }
    }
}
```

### 2. 性能测试客户端

创建文件: `NetEZ.PerformanceTest/Client/PerformanceTestClient.cs`

```csharp
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NetEZ.Core.Client;
using NetEZ.Core.Protocol.JMP;

namespace NetEZ.PerformanceTest.Client
{
    public class PerformanceTestClient
    {
        private string _serverIp;
        private int _serverPort;
        private int _clientCount;
        private int _messagesPerClient;
        private int _messageSize;

        private long _totalSent = 0;
        private long _totalReceived = 0;
        private long _totalLatency = 0;
        private long _minLatency = long.MaxValue;
        private long _maxLatency = 0;

        public PerformanceTestClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task RunBenchmark(int clientCount, int messagesPerClient, int messageSize)
        {
            _clientCount = clientCount;
            _messagesPerClient = messagesPerClient;
            _messageSize = messageSize;

            Console.WriteLine($"[Client] 性能测试参数:");
            Console.WriteLine($"  - 并发客户端数: {clientCount}");
            Console.WriteLine($"  - 每客户端消息数: {messagesPerClient}");
            Console.WriteLine($"  - 消息大小: {messageSize} 字节");
            Console.WriteLine($"  - 总消息数: {clientCount * messagesPerClient}");
            Console.WriteLine();

            var sw = Stopwatch.StartNew();

            // 并发启动多个客户端
            var tasks = new Task[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                int clientId = i;
                tasks[i] = Task.Run(() => RunClientAsync(clientId));
            }

            await Task.WhenAll(tasks);
            sw.Stop();

            PrintResults(sw.Elapsed);
        }

        private async Task RunClientAsync(int clientId)
        {
            var client = new TcpClientBase(_serverIp, _serverPort);
            var parser = new JMPParser();
            client.RegisterParser(parser);

            long receivedCount = 0;
            var receiveEvent = new AutoResetEvent(false);

            // 注册接收回调
            client.RegisterOnRecvServerDataCallback((msg) =>
            {
                Interlocked.Increment(ref _totalReceived);
                receivedCount++;

                if (receivedCount >= _messagesPerClient)
                {
                    receiveEvent.Set();
                }
            });

            try
            {
                // 连接服务器
                await client.ConnectServerAsync();

                // 准备测试数据
                string testData = new string('X', _messageSize);
                var message = new JMPMessageBase("TestSignal", testData);

                // 发送消息并记录延迟
                for (int i = 0; i < _messagesPerClient; i++)
                {
                    var sw = Stopwatch.StartNew();
                    await client.SendMessageAsync(message);
                    sw.Stop();

                    Interlocked.Increment(ref _totalSent);
                    Interlocked.Add(ref _totalLatency, sw.ElapsedMilliseconds);

                    // 更新最小/最大延迟
                    UpdateLatencyStats(sw.ElapsedMilliseconds);
                }

                // 等待所有响应
                receiveEvent.WaitOne(TimeSpan.FromSeconds(30));

                client.Disconnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Client-{clientId}] 错误: {ex.Message}");
            }
        }

        private void UpdateLatencyStats(long latencyMs)
        {
            long current;

            // 更新最小值
            do
            {
                current = _minLatency;
                if (latencyMs >= current) break;
            } while (Interlocked.CompareExchange(ref _minLatency, latencyMs, current) != current);

            // 更新最大值
            do
            {
                current = _maxLatency;
                if (latencyMs <= current) break;
            } while (Interlocked.CompareExchange(ref _maxLatency, latencyMs, current) != current);
        }

        private void PrintResults(TimeSpan elapsed)
        {
            Console.WriteLine("\n========== 性能测试结果 ==========");
            Console.WriteLine($"总耗时: {elapsed.TotalSeconds:F2} 秒");
            Console.WriteLine($"发送消息数: {_totalSent}");
            Console.WriteLine($"接收消息数: {_totalReceived}");
            Console.WriteLine($"成功率: {(_totalReceived * 100.0 / _totalSent):F2}%");
            Console.WriteLine();

            Console.WriteLine($"QPS (每秒请求数): {_totalSent / elapsed.TotalSeconds:F0}");
            Console.WriteLine($"吞吐量: {(_totalSent * _messageSize / elapsed.TotalSeconds) / (1024 * 1024):F2} MB/s");
            Console.WriteLine();

            if (_totalSent > 0)
            {
                Console.WriteLine($"平均延迟: {_totalLatency / (double)_totalSent:F2} ms");
                Console.WriteLine($"最小延迟: {_minLatency} ms");
                Console.WriteLine($"最大延迟: {_maxLatency} ms");
            }

            Console.WriteLine("=================================\n");
        }
    }
}
```

### 3. 基准测试执行器

创建文件: `NetEZ.PerformanceTest/Benchmark/BenchmarkRunner.cs`

```csharp
using System;
using System.Threading.Tasks;
using NetEZ.PerformanceTest.Client;

namespace NetEZ.PerformanceTest.Benchmark
{
    public class BenchmarkRunner
    {
        private string _serverIp;
        private int _serverPort;

        public BenchmarkRunner(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        public async Task RunAllBenchmarks()
        {
            Console.WriteLine("======================================");
            Console.WriteLine("       NetEZ 性能基准测试套件");
            Console.WriteLine("======================================\n");

            // 等待服务器就绪
            await Task.Delay(1000);

            // 1. 小消息高并发测试
            Console.WriteLine("【测试 1】小消息高并发 (100字节)");
            await RunTest(100, 1000, 100);
            await Task.Delay(2000);

            // 2. 中等消息测试
            Console.WriteLine("【测试 2】中等消息 (1KB)");
            await RunTest(50, 500, 1024);
            await Task.Delay(2000);

            // 3. 大消息测试
            Console.WriteLine("【测试 3】大消息 (10KB)");
            await RunTest(20, 200, 10240);
            await Task.Delay(2000);

            // 4. 单客户端持续发送
            Console.WriteLine("【测试 4】单客户端持续发送");
            await RunTest(1, 10000, 512);
            await Task.Delay(2000);

            // 5. 极限并发测试
            Console.WriteLine("【测试 5】极限并发连接");
            await RunTest(500, 100, 256);

            Console.WriteLine("\n所有基准测试完成！");
        }

        private async Task RunTest(int clients, int messagesPerClient, int messageSize)
        {
            var testClient = new PerformanceTestClient(_serverIp, _serverPort);
            await testClient.RunBenchmark(clients, messagesPerClient, messageSize);
        }
    }
}
```

### 4. 主程序

创建文件: `NetEZ.PerformanceTest/Program.cs`

```csharp
using System;
using System.Threading.Tasks;
using NetEZ.PerformanceTest.Server;
using NetEZ.PerformanceTest.Benchmark;

namespace NetEZ.PerformanceTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NetEZ 性能测试工具\n");
            Console.WriteLine("选择模式:");
            Console.WriteLine("1. 启动服务器");
            Console.WriteLine("2. 运行客户端压测");
            Console.WriteLine("3. 运行完整基准测试");
            Console.Write("\n请选择 (1-3): ");

            var choice = Console.ReadLine();

            string ip = "127.0.0.1";
            int port = 8888;

            switch (choice)
            {
                case "1":
                    RunServer(ip, port);
                    break;

                case "2":
                    await RunClientTest(ip, port);
                    break;

                case "3":
                    // 在单独的任务中启动服务器
                    Task.Run(() => RunServer(ip, port));

                    // 等待服务器启动
                    await Task.Delay(2000);

                    // 运行基准测试
                    var runner = new BenchmarkRunner(ip, port);
                    await runner.RunAllBenchmarks();
                    break;

                default:
                    Console.WriteLine("无效选择");
                    break;
            }

            Console.WriteLine("\n按任意键退出...");
            Console.ReadKey();
        }

        static void RunServer(string ip, int port)
        {
            var server = new PerformanceTestServer();
            server.Start(ip, port);

            Console.WriteLine("\n服务器运行中... 按 Ctrl+C 停止");

            // 保持运行
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        static async Task RunClientTest(string ip, int port)
        {
            Console.Write("并发客户端数: ");
            int clients = int.Parse(Console.ReadLine());

            Console.Write("每客户端消息数: ");
            int messages = int.Parse(Console.ReadLine());

            Console.Write("消息大小 (字节): ");
            int size = int.Parse(Console.ReadLine());

            var testClient = new Client.PerformanceTestClient(ip, port);
            await testClient.RunBenchmark(clients, messages, size);
        }
    }
}
```

---

## 📈 使用 Windows 性能监视器

### 监控 .NET 应用性能

1. 打开 Performance Monitor (perfmon.exe)
2. 添加计数器:

**CPU 监控**:
- `Processor` → `% Processor Time` → `_Total`

**内存监控**:
- `Process` → `Private Bytes` → 选择你的进程
- `Process` → `Working Set` → 选择你的进程
- `.NET CLR Memory` → `# Bytes in all Heaps`

**GC 监控**:
- `.NET CLR Memory` → `% Time in GC`
- `.NET CLR Memory` → `Gen 0 Collections`
- `.NET CLR Memory` → `Gen 1 Collections`
- `.NET CLR Memory` → `Gen 2 Collections`

**网络监控**:
- `Network Interface` → `Bytes Received/sec`
- `Network Interface` → `Bytes Sent/sec`

---

## 🎯 性能优化建议

基于测试结果的优化方向:

### 如果 QPS 低
1. 增加消息处理线程数 (`MessageProcessingThreadCount`)
2. 检查是否有同步阻塞操作
3. 考虑使用连接池

### 如果延迟高
1. 减小接收缓冲区大小
2. 检查网络状况
3. 优化协议解析逻辑

### 如果内存占用高
1. 检查对象是否正确释放
2. 调整 SAEA 对象池大小
3. 检查是否有内存泄漏

### 如果 CPU 占用高
1. 检查是否有热循环
2. 优化消息处理逻辑
3. 考虑减少线程上下文切换

---

## 📝 性能测试检查清单

- [ ] 测试环境准备 (关闭其他应用、禁用杀毒软件)
- [ ] 服务器和客户端分别运行在不同机器
- [ ] 测试多种消息大小 (100B, 1KB, 10KB, 100KB)
- [ ] 测试不同并发数 (1, 10, 100, 1000)
- [ ] 长时间稳定性测试 (24小时)
- [ ] 监控系统资源 (CPU, 内存, 网络)
- [ ] 记录 GC 统计信息
- [ ] 对比不同协议的性能 (JMP vs PureText)
- [ ] 测试连接池的效果
- [ ] 记录测试结果和配置参数

---

## 📊 性能基准参考

以下是 NetEZ 在典型硬件上的预期性能 (仅供参考):

| 场景 | QPS | 延迟 (P99) | CPU | 内存 |
|-----|-----|-----------|-----|------|
| 100并发, 1KB消息 | 50K+ | < 10ms | < 30% | < 500MB |
| 10并发, 100B消息 | 100K+ | < 5ms | < 20% | < 200MB |
| 1000并发, 10KB消息 | 20K+ | < 50ms | < 50% | < 1GB |

实际性能取决于:
- 硬件配置 (CPU, 内存, 网络)
- 操作系统配置
- 网络环境
- 业务逻辑复杂度

---

## 🔗 相关资源

- [README.md](README.md) - 项目概览
- [ARCHITECTURE.md](ARCHITECTURE.md) - 架构设计
- [.NET Performance Tips](https://docs.microsoft.com/en-us/dotnet/framework/performance/)
