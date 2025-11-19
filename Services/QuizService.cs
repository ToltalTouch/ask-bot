using ML_2025.Models;
using System.Globalization;

namespace ML_2025.Services
{
    public class QuizService
    {
        private readonly IWebHostEnvironment _env;
        private List<HistoricalFact> _facts = new List<HistoricalFact>();
        private readonly Random _random = new Random();

        public QuizService(IWebHostEnvironment env)
        {
            _env = env;
            LoadFacts();
        }

        private void LoadFacts()
        {
            _facts = new List<HistoricalFact>();
            var filePath = Path.Combine(AppContext.BaseDirectory, "MLModels", "sentiment.csv");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Arquivo CSV não encontrado: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);
            
            // Pular o cabeçalho (primeira linha)
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // O formato é Label,Text
                    var parts = line.Split(new[] { ',' }, 2);
                    
                    if (parts.Length == 2)
                    {
                        var text = parts[1].Trim();
                        // Remove aspas do início e do fim, se existirem
                        if (text.StartsWith("\"") && text.EndsWith("\""))
                        {
                            text = text.Substring(1, text.Length - 2).Replace("\"\"", "\""); // Trata aspas duplas escapadas
                        }

                        _facts.Add(new HistoricalFact
                        {
                            Id = i, // Usar o número da linha como ID
                            FatoHistorico = text,
                            Verdadeiro = parts[0].Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || parts[0].Trim().Equals("\"true\"", StringComparison.OrdinalIgnoreCase),
                            Curiosidade = "" // Curiosidade não está mais disponível neste formato
                        });
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue processing
                    Console.WriteLine($"Erro ao processar linha {i}: {ex.Message}");
                }
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var currentField = "";
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField);
            return result.ToArray();
        }

        public HistoricalFact GetRandomFact()
        {
            if (_facts == null || _facts.Count == 0)
            {
                throw new InvalidOperationException("Nenhum fato histórico carregado.");
            }

            int index = _random.Next(_facts.Count);
            return _facts[index];
        }

        public List<HistoricalFact> GetRandomFacts(int count)
        {
            if (_facts == null || _facts.Count == 0)
            {
                throw new InvalidOperationException("Nenhum fato histórico carregado.");
            }

            // Embaralhar e pegar 'count' itens únicos
            return _facts.OrderBy(x => _random.Next()).Take(count).ToList();
        }

        public int GetTotalFactsCount()
        {
            return _facts?.Count ?? 0;
        }

        public void ReloadFacts()
        {
            LoadFacts();
        }

        public HistoricalFact? SearchFactByQuestion(string question)
        {
            if (_facts == null || _facts.Count == 0 || string.IsNullOrWhiteSpace(question))
            {
                return null;
            }

            var normalizedQuestion = NormalizeText(question);

            // Busca exata ou muito similar
            var exactMatch = _facts.FirstOrDefault(f =>
                NormalizeText(f.FatoHistorico).Equals(normalizedQuestion, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;

            // Busca por similaridade (contém)
            var similarMatch = _facts.FirstOrDefault(f =>
                NormalizeText(f.FatoHistorico).Contains(normalizedQuestion) ||
                normalizedQuestion.Contains(NormalizeText(f.FatoHistorico)));

            if (similarMatch != null)
                return similarMatch;

            // Busca por palavras-chave principais
            var keywords = ExtractKeywords(normalizedQuestion);
            var bestMatch = _facts
                .Select(f => new
                {
                    Fact = f,
                    Score = CalculateSimilarityScore(keywords, ExtractKeywords(NormalizeText(f.FatoHistorico)))
                })
                .Where(x => x.Score > 0.5)
                .OrderByDescending(x => x.Score)
                .FirstOrDefault();

            return bestMatch?.Fact;
        }

        private string NormalizeText(string text)
        {
            return text.ToLower()
                .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
                .Replace("é", "e").Replace("ê", "e")
                .Replace("í", "i")
                .Replace("ó", "o").Replace("õ", "o").Replace("ô", "o")
                .Replace("ú", "u").Replace("ü", "u")
                .Replace("ç", "c")
                .Trim();
        }

        private List<string> ExtractKeywords(string text)
        {
            var stopWords = new HashSet<string> { "o", "a", "de", "da", "do", "em", "na", "no", "e", "ou", "um", "uma", "é", "são" };
            
            return text.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .ToList();
        }

        private double CalculateSimilarityScore(List<string> keywords1, List<string> keywords2)
        {
            if (keywords1.Count == 0 || keywords2.Count == 0)
                return 0;

            var matches = keywords1.Intersect(keywords2).Count();
            var total = Math.Max(keywords1.Count, keywords2.Count);

            return (double)matches / total;
        }
    }
}
