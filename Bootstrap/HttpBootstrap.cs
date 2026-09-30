 using Microsoft.AspNetCore.Server.Kestrel.Core;
 using Serilog;

namespace N2N_USER_SERVER.Bootstrap
{
    public static class HttpBootstrap
    {
        public static void Setup() {
            var app = CreateApp();
            ConfigureRoute(app);
            try {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"路由注册完成-HTTPS监听端口{Initialization.config.port}");
                app.Run();
            }
            finally {
                //退出前刷新日志缓冲
                Log.CloseAndFlush();
            }
        }
        private static WebApplication CreateApp() {
            //获得一个WebApplicationBuilder实例
            var builder = WebApplication.CreateBuilder();
            ConfigureWebHost(builder);
            return builder.Build();

            //构建
            static void ConfigureWebHost(WebApplicationBuilder builder) {
                builder.WebHost.ConfigureKestrel(ConfigureKestrel);
            }
            //监听端口配置
            static void ConfigureKestrel(KestrelServerOptions options) {
                options.ListenAnyIP(Initialization.config.port,ConfigureListen);
            }
            //https配置
            static void ConfigureListen(ListenOptions options) {
                options.UseHttps(Initialization.config.pfxpath, Initialization.config.pfxpassword);
            }
        }

        private static void ConfigureRoute(WebApplication app) {
            app.UseDefaultFiles();
            app.UseStaticFiles();
            //app登录api
            app.MapPost("/login", (Func<HttpContext, Task<IResult>>)API.LoginApi.Login);
            //后端登录界面
            app.MapGet("/admin_login", (Func<HttpContext, Task<IResult>>)API.LoginApi.Admin_login_Html);
            //后端登录api
            app.MapPost("/api/login", (Func<HttpContext, Task<IResult>>)API.LoginApi.Admin_Login);
            //后端管理index界面
            app.MapGet("/admin",(Func<HttpContext,Task<IResult>>)API.LoginApi.Admin_Index);
            //用户管理界面
            app.MapGet("/users", (Func<HttpContext, Task<IResult>>)API.LoginApi.Admin_Users_Tools);
            //用户配置管界面
            app.MapGet("/configs", (Func<HttpContext, Task<IResult>>)API.LoginApi.Admin_Config_Tools);
            //获取用户数据api
            app.MapPost("/api/getuserdata", (Func<HttpContext, Task<IResult>>)API.LoginApi.Re_User_Data);
            //获取用户配置api
            app.MapPost("/api/getusersconfig", (Func<HttpContext, Task<IResult>>)API.LoginApi.Re_User_Config);
            //创建用户api
            app.MapPost("/api/create_user", (Func<HttpContext, Task<IResult>>)API.LoginApi.Create_User);
            //创建用户配置api
            app.MapPost("/api/create_config", (Func<HttpContext, Task<IResult>>)API.LoginApi.Create_UserConfig);
            //删除用户api
            app.MapPost("/api/delete_user", (Func<HttpContext, Task<IResult>>)API.LoginApi.Delete_User);
            //修改用户数据api
            app.MapPost("/api/revise_user", (Func<HttpContext, Task<IResult>>)API.LoginApi.Revise_User);
            //修改用户配置api
            app.MapPost("/api/revise_config", (Func<HttpContext, Task<IResult>>)API.LoginApi.Revise_UserConfig);
            //退出登录api
            app.MapPost("/api/logout", (Func<HttpContext, Task<IResult>>)API.LoginApi.LogOut);
        }
    }
}
