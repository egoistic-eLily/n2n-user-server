using System.Diagnostics;
using System.Text;

namespace N2nSupernode;

// n2n supernode 进程宿主（静态类）：负责启动/停止进程，并把进程输出
// 与生命周期事件以事件形式对外发布。
//
// 单实例设计：静态状态意味着同一时刻只托管一个 supernode 进程，
// 与本项目的部署形态（每台服务器一个 supernode）一致。
//
// 职责边界：本库不做任何配置文件的创建/校验/维护——白名单文件、
// 证书、config.json 等全部由主程序负责；本类只接收 SupernodeOptions
// 配置对象并按其拉起进程。
//
// 跨平台说明：不使用 Windows 命名管道；进程输出通过
// Process.StandardOutput/Error（全平台一致）读取，
// 命令通信走回环 UDP 管理口（见内部 SupernodeManagement）。
//
// 线程说明：事件在后台线程触发，订阅方如需更新 UI 请自行调度。
public static class SupernodeHost
{
    private static SupernodeOptions? _options;
    private static Process? _process;
    private static CancellationTokenSource? _pumpCts;

    // 所有消息（进程输出 + 宿主生命周期 + 管理应答）都从此事件发布
    public static event EventHandler<SupernodeMessage>? MessageReceived;

    // 进程退出事件（正常 Stop 也会触发），参数为退出码
    public static event EventHandler<int>? Exited;

    // 进程是否正在运行
    public static bool IsRunning
    {
        get
        {
            Process? p = _process;
            return p != null && !p.HasExited;
        }
    }

    // 当前进程的退出码（未启动或仍在运行时为 null）
    public static int? ExitCode
    {
        get
        {
            Process? p = _process;
            return p != null && p.HasExited ? p.ExitCode : null;
        }
    }

    // 启动 supernode 进程。
    // 注意：本库不做任何配置文件的创建/校验/维护——白名单文件、
    // 证书、config.json 等全部由主程序准备；若配置指向的文件缺失，
    // supernode 自身的处理行为（警告/退出）即为最终行为。
    public static void Start(SupernodeOptions options)
    {
        if (IsRunning)
        {
            Publish(SupernodeMessageLevel.Host, "Start() ignored: supernode is already running");
            return;
        }

        _options = options ?? throw new ArgumentNullException(nameof(options));

        KillExistingProcesses(); // 单实例：清理遗留的同名进程后再启动

        string arguments = _options.BuildArguments();

        var psi = new ProcessStartInfo
        {
            FileName = _options.ExecutablePath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        _pumpCts = new CancellationTokenSource();

        try
        {
            _process = Process.Start(psi);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"failed to start supernode '{_options.ExecutablePath}' (check the executable path): {ex.Message}", ex);
        }

        if (_process == null)
        {
            throw new InvalidOperationException("failed to start supernode process");
        }

        Publish(SupernodeMessageLevel.Host, $"supernode started: {_options.ExecutablePath} {arguments}");

        // 主程序退出（Ctrl+C、窗口关闭、Main 返回等优雅路径，全平台一致）时自动带走 supernode
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Stop(2000);

        _process.OutputDataReceived += (_, e) => PublishLine(e.Data);
        _process.ErrorDataReceived += (_, e) => PublishLine(e.Data);
        _process.Exited += (_, _) =>
        {
            int code = ExitCode ?? -1;
            Publish(SupernodeMessageLevel.Host, $"supernode exited with code {code}");
            Exited?.Invoke(null, code);
        };
        _process.EnableRaisingEvents = true;

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();
    }

    // 停止进程（连同其全部子进程）
    public static void Stop(int waitForExitMs = 5000)
    {
        Process? p = _process;

        if (p == null || p.HasExited)
        {
            return;
        }

        Publish(SupernodeMessageLevel.Host, "stopping supernode...");

        try
        {
            p.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or SystemException)
        {
            // 进程恰好自行退出时的竞态，忽略
        }

        if (!p.WaitForExit(waitForExitMs))
        {
            Publish(SupernodeMessageLevel.Warning, "supernode did not exit within the timeout");
        }

        _pumpCts?.Cancel();
    }

    // 向管理端口发送命令（reload_communities / edges / communities 等），
    // 返回应答文本（n2n 3.x 为 JSON）。使用 Start 时传入配置里的管理端口。
    public static Task<string> SendManagementCommandAsync(string command, int timeoutMs = 3000,
        CancellationToken cancellationToken = default)
    {
        int port = _options?.ManagementPort ?? 5644;
        return SupernodeManagement.SendAsync(command, port, "127.0.0.1", timeoutMs, cancellationToken);
    }

    // 单实例清理：检测并终止与配置中可执行文件同名的遗留进程。
    // 默认部署形态为单 supernode；启动新的之前先清掉旧的，
    // 保证最终只保留一个。跨平台：按进程名匹配（不含扩展名）。
    private static void KillExistingProcesses()
    {
        string exePath = _options?.ExecutablePath ?? "supernode";
        string name = Path.GetFileNameWithoutExtension(exePath);

        Process[] existing;
        try
        {
            existing = Process.GetProcessesByName(name);
        }
        catch (Exception ex)
        {
            Publish(SupernodeMessageLevel.Warning, $"could not enumerate '{name}' processes: {ex.Message}");
            return;
        }

        if (existing.Length == 0)
        {
            return;
        }

        Publish(SupernodeMessageLevel.Host, $"found {existing.Length} existing '{name}' process(es), terminating...");

        foreach (Process p in existing)
        {
            try
            {
                p.Kill(entireProcessTree: true);
                Publish(SupernodeMessageLevel.Host, $"terminated existing '{name}' process (pid {p.Id})");
            }
            catch (Exception ex)
            {
                // 无权限终止他人进程等场景：警告并继续
                Publish(SupernodeMessageLevel.Warning, $"could not terminate pid {p.Id}: {ex.Message}");
            }
            finally
            {
                p.Dispose();
            }
        }
    }

    private static void PublishLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        SupernodeMessageLevel level = SupernodeMessageLevel.Info;

        if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
        {
            level = SupernodeMessageLevel.Error;
        }
        else if (line.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
        {
            level = SupernodeMessageLevel.Warning;
        }
        else if (line.Contains("TRACE_DEBUG", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("TRACE_INFO", StringComparison.OrdinalIgnoreCase))
        {
            level = SupernodeMessageLevel.Trace;
        }

        Publish(level, line.TrimEnd());
    }

    private static void Publish(SupernodeMessageLevel level, string text)
    {
        MessageReceived?.Invoke(null, new SupernodeMessage
        {
            Timestamp = DateTimeOffset.Now,
            Level = level,
            Source = SupernodeMessageSource.Host,
            Text = text,
        });
    }

}