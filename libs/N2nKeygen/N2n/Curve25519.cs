// N2nKeygen - C# port of n2n's key derivation (tools/n2n-keygen.c, src/auth.c).
// Copyright (C) 2026 egoistic-eLily (TSUKASA SAKURAOCHI)
// Portions Copyright (C) ntop.org and contributors (n2n, GPLv3).
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See libs/N2nKeygen/LICENSE for details.

using System.Numerics;

namespace N2nKeygen.Core;

// Curve25519 标量乘法（X25519）的纯托管实现，供复刻 n2n 密钥派生使用。
// 按 RFC 7748 §5 的 Montgomery ladder 伪代码直译；使用 BigInteger，
// 非常量时间——本实现定位为离线密钥生成工具/研究用途，不适用于
// 需要侧信道防护的在线服务。
//
// 与 n2n src/curve25519.c（Dempsky 公有领域实现）的语义一致性：
//   - 标量 clamp：e[0]&=248; e[31]&=127; e[31]|=64（curve25519.c:341-343）
//   - u 坐标最高位屏蔽：work[31] &= 127（curve25519.c:347）
//   - 结果 = x_2 * z_2^(p-2)（费马小定理求逆）
internal static class Curve25519
{
    private static readonly BigInteger P = BigInteger.Pow(2, 255) - 19;
    private const ulong A24 = 121665; // (486662 - 2) / 4

    // 生成元点 '9'（31 字节 0x00 + 0x09），对应 auth.c 中的 static uint8_t gen[32]
    public static byte[] GeneratorPoint()
    {
        var gen = new byte[32];
        gen[31] = 9;
        return gen;
    }

    // 公钥 = 标量 × 生成元(9)，等价于 auth.c 的 generate_public_key()
    public static byte[] PublicKey(ReadOnlySpan<byte> privateKey)
    {
        return ScalarMultiplication(privateKey, GeneratorPoint());
    }

    // 共享密钥 = 标量 × 对方公钥，等价于 X25519(k, u)
    public static byte[] DiffieHellman(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> peerPublicKey)
    {
        return ScalarMultiplication(privateKey, peerPublicKey);
    }

    // RFC 7748 §5 X25519(k, u)
    public static byte[] ScalarMultiplication(ReadOnlySpan<byte> scalar, ReadOnlySpan<byte> uCoordinate)
    {
        // decodeScalar25519 + clamp
        byte[] k = scalar.ToArray();
        k[0] &= 248;
        k[31] &= 127;
        k[31] |= 64;
        BigInteger kInt = DecodeLittleEndian(k);

        // decodeUCoordinate：屏蔽最高位
        byte[] u = uCoordinate.ToArray();
        u[31] &= 127;
        BigInteger x1 = Mod(DecodeLittleEndian(u));

        // Montgomery ladder
        BigInteger x2 = 1, z2 = 0, x3 = x1, z3 = 1;
        bool swap = false;

        for (int t = 254; t >= 0; t--)
        {
            bool kt = ((kInt >> t) & 1) == 1;
            swap ^= kt;

            ConditionalSwap(swap, ref x2, ref x3);
            ConditionalSwap(swap, ref z2, ref z3);
            swap = kt;

            BigInteger a = Mod(x2 + z2);
            BigInteger aa = Mod(a * a);
            BigInteger b = Mod(x2 - z2);
            BigInteger bb = Mod(b * b);
            BigInteger e = Mod(aa - bb);
            BigInteger c = Mod(x3 + z3);
            BigInteger d = Mod(x3 - z3);
            BigInteger da = Mod(d * a);
            BigInteger cb = Mod(c * b);

            x3 = Mod((da + cb) * (da + cb));
            z3 = Mod(x1 * Mod((da - cb) * (da - cb)));
            x2 = Mod(aa * bb);
            z2 = Mod(e * Mod(aa + A24 * e));
        }

        ConditionalSwap(swap, ref x2, ref x3);
        ConditionalSwap(swap, ref z2, ref z3);

        // return x_2 * (z_2^(p - 2))，即乘以 z_2 的模逆
        BigInteger result = Mod(x2 * BigInteger.ModPow(z2, P - 2, P));
        return EncodeLittleEndian(result);
    }

    private static BigInteger Mod(BigInteger x) => BigInteger.Remainder(x, P);

    private static BigInteger DecodeLittleEndian(byte[] bytes)
    {
        // BigInteger(byte[]) 按小端二进制补码解释；
        // 经 clamp / 最高位屏蔽后，末字节 < 0x80，不会解读为负数。
        return new BigInteger(bytes);
    }

    private static byte[] EncodeLittleEndian(BigInteger value)
    {
        byte[] bytes = value.ToByteArray(); // little-endian 二进制补码
        Array.Resize(ref bytes, 32);        // 补零/截断到 32 字节
        return bytes;
    }

    private static void ConditionalSwap(bool swap, ref BigInteger a, ref BigInteger b)
    {
        if (swap)
        {
            (a, b) = (b, a);
        }
    }
}
