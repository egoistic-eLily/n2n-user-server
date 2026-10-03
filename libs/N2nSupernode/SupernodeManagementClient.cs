using System.Net.Sockets;
using System.Text;

namespace N2nSupernode;

// n2n supernode 管理端口通信（回环 UDP），内部实现，
// 对外经由 SupernodeHost.SendManagementCommandAsync() 使用。
// 可用命令：reload_communities / edges / communities / timestamps /
// packetstats / verbose / stop（应答为 JSON 文本）。
internal static class SupernodeManagement
{
    // 发送命令并等待应答；超时抛出 TimeoutException。
    // 全平台可用：UDP 回环通信不依赖任何 Windows 专有机制。
    public static async Task<string> SendAsync(string command, int port = 5644,
        string host = "127.0.0.1", int timeoutMs = 3000, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        using var udp = new UdpClient();

        byte[] payload = Encoding.ASCII.GetBytes(command);
        await udp.SendAsync(payload, payload.Length, host, port);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        while (true)
        {
            UdpReceiveResult result = await udp.ReceiveAsync(timeoutCts.Token);

            // n2n 管理口会向命令源回发应答；忽略其他来源的杂散包
            if (result.RemoteEndPoint.Port == port)
            {
                return Encoding.UTF8.GetString(result.Buffer);
            }
        }
    }
}
