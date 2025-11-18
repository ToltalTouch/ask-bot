using Microsoft.AspNetCore.Mvc.RazorPages;
using ML_2025.Models;
using ML_2025.Services;

public class FeedbackPageModel : PageModel
{
    private readonly FeedbackService _feedbackService;

    public FeedbackStatistics Stats { get; set; } = new FeedbackStatistics();
    public List<FeedbackData> RecentFeedback { get; set; } = new List<FeedbackData>();

    public FeedbackPageModel(FeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    public void OnGet()
    {
        // Carregar estatísticas
        Stats = _feedbackService.GetStatistics();

        // Carregar últimas 10 requisições
        var allFeedback = _feedbackService.GetAllFeedback();
        RecentFeedback = allFeedback
            .OrderByDescending(f => f.Timestamp)
            .Take(10)
            .ToList();
    }
}
