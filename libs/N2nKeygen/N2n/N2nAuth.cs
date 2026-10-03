// N2nKeygen - C# port of n2n's key derivation (tools/n2n-keygen.c, src/auth.c).
// Copyright (C) 2026 egoistic-eLily (TSUKASA SAKURAOCHI)
// Portions Copyright (C) ntop.org and contributors (n2n, GPLv3).
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See libs/N2nKeygen/LICENSE for details.

using System.Text;

namespace N2nKeygen.Core;

// n2n 用户/联邦密钥派生算法的忠实移植。
// 复刻自 ntop/n2n 3.1.1 src/auth.c：
//   generate_private_key / bind_private_key_to_username / generate_public_key
// 仅供类库唯一公开入口 N2nUserKey.Generate() 使用，不直接对外暴露。
internal static class N2nAuth
{
    public const int PrivatePublicKeySize = 32;   // n2n_private_public_key_t

    // int generate_private_key (n2n_private_public_key_t key, char *in)
    // hash the 0-terminated string input twice to generate private key
    // 注：C 版按字节串处理（strlen），此处统一按 UTF-8 编码；
    //     纯 ASCII 的 username/password 与官方工具逐字节一致。
    public static byte[] GeneratePrivateKey(string input)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] key = new byte[PrivatePublicKeySize];

        PearsonHash.Hash256(bytes, key);
        // 第二次哈希的输入是第一次的 32 字节输出（原地安全，实现先读后写）
        PearsonHash.Hash256(key, key);

        return key;
    }

    // int bind_private_key_to_username (n2n_private_public_key_t prv, char *username)
    // hash username once, hash password twice (so password is bound to username
    // but username and password are not interchangeable)
    public static void BindPrivateKeyToUsername(byte[] privateKey, string username)
    {
        byte[] tmp = new byte[PrivatePublicKeySize];
        PearsonHash.Hash256(Encoding.UTF8.GetBytes(username), tmp);

        for (int i = 0; i < PrivatePublicKeySize; i++)
        {
            privateKey[i] ^= tmp[i];
        }
    }

    // int generate_public_key (n2n_private_public_key_t pub, n2n_private_public_key_t prv)
    // generator point '9' on curve
    // 注意：n2n 的 gen[31]=9 在小端 field 表示下实际是 u = 9*2^248，
    // 而非 RFC 7748 标准基点 9 —— Curve25519.PublicKey 已按 n2n 行为复刻
    // （curve25519.c:341-343 的 RFC 7748 clamp 与 .NET 无关，见该文件注释）。
    public static byte[] GeneratePublicKey(ReadOnlySpan<byte> privateKey)
    {
        return Curve25519.PublicKey(privateKey);
    }
}
