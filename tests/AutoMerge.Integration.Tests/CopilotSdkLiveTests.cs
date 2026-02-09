using AutoMerge.Core.Models;
using AutoMerge.Infrastructure.AI;
using FluentAssertions;
using GitHub.Copilot.SDK;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace AutoMerge.Integration.Tests;

[Trait("Category", "Live")]
public sealed class CopilotSdkLiveTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _tempDir;

    public CopilotSdkLiveTests(ITestOutputHelper output)
    {
        _output = output;
        _tempDir = Path.Combine(Path.GetTempPath(), "AutoMerge.LiveTest", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public ValueTask DisposeAsync()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
        return ValueTask.CompletedTask;
    }

    private string WriteFile(string name, string content)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private MergeSession CreateSimpleConflictSession()
    {
        var basePath = WriteFile("base.txt", "line1\nshared\nline3\n");
        var localPath = WriteFile("local.txt", "line1\nlocal change\nline3\n");
        var remotePath = WriteFile("remote.txt", "line1\nremote change\nline3\n");
        var mergedPath = WriteFile("merged.txt",
            "line1\n<<<<<<< LOCAL\nlocal change\n=======\nremote change\n>>>>>>> REMOTE\nline3\n");
        return new MergeSession(new MergeInput(basePath, localPath, remotePath, mergedPath));
    }

    // ──────────────────────────────────────────────────────────────
    //  Step 1: Discover ALL methods/properties on CopilotSession
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DiscoverSessionApi()
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        });
        await client.StartAsync();

        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "GPT-5 mini",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = "Test"
            }
        });

        _output.WriteLine($"Session type: {session.GetType().FullName}");
        _output.WriteLine("--- METHODS ---");
        foreach (var m in session.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name))
        {
            var paramStr = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            _output.WriteLine($"  {m.ReturnType.Name} {m.Name}({paramStr})");
        }

        _output.WriteLine("--- PROPERTIES ---");
        foreach (var p in session.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name))
        {
            try
            {
                var val = p.GetValue(session);
                _output.WriteLine($"  {p.PropertyType.Name} {p.Name} = {val}");
            }
            catch
            {
                _output.WriteLine($"  {p.PropertyType.Name} {p.Name} = (error reading)");
            }
        }

        _output.WriteLine("--- CLIENT METHODS ---");
        foreach (var m in client.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name))
        {
            var paramStr = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
            _output.WriteLine($"  {m.ReturnType.Name} {m.Name}({paramStr})");
        }

        _output.WriteLine("--- CLIENT PROPERTIES ---");
        foreach (var p in client.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name))
        {
            try
            {
                var val = p.GetValue(client);
                _output.WriteLine($"  {p.PropertyType.Name} {p.Name} = {val}");
            }
            catch
            {
                _output.WriteLine($"  {p.PropertyType.Name} {p.Name} = (error reading)");
            }
        }

        // Also check SessionConfig for any extra properties we might be missing
        _output.WriteLine("--- SessionConfig PROPERTIES ---");
        foreach (var p in typeof(SessionConfig).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name))
        {
            _output.WriteLine($"  {p.PropertyType.Name} {p.Name}");
        }

        // Check MessageOptions
        _output.WriteLine("--- MessageOptions PROPERTIES ---");
        foreach (var p in typeof(MessageOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name))
        {
            _output.WriteLine($"  {p.PropertyType.Name} {p.Name}");
        }

        // Check CopilotClientOptions
        _output.WriteLine("--- CopilotClientOptions PROPERTIES ---");
        foreach (var p in typeof(CopilotClientOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name))
        {
            _output.WriteLine($"  {p.PropertyType.Name} {p.Name}");
        }

        // Check all event types in the SDK namespace
        _output.WriteLine("--- ALL SDK EVENT TYPES ---");
        var sdkAsm = typeof(CopilotClient).Assembly;
        foreach (var t in sdkAsm.GetExportedTypes()
            .Where(t => t.Name.Contains("Event") || t.Name.Contains("event"))
            .OrderBy(t => t.Name))
        {
            _output.WriteLine($"  {t.FullName}");
        }

        await session.DisposeAsync();
        await client.StopAsync();

        Assert.True(true); // Pass — this test is diagnostic
    }

    // ──────────────────────────────────────────────────────────────
    //  Step 1B: Check auth, status, available models
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DiagnosticStatus()
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        });
        await client.StartAsync();

        // Client-level events
        client.On(evt =>
        {
            _output.WriteLine($"  CLIENT EVENT: {evt}");
        });

        _output.WriteLine("--- AuthStatus ---");
        try
        {
            var auth = await client.GetAuthStatusAsync();
            _output.WriteLine($"  Auth: {auth}");
            foreach (var p in auth.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try { _output.WriteLine($"  .{p.Name} = {p.GetValue(auth)}"); } catch { }
            }
        }
        catch (Exception ex) { _output.WriteLine($"  Error: {ex.Message}"); }

        _output.WriteLine("--- Status ---");
        try
        {
            var status = await client.GetStatusAsync();
            _output.WriteLine($"  Status: {status}");
            foreach (var p in status.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try { _output.WriteLine($"  .{p.Name} = {p.GetValue(status)}"); } catch { }
            }
        }
        catch (Exception ex) { _output.WriteLine($"  Error: {ex.Message}"); }

        _output.WriteLine("--- ListModels ---");
        try
        {
            var models = await client.ListModelsAsync();
            _output.WriteLine($"  Models type: {models?.GetType().FullName ?? "null"}");
            if (models is System.Collections.IEnumerable enumerable)
            {
                foreach (var m in enumerable)
                {
                    _output.WriteLine($"  Model: {m}");
                    if (m is not null)
                    {
                        foreach (var p in m.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            try { _output.WriteLine($"    .{p.Name} = {p.GetValue(m)}"); } catch { }
                        }
                    }
                }
            }
            else
            {
                _output.WriteLine($"  Value: {models}");
            }
        }
        catch (Exception ex) { _output.WriteLine($"  Error: {ex.Message}"); }

        _output.WriteLine("--- Ping ---");
        try
        {
            var ping = await client.PingAsync("test");
            _output.WriteLine($"  Ping: {ping}");
        }
        catch (Exception ex) { _output.WriteLine($"  Error: {ex.Message}"); }

        await client.StopAsync();
        Assert.True(true);
    }

    // ──────────────────────────────────────────────────────────────
    //  Step 2A: Try SendAndWaitAsync (non-streaming)
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RawSdk_SendAndWaitAsync_NonStreaming()
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        });
        await client.StartAsync();

        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "GPT-5 mini",
            Streaming = false,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = "You are helpful. Reply in 5 words max."
            }
        });

        var allEvents = new List<string>();
        session.On(evt =>
        {
            allEvents.Add(evt.GetType().Name);
            _output.WriteLine($"  EVENT: {evt.GetType().Name}");
        });

        _output.WriteLine("Calling SendAndWaitAsync...");
        var result = await session.SendAndWaitAsync(
            new MessageOptions { Prompt = "Say hello." },
            timeout: TimeSpan.FromSeconds(30));

        _output.WriteLine($"SendAndWaitAsync returned type: {result?.GetType().FullName ?? "null"}");
        _output.WriteLine($"SendAndWaitAsync returned value: {result}");

        // Dump properties of result
        if (result is not null)
        {
            foreach (var p in result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    var val = p.GetValue(result);
                    _output.WriteLine($"  .{p.Name} = {val}");
                }
                catch { }
            }
        }

        _output.WriteLine($"Events during call: [{string.Join(", ", allEvents)}]");
        result.Should().NotBeNull("SendAndWaitAsync should return a response");

        await session.DisposeAsync();
        await client.StopAsync();
    }

    // ──────────────────────────────────────────────────────────────
    //  Step 2B: Try SendAndWaitAsync (streaming) to see events
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RawSdk_SendAndWaitAsync_Streaming()
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        });
        await client.StartAsync();

        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "GPT-5 mini",
            Streaming = true,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = "You are helpful. Reply in 5 words max."
            }
        });

        var chunks = new System.Text.StringBuilder();
        var allEvents = new List<string>();

        session.On(evt =>
        {
            allEvents.Add(evt.GetType().Name);
            _output.WriteLine($"  EVENT: {evt.GetType().Name}");

            if (evt is AssistantMessageDeltaEvent delta)
            {
                var c = delta.Data?.DeltaContent ?? "";
                chunks.Append(c);
                _output.WriteLine($"    DELTA: '{c}'");
            }
            else if (evt is AssistantMessageEvent msg)
            {
                _output.WriteLine($"    FINAL: '{msg.Data?.Content}'");
            }
        });

        _output.WriteLine("Calling SendAndWaitAsync (streaming)...");
        var result = await session.SendAndWaitAsync(
            new MessageOptions { Prompt = "Say hello." },
            timeout: TimeSpan.FromSeconds(30));

        _output.WriteLine($"SendAndWaitAsync returned type: {result?.GetType().FullName ?? "null"}");
        _output.WriteLine($"SendAndWaitAsync returned value: {result}");

        if (result is not null)
        {
            foreach (var p in result.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    var val = p.GetValue(result);
                    _output.WriteLine($"  .{p.Name} = {val}");
                }
                catch { }
            }
        }

        _output.WriteLine($"Chunks: '{chunks}'");
        _output.WriteLine($"Events: [{string.Join(", ", allEvents)}]");
        result.Should().NotBeNull();

        await session.DisposeAsync();
        await client.StopAsync();
    }

    // ──────────────────────────────────────────────────────────────
    //  Step 2C: Original pattern (SendAsync + events) for comparison
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task RawSdk_SendAsync_EventPattern()
    {
        await using var client = new CopilotClient(new CopilotClientOptions
        {
            AutoStart = false,
            UseLoggedInUser = true
        });
        await client.StartAsync();

        var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = "GPT-5 mini",
            Streaming = false,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = "You are helpful. Reply in 5 words max."
            }
        });

        var responseText = new System.Text.StringBuilder();
        var allEvents = new List<string>();
        var done = new TaskCompletionSource();

        session.On(evt =>
        {
            allEvents.Add(evt.GetType().Name);
            _output.WriteLine($"  EVENT: {evt.GetType().Name}");

            switch (evt)
            {
                case AssistantMessageEvent msg:
                    if (!string.IsNullOrEmpty(msg.Data?.Content))
                        responseText.Append(msg.Data.Content);
                    done.TrySetResult();
                    break;
                case SessionIdleEvent:
                    _output.WriteLine("  -> SessionIdleEvent received");
                    done.TrySetResult();
                    break;
                case SessionErrorEvent err:
                    _output.WriteLine($"  -> Error: {err.Data?.Message}");
                    done.TrySetException(new Exception(err.Data?.Message ?? "error"));
                    break;
                case AssistantTurnEndEvent:
                    _output.WriteLine("  -> AssistantTurnEndEvent received");
                    done.TrySetResult();
                    break;
            }
        });

        _output.WriteLine("Calling SendAsync...");
        var sendResult = await session.SendAsync(new MessageOptions { Prompt = "Say hello." });
        _output.WriteLine($"SendAsync returned: {sendResult?.GetType().FullName ?? "null"}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            await done.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine($"TIMED OUT after 30s. Events: [{string.Join(", ", allEvents)}]");
        }

        _output.WriteLine($"Response: '{responseText}'");
        _output.WriteLine($"Events: [{string.Join(", ", allEvents)}]");

        await session.DisposeAsync();
        await client.StopAsync();
    }
}
