using Microsoft.Maui.Storage;

namespace JassTournamentManager.App.Tests.Support;

internal sealed class FakeSecureStorage : ISecureStorage
{
    public Dictionary<string, string> Values { get; } = new();
    public List<string> Operations { get; } = new();

    public Task<string?> GetAsync(string key)
    {
        Operations.Add($"Get:{key}");
        return Task.FromResult(Values.GetValueOrDefault(key));
    }

    public Task SetAsync(string key, string value)
    {
        Operations.Add($"Set:{key}");
        Values[key] = value;
        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        Operations.Add($"Remove:{key}");
        return Values.Remove(key);
    }

    public void RemoveAll()
    {
        Operations.Add(nameof(RemoveAll));
        Values.Clear();
    }
}