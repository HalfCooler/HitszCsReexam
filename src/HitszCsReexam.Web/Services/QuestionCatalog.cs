using System.Text.Json;
using System.Text.Json.Serialization;
using Markdig;

namespace HitszCsReexam.Web.Services;

public sealed record Question(int Id, string Type, int Number, int Score, string Stem, IReadOnlyList<string> Options,
    string StemHtml, IReadOnlyList<string> OptionHtml, string? Extra, string? ExtraBody, string? ExtraHtml,
    bool Multiple, IReadOnlyList<string> MultipleOptions, IReadOnlyList<string> MultipleOptionHtml,
    string? Answer, string? ExplainHtml);

public sealed class QuestionCatalog
{
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseMathematics()
        .DisableHtml()
        .Build();
    private readonly Lazy<IReadOnlyList<Question>> _questions;

    public QuestionCatalog()
    {
        _questions = new Lazy<IReadOnlyList<Question>>(() => Load(Path.Combine(AppContext.BaseDirectory, "Data", "answer.json")));
    }

    public IReadOnlyList<Question> Questions => _questions.Value;
    public int ChoiceCount => Questions.Count(q => q.Type == "单项选择");
    public int JudgmentCount => Questions.Count(q => q.Type == "判断");

    private static IReadOnlyList<Question> Load(string path)
    {
        using var stream = File.OpenRead(path);
        var bank = JsonSerializer.Deserialize<AnswerBank>(stream)
            ?? throw new JsonException("题库 JSON 不能为空。");
        var result = new List<Question>();

        AddQuestions(bank.SingleChoice, "单项选择", 2);
        AddQuestions(bank.Judgement, "判断", 1);
        return result;

        void AddQuestions(IEnumerable<AnswerQuestion> source, string type, int score)
        {
            foreach (var item in source)
            {
                string[] options = type == "判断"
                    ? ["是", "否"]
                    : item.Options.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => pair.Value).ToArray();
                List<string> statements = item.Multiple
                    ? item.MultipleOptions ?? []
                    : [];
                result.Add(new Question(result.Count + 1, type, item.Id, score, item.Question, options,
                    Render(item.Question), options.Select(Render).ToArray(), item.Extra, item.ExtraBody,
                    RenderExtra(item.Extra, item.ExtraBody), item.Multiple, statements,
                    statements.Select(Render).ToArray(), item.Answer,
                    string.IsNullOrWhiteSpace(item.Explain) ? null : Render(item.Explain)));
            }
        }
    }

    private static string? RenderExtra(string? extra, string? body) => extra switch
    {
        "image" when !string.IsNullOrWhiteSpace(body) => Render($"![题目附图]({ImageUrl(body)})"),
        "markdown" when !string.IsNullOrWhiteSpace(body) => Render(body),
        _ => null
    };

    private static string ImageUrl(string path)
    {
        if (!path.StartsWith("image/", StringComparison.Ordinal) ||
            path[6..].Contains('/') || path[6..].Contains('\\'))
            throw new JsonException($"无效的题目图片路径：{path}");
        return "/questions/image/" + Uri.EscapeDataString(path[6..]);
    }

    private static string Render(string markdown) => Markdown.ToHtml(
        markdown.Replace("(image/", "(/questions/image/", StringComparison.Ordinal), MarkdownPipeline);

    private sealed class AnswerBank
    {
        [JsonPropertyName("single_choice")]
        public List<AnswerQuestion> SingleChoice { get; set; } = [];

        [JsonPropertyName("judgement")]
        public List<AnswerQuestion> Judgement { get; set; } = [];
    }

    private sealed class AnswerQuestion
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("question")]
        public string Question { get; set; } = "";

        [JsonPropertyName("extra")]
        public string? Extra { get; set; }

        [JsonPropertyName("extra_body")]
        public string? ExtraBody { get; set; }

        [JsonPropertyName("multiple")]
        public bool Multiple { get; set; }

        [JsonPropertyName("multiple_options")]
        public List<string>? MultipleOptions { get; set; }

        [JsonPropertyName("options")]
        public Dictionary<string, string> Options { get; set; } = [];

        [JsonPropertyName("answer")]
        public string? Answer { get; set; }

        [JsonPropertyName("explain")]
        public string? Explain { get; set; }
    }
}
