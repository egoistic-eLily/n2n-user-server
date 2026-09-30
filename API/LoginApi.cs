using LiteDB;
using N2N_USER_SERVER.Bootstrap;
using N2N_USER_SERVER.Core;
using System.Text.Json;

namespace N2N_USER_SERVER.API
{
    //定义结构体
    public class LoginRequest
    {
        public string userid { get; set; }

        public string password { get; set; }
    }
    public class Request //浏览器发送的请求结构体
    {
        public string RequestType { get; set; }
        public string token { get; set; }
    }
    public class Re_UserData 
    {
        public bool success { get; set; }
        public int recode { get; set; }
        public string hint { get; set; }
        public List<User> usersdata { get; set; }
    }
    public class Re_UserConfig
    {
        public bool success { get; set; }
        public int recode {  set; get; }
        public string hint {  set; get; }
        public List<UserData> usersconfig { get; set; }
    }

    public class CreateUserRequest //创建用户请求时发送的结构体
    {
        public string token { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public bool enabled { get; set; }
    }
    public class CreateUserConfigRequest //创建用户配置请求时发送的结构体
    {
        public string token { set; get; }
        public int user_id { get; set; }
        public string supernode_ip { get; set; }
        public int supernode_port { get; set; }
        public string community_name { get; set; }
        public string device_name { get; set; }
        public string password { get; set; }
        public string community_key { get; set; }
        public int encrypt_algorithm { get; set; }
    }
    //修改用户请求时发送的结构体
    public class UserRequest
    {
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public bool enabled { get; set; }
        public string token {  set; get; }
    }
    public static class LoginApi {
        public static async Task<IResult> Login(HttpContext context) {
            LoginRequest? request =
            await context.Request.ReadFromJsonAsync<LoginRequest>();
            if (request == null) {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }
            string userid = request.userid;
            string password = request.password;
            Services.ErrorReporter.Report(Services.LogLevel.Info, "接口调用-/login");

            Core.UserData userdata = new();

            if (!Core.DatabaseOper.LoginProcessing(userid, password, out userdata)) {
                return Results.Json(new
                {
                    success = false,
                }, statusCode: 401);
            }
            return Results.Json(
            new
            {
                success = true,
                data = userdata
            },
            statusCode: 200
);
        }
        #region 后端
        class ReturnStructure
        {
            public bool success {  get; set; }
            public string token { get; set; }
        }
        public static async Task<IResult> Admin_login_Html(HttpContext context)
        {
            Services.ErrorReporter.Report(Services.LogLevel.Info, "页面请求-/admin_login");
            string html = await File.ReadAllTextAsync(GetFilePath("login.html"));
            return Results.Content(html, "text/html");
        }
        public static async Task<IResult> Admin_Login(HttpContext context)
        {
            LoginRequest? request = await context.Request.ReadFromJsonAsync<LoginRequest>();
            if (request == null)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }

            string userid = request.userid;
            string password = request.password;
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/login-管理员{userid}");

            if (userid != Initialization.config.admin_username || password != Initialization.config.admin_password)
            {
                return Results.Json(new ReturnStructure()
                {
                    success = false,
                    token = "Fayou"
                },
                statusCode: 401);
            }
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"管理员登录成功-{userid}");
            string token = Core.Tokens.ObtainToken(Core.TokenType.Admin,10);
            return Results.Json(new ReturnStructure()
            {
                success = true,
                token = token
            }, statusCode: 200);

        }
        public static async Task<IResult> Admin_Index(HttpContext context) {
            //获取查询字符串
            string? token = context.Request.Query["token"].FirstOrDefault();
            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.BadRequest(403);
            }
            if (!Tokens.VerifyToken(token, TokenType.Admin,out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    string timeout = await File.ReadAllTextAsync(GetFilePath("timeout.html"));
                    return Results.Content(timeout, "text/html");
                }
                return Results.StatusCode(403);
            }

