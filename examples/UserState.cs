using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

namespace Examples
{
    internal class UserState : ScriptObject
    {
        public UserState()
        {
            Define("Name", StringValue.Of("Hanks"));
            Define("Identity", ScriptObject.Null);
            Define("Nick", StringValue.Of("Bpp"));
            Define("Age", NumberValue.Of(18));
            Define("Context", ScriptObject.Null);
        }

        public void Test(double offset, string str)
        {
        }
    }
}
