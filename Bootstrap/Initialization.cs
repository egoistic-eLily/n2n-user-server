using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace N2N_USER_SERVER.Bootstrap
{
    public static class Initialization
    {
        public static Config? config { get; private set; }

        public static void Init() {
            GetConfig();//获取配置
            InitCert();
            initDB();
        }
        
        //配置证书的路径

        private static void InitCert() {
            if (config?.certmode != CertMode.Default) {
                if (!File.Exists(config?.pfxpath)) {
                    Services.ErrorReporter.Report(Services.ExceptionType.NoFile,Services.LogLevel.Fatal, "证书缺失");
                }
                return;
            }
            string? pfxpath;
            GetCertPath(out pfxpath);
            config.pfxpath = pfxpath;
            return;
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
            return;
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
            catch (JsonException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.JsonException,Services.LogLevel.Fatal, "Json序列化错误");
            }
            catch (Exception ex) {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown,Services.LogLevel.Fatal, "Json序列化-未知错误");
            }


            return;

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
                    pfxpath = "0",
                    pfxpassword = "0",
                    url = "saenai.asia",
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
                return;
            }
        }

        //获取证书目录
        private static void GetCertPath(out string? pfxpath) {
            string certpath = Path.Combine(AppContext.BaseDirectory, "Cert");
            Directory.CreateDirectory(certpath);//如果目录不存在则创建
            pfxpath = Path.Combine(certpath, "saenai.asia.pfx");
            if (!File.Exists(pfxpath)) { //判断是否存在文件
                pfxpath = null;
                //返回错误
                Services.ErrorReporter.Report(Services.ExceptionType.NoFile,Services.LogLevel.Fatal, "证书缺失");
                return;
            }
            return;
        }

        //获取litedb文件目录
        private static string GetDbPath()
        {
            string DbPath = Path.Combine(AppContext.BaseDirectory, "Config","userdb.db");
            return DbPath;
        }
    }
}
