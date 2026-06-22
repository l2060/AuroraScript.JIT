using System;

namespace AuroraBenchmark
{
    public sealed class HostObject
    {
        public string Name { get; set; } = "Aurora";

        public int Count { get; set; }

        public void Say(int value, string text)
        {
            Count += value;
            Name = text;
        }

        public static string Cat(string left, string middle, string right)
        {
            return string.Concat(left, middle, right);
        }

        public static string CatArray(string[] values)
        {
            return string.Concat(values);
        }
    }
}
