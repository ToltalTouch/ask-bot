namespace ML_2025.Models
{
    public class FeedbackData
    {
        public DateTime Timestamp { get; set; }
        public string Question { get; set; } = string.Empty;
        public bool Answer { get; set; }
        public string Curiosity { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool? IsUseful { get; set; } // null = sem feedback, true = útil, false = não útil
        public string UserFeedbackComment { get; set; } = string.Empty;
    }
}
