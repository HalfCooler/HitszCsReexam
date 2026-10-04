using System.Text;
using ToolGood.Words.Pinyin;

namespace HitszCsReexam.Web.Services;

public sealed class QuestionSearchIndex
{
    // Correct technical terms and overlapping phrases such as "并重新" and "总长度".
    private static readonly KeyValuePair<string, string[]>[] PronunciationOverrides =
        new Dictionary<string, string[]>
        {
            ["重传"] = ["chong", "chuan"],
            ["重发"] = ["chong", "fa"],
            ["重新"] = ["chong", "xin"],
            ["长度"] = ["chang", "du"]
        }.OrderByDescending(pair => pair.Key.Length).ToArray();

    private readonly Entry[] _entries;

    public QuestionSearchIndex(IEnumerable<Question> questions)
    {
        _entries = questions.Select(question => new Entry(question,
            new[] { question.Stem, question.ExtraBody ?? "" }
                .Concat(question.MultipleOptions)
                .Concat(question.Options)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .Select(CreateSearchText)
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

    private static SearchText CreateSearchText(string text)
    {
        var forms = new List<string> { WordsHelper.GetPinyin(text), WordsHelper.GetFirstPinyin(text) };
        var correctedPinyin = new StringBuilder();
        var correctedInitials = new StringBuilder();
        var pendingStart = 0;
        var hasOverride = false;

        for (var position = 0; position < text.Length;)
        {
            var match = PronunciationOverrides.FirstOrDefault(pair =>
                text.AsSpan(position).StartsWith(pair.Key, StringComparison.Ordinal));
            if (match.Key is null)
            {
                position++;
                continue;
            }

            AppendUnchanged(text[pendingStart..position]);
            foreach (var syllable in match.Value)
            {
                correctedPinyin.Append(syllable);
                correctedInitials.Append(syllable[0]);
            }
            position += match.Key.Length;
            pendingStart = position;
            hasOverride = true;
        }

        if (hasOverride)
        {
            AppendUnchanged(text[pendingStart..]);
            // Supplement the library's index so correcting a term preserves other phrase matches.
            forms.Add(correctedPinyin.ToString());
            forms.Add(correctedInitials.ToString());
        }
        return new SearchText(text, forms.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());

        void AppendUnchanged(string segment)
        {
            if (segment.Length == 0) return;
            correctedPinyin.Append(WordsHelper.GetPinyin(segment));
            correctedInitials.Append(WordsHelper.GetFirstPinyin(segment));
        }
    }

    // Keep fields separate so a keyword cannot span unrelated options or the stem boundary.
    private sealed record SearchText(string Original, string[] PhoneticForms)
    {
        public bool Matches(string keyword) =>
            Original.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            PhoneticForms.Any(form => form.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
