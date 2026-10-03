# N2nKeygen

官方 [ntop/n2n](https://github.com/ntop/n2n) 3.1.1 `tools/n2n-keygen.c` 密钥派生算法的 C# 类库复刻，
**与官方工具输出逐字节一致**（已交叉验证）。

## 使用

项目引用本库后，唯一公开 API：

```csharp
using N2nKeygen.Core;

string publicKey = N2nUserKey.Generate(username, password);
// 返回 43 字符公钥字符串，即写入 supernode community.list 的
// "* <username> <publicKey>" 行中的公钥部分（不含用户名前缀）。
```

## 项目结构

```
├── UserKey.cs            # 唯一公开 API：N2nUserKey.Generate(username, password)
└── N2n/                  # 核心实现（全部 internal，不对外暴露）
    ├── PearsonHash.cs    # pearsonB 256 位哈希（4×64bit 链，非经典 Pearson）
    ├── N2nAuth.cs        # 派生流水线：H(H(pw)) ^ H(user) → X25519 → 公钥
    ├── BaseCodec.cs      # 自定义 6-bit ASCII 编解码（0-9 A-Z a-z + -）
    └── Curve25519.cs     # 纯托管 X25519（RFC 7748 Montgomery ladder）
```

## 算法流水线（password → key）

```
私钥 = pearson_hash_256(pearson_hash_256(password))
私钥 ^= pearson_hash_256(username)
公钥 = curve25519(私钥, n2n 生成元)     # gen[31]=9，即 u = 9·2^248，非 RFC 标准基点
输出 = bin_to_ascii(公钥)                # 43 字符，无填充
```

## 保真度与验证结论

- 与官方工具交叉验证（MSVC 编译 `tools/n2n-keygen.c`，见 `../verify-msvc/`）：
  **7 组用户模式 + 1 组联邦模式输入，输出逐字节一致**。
- `Curve25519` 实现额外通过 RFC 7748 §6.1 官方向量（共享密钥与标准基点标量乘）。
- n2n 的 `gen[31]=9`（"generator point '9'"）在小端 field 表示下实际是
  u = 9·2^248，并非 RFC 标准基点 9 —— 本库保留了这一 n2n 特有行为
  （与官方 supernode/edge 互通的前提）。
- 文本编码统一按 UTF-8；纯 ASCII 输入与官方工具逐字节一致。
- 类库化的改造由临时控制台项目对拍官方输出确认后移除（结果不变）。

详细分析文档（《n2n-keygen密码转密钥分析.txt》等）保存在本地的
`research/` 研究目录中（已被 git 排除，不入库）。

## 许可

本库是 n2n（GPLv3）源码的跨语言移植，属于衍生作品，因此以 **GPLv3** 发布
（见 [LICENSE](LICENSE)）。上游的 pearsonB、Mix13、curve25519 组件本身为
公有领域，但整体编排源自 n2n 的 GPL 代码。

注意：主服务器通过 `N2nKeygen.dll` 静态链接本库——**分发服务器二进制时，
需按 GPLv3 一并提供对应源码**。仅自部署、不分发则不触发该义务。
