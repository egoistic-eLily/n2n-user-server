using N2nSupernode;

namespace N2N_USER_SERVER.Core.Supernode
{
    static class SupernodeOperations
    {
        public static void Init()
        {
            Start_Supernode();
        }
        public static void Start_Supernode()
        {
            var options = new N2nSupernode.SupernodeOptions()
            {
                ExecutablePath = Bootstrap.Initialization.config.supernode_path,
                Port = Bootstrap.Initialization.config.supernode_port,
                ManagementPort = Bootstrap.Initialization.config.supernode_ManagementPort,
                CommunityListPath = Bootstrap.Initialization.config.supernode_CommunityListPath
            };
            SupernodeHost.MessageReceived += information;
            SupernodeHost.Exited += Exitedinformation;
            N2nSupernode.SupernodeHost.Start(options);
        }
        public static void Stop_Supernode()
        {
            N2nSupernode.SupernodeHost.Stop();
        }
        private static void information(object? sender, SupernodeMessage m)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"Supernode: {m.Timestamp}  Level: {m.Level} Source: {m.Source} Text:{m.Text}");
        }
        private static void Exitedinformation(object? sender, int code)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"Supernode已退出: code:{code}");
        }
    }
}
