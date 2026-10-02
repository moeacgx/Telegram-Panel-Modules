# 踢人 / 封禁演示模块

该模块从 Telegram Panel 原内置 `builtin.kick-api` 拆出，用于演示 API、静态 Vue 页面、
后台任务执行器和任务生命周期的完整扩展。新模块 ID 为 `demo.kick-api`，可以安装、停用和卸载。
不是空壳示例：启用后可以实际踢人/封禁，请只对已获授权的测试 Bot、群组和用户进行验收。

## 版本与安装

- 模块版本：`1.0.1`。
- 目标宿主：`1.31.77`，需包含取消内置踢人 API 的改动；旧 `1.31.76` 不兼容。
- 宿主版本范围有意收紧，因为模块引用了宿主 Core 中的 Bot/任务服务；升级宿主后需重新验证。
- 模块包入口为 `Demo.KickApi.dll`，只包含入口程序集和 manifest。
  Vue 页面及 Vue 3.5.39 资源嵌入程序集，无需网络 CDN或宿主的旧静态资源目录。

面板「模块管理 → 官方模块仓库 → 连接 / 刷新目录 → 安装此版本」，安装后启用并重启。
也可以手动上传 Release 中的 `demo.kick-api-1.0.1.tpm`。
在 API 管理添加 `kick` 配置，设置 API 密钥、Bot 和允许操作的频道/群组。

### 从旧内置功能迁移

1. 升级前等待旧踢人任务完成，或取消/暂停尚未完成的任务，并备份数据目录。
2. 升级包含外置改动的宿主。启动时仅删除 `builtin.kick-api` 的内置状态登记；不会删除 API 配置、密钥或任务。
3. 安装本模块，启用并重启。旧 `/api/kick`、`X-API-Key`、`ExternalApi:Apis` 中的 `Type=kick` 配置保持可用。
4. 页面入口改为 `/ext/demo.kick-api/kick`，旧内置页面不再提供。
5. 历史 `external_api_kick`、`OwnerModuleId=host.legacy` 仍可由此模块执行；新任务明确归属 `demo.kick-api`。
   未安装模块期间没有踢人执行器，不能保证待执行的旧任务会自动等待安装，因此升级前应先排空旧队列。

## 页面与请求

- `/ext/demo.kick-api/kick`：管理员页面，可选 Bot、分类和具体频道/群组并提交用户 ID。
- `/api/panel/extensions/kick-api`：页面管理接口，继承宿主管理员认证。
- `POST /api/kick`：外部 API，验证 `X-API-Key`；成功以 HTTP 202 返回宿主任务 ID。
- `ExternalApi:Enabled=false` 时外部 API 返回 404。
- 任务支持取消，不提供通用暂停、编辑、重跑，避免复用旧执行结果造成重复操作；需要再次执行时从模块页明确新建。

```bash
curl -X POST https://YOUR-PANEL/api/kick \
  -H "Content-Type: application/json" \
  -H "X-API-Key: YOUR-KEY" \
  -d '{"user_id":123456789,"permanent_ban":false}'
```

`permanent_ban=false` 为踢出后允许再加入；`true` 为封禁。Bot 必须拥有目标聊天的相应管理权限。
固定 Bot 且 `UseAllChats=false` 时严格限制在所选目标中，空选择不会扩大为全部群组。

## 源码构建

前置：.NET 8 SDK、匹配版本的 Telegram Panel 源码。模块目录独立克隆时可通过 `-HostRoot` 指定宿主。
从模块仓库根目录执行：

```powershell
powershell -ExecutionPolicy Bypass -File tools/package.ps1 -HostRoot C:/path/Telegram-Panel
dotnet test tests/Demo.KickApi.Tests.csproj -c Release -p:HostRoot=C:/path/Telegram-Panel
```

主项目子模块布局中可省略 `HostRoot`。输出位于 `artifacts/demo.kick-api-1.0.1.tpm`。
脚本只打包入口程序集；页面与 Vue 资源已嵌入，不附带宿主的 Core/Data/Abstractions DLL。
发布步骤见仓库根目录维护说明；不要覆盖已发布的同版本包。

## 生命周期与安全边界

`KickTaskLifecycleHandler` 校验任务归属、执行通道、配置和目标范围。
全部运行状态保存在宿主 `task.Config`，没有模块自有数据库、文件或订阅，所以删除/对账钩子无额外事务。
这不是给不兼容模块自动补空处理器；有自有状态的模块必须实现其真正的提交和回滚逻辑。
API 配置 DTO 位于模块源码，保留宿主持久化字段合同；不依赖宿主 Web 的已删除业务实现。

## 验收、排障和回滚

- 未安装时宿主没有此 API 类型、导航和执行器；安装并重启后出现对应入口。
- 模块目录页检查无任务合同诊断，API 未配置返回 404、配置后缺少/错误密钥返回 401。
- 页面和内置 Vue 资源均应为 200，且无需旧 `wwwroot/builtin-kick-api`。
- 实际业务验收需管理员指定测试 Bot/群组/用户，确认 202 任务完成及每个目标的结果。
  自动测试不调用 Telegram，不等于已经验证真实踢人操作。
- 无入口：检查安装是否启用、是否重启、宿主版本是否匹配和模块加载日志。
- 401/404：检查 API 总开关、类型为 kick 的配置、启用状态与密钥，不要把密钥写入日志或 Issue。
- 任务失败：检查 Bot 活跃状态、目标选择、Telegram 管理权限和任务明细。
- 回滚：停用模块、重启确认端点消失；需要回到旧宿主时先停用模块，再恢复旧宿主和配套备份，
  避免同一进程同时加载新模块和旧内置端点。卸载模块不删除 API 配置或任务历史。

Vue 资源按 MIT 许可分发，许可文件见 `wwwroot/vendor/LICENSE`。
