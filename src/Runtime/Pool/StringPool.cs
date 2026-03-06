using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraScript.Runtime.Pool
{
    using AuroraScript.Runtime.Types;
    using System.Collections.Concurrent;
    using System.Runtime.CompilerServices;

    internal sealed class StringPool
    {
        internal static readonly StringPool Instance = new StringPool();


        private readonly ConcurrentDictionary<string, WeakReference<StringValue>> _dict = new(StringComparer.Ordinal);

        // 清理计数（无锁）
        private int _allocCounter;
        private const int CleanupInterval = 1024;

        private const int MaxPooledStringLength = 128;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringValue Allocation(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            // Extremely long strings often have low reuse and increase dictionary pressure.
            if (key.Length == 0 || key.Length > MaxPooledStringLength)
            {
                return new StringValue(key);
            }

            if (_dict.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var cached))
            {
                return cached;
            }

            var newValue = new StringValue(key);
            _dict.AddOrUpdate(
                key,
                static (_, v) => new WeakReference<StringValue>(v),
                static (_, oldRef, v) =>
                {
                    if (oldRef.TryGetTarget(out _)) return oldRef;
                    return new WeakReference<StringValue>(v);
                },
                newValue
            );

            if ((Interlocked.Increment(ref _allocCounter) & (CleanupInterval - 1)) == 0)
            {
                _ = Task.Run(CleanupDeadEntries);
            }

            return newValue;
        }

        /// <summary>
        /// 后台清理失效弱引用（非阻塞）
        /// </summary>
        private void CleanupDeadEntries()
        {
            foreach (var (key, weakRef) in _dict)
            {
                if (!weakRef.TryGetTarget(out _))
                {
                    _dict.TryRemove(key, out _);
                }
            }
        }

        public int Count => _dict.Count;

        public void Clear()
        {
            _dict.Clear();
            _allocCounter = 0;
        }
    }

}
