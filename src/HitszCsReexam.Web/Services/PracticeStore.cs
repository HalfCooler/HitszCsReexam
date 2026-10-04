using System.Text.Json;
using Microsoft.JSInterop;

namespace HitszCsReexam.Web.Services;

public sealed class PracticeRecord
{
    public int Attempts { get; set; }
    public int Correct { get; set; }
    public bool NeedsReview { get; set; }
}

public sealed class PracticeStore(IJSRuntime js)
{
    private const string StorageKey = "hitsz-cs-reexam-practice-v1";
    private Dictionary<string, PracticeRecord> _records = [];

    public bool Loaded { get; private set; }
    public bool StorageAvailable { get; private set; } = true;
    public int WrongCount => _records.Values.Count(record => record.NeedsReview);
    public int AttemptCount => _records.Values.Sum(record => record.Attempts);
    public int CorrectCount => _records.Values.Sum(record => record.Correct);
    public bool NeedsReview(Question question) => _records.TryGetValue(question.Key, out var record) && record.NeedsReview;

    public async Task LoadAsync()
    {
        if (Loaded) return;
        try
        {
            var json = await js.InvokeAsync<string?>("practiceStorage.load", StorageKey);
            if (!string.IsNullOrWhiteSpace(json))
                _records = JsonSerializer.Deserialize<Dictionary<string, PracticeRecord>>(json) ?? [];
        }
        catch (Exception exception) when (exception is JSException or JsonException or InvalidOperationException)
        {
            StorageAvailable = false;
        }
        Loaded = true;
    }

    public async Task RecordAsync(Question question, bool correct)
    {
        Record(question, correct);
        await SaveAsync();
    }

    public async Task RecordManyAsync(IEnumerable<(Question Question, bool Correct)> answers)
    {
        foreach (var (question, correct) in answers)
            Record(question, correct);
        await SaveAsync();
    }

    private void Record(Question question, bool correct)
    {
        if (!_records.TryGetValue(question.Key, out var record))
            _records[question.Key] = record = new PracticeRecord();
        record.Attempts++;
        if (correct) record.Correct++;
        record.NeedsReview = !correct;
    }

    private async Task SaveAsync()
    {
        try
        {
            await js.InvokeVoidAsync("practiceStorage.save", StorageKey, JsonSerializer.Serialize(_records));
            StorageAvailable = true;
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            StorageAvailable = false;
        }
    }
}
