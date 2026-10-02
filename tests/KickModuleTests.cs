using System.Text.Json;
using Demo.KickApi;
using Microsoft.Extensions.DependencyInjection;
using TelegramPanel.Core.BatchTasks;
using TelegramPanel.Modules;
using Xunit;

public sealed class KickModuleTests
{
    [Fact]
    public void Module_registers_complete_task_api_and_self_contained_page()
    {
        var module = new KickApiModule();
        var context = new ModuleHostContext("1.31.77", Path.GetTempPath());
        var services = new ServiceCollection();
        module.ConfigureServices(services, context);
        using var provider = services.BuildServiceProvider();
        Assert.Equal(BatchTaskTypes.ExternalApiKick, Assert.Single(provider.GetServices<IModuleTaskHandler>()).TaskType);
        Assert.Equal(BatchTaskTypes.ExternalApiKick, Assert.Single(provider.GetServices<IModuleTaskLifecycleHandler>()).TaskType);
        var task = Assert.Single(module.GetTasks(context));
        Assert.Equal("/ext/demo.kick-api/kick", task.CreateRoute);
        Assert.Equal("/api/kick", Assert.Single(module.GetApis(context)).Route);
        Assert.Equal("demo.kick-api", module.Manifest.Id);
        Assert.Equal("Demo.KickApi.dll", module.Manifest.Entry.Assembly);
        Assert.Empty(module.GetPages(context));
        using var resource = typeof(KickApiModule).Assembly.GetManifestResourceStream("Demo.KickApi.settings.html");
        Assert.NotNull(resource);
        using var reader = new StreamReader(resource!);
        var html = reader.ReadToEnd();
        Assert.Contains("/ext/demo.kick-api/assets/vue.esm-browser.prod.js", html);
        Assert.DoesNotContain("builtin.kick-api", html);
        using var vue = typeof(KickApiModule).Assembly.GetManifestResourceStream("Demo.KickApi.vue.js");
        Assert.NotNull(vue);
    }

    [Theory]
    [InlineData("demo.kick-api", "batch", 123, 1, false, true)]
    [InlineData("host.legacy", "batch", 123, 0, true, true)]
    [InlineData("another.module", "batch", 123, 1, false, false)]
    [InlineData("demo.kick-api", "persistent", 123, 1, false, false)]
    [InlineData("demo.kick-api", "batch", 0, 1, false, false)]
    [InlineData("demo.kick-api", "batch", 123, -1, false, false)]
    public async Task Lifecycle_validates_owner_execution_and_target(string owner, string kind, long user, int bot, bool all, bool valid)
    {
        var handler = new KickTaskLifecycleHandler();
        var context = new ModuleTaskLifecycleContext
        {
            Task = new ModuleTaskSnapshot
            {
                TaskType = handler.TaskType, OwnerModuleId = owner, ExecutionKind = kind,
                Config = JsonSerializer.Serialize(new KickTaskLog { UserId = user, BotId = bot, UseAllChats = all, ChatIds = [-100123] })
            }
        };
        if (valid) await handler.ValidateAsync(context);
        else await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ValidateAsync(context));
    }

    [Fact]
    public async Task Empty_selected_targets_and_invalid_json_are_rejected()
    {
        var handler = new KickTaskLifecycleHandler();
        foreach (var config in new[] { "{", "null", JsonSerializer.Serialize(new KickTaskLog { UserId = 123, BotId = 1, UseAllChats = false, ChatIds = [] }) })
            await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ValidateAsync(new()
            {
                Task = new() { TaskType = handler.TaskType, OwnerModuleId = "demo.kick-api", ExecutionKind = "batch", Config = config }
            }));
    }

    [Fact]
    public async Task Executor_rejects_negative_bot_before_resolving_any_telegram_service()
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new ExternalApiKickTaskHandler().ExecuteAsync(
            new TestHost(services, JsonSerializer.Serialize(new KickTaskLog { BotId = -1, UserId = 123, UseAllChats = true })), default));
        Assert.Equal("任务 BotId 无效", error.Message);
    }

    private sealed class TestHost(IServiceProvider services, string config) : IModuleTaskExecutionHost
    {
        public int TaskId => 1;
        public string TaskType => BatchTaskTypes.ExternalApiKick;
        public int Total => 1;
        public string? Config => config;
        public IServiceProvider Services => services;
        public Task<bool> IsStillRunningAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        public Task UpdateProgressAsync(int completed, int failed, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
