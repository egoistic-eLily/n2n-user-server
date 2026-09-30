using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace N2N_USER_SERVER.Bootstrap
{
    public static class Initialization
    {
        public static Config? config { get; private set; }

        public static void Init() {
            InitLogger();
            GetConfig();//获取配置
            InitCert();
            initDB();
        }

        //初始化Serilog：控制台 + Logs/目录下按天滚动的日志文件
        private static void InitLogger() {
            string logdir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(logdir, "server-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
        
        //配置证书的路径

        private static void InitCert() {
            if (config == null) {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Fatal, "配置未加载，无法初始化证书");
                return;
            }
            if (config.certmode != CertMode.Default) {
                //Manual模式：使用配置指定的证书路径
                if (string.IsNullOrWhiteSpace(config.pfxpath)) {
                    Services.ErrorReporter.Report(Services.ExceptionType.NoFile, Services.LogLevel.Fatal, "未配置证书路径(pfxpath)");
                    return;
                }
                if (!File.Exists(config.pfxpath)) {
                    Services.ErrorReporter.Report(Services.ExceptionType.NoFile, Services.LogLevel.Fatal, "证书缺失");
                    return;
                }
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"证书加载完成-{config.pfxpath}");
                return;
            }
            //Default模式：依据配置的域名在Cert目录下寻找 <域名>.pfx
            if (string.IsNullOrWhiteSpace(config.url)) {
                Services.ErrorReporter.Report(Services.ExceptionType.NoFile, Services.LogLevel.Fatal, "未配置域名(url)，无法定位证书");
                return;
            }
            string certpath = Path.Combine(AppContext.BaseDirectory, "Cert");
            Directory.CreateDirectory(certpath);
            string pfxpath = Path.Combine(certpath, $"{config.url}.pfx");
            if (!File.Exists(pfxpath)) {
                Services.ErrorReporter.Report(Services.ExceptionType.NoFile, Services.LogLevel.Fatal, $"证书缺失-请将PFX证书放置于{pfxpath}");
                return;
            }
            config.pfxpath = pfxpath;
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"证书加载完成-{pfxpath}");
        }

        private static void initDB()
        {
            if (config.userdbmode != UserDbMode.Default) { 
                if (!File.Exists(config.userdbpath))
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.NoFile, Services.LogLevel.Fatal, "数据库文件缺失");
                }
                return;
            }
            string dbpath = GetDbPath();
            config.userdbpath = dbpath;
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"数据库路径-{dbpath}");
        }

        //获取配置文件并反序列化
        private static void GetConfig() {

            string configpath = GetConfigPath();
            string readJson = File.ReadAllText(configpath);

            try
            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new JsonStringEnumConverter());

                var _config = JsonSerializer.Deserialize<Config>(readJson, options);

                if (_config == null) {
                    Services.ErrorReporter.Report(Services.ExceptionType.JsonException,Services.LogLevel.Fatal, "配置文件为空");
                }
                config = _config;
            }
            catch (JsonException)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.JsonException,Services.LogLevel.Fatal, "Json序列化错误");
            }
            catch (Exception)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown,Services.LogLevel.Fatal, "Json序列化-未知错误");
            }
            Services.ErrorReporter.Report(Services.LogLevel.Info, "配置文件加载完成");


            //获取配置文件路径
            static string GetConfigPath() {
                string configpath = Path.Combine(AppContext.BaseDirectory, "Config");
                Directory.CreateDirectory(configpath);
                string jsonpath = Path.Combine(configpath, "config.json");
                if (!File.Exists(jsonpath)) {
                    InitJson(jsonpath);
                }
                return jsonpath;
            }

            //如果配置文件不存在则新建一个
            static void InitJson(string jsonpath) {
                var config = new Config
                {
                    certmode = CertMode.Default,
                    pfxpath = "",
                    pfxpassword = "",
                    url = "", //必须自行配置域名
                    port = 8197,
                    userdbmode = UserDbMode.Default,
                    userdbpath = "0",
                    admin_username = "admin",
                    admin_password = "_admin_Yukino**##31_"
                };
                var options = new JsonSerializerOptions{WriteIndented = true};
                options.Converters.Add(new JsonStringEnumConverter());
                string jsonString = JsonSerializer.Serialize(config,options);
                File.WriteAllText(jsonpath,jsonString);
            }
        }

        //获取litedb文件目录
        private static string GetDbPath()
        {
            string DbPath = Path.Combine(AppContext.BaseDirectory, "Config","userdb.db");
            return DbPath;
        }
    }
}
