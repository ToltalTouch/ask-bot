using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ML_2025.Models;
using ML_2025.Services;
using System.Text.Json;

namespace ML_2025.Pages
{
    public class TriviaManagementModel : PageModel
    {
        private readonly TriviaService _triviaService;
        private readonly QuizService _quizService;

        public TriviaManagementModel(TriviaService triviaService, QuizService quizService)
        {
            _triviaService = triviaService;
            _quizService = quizService;
        }

        public void OnGet()
        {
            // Aqui você pode adicionar lógica para inicializar a página, se necessário.
        }

        // lang: "pt" (padrão) retorna campos em português quando disponíveis; "en" retorna os campos originais em inglês
        public async Task<IActionResult> OnGetFetchQuestions(int amount = 10, string? category = null, string? difficulty = null, string? lang = "pt")
        {
            try
            {
                var questions = await _triviaService.GetTriviaBooleanQuestions(amount, category, difficulty);
                // Se o usuário pediu português (padrão), mapear para os campos traduzidos (quando existentes)
                if (string.IsNullOrEmpty(lang) || lang.ToLower() == "pt")
                {
                    var pt = questions.Select(q => new
                    {
                        category = string.IsNullOrEmpty(q.CategoryPt) ? q.Category : q.CategoryPt,
                        type = q.Type,
                        difficulty = q.Difficulty,
                        question = string.IsNullOrEmpty(q.QuestionPt) ? q.Question : q.QuestionPt,
                        correctAnswer = string.IsNullOrEmpty(q.CorrectAnswerPt) ? q.CorrectAnswer : q.CorrectAnswerPt,
                        incorrectAnswers = q.IncorrectAnswers,
                        curiosidade = q.Curiosidade
                    }).ToList();

                    return new JsonResult(pt);
                }

                // Caso contrário, retornar os campos originais (inglês)
                return new JsonResult(questions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostSaveQuestion([FromBody] TriviaQuestion question)
        {
            try
            {
                if (question == null)
                    return BadRequest(new { error = "Pergunta inválida" });

                var success = _triviaService.SaveToDataset(question);
                
                if (success)
                {
                    // Recarregar o QuizService para incluir a nova pergunta
                    _quizService.ReloadFacts();
                    return new JsonResult(new { success = true, message = "Pergunta salva com sucesso!" });
                }
                else
                {
                    return BadRequest(new { error = "Erro ao salvar pergunta no dataset" });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
