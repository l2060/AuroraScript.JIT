using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace Examples
{
    [NativeType("UserState")]
    public sealed partial class UserState : ScriptObject
    {
        [Export("x")] public double X;
        [Export("y")] public double Y;
        [Export("name")] public String Name = "Hanks";
        [Export("identity")] public String Identity = null;
        [Export("age")] public int Age = 18;



        [Export("test")]
        public String Test(double offset, string str)
        {
            return str + offset;
        }



    }
}
