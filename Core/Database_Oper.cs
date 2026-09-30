using LiteDB;
using N2N_USER_SERVER.API;
using N2N_USER_SERVER.Bootstrap;

namespace N2N_USER_SERVER.Core
{
    //用户账号文档模型
    public class User
    {
        [BsonId]
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public bool enabled { get; set; }
    }

    //用户N2N边缘配置文档模型
    public class UserData {
        [BsonId]
        public int user_id { get; set; }
        public string supernode_ip { get; set; }
        public int supernode_port { get; set; }
        public string community_name { get; set; }
        public string device_name { get; set; }
        public string password { get; set; }
        public string community_key { get; set; }
        public int encrypt_algorithm { get; set; }
    }

    //提供LiteDB实例的封装，IDisposable-可释放
    public class DatabaseService : IDisposable
    {
        private readonly LiteDatabase _database;

        //构造函数：初始化并建立索引
        public DatabaseService()
        {
            _database = new LiteDatabase(Initialization.config.userdbpath);
            var users = _database.GetCollection<User>("users");
            users.EnsureIndex(x => x.username, true);
        }

        //users集合
        public ILiteCollection<User> Users
        {
            get
            {
                return _database.GetCollection<User>("users");
            }
        }

        //n2n_configs集合
        public ILiteCollection<UserData> N2nConfigs
        {
            get
            {
                return _database.GetCollection<UserData>("n2n_configs");
            }
        }

        public void Dispose()
        {
            _database.Dispose();
        }
    }

    public static class DatabaseOper {
        #region API操作

        //数据库实例
        public static DatabaseService lite = new DatabaseService();

