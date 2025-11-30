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
    private readonly QuizService _quizService;
    private readonly FeedbackService _feedbackService;

    public AskBotModel(QuizService quizService, FeedbackService feedbackService)
    {
        _quizService = quizService;
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

            // Buscar resposta no dataset
            var result = _quizService.SearchFactByQuestion(question);

            string curiosity;
            bool answer;
            double confidence;
            string source;

            if (result != null)
            {
                // Encontrou no dataset
                answer = result.Verdadeiro;
                curiosity = result.Curiosidade;
                confidence = 1.0;
                source = "dataset";
            }
            else
            {
                // Não encontrou - usar heurística ou ML simples
                (answer, confidence) = AnalyzeQuestionWithHeuristics(question);
                curiosity = "Não encontrei essa informação no meu banco de dados. Esta é uma análise baseada em padrões!";
                source = "ml";
            }

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

    private (bool answer, double confidence) AnalyzeQuestionWithHeuristics(string question)
    {
        // Normalizar pergunta
        var normalizedQuestion = question.ToLower().Trim();

        // Palavras-chave que geralmente indicam FALSO
        var falseKeywords = new[]
        {
            "alienígena", "alien", "100%", "sempre", "nunca", "impossível",
            "chifre" // vikings com chifres
        };

        // Palavras-chave que geralmente indicam VERDADEIRO
        var trueKeywords = new[]
        {
            "terra", "redonda", "esférica", "gira", "sol",
            "lua", "satélite", "natural"
        };

        // Padrões específicos conhecidos como FALSOS
        if (normalizedQuestion.Contains("viking") && normalizedQuestion.Contains("chifre"))
            return (false, 0.85);
        
        if (normalizedQuestion.Contains("muralha") && normalizedQuestion.Contains("china") && normalizedQuestion.Contains("espaço"))
            return (false, 0.90);

        if (normalizedQuestion.Contains("einstein") && normalizedQuestion.Contains("matemática") && normalizedQuestion.Contains("ruim"))
            return (false, 0.85);

        if (normalizedQuestion.Contains("10%") && normalizedQuestion.Contains("cérebro"))
            return (false, 0.90);

        // Padrões específicos conhecidos como VERDADEIROS
        if (normalizedQuestion.Contains("terra") && (normalizedQuestion.Contains("redonda") || normalizedQuestion.Contains("esférica")))
            return (true, 0.95);

        if (normalizedQuestion.Contains("terra") && normalizedQuestion.Contains("sol") && normalizedQuestion.Contains("gira"))
            return (true, 0.95);

        // Análise por palavras-chave
        int falseScore = falseKeywords.Count(k => normalizedQuestion.Contains(k));
        int trueScore = trueKeywords.Count(k => normalizedQuestion.Contains(k));

        if (falseScore > trueScore)
            return (false, 0.60 + (falseScore * 0.05));
        
        if (trueScore > falseScore)
            return (true, 0.60 + (trueScore * 0.05));

        // Se não conseguir determinar, retornar com baixa confiança
        return (true, 0.50);
    }
}
