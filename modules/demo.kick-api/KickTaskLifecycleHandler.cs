using System.Text.Json;
using TelegramPanel.Core.BatchTasks;
using TelegramPanel.Modules;

namespace Demo.KickApi;

/// <summary>所有状态都由宿主 task.Config 持久化，模块没有需单独提交或回滚的外部状态。</summary>
public sealed class KickTaskLifecycleHandler : IModuleTaskLifecycleHandler
{
    public string TaskType => BatchTaskTypes.ExternalApiKick;

    public Task ValidateAsync(ModuleTaskLifecycleContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (context.Task.TaskType != TaskType || context.Task.ExecutionKind != ModuleTaskExecutionKinds.Batch
            || context.Task.OwnerModuleId is not ("demo.kick-api" or "host.legacy"))
            throw new InvalidOperationException("踢人任务归属或执行通道不匹配");
        KickTaskLog input;
        try { input = JsonSerializer.Deserialize<KickTaskLog>(context.Task.Config ?? "") ?? throw new JsonException(); }
        catch (JsonException) { throw new InvalidOperationException("踢人任务配置格式无效"); }
        if (input.UserId <= 0 || input.BotId < 0
            || (input.BotId > 0 && !input.UseAllChats && !(input.ChatIds?.Any(id => id != 0) ?? false)))
            throw new InvalidOperationException("踢人任务必须指定有效用户和目标范围");
        return Task.CompletedTask;
    }

    // 删除只涉及宿主任务行；无模块文件、订阅或数据库记录，不需要额外事务。
    public Task PrepareDeleteAsync(ModuleTaskLifecycleContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CommitDeleteAsync(ModuleTaskLifecycleContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task AbortDeleteAsync(ModuleTaskLifecycleContext context, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ReconcileAsync(IReadOnlyCollection<int> existingTaskIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
