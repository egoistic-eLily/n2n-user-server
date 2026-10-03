using N2N_USER_SERVER.Core;

namespace N2N_USER_SERVER.API.Supernode
{
    public class ModifyCommunityFileRequest
    {
        public string RequestType { get; set; }
        public string? token { get; set; }

    }

    public class ADD_CommunityRequest
    {
        public string name { get; set; }
        public Core.Supernode_Parser.CommunityType type { get; set; }
        public string? network { get; set; }
        public string? communityKey { get; set; }
        public string requestType { get; set; }
        public string? token { get; set; }

    }
    public class Add_UserDataRequest
    {
        public int community_id { get; set; }
        public int user_id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string token { get; set; }
    }

    public class DeleteRequest
    {
        public int id { get; set; }
        public string name { get;set;  }
        public string RequestType { get; set; }
        public string? token { get; set; }
    }
    public static class SupernodeAPI
    {
        //获取所有社区数据
        public static async Task<IResult> Re_Community_Data(HttpContext context)
        {

            Request? request = await context.Request.ReadFromJsonAsync<Request>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/get_communitydata");

            var RE = LoginApi.TokenVerify(request);

            if (RE != null) return RE;

            //返回

            var CommunityList = new List<Core.CommunityConfig>();

            var RECO = Core.DatabaseOper.GetCommunityAll(out CommunityList);

            if (!RECO.success)
            {   
                return Results.Json(new
                {
                    data = CommunityList,
                    sucess = RECO.success,
                    type = RECO.type,
                    recode = RECO.recode,
                    hint = RECO.hint,
                },statusCode:500);
            }
            return Results.Json(new
            {
                sucess = RECO.success,
                type = RECO.type,
                recode = RECO.recode,
                hint = RECO.hint,
            }, statusCode: 200);
        }

        //获取所有用户配置 
        public static async Task<IResult> Re_User_Config(HttpContext context)
        {
            Request? request = await context.Request.ReadFromJsonAsync<Request>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/getusersconfig");

            var RE = LoginApi.TokenVerify(request);

            if (RE != null) return RE;

            //返回

            var UserConfigList = new List<Core.UserConfig>();

            var RECO = Core.DatabaseOper.Get_UserConfigAll(out UserConfigList);

            if (!RECO.success)
            {
                return Results.Json(new
                {
                    data = UserConfigList,
                    sucess = RECO.success,
                    type = RECO.type,
                    recode = RECO.recode,
                    hint = RECO.hint,
                }, statusCode: 500);
            }
            return Results.Json(new
            {
                sucess = RECO.success,
                type = RECO.type,
                recode = RECO.recode,
                hint = RECO.hint,
            }, statusCode: 200);
        }

