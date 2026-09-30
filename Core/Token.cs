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

        //签发一个新的Token
        //注-现在是限制单用户使用,如将来改用多用户,请重新设计这个 以及 VerifyToken()里的限制
        public static string ObtainToken(TokenType type, int expireMinutes)
        {
            //签发前清空之前的Token，保证同一时间只有一个有效会话
            _Tokens.Clear();
            string token = $"token_{GenerateSecureToken()}";
            _Tokens.Add(token, new Token
            {
                Value = token,
                Type = type,
                ExpireTime = DateTime.UtcNow.AddMinutes(expireMinutes)
            });
            //不记录Token明文
            Services.ErrorReporter.Report(Services.LogLevel.Info, $"签发新Token-类型{type}-有效期{expireMinutes}分钟");
            return token;
        }

        //校验Token，失败原因通过error返回
        public static bool VerifyToken(string value, TokenType type, out TokenErrorType error)
        {
            if (!_Tokens.TryGetValue(value, out Token? token))
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, "Token校验失败-不存在或已失效");
                error = TokenErrorType.TokenError;
                return false;
            }
            if (token.Type != type)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, "Token校验失败-类型不匹配");
                error = TokenErrorType.TokenTypeError;
                return false;
            }
            if (token.ExpireTime < DateTime.UtcNow)
            {
                Services.ErrorReporter.Report(Services.LogLevel.Warn, "Token校验失败-已过期");
                _Tokens.Remove(value);
                error = TokenErrorType.TimeOut;
                return false;
            }
            error = TokenErrorType.True;
            return true;
        }

        //清空所有Token
        public static void ClearToken()
        {
            _Tokens.Clear();
        }

        //生成密码学安全的随机字符串（字符池已去除易混淆的 0/O/1/I/l）
        private static string GenerateSecureToken(int length = 16)
        {
            const string validChars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*";
            return RandomNumberGenerator.GetString(validChars, length);
        }

    }
}
