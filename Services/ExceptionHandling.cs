using Serilog;

namespace N2N_USER_SERVER.Services
{
    //异常类别，用于日志前缀标注来源
    public enum ExceptionType
    {
        JsonException, //JSON序列、反序列时遇到的问题
        Unknown, //泛指一切未收录的异常
        NoFile, //没有文件
        DataBase, //数据库异常
        Http, //HTTP相关错误
        None //不标注类别（普通业务日志）
    }

    //日志级别
    public enum LogLevel
    {
        Info,
        Warn,
        Error,
        Fatal
    }

    //统一日志出口：所有日志都经由本类写入Serilog（控制台+文件），禁止直接使用Console
    public static class ErrorReporter
    {
        //带异常类别的日志
        static public void Report(ExceptionType ex, LogLevel lv, string described)
        {
            string msg = ex == ExceptionType.None ? described : $"[{ex}] {described}";
            Write(lv, msg);
        }

        //普通业务日志（不标注异常类别）
        static public void Report(LogLevel lv, string described)
        {
            Write(lv, described);
        }

        static private void Write(LogLevel lv, string msg)
        {
            switch (lv)
            {
                case LogLevel.Info:
                    Log.Information(msg);
                    break;
                case LogLevel.Warn:
                    Log.Warning(msg);
                    break;
                case LogLevel.Error:
                    Log.Error(msg);
                    break;
                case LogLevel.Fatal:
                    Log.Fatal(msg);
                    Log.CloseAndFlush();
                    Environment.Exit(1);
                    break;
            }
        }
    }
}
