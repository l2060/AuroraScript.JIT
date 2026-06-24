using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

namespace Examples
{
    internal class UserState : ScriptObject
    {
        public UserState()
        {
            Define("Name", ScriptDatum.FromString("Hanks"));
            Define("Identity", ScriptDatum.Null);
            Define("Nick", ScriptDatum.FromString("Bpp"));
            Define("Age", ScriptDatum.FromNumber(18));
            Define("Context", ScriptDatum.Null);
        }

        public void Test(double offset, string str)
        {
        }
    }
}
