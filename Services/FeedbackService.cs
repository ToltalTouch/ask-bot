using ML_2025.Models;
using System.Globalization;
using System.Text;

namespace ML_2025.Services
{
    public class FeedbackService
    {
        private readonly IWebHostEnvironment _env;
        private readonly string _allFeedbackPath;
        private readonly string _usefulFeedbackPath;
        private readonly object _lockObject = new object();

        public FeedbackService(IWebHostEnvironment env)
        {
            _env = env;
            
            var feedbackFolder = Path.Combine(_env.WebRootPath, "feedback");
            if (!Directory.Exists(feedbackFolder))
            {
                Directory.CreateDirectory(feedbackFolder);
            }

            _allFeedbackPath = Path.Combine(feedbackFolder, "all_feedback.csv");
            _usefulFeedbackPath = Path.Combine(feedbackFolder, "useful_feedback.csv");

            // Criar arquivos com cabeçalho se não existirem
            InitializeCsvFiles();
        }

        private void InitializeCsvFiles()
        {
            lock (_lockObject)
            {
                if (!File.Exists(_allFeedbackPath))
                {
                    File.WriteAllText(_allFeedbackPath, GetCsvHeader(), Encoding.UTF8);
                }

                if (!File.Exists(_usefulFeedbackPath))
                {
                    File.WriteAllText(_usefulFeedbackPath, GetCsvHeader(), Encoding.UTF8);
                }
            }
        }

        private string GetCsvHeader()
        {
            return "Timestamp,Question,Answer,Curiosity,Confidence,Source,IsUseful,UserFeedbackComment\n";
        }

        public void SaveFeedback(FeedbackData feedback)
        {
            lock (_lockObject)
            {
                try
                {
                    var csvLine = ConvertToCsvLine(feedback);

                    // Sempre salvar em all_feedback.csv
                    File.AppendAllText(_allFeedbackPath, csvLine + "\n", Encoding.UTF8);

                    // Se foi marcado como útil, salvar também em useful_feedback.csv
                    if (feedback.IsUseful == true)
                    {
                        File.AppendAllText(_usefulFeedbackPath, csvLine + "\n", Encoding.UTF8);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao salvar feedback: {ex.Message}");
                }
            }
        }

        private string ConvertToCsvLine(FeedbackData feedback)
        {
            // Usar ponto como separador decimal em vez de vírgula
            var confidenceStr = feedback.Confidence.ToString("F2", CultureInfo.InvariantCulture);
            
            return $"{EscapeCsvField(feedback.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))}," +
                   $"{EscapeCsvField(feedback.Question)}," +
                   $"{feedback.Answer}," +
                   $"{EscapeCsvField(feedback.Curiosity)}," +
                   $"{confidenceStr}," +
                   $"{EscapeCsvField(feedback.Source)}," +
                   $"{(feedback.IsUseful?.ToString() ?? "null")}," +
                   $"{EscapeCsvField(feedback.UserFeedbackComment)}";
        }

        private string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            // Se contém vírgula, aspas ou quebra de linha, envolver em aspas e escapar aspas internas
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return "\"" + field + "\"";
        }

        public List<FeedbackData> GetAllFeedback()
        {
            return ReadFeedbackFromFile(_allFeedbackPath);
        }

        public List<FeedbackData> GetUsefulFeedback()
        {
            return ReadFeedbackFromFile(_usefulFeedbackPath);
        }

        private List<FeedbackData> ReadFeedbackFromFile(string filePath)
        {
            var feedbackList = new List<FeedbackData>();

            lock (_lockObject)
            {
                try
                {
                    if (!File.Exists(filePath))
                        return feedbackList;

                    var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                    
                    // Pular cabeçalho
                    for (int i = 1; i < lines.Length; i++)
                    {
                        try
                        {
                            var parts = ParseCsvLine(lines[i]);
                            if (parts.Length >= 7)
                            {
                                feedbackList.Add(new FeedbackData
                                {
                                    Timestamp = DateTime.Parse(parts[0]),
                                    Question = parts[1],
                                    Answer = bool.Parse(parts[2]),
                                    Curiosity = parts[3],
                                    Confidence = double.Parse(parts[4], CultureInfo.InvariantCulture),
                                    Source = parts[5],
                                    IsUseful = parts[6] == "null" ? null : bool.Parse(parts[6]),
                                    UserFeedbackComment = parts.Length > 7 ? parts[7] : string.Empty
                                });
                            }
                        }
                        catch (Exception lineEx)
                        {
                            Console.WriteLine($"Erro ao processar linha {i}: {lineEx.Message}");
                            // Continua processando as outras linhas
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao ler feedback: {ex.Message}");
                }
            }

            return feedbackList;
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Aspas duplas escapadas
                        currentField.Append('"');
                        i++; // Pular próxima aspa
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result.ToArray();
        }

        public FeedbackStatistics GetStatistics()
        {
            var allFeedback = GetAllFeedback();
            var usefulFeedback = GetUsefulFeedback();

            return new FeedbackStatistics
            {
                TotalRequests = allFeedback.Count,
                UsefulRequests = usefulFeedback.Count,
                NotUsefulRequests = allFeedback.Count(f => f.IsUseful == false),
                NoFeedbackRequests = allFeedback.Count(f => f.IsUseful == null),
                AverageConfidence = allFeedback.Any() ? allFeedback.Average(f => f.Confidence) : 0,
                DatasetSourceCount = allFeedback.Count(f => f.Source == "dataset"),
                MlSourceCount = allFeedback.Count(f => f.Source == "ml")
            };
        }
    }

    public class FeedbackStatistics
    {
        public int TotalRequests { get; set; }
        public int UsefulRequests { get; set; }
        public int NotUsefulRequests { get; set; }
        public int NoFeedbackRequests { get; set; }
        public double AverageConfidence { get; set; }
        public int DatasetSourceCount { get; set; }
        public int MlSourceCount { get; set; }
    }
}
