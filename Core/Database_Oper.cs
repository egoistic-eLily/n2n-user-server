using LiteDB;
using N2N_USER_SERVER.API;
using N2N_USER_SERVER.Bootstrap;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace N2N_USER_SERVER.Core
{
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

    public class User
    {
        [BsonId]
        public int id { get; set; }
        public string username { get; set; }
        public string password_hash { get; set; }
        public bool enabled { get; set; }
    }


    //负责提供db实例的封装 IDisposable-可释放
    public class DatabaseService : IDisposable
    {
        //readonly-只读
        private readonly LiteDatabase _database;

        //构建函数:初始化自动执行 
        public DatabaseService()
        {
            _database = new LiteDatabase(Initialization.config.userdbpath);
            //建立索引
            var users = _database.GetCollection<User>("users");
            users.EnsureIndex(x => x.username,true);
        }

        //一个属性类型是 GetCollection() 的返回值
        public ILiteCollection<User> Users
        {
            get
            {
                return _database.GetCollection<User>("users");
            }
        }

        public ILiteCollection<UserData> N2nConfigs
        {
            get
            {
                return _database.GetCollection<UserData>("n2n_configs");
            }
        }

        //释放(的)方法
        public void Dispose() { 
            _database.Dispose();
        }
    }

    public static class DatabaseOper {
        #region API操作

        public static DatabaseService lite = new DatabaseService();//数据库实例
        public static bool LoginProcessing(string userid, string inputPassword, out UserData? userdata) {
            Console.WriteLine("准备连接数据库");

            //外部调用入口
            int? useridhave = UserVerification(userid, inputPassword, lite.Users);
            Console.WriteLine($"验证结果：{(useridhave != null)}");
            if (useridhave == null) {
                userdata = null;
                return false;
            }
            userdata = GetUserConfig((int)useridhave, lite.N2nConfigs);
            Console.WriteLine($"Verification-{userdata},isnull{userdata == null}");
            return userdata != null;
        }

        //验证账户
        private static int? UserVerification(string userid, string inputPassword, ILiteCollection<User> lite) {

            try
            {
                Console.WriteLine($"开始查询用户-{userid}");

                //找到其中 username == userid 的项 赋给 User
                User? _user = lite.FindOne(x => x.username == userid);

                if (_user == null) return null;
                if (!_user.enabled) return null;

                if (BCrypt.Net.BCrypt.Verify(inputPassword, _user.password_hash))
                {
                    return _user.id;
                }
                else
                {
                    return null;
                }
            }

            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                Console.WriteLine($"准备返回登录失败{ex}");
                return null;
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                Console.WriteLine($"准备返回登录失败{ex}");
                return null;
            }


        }


        private static UserData? GetUserConfig(int userid, ILiteCollection<UserData> litedb)
        {
            return litedb.FindById(userid);
        }

        #endregion

        #region 后端管理

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
        public static WriteResults AddUser(string username,string password,bool enabled) {

            if (InsertData(lite.Users))
            {
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

            bool InsertData(ILiteCollection<User> lite) {
                var user = new User()
                {
                    username = username,
                    password_hash = BCrypt.Net.BCrypt.HashPassword(password),
                    enabled = enabled
                };
                try
                {
                    lite.Insert(user);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户失败{ex}");
                    return false;
                }

                return true;
            }
        }

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

            bool InsertData(ILiteCollection<UserData> lite)
            {
                try
                {
                    lite.Insert(data);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }


            bool isUser(int userid, ILiteCollection<User> litedb)
            {
                try
                {
                    var User = litedb.FindById(userid);
                    if (User == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }

        }

        

        public static WriteResults ReviseUser(User data) //修改用户数据
        {
            //先判断用户是否存在
            if (!isUser(data.id))
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            if (ReviseData(lite.Users))
            {
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



            bool ReviseData(ILiteCollection<User> litedb)
            {
                try
                {
                    if(data.password_hash == "null")
                    {
                        var user = litedb.FindById(data.id);
                        user.username = data.username;
                        user.enabled = data.enabled;

                        litedb.Update(user);
                    }
                    else
                    {
                        var user = litedb.FindById(data.id);
                        user.username = data.username;
                        user.password_hash = data.password_hash;
                        user.enabled = data.enabled;

                        litedb.Update(user);
                    }
                    
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;

            }

            bool isUser(int userid)
            {
                try
                {
                    var User = lite.Users.FindById(userid);
                    if (User == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }
        }

        public static WriteResults ReviseUserConfig(UserData data)//修改用户配置文件
        {

            //先判断用户是否存在
            if (!isUser(data.user_id))
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            if (ReviseConfig(lite.N2nConfigs))
            {
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


            bool ReviseConfig(ILiteCollection<UserData> litedb)
            {
                try
                {
                    var config = litedb.FindById(data.user_id);
                    config.supernode_ip = data.supernode_ip;
                    config.supernode_port = data.supernode_port;
                    config.community_name = data.community_name;
                    config.device_name = data.device_name;
                    config.password = data.password;
                    config.community_key = data.community_key;
                    config.encrypt_algorithm = data.encrypt_algorithm;

                    litedb.Update(config);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var User = lite.N2nConfigs.FindById(userid);
                    if (User == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }
        }

        public static WriteResults DeleteUser(int userid)
        {
            //先判断用户是否存在
            if (!isUser(userid))
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "NOTUSER"
                };
            }
            bool User = Delete(lite.Users); bool Config = DeleteConfig(lite.N2nConfigs);

            if (User && Config)
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = true,
                    recode = 200,
                    hint = "Operation Successful"
                };
            }
            else if(!User)
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "User deletion failed"
                };
            }
            else
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "UserConfig deletion failed"
                };
            }


            bool Delete(ILiteCollection<User> litedb)
            {
                try
                {
                    litedb.Delete(userid);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;

            }

            bool DeleteConfig(ILiteCollection<UserData> litedb)
            {
                try
                {
                    litedb.Delete(userid);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }

            bool isUser(int userid)
            {
                try
                {
                    var User = lite.Users.FindById(userid);
                    if (User == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }
        }

        public static WriteResults Delete_UserData(int id)
        {
            //先判断用户是否存在
            if (!isUser(id))
            {
                return new WriteResults()
                {
                    type = "AddUserData",
                    success = false,
                    recode = 500,
                    hint = "Database Error"
                };
            }
            if (Delete(lite.N2nConfigs))
            {
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

            bool Delete(ILiteCollection<UserData> litedb)
            {
                try
                {
                    litedb.Delete(id);
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;

            }

            bool isUser(int userid)
            {
                try
                {
                    var User = lite.N2nConfigs.FindById(userid);
                    if (User == null) return false;
                }
                catch (LiteException ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                catch (Exception ex)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                    Console.WriteLine($"插入用户数据失败{ex}");
                    return false;
                }
                return true;
            }
        }

        //获取所有用户数据
        public static List<User> GetUserDataAll() {
            try
            {
                var litedb = lite.Users;
                var Users = litedb.FindAll().ToList();
                return Users;
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                Console.WriteLine($"插入用户数据失败{ex}");
                return new List<User>() { };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                Console.WriteLine($"插入用户数据失败{ex}");
                return new List<User>() { };
            }
        }
        //获取所有用户配置
        public static List<UserData> GetUserConfigAll()
        {
            try
            {
                var litedb = lite.N2nConfigs;
                var UsersConfig = litedb.FindAll().ToList();
                return UsersConfig;
            }
            catch (LiteException ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, "数据库错误");
                Console.WriteLine($"插入用户数据失败{ex}");
                return new List<UserData>() { };
            }
            catch (Exception ex)
            {
                Services.ErrorReporter.Report(Services.ExceptionType.Unknown, Services.LogLevel.Error, "数据库-未知错误");
                Console.WriteLine($"插入用户数据失败{ex}");
                return new List<UserData>() { };
            }

        }
        #endregion
    }
}
