using Microsoft.ML;
using ML_2025.Models;
using ML_2025.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddRazorPages();

// Registrar o serviço do Quiz
builder.Services.AddSingleton<QuizService>();

// Registrar o serviço de Feedback
builder.Services.AddSingleton<FeedbackService>();

var pastaModelos = Path.Combine(AppContext.BaseDirectory, "MLModels");
if (!File.Exists(Path.Combine(pastaModelos, "model.zip")))
    ModelBuilder.Treinar(pastaModelos);

var mlContext = new MLContext();
var modelPath = Path.Combine(pastaModelos, "model.zip");
var model = mlContext.Model.Load(modelPath, out _);
var engine = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);


builder.Services.AddSingleton(engine);

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

app.MapPost("/predict", (PredictRequest request, PredictionEngine<SentimentData, SentimentPrediction> engine) =>
{
    var prediction = engine.Predict(new SentimentData { Text = request.Text });
    return Results.Ok(prediction);
});

app.Run();