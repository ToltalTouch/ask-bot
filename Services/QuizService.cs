using ML_2025.Models;
using System.Globalization;

namespace ML_2025.Services
{
    public class QuizService
    {
        private readonly ILogger<QuizService> _logger;
        private readonly IWebHostEnvironment _env;
        private List<HistoricalFact> _facts = new List<HistoricalFact>();
        private readonly Random _random = new Random();

        public QuizService(IWebHostEnvironment env, ILogger<QuizService> logger)
        {
            _logger = logger;
            _env = env;
            _logger.LogInformation("Inicializando QuizService");
            LoadFacts();
        }

        private void LoadFacts()
        {
            _facts = new List<HistoricalFact>();
            var filePath = Path.Combine(_env.WebRootPath, "data", "dataset_fatos_historicos_3000.csv");

            _logger.LogInformation("Carregando dataset de: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                _logger.LogError("Arquivo CSV não encontrado: {FilePath}", filePath);
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
                    var parts = ParseCsvLine(line);
                    
                    if (parts.Length >= 4)
                    {
                        _facts.Add(new HistoricalFact
                        {
                            Id = int.Parse(parts[0]),
                            FatoHistorico = parts[1].Trim(),
                            Verdadeiro = parts[2].Trim().Equals("Verdadeiro", StringComparison.OrdinalIgnoreCase),
                            Curiosidade = parts[3].Trim()
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao processar linha {LineNumber} do dataset", i);
                }
            }
            
            _logger.LogInformation("Dataset carregado com sucesso. Total de fatos históricos: {Count}", _facts.Count);
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
            _logger.LogInformation("Buscando fato histórico para pergunta: {Question}", question);
            
            if (_facts == null || _facts.Count == 0 || string.IsNullOrWhiteSpace(question))
            {
                return null;
            }

            var normalizedQuestion = NormalizeText(question);

            // Busca exata ou muito similar
            var exactMatch = _facts.FirstOrDefault(f =>
                NormalizeText(f.FatoHistorico).Equals(normalizedQuestion, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                _logger.LogInformation("Fato encontrado no dataset (match exato). Resposta: {Answer}", exactMatch.Verdadeiro);
                return exactMatch;
            }

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

            if (bestMatch != null)
            {
                _logger.LogInformation("Fato encontrado no dataset (similaridade). Score: {Score:F2}, Resposta: {Answer}", 
                    bestMatch.Score, bestMatch.Fact.Verdadeiro);
            }
            else
            {
                _logger.LogInformation("Nenhum fato encontrado no dataset com similaridade suficiente");
            }

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
