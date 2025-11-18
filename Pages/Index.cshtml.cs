using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.ML;
using ML_2025.Models;
using ML_2025.Services;
using System.Text.Json;

public class IndexModel : PageModel
{
    private readonly QuizService _quizService;

    public IndexModel(QuizService quizService)
    {
        _quizService = quizService;
    }

    public void OnGet()
    {
    }

    public IActionResult OnGetNextQuestion()
    {
        try
        {
            var fact = _quizService.GetRandomFact();
            
            var question = new
            {
                id = fact.Id,
                text = fact.FatoHistorico,
                answer = fact.Verdadeiro,
                curiosity = fact.Curiosidade
            };

            return new JsonResult(question);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public IActionResult OnGetQuestions(int count = 10)
    {
        try
        {
            var facts = _quizService.GetRandomFacts(count);
            
            var questions = facts.Select(f => new
            {
                id = f.Id,
                text = f.FatoHistorico,
                answer = f.Verdadeiro,
                curiosity = f.Curiosidade
            }).ToList();

            return new JsonResult(questions);
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }

    public IActionResult OnGetStats()
    {
        try
        {
            var totalFacts = _quizService.GetTotalFactsCount();
            return new JsonResult(new { totalFacts });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { error = ex.Message }) { StatusCode = 500 };
        }
    }
}