param(
    [string]$Module = "demo.kick-api",
    [string]$HostRoot = (Join-Path $PSScriptRoot "../..")
)
$ErrorActionPreference = "Stop"
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
if ($Module -notmatch '^[a-zA-Z0-9][a-zA-Z0-9._-]*$') { throw "模块 ID 无效" }
$moduleRoot = Join-Path $repoRoot "modules/$Module"
$manifestPath = Join-Path $moduleRoot "manifest.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
$projects = @(Get-ChildItem -LiteralPath $moduleRoot -Filter '*.csproj' -File)
if ($projects.Count -ne 1) { throw "模块目录必须包含一个项目" }
$project = $projects[0]
$runRoot = Join-Path $repoRoot ("artifacts/build/" + [Guid]::NewGuid().ToString('N'))
$publish = Join-Path $runRoot 'publish'
$staging = Join-Path $runRoot 'staging'
New-Item -ItemType Directory -Path $publish,$staging,(Join-Path $staging 'lib') -Force | Out-Null

# 在本机已有 .NET SDK 下构建，宿主源码只作为引用，不进入安装包。
dotnet publish $project.FullName -c Release -o $publish "-p:HostRoot=$([IO.Path]::GetFullPath($HostRoot))" -p:UseAppHost=false -p:UseSharedCompilation=false -nodeReuse:false
if ($LASTEXITCODE -ne 0) { throw "模块编译失败" }
$entry = [string]$manifest.entry.assembly
if ($entry -ne [IO.Path]::GetFileName($entry) -or !$entry.EndsWith('.dll')) { throw "模块入口文件无效" }
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $staging 'manifest.json')
Copy-Item -LiteralPath (Join-Path $publish $entry) -Destination (Join-Path $staging 'lib')
Copy-Item -LiteralPath (Join-Path $moduleRoot 'wwwroot/vendor/LICENSE') -Destination (Join-Path $staging 'VUE-LICENSE.txt')
# 页面与 Vue 资源嵌入入口程序集。示例无其它私有运行依赖，禁止打包宿主 DLL。
$destination = Join-Path $repoRoot "artifacts/$($manifest.id)-$($manifest.version).tpm"
if (Test-Path -LiteralPath $destination) { throw "产物已存在，请递增版本或使用新的输出目录，禁止覆盖发布包" }
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($staging, $destination)
Write-Output $destination
Get-FileHash -LiteralPath $destination -Algorithm SHA256 | Format-List
