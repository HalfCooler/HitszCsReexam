using TinyPinyin;

namespace HitszCsReexam.Web.Services;

public sealed class QuestionSearchIndex
{
    private readonly Entry[] _entries;

    public QuestionSearchIndex(IEnumerable<Question> questions)
    {
        _entries = questions.Select(question => new Entry(question,
            new[] { question.Stem, question.ExtraBody ?? "" }
                .Concat(question.MultipleOptions)
                .Concat(question.Options)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(text => new SearchText(text,
                    PinyinHelper.GetPinyin(text, ""), PinyinHelper.GetPinyinInitials(text)))
                .ToArray())).ToArray();
    }

    public IReadOnlyList<Question> Search(string searchText, string type = "全部")
    {
        // Each whitespace-separated keyword must match, possibly in different fields.
        var keywords = searchText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return _entries
            .Where(entry => type == "全部" || entry.Question.Type == type)
            .Where(entry => keywords.All(keyword => entry.Fields.Any(field => field.Matches(keyword))))
            .Select(entry => entry.Question)
            .ToArray();
    }

    private sealed record Entry(Question Question, SearchText[] Fields);

    // Keep fields separate so a keyword cannot span unrelated options or the stem boundary.
    private sealed record SearchText(string Original, string Pinyin, string Initials)
    {
        public bool Matches(string keyword) =>
            Original.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            Pinyin.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            Initials.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}
