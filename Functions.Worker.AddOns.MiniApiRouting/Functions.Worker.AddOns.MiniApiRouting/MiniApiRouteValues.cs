using System.Collections;

namespace Functions.Worker.AddOns.MiniApiRouting;

public sealed class MiniApiRouteValues(IReadOnlyDictionary<string, string> values) : IReadOnlyDictionary<string, string>
{
    private readonly IReadOnlyDictionary<string, string> _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);

    public string this[string key] => _values[key];
    public IEnumerable<string> Keys => _values.Keys;
    public IEnumerable<string> Values => _values.Values;
    public int Count => _values.Count;
    public bool ContainsKey(string key) => _values.ContainsKey(key);
    public bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value!);
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
