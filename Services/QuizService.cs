using ML_2025.Models;
using System.Globalization;
using System.IO;
using System.Text;

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
            var filePath = Path.Combine(_env.WebRootPath, "data", "quiz_data.csv");

            if (!File.Exists(filePath))
            {
                // Se o arquivo de quiz não existir, podemos opcionalmente carregar do sentiment.csv como fallback, sem curiosidades.
                LoadFactsFromSentimentCsv();
                return;
            }

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                // Pular o cabeçalho
                if (!reader.EndOfStream)
                {
                    reader.ReadLine();
                }

                int id = 1;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var parts = ParseCsvLine(line);

                    if (parts.Length >= 3)
                    {
                        // Converter "Verdadeiro" ou "Falso" para booleano
                        bool isTrue = parts[0].Equals("Verdadeiro", StringComparison.OrdinalIgnoreCase) 
                            || parts[0].Equals("True", StringComparison.OrdinalIgnoreCase)
                            || parts[0] == "1";
                        
                        _facts.Add(new HistoricalFact
                        {
                            Id = id++,
                            FatoHistorico = parts[1],
                            Verdadeiro = isTrue,
                            Curiosidade = parts[2]
                        });
                    }
                }
            }
        }

        private void LoadFactsFromSentimentCsv()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "MLModels", "sentiment.csv");
            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(new[] { ',' }, 2);
                if (parts.Length == 2)
                {
                    var text = parts[1].Trim();
                    if (text.StartsWith("\"") && text.EndsWith("\""))
                    {
                        text = text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
                    }

                    // Adiciona apenas fatos que parecem ser históricos (mais de 5 palavras)
                    if (text.Split(' ').Length > 5)
                    {
                        _facts.Add(new HistoricalFact
                        {
                            Id = i,
                            FatoHistorico = text,
                            Verdadeiro = parts[0].Trim() == "1",
                            Curiosidade = "" // Sem curiosidade neste arquivo
                        });
                    }
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
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Aspas duplas escapadas
                        currentField += '"';
                        i++;
                    }
                    else
                    {
                        // Abre/fecha aspas
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.Trim());
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            
            result.Add(currentField.Trim());
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
