// N2nKeygen - C# port of n2n's key derivation (tools/n2n-keygen.c, src/auth.c).
// Copyright (C) 2026 egoistic-eLily (TSUKASA SAKURAOCHI)
// Portions Copyright (C) ntop.org and contributors (n2n, GPLv3).
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See libs/N2nKeygen/LICENSE for details.

using System.Buffers.Binary;

namespace N2nKeygen.Core;

// pearsonB 256 位哈希的忠实移植。
// 复刻自 ntop/n2n 3.1.1 src/pearson.c 的 pearson_hash_256()
// （算法取自 https://github.com/Logan007/pearsonB，公有领域；
//   64 位混合函数为 David Stafford 的 Mix13，公有领域）。
//
// 注意：它与经典"256 字节查找表"的 Pearson 算法无关，仅同名。
// 结构：4 条独立的 64 位哈希链（hash1..hash4），
//       输出 32 字节 = BE(hash4) ‖ BE(hash3) ‖ BE(hash2) ‖ BE(hash1)。
internal static class PearsonHash
{
    // David Stafford's Mix13 from http://zimbry.blogspot.com/2011/09/better-bit-mixing-improving-on.html
    private static ulong Mix64(ulong x)
    {
        x ^= x >> 30;
        x *= 0xbf58476d1ce4e5b9UL;
        x ^= x >> 27;
        x *= 0x94d049bb133111ebUL;
        x ^= x >> 31;
        return x;
    }

    // #define hash_round(hash, in, part) hash##part ^= in; dec##part(hash##part); permute64(hash##part)
    // 其中 dec1..dec4 分别为把状态减去 1/2/3/4
    private static void Round(ref ulong hash, ulong input, int part)
    {
        hash ^= input;
        hash -= (ulong)part;
        hash = Mix64(hash);
    }

    // void pearson_hash_256 (uint8_t *out, const uint8_t *in, size_t len)
    // out 必须有 32 字节空间。允许 out 与 in 引用同一缓冲区（与 C 版一致）：
    // 实现先读完全部输入、最后才写输出。
    public static void Hash256(ReadOnlySpan<byte> input, Span<byte> output)
    {
        ulong orgLen = (ulong)input.Length;
        ulong hash1 = 0, hash2 = 0, hash3 = 0, hash4 = 0;

        int pos = 0;
        int len = input.Length;

        while (len > 7)
        {
            // digest words little endian first
            ulong w = BinaryPrimitives.ReadUInt64LittleEndian(input.Slice(pos, 8));

            Round(ref hash1, w, 1);
            Round(ref hash2, w, 2);
            Round(ref hash3, w, 3);
            Round(ref hash4, w, 4);

            pos += 8;
            len -= 8;
        }

        // handle the rest
        hash1 = ~hash1;
        hash2 = ~hash2;
        hash3 = ~hash3;
        hash4 = ~hash4;

        while (len > 0)
        {
            // byte-wise, no endianess
            ulong b = input[pos];

            Round(ref hash1, b, 1);
            Round(ref hash2, b, 2);
            Round(ref hash3, b, 3);
            Round(ref hash4, b, 4);

            pos++;
            len--;
        }

        // digest length
        hash1 = ~hash1;
        hash2 = ~hash2;
        hash3 = ~hash3;
        hash4 = ~hash4;

        Round(ref hash1, orgLen, 1);
        Round(ref hash2, orgLen, 2);
        Round(ref hash3, orgLen, 3);
        Round(ref hash4, orgLen, 4);

        // hash string is stored big endian, the natural way to read
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(0, 8), hash4);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(8, 8), hash3);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(16, 8), hash2);
        BinaryPrimitives.WriteUInt64BigEndian(output.Slice(24, 8), hash1);
    }
}
