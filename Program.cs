using Microsoft.Extensions.ML;
using Microsoft.ML;
using ML_2025.Models;
using ML_2025.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();

// Registrar o serviço do Quiz
builder.Services.AddSingleton<QuizService>();

// Registrar o serviço de Feedback
builder.Services.AddSingleton<FeedbackService>();

// Registrar o serviço de Trivia com HttpClient
builder.Services.AddHttpClient<TriviaService>();

var pastaModelos = Path.Combine(AppContext.BaseDirectory, "MLModels");
if (!File.Exists(Path.Combine(pastaModelos, "model.zip")))
    ModelBuilder.Treinar(pastaModelos);

var modelPath = Path.Combine(pastaModelos, "model.zip");

builder.Services.AddPredictionEnginePool<SentimentData, SentimentPrediction>()
    .FromFile(modelName: "SentimentAnalysisModel", filePath: modelPath, watchForChanges: true);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages(); 

app.MapPost("/predict", (PredictRequest request, TriviaService triviaService) =>
{
    var prediction = triviaService.Predict(request);
    return Results.Ok(prediction);
});

app.Run();