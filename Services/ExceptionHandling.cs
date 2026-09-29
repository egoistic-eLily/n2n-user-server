using Serilog;
namespace N2N_USER_SERVER.Services
{
    public enum ExceptionType
    {
        JsonException, //JSON序列、反序列时遇到的问题
        Unknown, //泛指一切未收录的异常
        NoFile, //没有文件
        DataBase, //数据库异常
        Http //HTTP相关错误
    }
    public enum LogLevel
    {
        Info,
        Warn,
        Error,
        Fatal
    }


    public static class ErrorReporter
    {
        static public void Report(ExceptionType ex,LogLevel lv,string described){
            string msg = $"[{ex}] {described}";
            switch (lv) {
                case LogLevel.Info:
                    Info(msg);
                    break;
                case LogLevel.Warn:
                    Warn(msg);
                    break;
                case LogLevel.Error:
                    Error(msg);
                    break;
                case LogLevel.Fatal:
                    Fatal(msg);
                    break;
            }


            static void Info(string msg) {
                Log.Information(msg);

            }
            static void Warn(string msg) { 
                Log.Warning(msg);

            }
            static void Error(string msg) {
                Log.Error(msg);

            }
            static void Fatal(string msg) { 
                Log.Fatal(msg);
                Environment.Exit(1);
            }
        }
    }
}
