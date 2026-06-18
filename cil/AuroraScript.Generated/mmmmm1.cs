using System;
using AuroraScript.Runtime;

public sealed class mmmmm1
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Location = 6201580477L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("qwertyuiop"));
	}
}
