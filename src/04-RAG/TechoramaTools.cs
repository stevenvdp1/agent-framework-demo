using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;

namespace RAG;

public static class TechoramaSearch
{
    private static List<TechoramaSession>? _sessions;

    public static void Load(string dataPath)
    {
        var json = File.ReadAllText(dataPath);
        _sessions = JsonSerializer.Deserialize<List<TechoramaSession>>(json)!;
    }

    public static Task<IEnumerable<TextSearchProvider.TextSearchResult>> SearchAdapter(
        string query, CancellationToken cancellationToken)
    {
        var results = new List<TextSearchProvider.TextSearchResult>();

        if (_sessions is null)
            return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);

        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToArray();

        if (words.Length == 0)
            return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);

        var matches = _sessions.Where(s =>
            words.Any(w =>
                s.Topic.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                s.Speaker.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                s.Room.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                s.Date.Contains(w, StringComparison.OrdinalIgnoreCase) ||
                s.Difficulty.Contains(w, StringComparison.OrdinalIgnoreCase)));

        foreach (var session in matches)
        {
            results.Add(new TextSearchProvider.TextSearchResult
            {
                SourceName = session.Topic,
                Text = $"Date: {session.Date} | Time: {session.Time} | Room: {session.Room} | " +
                       $"Speaker: {session.Speaker} | Difficulty: {session.Difficulty} | " +
                       $"Topic: {session.Topic}"
            });
        }

        return Task.FromResult<IEnumerable<TextSearchProvider.TextSearchResult>>(results);
    }
}

public record TechoramaSession
{
    [JsonPropertyName("date")]
    public string Date { get; init; } = "";

    [JsonPropertyName("time")]
    public string Time { get; init; } = "";

    [JsonPropertyName("room")]
    public string Room { get; init; } = "";

    [JsonPropertyName("topic")]
    public string Topic { get; init; } = "";

    [JsonPropertyName("speaker")]
    public string Speaker { get; init; } = "";

    [JsonPropertyName("difficulty")]
    public string Difficulty { get; init; } = "";
}
