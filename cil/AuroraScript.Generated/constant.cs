using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class constant
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("_");
		A_0.Module.Define("log", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(constant.log), Array.Empty<Upvalue>(), "log"), false, true);
		A_0.Location = 14407730335L;
		A_0.Location = 40177534111L;
		A_0.Module.Define("NUM", NumberValue.Of(3.1415926789876543), false, true);
		A_0.Location = 44472501407L;
		A_0.Module.Define("STR", StringValue.Of("this is string"), false, true);
		A_0.Location = 48767468703L;
		A_0.Module.Define("BOOL", BooleanValue.Of(true), false, true);
		A_0.Location = 53062435999L;
		A_0.Module.Define("BASE", NumberValue.Of(10.0), false, true);
		A_0.Location = 57357403295L;
		A_0.Module.Define("COMPLEX", NumberValue.Of(36.41592678987654), false, true);
		A_0.Location = 61652370591L;
		A_0.Module.Define("TAG", StringValue.Of("10_1"), false, true);
		A_0.Location = 65947337887L;
		A_0.Module.Define("TEMPLATE", StringValue.Of("this is string10_10_1"), false, true);
	}

	public static ScriptDatum log(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Location = 83127207071L;
		ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
		string name = "log";
		ScriptArray scriptArray = new ScriptArray(0);
		CILHelper.SpreadInto(scriptArray, new ScriptArray(A_1));
		CILHelper.InvokeProperty(receiver, A_0, name, scriptArray.ToDatumArray());
		return ScriptDatum.Null;
	}
}
