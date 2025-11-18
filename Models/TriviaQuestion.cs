namespace ML_2025.Models
{
    public class TriviaQuestion
    {
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // boolean or multiple
        public string Difficulty { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public List<string> IncorrectAnswers { get; set; } = new List<string>();
        
        // Campos traduzidos
        public string QuestionPt { get; set; } = string.Empty;
        public string CorrectAnswerPt { get; set; } = string.Empty;
        public string CategoryPt { get; set; } = string.Empty;
        
        // Para salvar no dataset
        public string Curiosidade { get; set; } = string.Empty;
    }

    public class TriviaApiResponse
    {
        public int ResponseCode { get; set; }
        public List<TriviaApiQuestion> Results { get; set; } = new List<TriviaApiQuestion>();
    }

    public class TriviaApiQuestion
    {
        public string Category { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Correct_Answer { get; set; } = string.Empty;
        public List<string> Incorrect_Answers { get; set; } = new List<string>();
    }
}
