# 在线模块仓库合同

仓库保留源码供开发；面板读取根目录 index.json 并下载安装已编译的 .tpm，不在服务器上编译源码。
源码可使用 modules/模块ID 或已有分类目录，目录名称不是在线协议的一部分。

## 发布顺序

1. 构建并验证新版本 .tpm，保留旧版本资产供回滚。
2. 将包发布到自己的 GitHub Release，记录仓库内资产 API URL。
3. 使用实际包生成目录，禁止凭源码 manifest 猜测已发布版本或哈希：

```bash
gh api repos/OWNER/REPO/releases/tags/TAG --jq '.assets[] | {name,url}'
python tools/catalog.py add artifacts/module-1.0.0.tpm https://api.github.com/repos/OWNER/REPO/releases/assets/ASSET_ID
python tools/catalog.py validate
```

4. 提交 index.json，在面板添加 OWNER/REPO、main；连接后核对版本，先在测试宿主安装验收。

schemaVersion 为 1，modules 为数组。每条必填 id、name、version、downloadUrl、sha256；description 可选。
同 ID/版本唯一。包根目录有 manifest.json 和 lib/入口.dll。目录最多 2MB/1000 项，包最多 50MB，解压最多 200MB/5000 项。
Fork 不会复制原仓库 Release 资产；独立维护须发布自己的包并替换 URL/哈希，不能只改版本字符串。

## 私有仓库

使用同仓库的 https://api.github.com/repos/OWNER/REPO/releases/assets/ID，避免需要网页登录的下载 URL。
面板填写目标仓库 Contents: Read 细粒度令牌；令牌不能写进源码、目录、URL 或日志。
目录与私有包应在同一仓库，面板不会把令牌发给其他仓库或跨源重定向目标。
HTTPS 自托管目录只支持公网 HTTPS 443 和 Bearer 认证，不支持 SSH 克隆。

401/403 查权限、组织审批和限流；404 查分支、index.json 和资产 ID；哈希不符应发布递增版本，不能关闭校验。
目录校验只证明格式，私有授权、安装及业务功能仍须实网验收。回滚目录使用撤销提交，保留旧资产。
