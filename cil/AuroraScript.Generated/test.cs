using System;
using AuroraScript.Runtime;

public sealed class test
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromBoolean(true);
		ScriptDatum scriptDatum2 = ScriptDatum.FromBoolean(false);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum5 = ScriptDatum.FromNumber(3.0);
		ScriptDatum @null = ScriptDatum.Null;
		A_0.Location = 13414387758L;
		CILHelper.InvokeProperty(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", new ScriptDatum[]
		{
			scriptDatum,
			scriptDatum2,
			scriptDatum3,
			scriptDatum4,
			scriptDatum5,
			@null
		});
		A_0.Location = 17709355054L;
		CILHelper.InvokeProperty(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", new ScriptDatum[]
		{
			scriptDatum,
			scriptDatum2,
			scriptDatum3,
			scriptDatum4,
			scriptDatum5,
			@null
		});
		A_0.Location = 22004322350L;
		CILHelper.InvokeProperty(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", new ScriptDatum[]
		{
			scriptDatum,
			scriptDatum2,
			scriptDatum3,
			scriptDatum4,
			scriptDatum5,
			@null
		});
		A_0.Location = 26299289646L;
		CILHelper.InvokeProperty(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", new ScriptDatum[]
		{
			scriptDatum,
			scriptDatum2,
			scriptDatum3,
			scriptDatum4,
			scriptDatum5,
			@null
		});
	}
}
