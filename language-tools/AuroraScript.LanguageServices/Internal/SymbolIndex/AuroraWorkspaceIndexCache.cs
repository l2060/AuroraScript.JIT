using AuroraScript.Core;
using System;
using System.Collections.Generic;

namespace AuroraScript.LanguageServices.Internal.SymbolIndex;

internal sealed class AuroraWorkspaceIndexCache
{
    private const int MaxEntries = 256;
    private readonly Dictionary<string, CacheEntry> _entries = new(ScriptPath.Comparer);

    public bool TryGet(string path, string text, out AuroraModuleIndex module)
    {
        path = ScriptPath.NormalizeFullPath(path);
        if (_entries.TryGetValue(path, out var entry) &&
            entry.Matches(text))
        {
            entry.Touch();
            module = entry.Module;
            return true;
        }

        module = null!;
        return false;
    }

    public void Store(string path, string text, AuroraModuleIndex module)
    {
        path = ScriptPath.NormalizeFullPath(path);
        if (_entries.Count >= MaxEntries && !_entries.ContainsKey(path))
        {
            EvictLeastRecentlyUsed();
        }

        _entries[path] = new CacheEntry(text, module);
    }

    private void EvictLeastRecentlyUsed()
    {
        string? path = null;
        long oldest = long.MaxValue;
        foreach (var pair in _entries)
        {
            if (pair.Value.LastUsed < oldest)
            {
                oldest = pair.Value.LastUsed;
                path = pair.Key;
            }
        }

        if (path != null)
        {
            _entries.Remove(path);
        }
    }

    private sealed class CacheEntry
    {
        private readonly int _textLength;

        public CacheEntry(string text, AuroraModuleIndex module)
        {
            _textLength = text.Length;
            Module = module;
            LastUsed = StopwatchTicks();
        }

        public AuroraModuleIndex Module { get; }

        public long LastUsed { get; private set; }

        public bool Matches(string text)
        {
            return _textLength == text.Length &&
                string.Equals(Module.Text, text, StringComparison.Ordinal);
        }

        public void Touch()
        {
            LastUsed = StopwatchTicks();
        }

        private static long StopwatchTicks()
        {
            return System.Diagnostics.Stopwatch.GetTimestamp();
        }
    }
}
