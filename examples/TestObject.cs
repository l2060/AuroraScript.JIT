using System;

namespace Examples
{
    public class TestObject
    {
        public string Name { get; set; } = "*";

        public void Say(int n, string s)
        {
            //Console.WriteLine($"Say[{n}]: {s} ({Name})");
        }

        public static String Cat(String[] strings)
        {
            return $"Static Eat: [{String.Join(",", strings)}]";
        }

        public static String Cat(String left, String middle, String right)
        {
            return String.Concat("Static Eat: [", left, ",", middle, ",", right, "]");
        }
    }
}
