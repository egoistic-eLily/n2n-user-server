using N2N_USER_SERVER.Bootstrap;

namespace N2N_USER_SERVER.Core.Supernode_Parser
{
    public class CommunityFile //表示整个Community文件
    {
        public List<Community> Communitys { get; set; }
    }
    public enum CommunityType //为了区分是否是正则
    {
        _regex, //正则
        _fixed //固定
    }
    public class Community //表示一个Community的定义
    {
        public string Name { get; set; }
        public CommunityType Type { get; set; }
        public string? Network { get; set; }
        public List<User>? Users { get; set; }
    }
    public class User
    {
        
        public int Id { get; set; }
        public string username {  get; set; }
        public string? key { get; set; }
    }

    public class OperationResults
    {
        //操作类型
        public string type { get; set; }
        //是否操作成功
        public bool success { get; set; }
        //返回代码
        public int recode { get; set; }
        //其他提示
        public string hint { get; set; }
        public int NumberError { get; set; }
    }


    public static class Parser
    {
        public static CommunityFile _communityfile = new CommunityFile();
        public static void Init()
        {
            List<string> lines = File.ReadAllLines(Bootstrap.Initialization.config.supernode_CommunityListPath).ToList();
            if (lines.Count == 0)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile为空");
                //我们已经在Initialization确保CommunityFile存在
                return;
            }
            _communityfile = ParseCommunityFile(lines);
            return;
        }

        private static CommunityFile ParseCommunityFile(List<string> lines)
        {
            CommunityFile communityFile = new CommunityFile();
            List<Community> communities = new List<Community>();

            int number = 0;
            foreach (var line in lines)
            {
                number ++;
                //空行
                if (string.IsNullOrWhiteSpace(line)) continue;
                //注释-这里沿用n2n官方 即只有行首为'#'才视为注释
                else if(line.StartsWith("#")) continue;
                //用户-也沿用官方 同上
                else if (line.StartsWith("*")) isUser(line);
                //其余都视作Community
                else
                {
                    isCommunity(line);
                }
            }
            communityFile.Communitys = communities;
            return communityFile;

            void isCommunity(string line)
            {
                List<string> parts = line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries).ToList();
                if (parts.Count > 2 || parts.Count == 0)
                {
                    Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile读取问题: line:{number} 非法内容");
                    return;
                }
                if (Contains_regexCharacter(parts[0])) isCommunity_regex(parts);
                else isCommunity_fixed(parts);
            }

