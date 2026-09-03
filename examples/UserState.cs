using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace Examples
{
    [AuroraNativeType("UserState")]
    public sealed partial class UserState : ScriptObject
    {
        [AuroraExport("x")] public double X;
        [AuroraExport("y")] public double Y;
        [AuroraExport("name")] public String Name = "Hanks";
        [AuroraExport("identity")] public String Identity = null;
        [AuroraExport("age")] public int Age = 18;



        [AuroraExport("test")]
        public String Test(double offset, string str)
        {
            return str + offset;
        }



    }
}
