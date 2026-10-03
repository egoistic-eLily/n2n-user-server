# N2nSupernode

n2n supernode 的**进程宿主 + 管理客户端**类库：在应用程序内统一启动/停止
supernode 进程，并以事件形式接收其全部输出消息，同时提供回环 UDP 管理口
客户端用于运行时控制。

**跨平台**：不使用 Windows 命名管道——进程输出经 `Process.StandardOutput/Error`
（Windows/Linux 行为一致），命令通信走 n2n 自带的回环 UDP 管理口。

## 使用

```csharp
using N2nSupernode;

var options = new SupernodeOptions {
    ExecutablePath   = "thirdparty/bin/supernode.exe", // Linux 下指向自编译的 supernode
    Port             = 7654,                            // 主 UDP 端口（edge 的 -l 指向它）
    ManagementPort   = 5644,                            // 管理口（回环 UDP，仅本机可达）
    CommunityListPath = "Config/community.list",        // 白名单文件（必须已存在）
};

// 事件形式接收全部消息（级别/来源/文本），后台线程触发，UI 侧自行调度
SupernodeHost.MessageReceived += (_, m) => Console.WriteLine(m);

// 进程退出事件（携带退出码）
SupernodeHost.Exited += (_, code) => Console.WriteLine($"supernode exited: {code}");

SupernodeHost.Start(options);   // 传入配置对象，启动 supernode

// 运行时控制（n2n 管理口命令，应答为文本/JSON）
string reply = await SupernodeHost.SendManagementCommandAsync("reload_communities");
reply = await SupernodeHost.SendManagementCommandAsync("edges");

SupernodeHost.Stop();           // 连同子进程一起终止
```

## 消息对象

`SupernodeHost` 为静态类（单实例语义：同一时刻只托管一个 supernode 进程，
与本项目的部署形态一致），全部 API 均为静态方法。
`MessageReceived` 事件的载体是 `SupernodeMessage`：

| 属性 | 含义 |
| --- | --- |
| `Timestamp` | 本地时间戳 |
| `Level` | `Trace` / `Info` / `Warning` / `Error` / `Host`（宿主自身事件） |
| `Source` | `Supernode`（进程输出）/ `Host`（生命周期）/ `Management`（管理应答） |
| `Text` | 消息文本（进程输出的单行日志） |

级别判定：行内含 `ERROR` → Error、`WARNING` → Warning，其余 Info。

## 管理命令

| 命令 | 作用 |
| --- | --- |
| `reload_communities` | 重读白名单文件（增删社区/用户后调用；n2n 会通知 edge 重注册，建议节流合并） |
| `communities` | 列出当前社区 |
| `edges` | 列出在线 edge 节点 |
| `stop` | 令 supernode 优雅退出 |

## 设计约束

- **单实例保证**：`Start()` 会先检测与配置中可执行文件同名的遗留进程并
  全部终止，再启动新的——无论之前是否有残留，最终只保留一个 supernode
  进程（默认部署形态即单 supernode；跨平台按进程名匹配，不含扩展名）；
- **本库不做任何配置文件的创建、校验或维护**：community.list、config.json、
  证书等全部由主程序负责；库只接收一个纯数据配置（`SupernodeOptions`，
  含白名单路径、url 等字段）并按其拉起进程——文件缺失时 supernode 自身的
  处理行为即为最终行为；
- `SupernodeOptions.Url` 仅为信息载体（supernode 命令行不消费），
  供主程序在日志与 edge 配置下发时取用；
- 本库为原创实现（MIT，随仓库主许可），与 GPL 的 supernode 仅为**进程级协作**；
- Windows/Linux 均可用：Linux 侧把 `ExecutablePath` 指向自行编译的
  `supernode` 二进制即可，参数与协议完全相同。
- 进程联动采用**全平台机制**：主程序优雅退出（Ctrl+C、窗口关闭、`Main`
  返回、`Environment.Exit`）时经 `AppDomain.ProcessExit` 自动 `Stop()`。
  注意：主程序被强杀/崩溃时操作系统不会自动回收子进程（这是两个平台的
  共同行为），如有需要由主程序在启动时清理遗留的 supernode 进程。