            void isCommunity_fixed(List<string> parts)
            {
                if (parts.Count == 1)
                {
                    communities.Add(new Community() { Name = parts[0], Type = CommunityType._fixed, Users = new List<User>() });
                    return;
                }
                communities.Add(new Community() { Name = parts[0], Network = parts[1] ,Type = CommunityType._fixed, Users = new List<User>() });
            }
            void isCommunity_regex(List<string> parts)
            {
                if (parts.Count != 1)
                {
                    Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile读取问题: line:{number} 非法内容 hint: 是_regex的同时参数错误");
                    return;
                }
                communities.Add(new Community() { Name = parts[0], Type = CommunityType._regex, Users = new List<User>()});
            }
            void isUser(string line)
            {
                List<string> parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (parts.Count != 3)
                {
                    Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile读取问题: line:{number} 非法内容");
                    return;
                }
                if (communities.Count == 0) //我们已经提前在创建Communitys new了,这里理论上不应为null
                {
                    Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile读取问题: line:{number} 非法内容:尝试在非社区片段中添加用户");
                    return;
                }
                if (communities[communities.Count-1].Type== CommunityType._regex)
                {
                    Services.ErrorReporter.Report(Services.LogLevel.Warn, $"CommunityFile读取问题: line:{number} 非法内容:尝试在_regex社区中添加用户");
                    return;
                }
                communities[communities.Count - 1].Users.Add(new User() { username = parts[1],key = parts[2] });
                return;
            }

            
        }

        //写入社区文件
        public static OperationResults SerializationCommunityFile(CommunityFile communityfile)
        {
            string community_file = "";
            string rehint = "";
            int numbererror = 0;
            foreach (var community in communityfile.Communitys)
            {
                if (!iscorrect(community)) continue;
                if(community.Type == CommunityType._fixed)
                {
                    if (string.IsNullOrWhiteSpace(community.Network)){ 
                        community_file = $"{community_file}\n{community.Name}";
                        WriteUser(community.Users);
                        continue;
                    }
                    community_file = $"{community_file}\n{community.Name} {community.Network}";
                    WriteUser(community.Users);
                    continue;
                }
                else
                {
                    community_file = $"{community_file}\n{community.Name}";
                    continue;
                }
            }

            if(numbererror != 0)
            {
                return new OperationResults()
                {
                    success = false,
                    type = "WriteCommunityFile",
                    recode = 400,
                    hint = rehint,
                    NumberError = numbererror
                };
            }

            //真正写入文件
            File.WriteAllText(Initialization.config.supernode_CommunityListPath, community_file);
            return new OperationResults()
            {
                success = true,
                type = "WriteCommunityFile",
                recode = 200,
                hint = rehint,
                NumberError = numbererror
            };

            void WriteUser(List<User>? users)
            {
                if(users == null || users?.Count == 0) return;
                foreach(var user in users)
                {
                    if (!iscorrectUser(user))
                    {
                        continue;
                    }
                    string userkey = user.key;
                    community_file = $"{community_file}\n* {user.username} {userkey}";
                }
                return;
            }

            bool iscorrectUser(User user)
            {
                if (user == null)
                {
                    rehint = $"{rehint}\n User is Null";
                    numbererror++;
                    return false;
                }
                if (string.IsNullOrWhiteSpace(user.username))
                {
                    rehint = $"{rehint}\n community.Name is Null";
                    numbererror++;
                    return false;
                }
                if (user.username.Any(char.IsWhiteSpace))
                {
                    rehint = $"{rehint} \n User.Name is 有空格";
                    numbererror++;
                    return false;
                }
                else if (user.username.StartsWith("#"))
                {
                    rehint = $"{rehint} \n user.Name 不能为注释 - {user.username}";
                    numbererror++;
                    return false;
                }
                else if (user.username.StartsWith("*")){
                    rehint = $"{rehint} \n user.Name 不能加* - {user.username} - 由服务端负责";
                    numbererror++;
                    return false;
                }
                return true;
            }


            bool iscorrect(Community community)
            {
                if (string.IsNullOrWhiteSpace(community.Name))
                {
                    rehint = $"{rehint}\n community.Name is Null";
                    numbererror++;
                    return false;
                }
                //防空
                if (community.Name.Any(char.IsWhiteSpace))
                {
                    rehint = $"{rehint} \n community.Name 有空格";
                    numbererror++;
                    return false;
                }
                //防注释
                else if (community.Name.StartsWith("#"))
                {
                    rehint = $"{rehint} \n community.Name 不能为注释 - {community.Name}";
                    numbererror++;
                    return false;
                }
                else if (community.Name.StartsWith("*")) {
                    rehint = $"{rehint} \n community.Name 不能加* - {community.Name}";
                    numbererror++;
                    return false; }
                if(community.Type == CommunityType._regex)
                {
                    if(community.Users != null && community.Users?.Count != 0){
                        rehint = $"{rehint} \n 不能在_regex类型中指定User - {community.Name}";
                        numbererror++;
                        return false; } 
                    if (!string.IsNullOrWhiteSpace(community.Network))
                    {
                        rehint = $"{rehint} \n 不能在_regex类型中指定Network - {community.Name}";
                        numbererror++;
                        return false;
                    }
                    if (!Contains_regexCharacter(community.Name))
                    {
                        rehint = $"{rehint} \n 类型错误 - {community.Name}";
                        numbererror++;
                        return false;
                    }
                    return true;
                }
                else if (community.Type == CommunityType._fixed)
                {
                    if (Contains_regexCharacter(community.Name))
                    {
                        rehint = $"{rehint} \n 类型错误 - {community.Name}";
                        numbererror++;
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(community.Network))
                    {
                        var networkParts = community.Network.Split('/');
                        if (networkParts.Count() != 2)
                        {
                            rehint = $"{rehint} \n Network格式错误(严重) - {community.Name}";
                            numbererror++;
                            return false;
                        }
                        if (networkParts[0].Split('.').Count() != 4)
                        {
                            rehint = $"{rehint} \n Network格式错误(严重) - {community.Name}";
                            numbererror++;
                            return false;
                        }
                    }//无Network无需验证
                    return true;
                }
                rehint = $"{rehint} \n 未知类型(严重) - {community.Name}";
                return false;
            }
        }
        //帮助
        private static bool Contains_regexCharacter(string text)
        {
            var _regexCharacter = text.IndexOfAny(new[] { '.', '*', '+', '?', '[', ']', '\\' });
            return _regexCharacter >= 0;
        }
    }
}
