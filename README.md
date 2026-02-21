# NetEZ

[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.0-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

NetEZ 是一个高性能、易用的 .NET TCP 网络通信库，专为构建可靠的客户端-服务器应用而设计。

## ✨ 特性

- 🚀 **高性能** - 基于 SAEA (SocketAsyncEventArgs) 的异步 I/O 模型
- 🔌 **灵活的协议** - 内置 JMP (JSON 消息协议) 和纯文本协议，支持自定义扩展
- 🔄 **连接池** - 客户端连接池支持，提高连接复用效率
- 💓 **心跳监控** - 自动检测和清理无响应的客户端连接
- 🧵 **多线程处理** - 可配置的消息处理线程池
- 📦 **TCP 粘包处理** - 完善的数据包边界检测和缓冲机制
- 🎯 **事件驱动** - 基于事件的连接生命周期管理
- ⚙️ **易于配置** - 支持 XML 配置文件和编程式配置
- 🛠️ **丰富的工具库** - 包含日志、缓存、加密等常用工具

## 📦 安装

### NuGet 安装

```bash
# 核心库
dotnet add package NetEZ.Core

# 工具库
dotnet add package NetEZ.Utility
```

### 手动编译

```bash
git clone https://github.com/yourusername/NetEZ.git
cd NetEZ
dotnet build NetEZ.sln -c Release
```

## 🚀 快速开始

### 创建 TCP 服务器

```csharp
using NetEZ.Core.Server;
using NetEZ.Core.Protocol.JMP;
using NetEZ.Core.Event;

// 创建服务器实例
var server = new TcpServiceBase("MyServer", "127.0.0.1", 8888);

// 注册 JMP 协议解析器
server.RegisterMessageParser(new JMPParser());

// 注册事件处理器
server.RegisterOnClientConnectedCallback((clientId, context) =>
{
    Console.WriteLine($"客户端连接: {clientId}");
});

server.RegisterOnClientReceivedCallback((clientId, message) =>
{
    var jmpMsg = message as IJMPMessage;
    Console.WriteLine($"收到消息 - 信号: {jmpMsg.Signal}, 内容: {jmpMsg.Body}");

    // 回复消息
    server.SendMessageToClient(clientId, jmpMsg);
});

server.RegisterOnClientDisconnectedCallback((clientId) =>
{
    Console.WriteLine($"客户端断开: {clientId}");
});

// 启动服务器
if (server.Start())
{
    Console.WriteLine("服务器启动成功");
}
```

### 创建 TCP 客户端

```csharp
using NetEZ.Core.Client;
using NetEZ.Core.Protocol.JMP;

// 创建客户端实例
var client = new TcpClientBase("127.0.0.1", 8888);

// 注册协议解析器
client.RegisterParser(new JMPParser());

// 注册接收回调
client.RegisterOnRecvServerDataCallback((message) =>
{
    var jmpMsg = message as IJMPMessage;
    Console.WriteLine($"收到响应 - 信号: {jmpMsg.Signal}, 内容: {jmpMsg.Body}");
});

// 连接服务器
await client.ConnectServerAsync();

// 发送消息
var msg = new JMPMessageBase("HelloSignal", "{\"text\":\"Hello Server\"}");
await client.SendMessageAsync(msg);
```

### 使用连接池

```csharp
using NetEZ.Core.Client;

// 创建连接池（最大10个连接）
var pool = new TcpClientPool("127.0.0.1", 8888, 10);

// 从池中获取客户端
var client = await pool.GetClientAsync();

// 使用客户端发送消息
await client.SendMessageAsync(message);

// 归还到连接池
pool.ReleaseClient(client);
```

## 📚 项目结构

```
NetEZ/
├── NetEZ.Core/                 # 核心网络通信库
│   ├── Server/                 # 服务器实现
│   │   ├── TcpServiceBase.cs   # TCP 服务器基类
│   │   ├── TcpClientManager.cs # 客户端连接管理
│   │   └── TcpServiceConfigure.cs # 服务器配置
│   ├── Client/                 # 客户端实现
│   │   ├── TcpClientBase.cs    # TCP 客户端基类
│   │   └── TcpClientPool.cs    # 连接池
│   ├── Protocol/               # 协议层
│   │   ├── JMP/                # JSON 消息协议
│   │   └── PureText/           # 纯文本协议
│   ├── IO/                     # IO 处理
│   │   ├── BinaryIOBuffer.cs   # 数据包缓冲
│   │   └── SocketAsyncEventArgsPool.cs # SAEA 对象池
│   └── Event/                  # 事件定义
│
└── NetEZ.Utility/              # 工具库
    ├── Logger/                 # 日志系统
    ├── Cache/                  # 缓存（LRU）
    ├── Encryption/             # 加密工具
    ├── Configure/              # 配置解析
    ├── Algorithm/              # 算法工具
    └── Tools/                  # 通用工具
```

## 🔌 协议说明

### JMP (JSON Message Protocol)

JMP 是一种基于信号路由的 JSON 消息协议，消息格式如下：

```
+------------------+------------------+------------------+------------------+
| Signal Length    | Signal Name      | Body Length      | JSON Body        |
| (1 byte)         | (N bytes)        | (4 bytes)        | (M bytes)        |
+------------------+------------------+------------------+------------------+
```

**示例**：

```csharp
var message = new JMPMessageBase("UserLogin", "{\"username\":\"admin\",\"password\":\"123456\"}");
```

### PureText Protocol

纯文本协议，适用于简单的文本消息传输：

```
+------------------+------------------+
| Message Length   | UTF-8 Text       |
| (4 bytes)        | (N bytes)        |
+------------------+------------------+
```

### 自定义协议

实现 `IProtocolParser` 接口即可扩展自定义协议：

```csharp
public interface IProtocolParser
{
    IMessage Parse(byte[] buffer, int offset, int count);
    byte[] Pack(IMessage message);
}
```

## ⚙️ 配置

### 编程式配置

```csharp
var server = new TcpServiceBase("MyServer", "0.0.0.0", 8888)
{
    ClientReceivingBufferSize = 8192,
    HeartBeatInterval = 30000,          // 30秒心跳
    MessageProcessingThreadCount = 4     // 4个消息处理线程
};
```

### XML 配置文件

```xml
<TcpServiceConfigure>
    <ServiceName>MyServer</ServiceName>
    <ListenHost>
        <Host IP="0.0.0.0" Port="8888"/>
        <Host IP="0.0.0.0" Port="8889"/>
    </ListenHost>
    <ClientReceivingBufferSize>8192</ClientReceivingBufferSize>
    <HeartBeatInterval>30000</HeartBeatInterval>
    <MessageProcessingThreadCount>4</MessageProcessingThreadCount>
</TcpServiceConfigure>
```

加载配置：

```csharp
var config = TcpServiceConfigure.LoadFromFile("config.xml");
var server = new TcpServiceBase(config);
```

## 🛠️ 工具库功能

### 异步日志

```csharp
using NetEZ.Utility.Logger;

Logger.Info("应用启动");
Logger.Debug("调试信息");
Logger.Error("错误信息", exception);
```

### LRU 缓存

```csharp
using NetEZ.Utility.Cache;

var cache = new LRUCache<string, User>(capacity: 1000, expireSeconds: 3600);
cache.Put("user_123", userObject);
var user = cache.Get("user_123");
```

### RC4 加密

```csharp
using NetEZ.Utility.Encryption;

byte[] encrypted = RC4Encrypt.Encrypt(data, key);
byte[] decrypted = RC4Encrypt.Decrypt(encrypted, key);
```

## 🔧 核心常量

```csharp
// 接收缓冲区最大长度（支持粘包）
MAX_TRANSFER_LENGTH = 4MB

// 单个消息最大长度
MAX_MESSAGE_LENGTH = 512KB
```

## 📊 性能特性

- **异步 I/O** - 基于 SAEA 模式，零 I/O 线程阻塞
- **对象池** - SAEA 对象池和客户端连接池，减少 GC 压力
- **零拷贝** - 数据包解析过程避免不必要的内存拷贝
- **并发处理** - 支持多线程消息处理，充分利用多核 CPU
- **内存高效** - 缓冲区复用，避免频繁分配

## 🔍 故障排查

### 粘包问题

NetEZ 内置了完善的粘包处理机制 (`BinaryIOBuffer`)，会自动处理 TCP 流的粘包和拆包问题。

### 连接超时

检查心跳配置和网络状况：

```csharp
server.HeartBeatInterval = 30000;  // 调整心跳间隔
```

### 内存占用

调整接收缓冲区大小：

```csharp
server.ClientReceivingBufferSize = 4096;  // 根据实际消息大小调整
```

## 🤝 贡献

欢迎贡献代码、报告问题或提出新功能建议。

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 提交 Pull Request

## 📄 许可证

本项目采用 MIT 许可证 - 详见 [LICENSE](LICENSE) 文件

## 📮 联系方式

如有问题或建议，欢迎提交 Issue。

## 🙏 致谢

感谢所有为本项目做出贡献的开发者。

---

⭐ 如果这个项目对你有帮助，请给个 Star！
