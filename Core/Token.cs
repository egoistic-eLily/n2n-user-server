using System.Security.Cryptography;

namespace N2N_USER_SERVER.Core
{
    public enum TokenType
    {
        Admin,
        User
    }

    public class Token
    {
        public string Value { get; set; }

        public TokenType Type { get; set; }

        public DateTime ExpireTime { get; set; }
    }

    public enum TokenErrorType
    {
        TokenError,
        TimeOut,
        TokenTypeError,
        True
    }

    public static class Tokens
    {
        public static Dictionary<string, Token> _Tokens = new();
        //获取TOKEN
        public static string ObtainToken(TokenType type, int expireMinutes)
        {
            //每次获取就清空之前的token
            //注-现在是限制单用户使用,如将来改用多用户,请重新设计这个 以及 VerifyToken()里的限制

            _Tokens.Clear();
            string token = $"token_{GenerateSecurePassword()}";
            _Tokens.Add(token, new Token
            {
                Value = token,
                Type = type,
                ExpireTime = DateTime.UtcNow.AddMinutes(expireMinutes)
            });
            Console.WriteLine($"正在生成token-{token}"); 
            return token;
        }
        

        public static bool VerifyToken(string value, TokenType type, out TokenErrorType error)
        {
            Console.WriteLine($"正在校验token-{value}");
            if (!_Tokens.TryGetValue(value, out Token? token))
            {
                Console.WriteLine($"校验token失败-错误");
                error = TokenErrorType.TokenError;
                return false;
            }
            //if (_Tokens.Count > 1)
            //{
            //    Console.WriteLine($"有设备同时在线");
            //    return false;
            //}
            if (token.Type != type)
            {
                Console.WriteLine($"校验token失败-错误");
                error = TokenErrorType.TokenTypeError;
                return false;
            }
            if (token.ExpireTime < DateTime.UtcNow)
            {
                Console.WriteLine($"校验token失败-超时");
                _Tokens.Remove(value);
                error = TokenErrorType.TimeOut;
                return false;
            }
            Console.WriteLine($"校验token成功");
            error = TokenErrorType.True;
            return true;
        }

        public static void ClearToken() {
            _Tokens.Clear();
            return;
        }
        private static string GenerateSecurePassword(int length = 16)
        {
            // 定义你允许出现在密码里的字符池
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";

            // 从字符池中安全地随机抽取指定长度的字符组合
            return RandomNumberGenerator.GetString(validChars, length);
        }

    }
}
