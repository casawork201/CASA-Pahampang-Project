using CASAPahampang.Interfaces;

namespace CASAPahampang.Services;

public class ContentModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private HashSet<string> _bannedWords = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _bannedPhrases = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLoaded = false;
    private readonly SemaphoreSlim _lock = new(1, 1);

    // Guaranteed local fallback list for Tagalog & English profanities 🇵🇭🇺🇸
    private readonly string[] _defaultBannedTerms = {
        "gago", "putangina", "putang ina", "tanga", "bobo", "ulol", 
        "punyeta", "leche", "yawa", "kupal", "tarantado", "hindot"
    };

    public ContentModerationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Seed local terms immediately upon instantiation
        foreach (var term in _defaultBannedTerms)
        {
            if (term.Contains(' '))
                _bannedPhrases.Add(term);
            else
                _bannedWords.Add(term);
        }
    }

    public async Task InitializeAsync()
    {
        if (_isLoaded) return;

        await _lock.WaitAsync();
        try
        {
            if (_isLoaded) return;

            string[] urls = {
                "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/en",
                "https://raw.githubusercontent.com/LDNOOBW/List-of-Dirty-Naughty-Obscene-and-Otherwise-Bad-Words/master/fil"
            };

            foreach (var url in urls)
            {
                try
                {
                    var content = await _httpClient.GetStringAsync(url);
                    var words = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (var word in words)
                    {
                        var trimmed = word.Trim();
                        if (!string.IsNullOrEmpty(trimmed))
                        {
                            if (trimmed.Contains(' '))
                                _bannedPhrases.Add(trimmed);
                            else
                                _bannedWords.Add(trimmed);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Could not load word list from {url}: {ex.Message}");
                }
            }

            _isLoaded = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool IsFlagged(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;

        var lowerText = message.ToLowerInvariant();

        // 1. Check multi-word phrases (e.g., "putang ina") anywhere in the string 🛡️
        foreach (var phrase in _bannedPhrases)
        {
            if (lowerText.Contains(phrase.ToLowerInvariant()))
            {
                return true;
            }
        }

        // 2. Tokenize and check individual words (e.g., "gago") 🔍
        var words = lowerText.Split(new[] { ' ', '.', ',', '!', '?', '-', '_', '*', '@' }, StringSplitOptions.RemoveEmptyEntries);
        
        return words.Any(word => _bannedWords.Contains(word));
    }
}