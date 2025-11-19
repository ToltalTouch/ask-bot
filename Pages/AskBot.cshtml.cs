using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ML_2025.Models;
using ML_2025.Services;
using System.Text.RegularExpressions;

public class FeedbackRequest
{
    public DateTime Timestamp { get; set; }
    public string Question { get; set; } = string.Empty;
    public bool Answer { get; set; }
    public string Curiosity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsUseful { get; set; }
    public string? Comment { get; set; }
}

[IgnoreAntiforgeryToken]
public class AskBotModel : PageModel
{
    private readonly TriviaService _triviaService;
    private readonly FeedbackService _feedbackService;

    public AskBotModel(TriviaService triviaService, FeedbackService feedbackService)
    {
        _triviaService = triviaService;
        _feedbackService = feedbackService;
    }

    public void OnGet()
    {
    }

    public IActionResult OnGetAsk(string question)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new JsonResult(new { error = "Pergunta vazia" }) { StatusCode = 400 };
            }

            // Usar o serviço de Trivia para obter a predição
            var predictionResult = _triviaService.Predict(new PredictRequest { Text = question });

            string curiosity = "Esta é uma análise baseada em nosso modelo de Machine Learning.";
            bool answer = predictionResult.Prediction;
            double confidence = predictionResult.Score;
            string source = "ml";

            // Salvar a requisição em all_feedback.csv (sem feedback ainda)
            var feedbackData = new FeedbackData
            {
                Timestamp = DateTime.Now,
                Question = question,
                Answer = answer,
                Curiosity = curiosity,
                Confidence = confidence,
                Source = source,
                IsUseful = null // Sem feedback ainda
            };

            _feedbackService.SaveFeedback(feedbackData);

            return new JsonResult(new
            {
                answer = answer,
                curiosity = curiosity,
                confidence = confidence,
                source = source,
                timestamp = feedbackData.Timestamp
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public IActionResult OnPostFeedback([FromBody] FeedbackRequest request)
    {
        try
        {
            Console.WriteLine($"Recebendo feedback: {request?.Question}");
            
            if (request == null || string.IsNullOrWhiteSpace(request.Question))
            {
                Console.WriteLine("Erro: Dados inválidos");
                return new JsonResult(new { success = false, error = "Dados inválidos" }) { StatusCode = 400 };
            }

            // Salvar feedback atualizado
            var feedbackData = new FeedbackData
            {
                Timestamp = request.Timestamp,
                Question = request.Question,
                Answer = request.Answer,
                Curiosity = request.Curiosity,
                Confidence = request.Confidence,
                Source = request.Source,
                IsUseful = request.IsUseful,
                UserFeedbackComment = request.Comment ?? string.Empty
            };

            _feedbackService.SaveFeedback(feedbackData);
            Console.WriteLine($"Feedback salvo com sucesso! Útil: {request.IsUseful}");

            return new JsonResult(new { success = true, message = "Feedback registrado com sucesso!" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao salvar feedback: {ex.Message}");
            return new JsonResult(new { success = false, error = ex.Message }) { StatusCode = 500 };
        }
    }

    public IActionResult OnGetStatistics()
    {
        try
        {
            var stats = _feedbackService.GetStatistics();
            return new JsonResult(stats);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}
