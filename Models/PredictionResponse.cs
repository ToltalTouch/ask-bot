namespace ML_2025.Models
{
    public class PredictionResponse
    {

        public bool Prediction { get; set; }
        public float Score { get; set; }

        public string Sentiment => Prediction ? "Positivo " : "Negativo ";
        public bool IsPositive => Prediction;
    }
}
