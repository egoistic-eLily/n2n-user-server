namespace N2nSupernode;

// 消息级别
public enum SupernodeMessageLevel
{
    Trace,   // 调试细节（-v 及以上才会出现）
    Info,    // 正常运行信息
    Warning, // 警告
    Error,   // 错误
    Host     // 宿主自身事件（启动/退出/管理命令结果）
}

// 消息来源
public enum SupernodeMessageSource
{
    Supernode,   // supernode 进程的 stdout/stderr 输出
    Host,        // SupernodeHost 宿主自身的生命周期事件
    Management   // 管理端口命令的应答
}

// 事件携带的消息对象
public sealed class SupernodeMessage
{
    public DateTimeOffset Timestamp { get; init; }

    public SupernodeMessageLevel Level { get; init; }

    public SupernodeMessageSource Source { get; init; }

    public string Text { get; init; } = string.Empty;

    public override string ToString()
    {
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level,-7}] [{Source}] {Text}";
    }
}
