# thirdparty/ —— 第三方源码（git 子模块）

本目录存放以 git 子模块形式引入的第三方源码。

## 内容

| 子模块 | 上游 | 许可证 | 用途 |
| --- | --- | --- | --- |
| [`n2n/`](https://github.com/ntop/n2n) | ntop/n2n（3.1.1） | GPLv3 | N2N VPN 官方实现；白名单方案的研究与二次开发基础 |

> **注**：当前子模块指向 ntop 官方 3.1.1。n2n-winui 客户端中实际使用的
> 修改版 edge 是 n2n 的衍生作品，同样必须以 GPLv3 发布（建议在 fork 仓库
> 公开源码，并在就绪后将本子模块 URL 改指 fork）。

## 使用说明

- 首次克隆本仓库后拉取子模块：
  ```bash
  git submodule update --init --recursive
  ```
- 更新子模块到上游最新：
  ```bash
  git submodule update --remote thirdparty/n2n
  ```

## 许可证与修改约定（重要）

- n2n 为 **GPLv3** 软件：本项目与它仅进程级协作，MIT 许可不受影响；
- **对本目录源码的任何修改，都必须在独立的 fork 仓库中以 GPLv3 发布**
  （保留 ntop 原始版权声明），不要把修改直接提交进本仓库；
- supernode/edge 的白名单部署方式（community.list + 管理口热重载）
  不需要修改 n2n 源码，详见 `docs/` 与本地研究文档。
