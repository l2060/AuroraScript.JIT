using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class MAIN
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Module.Define("main", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(MAIN.main), Array.Empty<Upvalue>(), "main"), false, true);
		A_0.Location = 14552974221L;
		A_0.Module.Define("_testCases", new ScriptArray(0), true, true);
	}

	public static ScriptDatum main(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("===============================================================");
		ScriptDatum b = ScriptDatum.FromString("object");
		ScriptDatum b2 = ScriptDatum.FromString("function");
		ScriptDatum arg2 = ScriptDatum.FromString("test");
		ScriptDatum scriptDatum = ScriptDatum.FromString("start Test Case");
		A_0.Location = 27437876109L;
		ScriptDatum propertyDatum = A_0.Global.GetPropertyDatum(A_0, "modules");
		A_0.Location = 31732843405L;
		ScriptDatum d = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object")), A_0, "keys", propertyDatum);
		A_0.Location = 36027810701L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		A_0.Location = 40322777997L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("found modules: "), CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "join", ScriptDatum.FromString(", ")));
		A_0.Location = 44617745293L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		ScriptEnumerator enumerator = ScriptDatum.ToObject(d).GetEnumerator();
		ScriptDatum index;
		while (enumerator.NextValue(out index))
		{
			A_0.Location = 48912712589L;
			ScriptDatum a;
			if (CILHelper.ToBoolean(a = CILHelper.Equal(CILHelper.TypeOf(CILHelper.GetElement(ScriptDatum.ToObject(propertyDatum), index)), b)))
			{
				a = CILHelper.NotEqual(CILHelper.GetElement(ScriptDatum.ToObject(propertyDatum), index), A_0.Global.GetPropertyDatum(A_0, "this"));
			}
			if (CILHelper.ToBoolean(a))
			{
				A_0.Location = 53207679885L;
				ScriptDatum element = CILHelper.GetElement(ScriptDatum.ToObject(propertyDatum), index);
				ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object"));
				string name = "keys";
				ScriptDatum arg3 = element;
				A_0.Location = 61797614477L;
				ScriptEnumerator enumerator2 = ScriptDatum.ToObject(CILHelper.InvokeProperty1(receiver, A_0, name, arg3)).GetEnumerator();
				ScriptDatum scriptDatum2;
				while (enumerator2.NextValue(out scriptDatum2))
				{
					A_0.Location = 66092581773L;
					ScriptDatum a2;
					if (CILHelper.ToBoolean(a2 = CILHelper.Equal(CILHelper.TypeOf(CILHelper.GetElement(ScriptDatum.ToObject(element), scriptDatum2)), b2)))
					{
						a2 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(scriptDatum2), A_0, "startsWith", arg2);
					}
					if (CILHelper.ToBoolean(a2))
					{
						A_0.Location = 70387549069L;
						ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "_testCases"));
						string name2 = "push";
						ScriptObject scriptObject = new ScriptObject();
						scriptObject.SetPropertyDatum(A_0, "name", scriptDatum2);
						scriptObject.SetPropertyDatum(A_0, "method", CILHelper.GetElement(ScriptDatum.ToObject(element), scriptDatum2));
						CILHelper.InvokeProperty1(receiver2, A_0, name2, ScriptDatum.FromObject(scriptObject));
					}
				}
			}
		}
		A_0.Location = 91862385549L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", A_0.Module.GetPropertyDatum(A_0, "_testCases"));
		ScriptEnumerator enumerator3 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "_testCases")).GetEnumerator();
		ScriptDatum d2;
		while (enumerator3.NextValue(out d2))
		{
			A_0.Location = 96157352845L;
			ScriptObject receiver3 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name3 = "log";
			ScriptDatum arg4 = scriptDatum;
			ScriptDatum propertyDatum2 = ScriptDatum.ToObject(d2).GetPropertyDatum(A_0, "name");
			A_0.Location = 100452320141L;
			CILHelper.InvokeProperty2(receiver3, A_0, name3, arg4, propertyDatum2);
			A_0.Location = 104747287437L;
			CILHelper.InvokeProperty0(ScriptDatum.ToObject(d2), A_0, "method");
		}
		return ScriptDatum.Null;
	}
}
