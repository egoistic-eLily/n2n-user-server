// N2nKeygen - C# port of n2n's key derivation (tools/n2n-keygen.c, src/auth.c).
// Copyright (C) 2026 egoistic-eLily (TSUKASA SAKURAOCHI)
// Portions Copyright (C) ntop.org and contributors (n2n, GPLv3).
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See libs/N2nKeygen/LICENSE for details.

namespace N2nKeygen.Core;

// 类库唯一公开入口。
// 语义等价于官方 n2n-keygen <username> <password>（用户模式），
// 返回 43 字符公钥字符串——即写入 community.list 的
// "* <username> <本方法返回值>" 行中的公钥部分（不含用户名）。
public static class N2nUserKey
{
    public static string Generate(string username, string password)
    {
        byte[] privateKey = N2nAuth.GeneratePrivateKey(password);
        N2nAuth.BindPrivateKeyToUsername(privateKey, username);
        byte[] publicKey = N2nAuth.GeneratePublicKey(privateKey);
        string ascii = BaseCodec.BinToAscii(publicKey);

        Array.Clear(privateKey);
        Array.Clear(publicKey);

        return ascii;
    }
}
