---
name: tgpanel-module-workflow
description: 为 Telegram Panel 开发、修改、排查和轻量打包任务、API、静态 Vue 页面模块，并维护公开或私有在线模块目录。
---

# Telegram Panel 模块开发

本技能由 tgpanel-module-workflow 整理为随仓库分发的版本，不依赖维护者机器路径。
适用于支持在线模块仓库的宿主；接口以目标宿主源码为准，示例 demo.kick-api 1.0.1 仅适配 1.31.77。

## 先定位合同

- 检查 README、Git 状态及模块目录中的 manifest.json、唯一 csproj，保留已有改动。
- 宿主源码可能在本仓库、upstream/Telegram-Panel 或外部路径。读取目标宿主 docs/developer/modules.md、docs/developer/module-repositories.md 和 src/TelegramPanel.Web/Modules/ModuleLoadContext.cs。
- 公开示例在 modules/demo.kick-api。个人模块使用自己的 ID、命名空间、路由和 Release 地址，不要无意重复注册示例的 /api/kick。
- 区分源码版本、发布包版本和线上版本；没有构建来源证据时，不把旧包当作最新包。

## 实现

- 入口实现 ITelegramPanelModule，manifest 的 entry.assembly/type 必须匹配实际 DLL 和类型；行为变更递增版本。
- 独立分发优先模块自带静态 Vue 页面，使用普通 Microsoft.NET.Sdk。页面和资源由模块映射 /ext/{moduleId}/...；静态页模式 GetPages() 返回空，不假设宿主自动发布 wwwroot。
- 管理 API 使用 /api/panel/extensions/{slug} 并继承管理员认证；外部 API 明确自己的密钥验证，不以 AllowAnonymous 代替鉴权。
- 标准任务须有安全 CreateRoute、唯一且类型匹配的执行器及 IModuleTaskLifecycleHandler。按宿主 Batch/Persistent 合同选择执行通道，不靠空生命周期处理器消除告警。
- 编辑遵守暂停屏障；持久任务支持取消、重启恢复和独立运行态，不在静态变量中保存唯一状态。真实长期任务使用宿主持久通道；基础监听服务才使用 HostedService。
- Razor 仅用于已有兼容页，组件声明 ModuleId/PageKey。宿主原生 Vue 页需要主项目配套发布，不能声称只装模块就会增加宿主页面。

## 构建与检查

修改源码、manifest 或页面后，交付前构建受影响模块的 .tpm；只改技能、文档或目录不需要重编译未变化模块。
从模块仓库根目录调用已有脚本：

```powershell
# 公开示例：HostRoot 指向匹配版本的宿主源码
powershell -ExecutionPolicy Bypass -File tools/package.ps1 -Module demo.kick-api -HostRoot C:/path/Telegram-Panel
# 使用通用打包脚本的仓库
powershell -ExecutionPolicy Bypass -File tools/package-module.ps1 -Project "path/Module.csproj" -Manifest "path/manifest.json" -NoDocker
```

默认轻量包，不加 -Full。公开示例脚本只带入口 DLL（页面已嵌入）；有额外运行依赖的新模块须调整打包，不能机械照搬。
通用打包剔除宿主共享 DLL，保留入口 DLL、模块独有依赖和必要资源；以目标宿主加载规则为准。
检查 ZIP 根 manifest.json、lib/入口.dll、版本、路径和资源；禁止把源码、凭据、旧 publish 目录误装进包。
已发布同版本包不可覆盖，修复后递增版本。轻量化不是未编译，也不是混淆或源码保密保证。

## 发布在线目录

读取 [目录发布与验收](references/repository-contract.md)。源码提交不会自动变成可安装模块：先编译并验证 .tpm，再发布 Release，最后更新 index.json。
私有源码和包留在私有仓库，不能为解决访问失败改成公开。

## 验证与交付

- 运行受影响测试（单条测试最长 60 秒），记录命令、产物路径和 SHA-256。构建成功不等于云端业务可用。
- 在匹配宿主验证安装、重启、加载、页面/API、创建/暂停/编辑/取消/删除等受影响路径；真实 Telegram 操作遵守用户授权范围。
- 加载失败先查入口及共享 DLL；页面失败查静态资源端点；canCreate=false 查路由、执行器、生命周期和冲突，不能隐藏诊断。
- 回滚时停用问题版本、恢复最后可用版本并重启；保留旧 Release 与目录历史。
- 交付明确源码提交、发布包和线上状态，未执行的检查不能写成通过。