        public static async Task<IResult> ADD_Community(HttpContext context)
        {
            ADD_CommunityRequest? request = await context.Request.ReadFromJsonAsync<ADD_CommunityRequest>();

            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/create_community");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "ADD_Community",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            }); 
            
            var RE = LoginApi.TokenVerify(request.token);

            if (RE != null) return RE;

            //先写进数据库

            var AddCommunity_RE = Core.DatabaseOper.AddCommunity(new Core.CommunityConfig() { 
            name = request.name,
            type = request.type,
            network = request.network,
            communitykey = request.communityKey,
            });

            //先看数据库有没有写入成功
            if (!AddCommunity_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = AddCommunity_RE.type,
                    recode = AddCommunity_RE.recode,
                    hint = AddCommunity_RE.hint,
                }, statusCode: 500);
            }

            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                //回滚操作:删除社区
                var Delete_Community_RE = Core.DatabaseOper.Delete_Community(request.name);
                if (!Delete_Community_RE.success)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{Delete_Community_RE.hint}");
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "ADD_Community",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{Delete_Community_RE.hint}",
                    }, statusCode: 500);
                }
                
                return Results.Json(new
                {
                    sucess = false,
                    type = "ADD_Community",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件(已回滚) 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "ADD_Community",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);

        }

        public static async Task<IResult> AddUserConfig(HttpContext context)
        {
            Add_UserDataRequest? request = await context.Request.ReadFromJsonAsync<Add_UserDataRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/create_config");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "AddUserConfig",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            });

            var RE = LoginApi.TokenVerify (request?.token);

            if (RE != null) return RE;

            var AddUserConfig_RE = Core.DatabaseOper.AddUserConfig(new Core.UserConfig() { 
                user_id = request?.user_id == null ? -1 : request.user_id,//让其再内部报错
                community_id = request?.community_id == null ? -1 : request.community_id,
                device_name = request.username,
                password = request.password,
            });

            if (!AddUserConfig_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = AddUserConfig_RE.type,
                    recode = AddUserConfig_RE.recode,
                    hint = AddUserConfig_RE.hint,
                }, statusCode: 500);
            }
            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                //回滚操作:删除用户
                var DeleteUserData_RE = Core.DatabaseOper.DeleteUserData(request.username);
                if (!DeleteUserData_RE.success)
                {
                    Services.ErrorReporter.Report(Services.ExceptionType.DataBase, Services.LogLevel.Error, $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{DeleteUserData_RE.hint}");
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "AddUserConfig",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{DeleteUserData_RE.hint}",
                    }, statusCode: 500);
                }

                return Results.Json(new
                {
                    sucess = false,
                    type = "AddUserConfig",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件(已回滚) 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "AddUserConfig",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);
        }

        public static async Task<IResult> ReviseCommunity(HttpContext context)
        {
            ADD_CommunityRequest? request = await context.Request.ReadFromJsonAsync<ADD_CommunityRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/revise_community");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "ReviseCommunity",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            });

            var RE = LoginApi.TokenVerify(request?.token);

            if (RE != null) return RE;

            //写入前先备份

            var BackupCommunityList = new List<Core.CommunityConfig>();

            _= Core.DatabaseOper.GetCommunityAll(out BackupCommunityList);

            var BackupCommunity = BackupCommunityList.Find(x => x.name == request?.name);


            var ReviseCommunity_RE = Core.DatabaseOper.ReviseCommunity(new Core.CommunityConfig()
            {
                network = request?.network,
                name = request?.name == null ? "null" : request.name,
                type = request.type,
                communitykey = request.communityKey
            });

            if (!ReviseCommunity_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = ReviseCommunity_RE.type,
                    recode = ReviseCommunity_RE.recode,
                    hint = ReviseCommunity_RE.hint,
                }, statusCode: 500);
            }


            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                if(BackupCommunity == null)
                {
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "ReviseCommunity",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} - (备份数据丢失)",
                    }, statusCode: 500);
                }
                //回滚操作
                var RECO = Core.DatabaseOper.ReviseCommunity(BackupCommunity);
                if (!RECO.success)
                {
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "ReviseCommunity",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{RECO.hint}",
                    }, statusCode: 500);
                }
                return Results.Json(new
                {
                    sucess = false,
                    type = "ReviseCommunity",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件(已回滚) 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "ReviseCommunity",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);
        }

        public static async Task<IResult> Revise_UserConfig(HttpContext context) {
            Add_UserDataRequest ? request = await context.Request.ReadFromJsonAsync<Add_UserDataRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/revise_config");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "Revise_UserConfig",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            });

            var RE = LoginApi.TokenVerify(request?.token);

            if (RE != null) return RE;

            var BackupUserConfigList = new List<Core.UserConfig>();

            _ = Core.DatabaseOper.Get_UserConfigAll(out BackupUserConfigList);

            var BackupUserConfig = BackupUserConfigList.Find(x => x.device_name == request?.username);

            var Revise_UserConfig_RE = Core.DatabaseOper.Revise_UserConfig(new Core.UserConfig() { 
                 community_id = request?.community_id == null ? -1 : request.community_id,
                 device_name = request?.username,
                 password = request?.password,
            });

            if (!Revise_UserConfig_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = Revise_UserConfig_RE.type,
                    recode = Revise_UserConfig_RE.recode,
                    hint = Revise_UserConfig_RE .hint,
                }, statusCode: 500);
            }

            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                if (BackupUserConfig == null)
                {
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "Revise_UserConfig",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} - (备份数据丢失)",
                    }, statusCode: 500);
                }
                //回滚操作
                var RECO = Core.DatabaseOper.Revise_UserConfig(BackupUserConfig);
                if (!RECO.success)
                {
                    return Results.Json(new
                    {
                        sucess = false,
                        type = "Revise_UserConfig",
                        recode = 500,
                        hint = $"严重错误: 数据未能同步至LIST文件 且未能执行回滚 建议您立刻备份数据，并做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint} \n 回滚的提示{RECO.hint}",
                    }, statusCode: 500);
                }
                return Results.Json(new
                {
                    sucess = false,
                    type = "Revise_UserConfig",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件(已回滚) 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "Revise_UserConfig",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);
        }

        public static async Task<IResult> Delete_Community(HttpContext context)
        {
            DeleteRequest? request = await context.Request.ReadFromJsonAsync<DeleteRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/delete_community");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "Delete_Community",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            });

            var RE = LoginApi.TokenVerify(request?.token);

            if (RE != null) return RE;

            var Delete_Community_RE = Core.DatabaseOper.Delete_Community(request?.id == null ? -1 : request.id);

            if (!Delete_Community_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = Delete_Community_RE.type,
                    recode = Delete_Community_RE.recode,
                    hint = Delete_Community_RE.hint,
                }, statusCode: 401);
            }

            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = "Delete_Community",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件,不过您不需要担心，这通常是无害的，待下一次同步时应该会恢复正常，如果下一次同步仍然错误，您才需注意并备份和做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "Delete_Community",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);
        }

        public static async Task<IResult> DeleteUserData(HttpContext context)
        {

            DeleteRequest? request = await context.Request.ReadFromJsonAsync<DeleteRequest>();
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"接口调用-/api/revise_user");

            //只传token时，TokenVerify不会验证请求是否为空

            if (request == null) return Results.Json(new
            {
                sucess = false,
                type = "DeleteUserData",
                recode = 401,
                hint = "请求为空",
                NumberError = 1
            });

            var RE = LoginApi.TokenVerify(request?.token);

            if (RE != null) return RE;

            var DeleteUserData_RE = Core.DatabaseOper.DeleteUserData(request?.id == null ? -1 : request.id);

            if (!DeleteUserData_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = DeleteUserData_RE.type,
                    recode = DeleteUserData_RE.recode,
                    hint = DeleteUserData_RE.hint,
                }, statusCode: 401);
            }

            //然后再写入文件(全量同步:我们以数据库为准)

            var Synchronous_RE = Core.DatabaseOper.SynchronousListFile();
            if (!Synchronous_RE.success)
            {
                return Results.Json(new
                {
                    sucess = false,
                    type = "DeleteUserData",
                    recode = 500,
                    hint = $"数据未能同步至LIST文件,不过您不需要担心，这通常是无害的，待下一次同步时应该会恢复正常，如果下一次同步仍然错误，您才需注意并备份和做好重置的准备 \n 写入操作的提示-{Synchronous_RE.hint}",
                }, statusCode: 500);
            }

            return Results.Json(new
            {
                sucess = true,
                type = "DeleteUserData",
                recode = 200,
                hint = "操作成功完成",
            }, statusCode: 200);
        }
    }
}
