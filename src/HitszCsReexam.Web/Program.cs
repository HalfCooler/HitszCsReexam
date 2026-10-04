using HitszCsReexam.Web.Components;
using HitszCsReexam.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSingleton<QuestionCatalog>();
builder.Services.AddScoped<PracticeStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/report/manual.csv", () => Results.File(
    Path.Combine(AppContext.BaseDirectory, "Data", "manual.csv"),
    "text/csv; charset=utf-8", "manual.csv"));
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
