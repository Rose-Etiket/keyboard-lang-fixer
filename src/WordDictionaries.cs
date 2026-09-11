namespace KeyboardLangFixer;

internal sealed class WordDictionaries
{
    private readonly HashSet<string> _english = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _persian = new(StringComparer.Ordinal);

    public int EnglishCount => _english.Count;
    public int PersianCount => _persian.Count;

    public void Load(string dataDirectory)
    {
        LoadInto(_english, Path.Combine(dataDirectory, "en_words.txt"), NormalizeEnglish);
        LoadInto(_persian, Path.Combine(dataDirectory, "fa_words.txt"), NormalizePersian);
    }

    private static void LoadInto(HashSet<string> set, string path, Func<string, string> normalize)
    {
        if (!File.Exists(path)) return;

        foreach (var raw in File.ReadLines(path, System.Text.Encoding.UTF8))
        {
            var word = normalize(raw.Trim());
            if (word.Length > 0) set.Add(word);
        }
    }

    public static string NormalizeEnglish(string s) => s.ToLowerInvariant();

    public static string NormalizePersian(string s) => s
        .Replace('ي', 'ی')  // Arabic Yeh -> Persian Yeh
        .Replace('ك', 'ک'); // Arabic Kaf -> Persian Keheh

    public bool IsValid(AppLanguage language, string word)
    {
        if (word.Length < 2) return false; // avoid false positives on single letters

        return language switch
        {
            AppLanguage.English => _english.Contains(NormalizeEnglish(word)),
            AppLanguage.Persian => _persian.Contains(NormalizePersian(word)),
            _ => false
        };
    }
}