            Services.ErrorReporter.Report(Services.LogLevel.Info, "页面请求-/admin");
            string html = await File.ReadAllTextAsync(GetFilePath("admin.html"));
            return Results.Content(html, "text/html");
        }
        public static async Task<IResult> Admin_Users_Tools(HttpContext context)
        {
            //获取查询字符串
            string? token = context.Request.Query["token"].FirstOrDefault();
            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.BadRequest(403);
            }
            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    string timeout = await File.ReadAllTextAsync(GetFilePath("timeout.html"));
                    return Results.Content(timeout, "text/html");
                }
                return Results.StatusCode(403);
            }

            Services.ErrorReporter.Report(Services.LogLevel.Info, "页面请求-/users");
            string html = await File.ReadAllTextAsync(GetFilePath("users.html"));
            return Results.Content(html, "text/html");
        }

        public static async Task<IResult> Admin_Config_Tools(HttpContext context)
        {
            //获取查询字符串
            string? token = context.Request.Query["token"].FirstOrDefault();
            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.BadRequest(403);
            }
            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    string timeout = await File.ReadAllTextAsync(GetFilePath("timeout.html"));
                    return Results.Content(timeout, "text/html");
                }
                return Results.StatusCode(403);
            }

            Services.ErrorReporter.Report(Services.LogLevel.Info, "页面请求-/configs");
            string html = await File.ReadAllTextAsync(GetFilePath("configs.html"));
            return Results.Content(html, "text/html");
        }

        public static async Task<IResult> Re_User_Data(HttpContext context)
        {
            //先校验token

            Request? request = await context.Request.ReadFromJsonAsync<Request>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/getuserdata");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //查询
            var UsersList = Core.DatabaseOper.GetUserDataAll();

            var redata = new Re_UserData()
            {
                success = true,
                recode = 200,
                hint = "OK",
                usersdata = UsersList   
            };

            return Results.Json(redata,statusCode:200);
        }

        public static async Task<IResult> Re_User_Config(HttpContext context)
        {
            //先校验token

            Request? request = await context.Request.ReadFromJsonAsync<Request>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/getusersconfig");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //查询
            var UsersConfigList = Core.DatabaseOper.GetUserConfigAll();

            var redata = new Re_UserConfig()
            {
                success = true,
                recode = 200,
                hint = "OK",
                usersconfig = UsersConfigList
            };

            return Results.Json(redata, statusCode: 200);
        }

        public static async Task<IResult> Create_User(HttpContext context)
        {
            //先校验token

            CreateUserRequest? request = await context.Request.ReadFromJsonAsync<CreateUserRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/create_user-新用户{request?.username}");
            
            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //创建

            var DBRE = Core.DatabaseOper.AddUser(request.username,request.password,request.enabled);

            int id = Core.DatabaseOper.lite.Users.FindOne(x => x.username == request.username).id;

            _ = Core.DatabaseOper.AddUserData(new UserData { 
                user_id = id,
                supernode_ip  = "",
                supernode_port = 0,
                community_name = "",
                device_name = "",
                password = "",
                community_key = "",
                encrypt_algorithm = 4
            });

            if (!DBRE.success)
            {
                return Results.Json(new
                {
                    success = false,
                    recode = DBRE.recode,
                    hint = DBRE.hint
                },
                statusCode: 500);
            }

            return Results.Json(new
            {
                success = true,
                recode = DBRE.recode,
                hint = DBRE.hint
            },
            statusCode: 200);
        }

        public static async Task<IResult> Create_UserConfig(HttpContext context)
        {
            //先校验token

            CreateUserConfigRequest? request = await context.Request.ReadFromJsonAsync<CreateUserConfigRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/create_config");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //创建

            var DBRE = Core.DatabaseOper.AddUserData(new UserData
            {
                user_id = request.user_id,
                supernode_ip = request.supernode_ip,
                supernode_port = request.supernode_port,
                community_name = request.community_name,
                device_name =request.device_name,
                password = request.password,
                community_key = request.community_key,
                encrypt_algorithm = request.encrypt_algorithm
            });

            if (!DBRE.success)
            {
                return Results.Json(new
                {
                    success = false,
                    recode = DBRE.recode,
                    hint = DBRE.hint
                },
                statusCode: 500);
            }

            return Results.Json(new
            {
                success = true,
                recode = DBRE.recode,
                hint = DBRE.hint
            },
            statusCode: 200);
        }

        public static async Task<IResult> Revise_User(HttpContext context)
        {
            //先校验token

            UserRequest? request = await context.Request.ReadFromJsonAsync<UserRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/revise_user-id{request?.id}");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //修改

            string password = "";
            //"null"表示不需要更改密码
            if (request.password_hash == "null")
            {
                //这里将约定的"null"传入函数，函数自会处理
                password = "null";
            }
            else
            {
                //发送的是密码明文，这里转为HASH
                password = BCrypt.Net.BCrypt.HashPassword(request.password_hash);
            }

            

            var DBRE = Core.DatabaseOper.ReviseUser(new User { 
                id = request.id,
                username = request.username,
                password_hash = password,
                enabled = request.enabled
            });

            if (!DBRE.success)
            {
                return Results.Json(new
                {
                    success = false,
                    recode = DBRE.recode,
                    hint = DBRE.hint
                },
                statusCode: 500);
            }

            return Results.Json(new
            {
                success = true,
                recode = DBRE.recode,
                hint = DBRE.hint
            },
            statusCode: 200);
        }

        public static async Task<IResult> Revise_UserConfig(HttpContext context)
        {
            //先校验token

            CreateUserConfigRequest? request = await context.Request.ReadFromJsonAsync<CreateUserConfigRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/revise_config");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //修改
            var DBRE = Core.DatabaseOper.ReviseUserConfig(new UserData() { 
                user_id = request.user_id,
                supernode_ip = request.supernode_ip,
                supernode_port = request.supernode_port,
                community_name = request.community_name,
                device_name = request.device_name,
                password = request.password,
                community_key = request.community_key,
                encrypt_algorithm = request.encrypt_algorithm,
            });

            if (!DBRE.success)
            {
                return Results.Json(new
                {
                    success = false,
                    recode = DBRE.recode,
                    hint = DBRE.hint
                },
                statusCode: 500);
            }

            return Results.Json(new
            {
                success = true,
                recode = DBRE.recode,
                hint = DBRE.hint
            },
            statusCode: 200);

        }

        public static async Task<IResult> Delete_User(HttpContext context)
        {
            //先校验token

            UserRequest? request = await context.Request.ReadFromJsonAsync<UserRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/delete_user-id{request?.id}");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //删除

            var DBRE = Core.DatabaseOper.DeleteUser(request.id);
            _ = Core.DatabaseOper.Delete_UserData(request.id);

            if (!DBRE.success)
            {
                return Results.Json(new
                {
                    success = false,
                    recode = DBRE.recode,
                    hint = DBRE.hint
                },
                statusCode: 500);
            }

            return Results.Json(new
            {
                success = true,
                recode = DBRE.recode,
                hint = DBRE.hint
            },
            statusCode: 200);
        }

        public static async Task<IResult> LogOut(HttpContext context)
        {
            //先校验token

            Request? request = await context.Request.ReadFromJsonAsync<Request>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/logout");

            var RE = TokenVerify(request);

            if (RE != null) return RE;

            //清空Token

            Tokens.ClearToken();

            return Results.Json(new
            {
                success = true,
                recode = 200,
                hint = "OK"
            }, statusCode: 200);
        }

        #region 帮助函数

        private static IResult? TokenVerify(Request? request)
        {
            if (request == null)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }
            //先校验token

            string? token = request.token;

            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    return Results.Json(new Re_UserData()
                    {
                        success = false,
                        recode = 2000,
                        hint = "TimeOut"
                    },
                    statusCode: 401);
                }

                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            return null;
        }

        private static IResult? TokenVerify(CreateUserRequest? request)
        {
            if (request == null)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }
            //先校验token

            string? token = request.token;

            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    return Results.Json(new Re_UserData()
                    {
                        success = false,
                        recode = 2000,
                        hint = "TimeOut"
                    },
                    statusCode: 401);
                }

                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            return null;
        }

        private static IResult? TokenVerify(UserRequest? request)
        {
            if (request == null)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }
            //先校验token

            string? token = request.token;

            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    return Results.Json(new Re_UserData()
                    {
                        success = false,
                        recode = 2000,
                        hint = "TimeOut"
                    },
                    statusCode: 401);
                }

                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            return null;
        }

        private static IResult? TokenVerify(CreateUserConfigRequest? request)
        {
            if (request == null)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Http, Services.LogLevel.Warn, "HTTP-未知错误");
                return Results.BadRequest("请求数据错误");
            }
            //先校验token

            string? token = request.token;

            TokenErrorType error;

            if (string.IsNullOrWhiteSpace(token))
            {
                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            if (!Tokens.VerifyToken(token, TokenType.Admin, out error))
            {
                if (error == TokenErrorType.TimeOut)
                {
                    return Results.Json(new Re_UserData()
                    {
                        success = false,
                        recode = 2000,
                        hint = "TimeOut"
                    },
                    statusCode: 401);
                }

                return Results.Json(new Re_UserData()
                {
                    success = false,
                    recode = 401,
                    hint = "token error"
                },
                statusCode: 401);
            }

            return null;
        }

        //获取HTML文件PATH
        private static string GetFilePath(string filename)
        {
            string FilePath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            string infile = Path.Combine(FilePath, filename);
            if (!File.Exists(infile)) return null;
            return infile;
        }
        #endregion

    }
    #endregion

}
