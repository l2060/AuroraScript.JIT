using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class l123
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Module.Define("testStr2", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(l123.testStr2), Array.Empty<Upvalue>(), "testStr2"), false, true);
		A_0.Module.Define("testInput", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(l123.testInput), Array.Empty<Upvalue>(), "testInput"), false, true);
		A_0.Module.Define("throwTest", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(l123.throwTest), Array.Empty<Upvalue>(), "throwTest"), false, true);
		A_0.Module.Define("testCatch", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(l123.testCatch), Array.Empty<Upvalue>(), "testCatch"), false, true);
	}

	public static ScriptDatum testStr2(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("Hello");
		ScriptDatum c = ScriptDatum.FromString("Wrold");
		A_0.Location = 16251951105L;
		A_0.Location = 20546918401L;
		A_0.Location = 24841885697L;
		ScriptDatum arg = CILHelper.AddStringMiddle(scriptDatum, " ", c);
		A_0.Location = 29136852993L;
		ScriptDatum arg2 = CILHelper.AddStringRight(scriptDatum, "Wrold");
		A_0.Location = 33431820289L;
		CILHelper.InvokeProperty4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", scriptDatum, arg, arg2, scriptDatum);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testInput(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("��������");
		ScriptDatum arg2 = ScriptDatum.FromString("����һ��0-99��ֵ");
		ScriptDatum arg3 = ScriptDatum.FromString("number");
		A_0.Location = 72086525953L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "INPUT_NUMBER")), A_0, arg, arg2, arg3, A_0.Global.GetPropertyDatum(A_0, "input_change"));
		A_0.Location = 80676460545L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "INPUT_NUMBER")), A_0, arg, arg2, arg3, ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(l123.lambda_18_66), Array.Empty<Upvalue>(), "lambda_18_66")));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_18_66(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 84971427841L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "GIVE")), A_0, ScriptDatum.FromString("esd"), A_1);
		A_0.Location = 89266395137L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("����ֵ=", A_1));
		return ScriptDatum.Null;
	}

	public static ScriptDatum throwTest(ScriptContext A_0)
	{
		A_0.Location = 132216068097L;
		CILHelper.Throw(CILHelper.New(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Error")), A_0, new ScriptDatum[]
		{
			ScriptDatum.FromString("test")
		}));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testCatch(ScriptContext A_0)
	{
		A_0.Location = 162280839169L;
		try
		{
			CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "throwTest")), A_0);
		}
		catch (Exception)
		{
		}
		A_0.Location = 170870773761L;
		try
		{
			A_0.Location = 179460708353L;
			CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "throwTest")), A_0);
		}
		catch (Exception exception)
		{
			ScriptDatum arg = CILHelper.ExceptionToError(exception);
			A_0.Location = 196640577537L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		}
		finally
		{
			A_0.Location = 213820446721L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("finally"));
		}
		return ScriptDatum.Null;
	}
}
