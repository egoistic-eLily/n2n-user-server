// N2nKeygen - C# port of n2n's key derivation (tools/n2n-keygen.c, src/auth.c).
// Copyright (C) 2026 egoistic-eLily (TSUKASA SAKURAOCHI)
// Portions Copyright (C) ntop.org and contributors (n2n, GPLv3).
//
// This program is free software: you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the
// Free Software Foundation, either version 3 of the License, or (at your
// option) any later version. See libs/N2nKeygen/LICENSE for details.

namespace N2nKeygen.Core;

// n2n 自定义 6-bit ASCII 编解码的忠实移植。
// 复刻自 ntop/n2n 3.1.1 src/auth.c 的 bin_to_ascii() / ascii_to_bin()。
// 注意：字母表不是标准 Base64（0-9 A-Z a-z + -，且无 '=' 填充）。
internal static class BaseCodec
{
    // N2N_USER_KEY_LINE_STARTER（include/n2n_define.h:178）
    public const char UserKeyLineStarter = '*';

    // mapping six binary bits to printable ascii character
    private static readonly byte[] B2a =
    {
        0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46,   /* 0 ... 9, A ... F */
        0x47, 0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f, 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56,   /* G ... V          */
        0x57, 0x58, 0x59, 0x5a, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c,   /* W ... Z, a ... l */
        0x6d, 0x6e, 0x6f, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x2b, 0x2d,   /* m ... z, + , -   */
    };

    // mapping ascii 0x30 ...0x7f back to 6 bit binary, invalids are mapped to 0xff
    // （下标相对 0x20 偏移，与 C 版 a2b[ch - 0x20] 的取法一致）
    private static readonly byte[] A2b =
    {
        0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x3e, 0xff, 0x3f, 0xff, 0xff,   /* 0x20 ... 0x2f */
        0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xff, 0xff, 0x3e, 0xff, 0x3f, 0xff,   /* 0x30 ... 0x3f */
        0xff, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,   /* 0x40 ... 0x4f */
        0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f, 0x20, 0x21, 0x22, 0x23, 0xff, 0xff, 0xff, 0xff, 0xff,   /* 0x50 ... 0x5f */
        0xff, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f, 0x30, 0x31, 0x32,   /* 0x60 ... 0x6f */
        0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0xff, 0xff, 0xff, 0xff, 0xff,   /* 0x70 ... 0x7f */
    };

    // int bin_to_ascii (char *out, uint8_t *in, size_t in_len)
    // 32 字节 → 43 字符（⌈256/6⌉），无填充符。
    public static string BinToAscii(ReadOnlySpan<byte> input)
    {
        int inLen = input.Length;
        // out buffer is already allocated and of size ceiling(in_len * 8 / 6) + 1
        char[] output = new char[(inLen * 8 + 5) / 6];
        int outCount = 0;

        for (int bitCount = 0; bitCount < 8 * inLen; bitCount += 6)
        {
            byte buf1 = (byte)(input[bitCount / 8] << (bitCount % 8));

            byte buf2 = ((bitCount + 8) < (8 * inLen))
                ? (byte)(input[bitCount / 8 + 1] >> (8 - (bitCount % 8)))
                : (byte)0;

            buf1 = (byte)(buf1 | buf2);
            buf1 = (byte)(buf1 >> 2);

            output[outCount++] = (char)B2a[buf1];
        }

        return new string(output, 0, outCount);
    }

    // int ascii_to_bin (uint8_t *out, char *in)
    // 43 字符 → 32 字节。遇到非法字符时与 C 版一致地告警并跳过（贡献 0 bit）。
    public static byte[] AsciiToBin(string input)
    {
        byte[] output = new byte[input.Length * 6 / 8];
        int outCount = 0;
        int bitCount = 0;
        ushort buf = 0;

        for (int inCount = 0; inCount < input.Length; inCount++)
        {
            buf <<= 6;

            int ch = input[inCount];
            if ((ch > 0x20) && (ch < 0x80))
            {
                if (A2b[ch - 0x20] != 0xFF)
                {
                    buf |= A2b[ch - 0x20];
                }
                else
                {
                    Console.Error.WriteLine($"ascii_to_bin encountered the unknown character '{(char)ch}'");
                }
            }
            else
            {
                Console.Error.WriteLine("ascii_to_bin encountered a completely out-of-range character");
            }

            bitCount += 6;

            if (bitCount / 8 > 0)
            {
                bitCount -= 8;
                output[outCount++] = (byte)(buf >> bitCount);
            }
        }

        return output;
    }
}
