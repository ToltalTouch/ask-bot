using ML_2025.Models;
using Newtonsoft.Json;
using System.Text;
using System.Web;

namespace ML_2025.Services
{
    public class TriviaService
    {
        private readonly HttpClient _httpClient;
        private readonly IWebHostEnvironment _env;
        private readonly string _datasetPath;

        // Dicionário de tradução de categorias
        private readonly Dictionary<string, string> _categoryTranslations = new Dictionary<string, string>
        {
            { "General Knowledge", "Conhecimento Geral" },
            { "Entertainment: Books", "Entretenimento: Livros" },
            { "Entertainment: Film", "Entretenimento: Cinema" },
            { "Entertainment: Music", "Entretenimento: Música" },
            { "Entertainment: Television", "Entretenimento: TV" },
            { "Entertainment: Video Games", "Entretenimento: Videogames" },
            { "Entertainment: Board Games", "Entretenimento: Jogos de Tabuleiro" },
            { "Science & Nature", "Ciência e Natureza" },
            { "Science: Computers", "Ciência: Computadores" },
            { "Science: Mathematics", "Ciência: Matemática" },
            { "Mythology", "Mitologia" },
            { "Sports", "Esportes" },
            { "Geography", "Geografia" },
            { "History", "História" },
            { "Politics", "Política" },
            { "Art", "Arte" },
            { "Celebrities", "Celebridades" },
            { "Animals", "Animais" },
            { "Vehicles", "Veículos" }
        };

        public TriviaService(HttpClient httpClient, IWebHostEnvironment env)
        {
            _httpClient = httpClient;
            _env = env;
            _datasetPath = Path.Combine(_env.WebRootPath, "data", "dataset_fatos_historicos_3000.csv");
        }

        public async Task<List<TriviaQuestion>> GetTriviaBooleanQuestions(int amount = 10, string? category = null, string? difficulty = null)
        {
            try
            {
                // API da Open Trivia Database
                var url = $"https://opentdb.com/api.php?amount={amount}&type=boolean";
                
                if (!string.IsNullOrEmpty(category))
                    url += $"&category={category}";
                
                if (!string.IsNullOrEmpty(difficulty))
                    url += $"&difficulty={difficulty}";

                var response = await _httpClient.GetStringAsync(url);
                var apiResponse = JsonConvert.DeserializeObject<TriviaApiResponse>(response);

                if (apiResponse == null || apiResponse.Results == null)
                    return new List<TriviaQuestion>();

                var questions = new List<TriviaQuestion>();

                foreach (var item in apiResponse.Results)
                {
                    var question = new TriviaQuestion
                    {
                        Category = HttpUtility.HtmlDecode(item.Category),
                        Type = item.Type,
                        Difficulty = item.Difficulty,
                        Question = HttpUtility.HtmlDecode(item.Question),
                        CorrectAnswer = item.Correct_Answer,
                        IncorrectAnswers = item.Incorrect_Answers
                    };

                    // Traduzir para português
                    question.QuestionPt = TranslateToPortuguese(question.Question);
                    question.CorrectAnswerPt = question.CorrectAnswer == "True" ? "Verdadeiro" : "Falso";
                    question.CategoryPt = _categoryTranslations.ContainsKey(question.Category) 
                        ? _categoryTranslations[question.Category] 
                        : question.Category;

                    // Gerar curiosidade baseada na categoria
                    question.Curiosidade = $"Categoria: {question.CategoryPt}. Dificuldade: {TranslateDifficulty(question.Difficulty)}.";

                    questions.Add(question);
                }

                return questions;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao buscar perguntas da API: {ex.Message}");
                return new List<TriviaQuestion>();
            }
        }

        private string TranslateToPortuguese(string text)
        {
            // Tradução simples usando dicionário de termos comuns
            // Em produção, use Google Translate API ou DeepL API
            var translations = new Dictionary<string, string>
            {
                // Termos comuns
                { "The ", "A " },
                { "Is ", "É " },
                { "Are ", "São " },
                { "Was ", "Foi " },
                { "Were ", "Foram " },
                { "Has ", "Tem " },
                { "Have ", "Têm " },
                { "Did ", "" },
                { "Do ", "" },
                { "Does ", "" },
                { " is ", " é " },
                { " are ", " são " },
                { " was ", " foi " },
                { " were ", " foram " },
                { " in ", " em " },
                { " on ", " em " },
                { " at ", " em " },
                { " the ", " o " },
                { " a ", " um " },
                { " an ", " um " },
                { " of ", " de " },
                { " for ", " para " },
                { " with ", " com " },
                { " by ", " por " },
                { " from ", " de " },
                { " to ", " para " },
                { " and ", " e " },
                { " or ", " ou " },
                { " but ", " mas " },
                { " not ", " não " },
                { "True", "Verdadeiro" },
                { "False", "Falso" },
                { " capital ", " capital " },
                { " country ", " país " },
                { " city ", " cidade " },
                { " world ", " mundo " },
                { " first ", " primeiro " },
                { " last ", " último " },
                { " largest ", " maior " },
                { " smallest ", " menor " },
                { " highest ", " mais alto " },
                { " longest ", " mais longo " }
            };

            var result = text;
            foreach (var translation in translations)
            {
                result = result.Replace(translation.Key, translation.Value);
            }

            return result;
        }

        private string TranslateDifficulty(string difficulty)
        {
            return difficulty.ToLower() switch
            {
                "easy" => "Fácil",
                "medium" => "Médio",
                "hard" => "Difícil",
                _ => difficulty
            };
        }

        public bool SaveToDataset(TriviaQuestion question)
        {
            try
            {
                var isTrue = question.CorrectAnswer.Equals("True", StringComparison.OrdinalIgnoreCase);
                var line = $"\"{question.QuestionPt}\",\"{(isTrue ? "Verdadeiro" : "Falso")}\",\"{question.Curiosidade}\"";

                // Ler o arquivo existente para pegar o próximo ID
                var lines = File.ReadAllLines(_datasetPath);
                var lastId = 0;
                if (lines.Length > 1)
                {
                    var lastLine = lines[lines.Length - 1];
                    var firstComma = lastLine.IndexOf(',');
                    if (firstComma > 0 && int.TryParse(lastLine.Substring(0, firstComma), out var id))
                    {
                        lastId = id;
                    }
                }

                var newId = lastId + 1;
                var newLine = $"\n{newId},{line}";

                File.AppendAllText(_datasetPath, newLine, Encoding.UTF8);
                
                Console.WriteLine($"Pergunta salva no dataset com ID {newId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar no dataset: {ex.Message}");
                return false;
            }
        }

        public Dictionary<string, string> GetCategories()
        {
            // Retornar categorias com ID da API
            return new Dictionary<string, string>
            {
                { "9", "Conhecimento Geral" },
                { "17", "Ciência e Natureza" },
                { "22", "Geografia" },
                { "23", "História" },
                { "21", "Esportes" },
                { "27", "Animais" },
                { "18", "Ciência: Computadores" },
                { "19", "Ciência: Matemática" },
                { "20", "Mitologia" }
            };
        }
    }
}
