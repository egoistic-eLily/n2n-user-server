# libs/ —— 本地依赖与类库

本目录存放随本仓库维护的本地类库源码，以及对应的预编译产物（DLL）。

## 内容

| 条目 | 说明 |
| --- | --- |
| `N2nKeygen/` | 类库源码项目（`N2nUserKey.Generate(username, password)` → 43 字符 n2n 用户公钥）。详细说明见 [N2nKeygen/README.md](N2nKeygen/README.md) |
| `N2nKeygen.dll` | 上述项目的 Release 编译产物（net10.0），供不想引入源码工程、只想以二进制引用的场景使用 |
| `N2nSupernode/` | 类库源码项目：n2n supernode 进程宿主（启动/停止/输出事件）+ 回环 UDP 管理口客户端，跨平台（Windows/Linux），详见 [N2nSupernode/README.md](N2nSupernode/README.md) |
| `N2nSupernode.dll` | 上述项目的 Release 编译产物（net10.0） |

## 引用方式

**方式一：项目引用（推荐，随源码一起演进）**

```xml
<ItemGroup>
  <ProjectReference Include="libs\N2nKeygen\N2nKeygen.csproj" />
</ItemGroup>
```

**方式二：二进制引用（使用本目录的 DLL）**

```xml
<ItemGroup>
  <Reference Include="N2nKeygen">
    <HintPath>libs\N2nKeygen.dll</HintPath>
  </Reference>
</ItemGroup>
```

## 维护约定

- 修改 `N2nKeygen/` 源码后，请重新执行
  `dotnet build -c Release`（在 `libs/N2nKeygen` 下），
  并将 `bin/Release/net10.0/N2nKeygen.dll` 更新到本目录，
  保持 DLL 与源码同步；
- 源码项目的 `bin/`、`obj/` 已被 `.gitignore` 排除；
- DLL 产物按二进制文件管理（见 `.gitattributes`）。
