using System;
using System.Collections.Generic;
using System.Globalization;

namespace AuroraScript.LanguageServices.Builtins;

public sealed class BuiltinApiDocumentation
{
    private readonly Dictionary<string, IReadOnlyList<string>> _notesByLanguage;

    public BuiltinApiDocumentation(IReadOnlyDictionary<string, IReadOnlyList<string>> notesByLanguage)
    {
        _notesByLanguage = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        if (notesByLanguage != null)
        {
            foreach (var pair in notesByLanguage)
            {
                _notesByLanguage[pair.Key] = pair.Value ?? Array.Empty<string>();
            }
        }
    }

    public static BuiltinApiDocumentation Empty { get; } = new(
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

    public IReadOnlyList<string> GetNotes(string? locale)
    {
        if (TryGetNotes(locale, out var notes))
        {
            return notes;
        }

        if (TryGetNotes("en", out notes))
        {
            return notes;
        }

        if (TryGetNotes("zh-CN", out notes))
        {
            return notes;
        }

        foreach (var pair in _notesByLanguage)
        {
            if (pair.Value.Count != 0)
            {
                return pair.Value;
            }
        }

        return Array.Empty<string>();
    }

    private bool TryGetNotes(string? locale, out IReadOnlyList<string> notes)
    {
        notes = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(locale))
        {
            return false;
        }

        if (_notesByLanguage.TryGetValue(locale, out notes!) && notes.Count != 0)
        {
            return true;
        }

        try
        {
            var culture = CultureInfo.GetCultureInfo(locale);
            if (_notesByLanguage.TryGetValue(culture.Name, out notes!) && notes.Count != 0)
            {
                return true;
            }

            if (_notesByLanguage.TryGetValue(culture.TwoLetterISOLanguageName, out notes!) && notes.Count != 0)
            {
                return true;
            }
        }
        catch (CultureNotFoundException)
        {
        }

        return false;
    }
}
