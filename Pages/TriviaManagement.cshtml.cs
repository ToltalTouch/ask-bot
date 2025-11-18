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
        }

        public async Task<IActionResult> OnGetFetchQuestions(int amount = 10, string? category = null, string? difficulty = null)
        {
            try
            {
                var questions = await _triviaService.GetTriviaBooleanQuestions(amount, category, difficulty);
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
