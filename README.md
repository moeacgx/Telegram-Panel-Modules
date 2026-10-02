# Telegram Panel 在线模块仓库

Telegram Panel 官方公开模块目录，也可作为个人模块仓库模板。主项目：
[Telegram-Panel](https://github.com/moeacgx/Telegram-Panel)。

首个公开演示为[踢人 / 封禁 API](modules/demo.kick-api/README.md)，包含源码、静态 Vue 页面、
后台任务执行和可安装 `.tpm`。需要宿主 `1.31.77` 的外置模块改动，旧宿主不兼容。
主项目已有的私有模块不会自动复制到公开仓库。只有拥有公开分发权的包才能加入官方目录。

## 面板接入

前置条件：使用包含「在线模块仓库」功能的 Telegram Panel 版本。
在模块管理页选择 GitHub 类型，仓库填 `moeacgx/Telegram-Panel-Modules`，分支填 `main`。
连接成功后显示可安装版本。模块兼容范围、前置条件和验收边界见各模块 README。

个人使用者可以 Fork 本仓库或自行建立同结构仓库，在面板添加自己的 `owner/repo`。
仓库可以公开或私有。GitHub 私有仓库使用目标仓库 **Contents: Read** 的细粒度令牌；
组织需要审批时应先批准令牌。不要将令牌写入 index.json、URL、工作流或提交。

## 发布模块

1. 在主项目按 `docs/developer/modules.md` 开发模块、递增 manifest 版本，完成打包与实际功能验收。
2. 将 `.tpm` 上传到当前模块仓库的 GitHub Release。Release tag 和包一经发布不要覆盖。
3. 读取资产 API 地址，公开与私有仓库都推荐 `https://api.github.com/repos/OWNER/REPO/releases/assets/ASSET_ID`。
   可用 `gh api repos/OWNER/REPO/releases/tags/TAG --jq '.assets[] | {name,url}'` 查询。
4. 运行以下命令生成条目。脚本从包内 manifest 读取身份、计算 SHA-256，不会上传文件或读取令牌。

```bash
python tools/catalog.py add path/to/module.tpm https://api.github.com/repos/OWNER/REPO/releases/assets/ASSET_ID
python tools/catalog.py validate
```

5. 提交并推送 index.json；在面板连接仓库、核对 ID/版本后安装，重启并验收模块页面和业务功能。
   相同 ID/版本不能重复发布；需要修复时应发布新版本。

模块名称来自 manifest；可在条目中补充 `description`（最多 2000 字符）。目录格式：

```json
{
  "schemaVersion": 1,
  "modules": [
    {
      "id": "example.module",
      "name": "示例模块",
      "version": "1.0.0",
      "description": "模块用途和前置条件",
      "downloadUrl": "https://api.github.com/repos/OWNER/REPO/releases/assets/123",
      "sha256": "替换为模块包的64位SHA-256十六进制摘要"
    }
  ]
}
```

示例不能直接作为有效条目发布，必须使用真实资产地址与哈希。
面板强制校验哈希、manifest ID/版本、宿主兼容范围及依赖。
目录最多 1000 项、2MB；包最多 50MB，解压最多 200MB/5000 项。

## 自托管 HTTPS

把 index.json 和包托管在公网 HTTPS 443 站点，在面板选择 HTTPS 目录类型并填写完整目录地址。
自托管认证须支持 `Authorization: Bearer TOKEN`。令牌仅发送到目录同源地址；
跨域 CDN 须使用无需令牌的下载地址，跨源重定向不会携带令牌。
内网、回环地址、非 443 端口和 HTTP 被拒绝。不支持 SSH 克隆、Basic Auth 或 Gitea 专用 API 适配。

## 维护、故障排查与回滚

- HTTP 401/403：检查令牌有效期、目标仓库读取权限和组织审批；403 也可能是 API 限流。
- HTTP 404：检查分支、根目录 index.json、资产 ID 和私有仓库访问权限。
- 哈希失败：确认目录指向的包与计算哈希的文件完全一致，禁止覆盖同版本包。
- 模块不可创建：补齐标准创建路由、匹配执行器与生命周期处理器；清理旧包不能补齐接口。
- 回滚目录：撤销有问题的目录提交，保留旧 Release 资产供回滚，不要删除用户正在使用的版本。
- 回滚已安装模块：先停用问题版本，按主项目文档恢复 LastGoodVersion 或备份，再重启验证。
- 一键清理永久删除旧安装文件和包，但保护 active、last-good 和当前进程实际加载版本。
  个人模块业务数据不在清理范围。清理前确认备份；被清理版本需要重新下载或从备份恢复。

## 主项目子模块

主项目通过 `module-repository/` Git 子模块固定本仓库提交：

```bash
git submodule update --init module-repository
git -C module-repository fetch origin
git -C module-repository checkout origin/main
git add module-repository
```

最后一步只暂存引用，仍需在主项目走功能分支 → dev → 云端验收 → main。
运行中的面板通过 HTTPS 拉取目录，不依赖部署目录包含 Git 子模块。
