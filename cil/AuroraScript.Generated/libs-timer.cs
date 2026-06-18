using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class libs/timer
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		A_0.Module.Define("md5", A_0.Global.GetModule("MD5_LIB"), false, true);
		A_0.Module.Define("aaa", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(libs/timer.aaa), Array.Empty<Upvalue>(), "aaa"), false, true);
		A_0.Location = 23479920227L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("hello timer"));
		A_0.Location = 27774887523L;
		ScriptObject module = A_0.Module;
		string key = "a";
		ScriptArray scriptArray = new ScriptArray(4);
		scriptArray.SetElementValue(0, scriptDatum);
		scriptArray.SetElementValue(1, ScriptDatum.FromNumber(1.0));
		scriptArray.SetElementValue(2, ScriptDatum.FromNumber(2.0));
		scriptArray.SetElementValue(3, ScriptDatum.FromNumber(3.0));
		module.Define(key, scriptArray, true, true);
		A_0.Location = 32069854819L;
		CILHelper.IncrementElementPostfix(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "a")), scriptDatum);
		A_0.Location = 36364822115L;
		A_0.Module.Define("b", ScriptDatum.ToObject(CILHelper.IncrementElementPostfix(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "a")), scriptDatum)), true, true);
		A_0.Location = 40659789411L;
		CILHelper.IncrementPropertyPostfix(A_0.Module, "b");
		A_0.Location = 44954756707L;
		A_0.Module.Define("c", ScriptDatum.ToObject(CILHelper.IncrementPropertyPostfix(A_0.Module, "b")), true, true);
		A_0.Location = 49249724003L;
		CILHelper.IncrementPropertyPostfix(A_0.Global, "n");
	}

	public static ScriptDatum aaa(ScriptContext A_0)
	{
		ScriptDatum index = ScriptDatum.FromNumber(0.0);
		A_0.Location = 57839658595L;
		CILHelper.IncrementElementPostfix(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "a")), index);
		A_0.Location = 62134625891L;
		ScriptDatum scriptDatum = CILHelper.IncrementElementPostfix(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "a")), index);
		A_0.Location = 70724560483L;
		CILHelper.IncrementPostfix(ref scriptDatum);
		A_0.Location = 75019527779L;
		ScriptDatum scriptDatum2 = CILHelper.IncrementPostfix(ref scriptDatum);
		A_0.Location = 79314495075L;
		CILHelper.IncrementPropertyPostfix(A_0.Global, "n");
		return ScriptDatum.Null;
	}
}