        //登录处理：验证账号密码，成功时输出该用户的边缘配置
        public static bool LoginProcessing(string userid, string inputPassword, out UserData? userdata) {
            //外部调用入口
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"开始验证用户-{userid}");
            int? userId = UserVerification(userid, inputPassword, lite.Users);
            if (userId == null) {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, $"用户登录失败-{userid}");
                userdata = null;
                return false;
            }
            userdata = GetUserConfig((int)userId, lite.N2nConfigs);
            if (userdata == null) {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, $"用户登录失败-缺少边缘配置-{userid}");
                return false;
            }
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户登录成功-{userid}");
            return true;
        }

        //验证账户，成功返回用户id
        private static int? UserVerification(string userid, string inputPassword, ILiteCollection<User> users) {
            try
            {
                User? user = users.FindOne(x => x.username == userid);

                if (user == null) return null;
                if (!user.enabled) return null;

                if (BCrypt.Net.BCrypt.Verify(inputPassword, user.password_hash))
                {
                    return user.id;
                }
                else
                {
                    return null;
                }
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"数据库错误-{ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"数据库-未知错误-{ex.Message}");
                return null;
            }
        }

        //按用户id查询边缘配置
        private static UserData? GetUserConfig(int userid, ILiteCollection<UserData> configs)
        {
            return configs.FindById(userid);
        }

        #endregion

        #region 后端管理

        //写操作的结果
        public class WriteResults
        {
            //操作类型
            public string type { get; set; }
            //是否操作成功
            public bool success { get; set; }
            //返回代码
            public int recode { get; set; }
            //其他提示
            public string hint { get; set; }
        }

        //插入用户
        public static WriteResults AddUser(string username, string password, bool enabled) {

            if (InsertData(lite.Users))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户创建成功-{username}");
                return new WriteResults()
                {
                    type = "AddUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "AddUser",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool InsertData(ILiteCollection<User> users) {
                var user = new User()
                {
                    username = username,
                    password_hash = BCrypt.Net.BCrypt.HashPassword(password),
                    enabled = enabled
                };
                try
                {
                    users.Insert(user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户失败-未知错误-{ex.Message}");
                    return false;
                }

                return true;
            }
        }

        //插入用户边缘配置
        public static WriteResults AddUserData(UserData data) {

            if (!isUser(data.user_id, lite.Users))
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "No Prerequisites"
                };
            }

            if (InsertData(lite.N2nConfigs))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户边缘配置创建成功-user_id{data.user_id}");
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool InsertData(ILiteCollection<UserData> configs)
            {
                try
                {
                    configs.Insert(data);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"插入用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"插入用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid, ILiteCollection<User> users)
            {
                try
                {
                    var user = users.FindById(userid);
                    if (user == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

        }

        //修改用户数据
        public static WriteResults ReviseUser(User data)
        {
            //先判断用户是否存在
            if (!isUser(data.id))
            {
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            if (ReviseData(lite.Users))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户修改成功-id{data.id}");
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "ReviseUser",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool ReviseData(ILiteCollection<User> users)
            {
                try
                {
                    var user = users.FindById(data.id);
                    user.username = data.username;
                    //约定密码为"null"时表示不修改
                    if (data.password_hash != "null")
                    {
                        user.password_hash = data.password_hash;
                    }
                    user.enabled = data.enabled;

                    users.Update(user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"修改用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"修改用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var user = lite.Users.FindById(userid);
                    if (user == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //修改用户边缘配置
        public static WriteResults ReviseUserConfig(UserData data)
        {
            //先判断用户是否存在
            if (!isUser(data.user_id))
            {
                return new WriteResults()
                {
                    type = "ReviseUserConfig",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            if (ReviseConfig(lite.N2nConfigs))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户边缘配置修改成功-user_id{data.user_id}");
                return new WriteResults()
                {
                    type = "ReviseUserConfig",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "ReviseUserConfig",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool ReviseConfig(ILiteCollection<UserData> configs)
            {
                try
                {
                    var config = configs.FindById(data.user_id);
                    config.supernode_ip = data.supernode_ip;
                    config.supernode_port = data.supernode_port;
                    config.community_name = data.community_name;
                    config.device_name = data.device_name;
                    config.password = data.password;
                    config.community_key = data.community_key;
                    config.encrypt_algorithm = data.encrypt_algorithm;

                    configs.Update(config);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"修改用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"修改用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var config = lite.N2nConfigs.FindById(userid);
                    if (config == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //删除用户及其边缘配置
        public static WriteResults DeleteUser(int userid)
        {
            //先判断用户是否存在
            if (!isUser(userid))
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            bool userDeleted = Delete(lite.Users);
            bool configDeleted = DeleteConfig(lite.N2nConfigs);

            if (userDeleted && configDeleted)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户删除成功-id{userid}");
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else if (!userDeleted)
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "User deletion failed"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "DeleteUser",
                    success = false,
                    recode = 500,
                    hint = "UserConfig deletion failed"
                };
            }

            bool Delete(ILiteCollection<User> users)
            {
                try
                {
                    users.Delete(userid);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"删除用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"删除用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool DeleteConfig(ILiteCollection<UserData> configs)
            {
                try
                {
                    configs.Delete(userid);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"删除用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"删除用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var user = lite.Users.FindById(userid);
                    if (user == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //删除用户边缘配置
        public static WriteResults Delete_UserData(int id)
        {
            //先判断配置是否存在
            if (!isUser(id))
            {
                return new WriteResults()
                {
                    type = "DeleteUserData",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }
            if (Delete(lite.N2nConfigs))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Info, $"用户边缘配置删除成功-id{id}");
                return new WriteResults()
                {
                    type = "DeleteUserData",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "DeleteUserData",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }

            bool Delete(ILiteCollection<UserData> configs)
            {
                try
                {
                    configs.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"删除用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"删除用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var config = lite.N2nConfigs.FindById(userid);
                    if (config == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户配置失败-{ex.Message}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户配置失败-未知错误-{ex.Message}");
                    return false;
                }
                return true;
            }
        }

        //获取所有用户
        public static List<User> GetUserDataAll() {
            try
            {
                return lite.Users.FindAll().ToList();
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户列表失败-{ex.Message}");
                return new List<User>() { };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户列表失败-未知错误-{ex.Message}");
                return new List<User>() { };
            }
        }

        //获取所有用户边缘配置
        public static List<UserData> GetUserConfigAll()
        {
            try
            {
                return lite.N2nConfigs.FindAll().ToList();
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"查询用户配置列表失败-{ex.Message}");
                return new List<UserData>() { };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, $"查询用户配置列表失败-未知错误-{ex.Message}");
                return new List<UserData>() { };
            }
        }
        #endregion
    }
}
