using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class TIMER_LIB
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		A_0.Module.Define("time_proc", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(TIMER_LIB.time_proc), Array.Empty<Upvalue>(), "time_proc"), false, true);
		A_0.Module.Define("testCallback", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(TIMER_LIB.testCallback), Array.Empty<Upvalue>(), "testCallback"), false, true);
		A_0.Module.Define("createTimer", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(TIMER_LIB.createTimer), Array.Empty<Upvalue>(), "createTimer"), false, true);
		A_0.Module.Define("Buy", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(TIMER_LIB.Buy), Array.Empty<Upvalue>(), "Buy"), false, true);
		A_0.Module.Define("Close", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(TIMER_LIB.Close), Array.Empty<Upvalue>(), "Close"), false, true);
		A_0.Location = 48596152754L;
		A_0.Module.Define("timeCount", ScriptDatum.ToObject(scriptDatum), true, true);
		A_0.Location = 52891120050L;
		A_0.Module.Define("resetCount", ScriptDatum.ToObject(scriptDatum), true, true);
		A_0.Location = 57186087346L;
		ScriptObject module = A_0.Module;
		string key = "timers";
		ScriptArray scriptArray = new ScriptArray(7);
		scriptArray.SetElementValue(0, scriptDatum);
		scriptArray.SetElementValue(1, ScriptDatum.Null);
		scriptArray.SetElementValue(2, ScriptDatum.FromNumber(1.0));
		scriptArray.SetElementValue(3, ScriptDatum.FromNumber(2.0));
		scriptArray.SetElementValue(4, ScriptDatum.FromNumber(3.0));
		scriptArray.SetElementValue(5, ScriptDatum.FromNumber(4.0));
		scriptArray.SetElementValue(6, ScriptDatum.FromNumber(5.0));
		module.Define(key, scriptArray, true, true);
	}

	public static ScriptDatum time_proc(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		ScriptEnumerator enumerator = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "timers")).GetEnumerator();
		ScriptDatum scriptDatum;
		while (enumerator.NextValue(out scriptDatum))
		{
			A_0.Location = 82955891122L;
			CILHelper.IncrementPropertyPostfix(A_0.Module, "timeCount");
		}
		A_0.Location = 95840793010L;
		CILHelper.InvokeProperty4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", A_1, A_2, A_3, ScriptDatum.FromObject(A_0.UserState));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testCallback(ScriptContext A_0)
	{
		A_0.Location = 117315629490L;
		CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "CREATE_TIMER")), A_0, A_0.Module.GetPropertyDatum(A_0, "time_proc"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum createTimer(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum value = CILHelper.TryGetArg(A_1, 1, ScriptDatum.FromNumber(521.0));
		Upvalue[] array = new Upvalue[4];
		array[0] = new Upvalue();
		array[0].Value = arg;
		array[1] = new Upvalue();
		array[1].Value = value;
		array[2] = new Upvalue();
		array[3] = new Upvalue();
		array[2].Value = ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(TIMER_LIB.log), array, "log"));
		ClosureFunction value2 = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(TIMER_LIB.cancel), array, "cancel");
		A_0.Location = 147380400562L;
		Upvalue upvalue = array[3];
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "timeId", CILHelper.IncrementPropertyPostfix(A_0.Module, "timeCount"));
		scriptObject.SetPropertyDatum(A_0, "callback", array[0].Value);
		scriptObject.SetPropertyDatum(A_0, "interval", array[1].Value);
		scriptObject.SetPropertyDatum(A_0, "cancel", ScriptDatum.FromObject(value2));
		string key = "numbers";
		ScriptArray scriptArray = new ScriptArray(4);
		scriptArray.SetElementValue(0, ScriptDatum.FromNumber(1.0));
		scriptArray.SetElementValue(1, ScriptDatum.FromNumber(2.0));
		scriptArray.SetElementValue(2, ScriptDatum.FromNumber(3.0));
		scriptArray.SetElementValue(3, ScriptDatum.FromNumber(4.0));
		scriptObject.SetPropertyDatum(A_0, key, ScriptDatum.FromObject(scriptArray));
		string key2 = "strings";
		ScriptArray scriptArray2 = new ScriptArray(4);
		scriptArray2.SetElementValue(0, ScriptDatum.FromString("a"));
		scriptArray2.SetElementValue(1, ScriptDatum.FromString("b"));
		scriptArray2.SetElementValue(2, ScriptDatum.FromString("c"));
		scriptArray2.SetElementValue(3, ScriptDatum.FromString("d"));
		scriptObject.SetPropertyDatum(A_0, key2, ScriptDatum.FromObject(scriptArray2));
		scriptObject.SetPropertyDatum(A_0, "datas", ScriptDatum.FromObject(CILHelper.CreateObject3("v1", ScriptDatum.FromNumber(123.0), "v2", ScriptDatum.FromString("hello"), "v3", ScriptDatum.FromBoolean(true))));
		scriptObject.SetPropertyDatum(A_0, "count", ScriptDatum.FromNumber(50.0));
		scriptObject.SetPropertyDatum(A_0, "reset", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(TIMER_LIB.lambda_43_22), array, "lambda_43_22")));
		upvalue.Value = ScriptDatum.FromObject(scriptObject);
		A_0.Location = 276229419442L;
		CILHelper.Invoke1(ScriptDatum.ToObject(array[2].Value), A_0, ScriptDatum.FromString("\r\n1. 这是一个特殊的字符串模板\r\n2. 支持多行文本\\n123\r\n3. 它会让代码看起来更舒服\r\n4. <Buy/@Buy> <Close/@Close>\r\n5. <Buys/@Buys:input-number>\r\n"));
		A_0.Location = 323474059698L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "timers")), A_0, "push", array[3].Value);
		A_0.Location = 327769026994L;
		return CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object")), A_0, array[3].Value);
	}

	public static ScriptDatum log(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 211804910002L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringRight(CILHelper.AddStringMiddle(CILHelper.AddStringLeft("Timer:", ScriptDatum.ToObject(A_0.Upvalues[3].Value).GetPropertyDatum(A_0, "timeId")), " [", A_1), "]"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum cancel(ScriptContext A_0)
	{
		ScriptDatum @null = ScriptDatum.Null;
		A_0.Location = 224689811890L;
		CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Upvalues[2].Value), A_0, ScriptDatum.FromString("canceled"));
		A_0.Location = 228984779186L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "cancel", ScriptDatum.ToObject(@null));
		A_0.Location = 233279746482L;
		CILHelper.DecrementPropertyPostfix(A_0.Module, "timeCount");
		A_0.Location = 237574713778L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "timeId", ScriptDatum.ToObject(@null));
		A_0.Location = 241869681074L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "callback", ScriptDatum.ToObject(@null));
		A_0.Location = 246164648370L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "interval", ScriptDatum.ToObject(@null));
		A_0.Location = 250459615666L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "reset", ScriptDatum.ToObject(@null));
		A_0.Location = 254754582962L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "count", ScriptDatum.ToObject(@null));
		A_0.Location = 259049550258L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "abc", StringValue.Of("abc"));
		A_0.Location = 263344517554L;
		return ScriptDatum.FromBoolean(true);
	}

	public static ScriptDatum lambda_43_22(ScriptContext A_0)
	{
		A_0.Location = 190330073522L;
		ScriptDatum.ToObject(A_0.Upvalues[3].Value).SetPropertyValue(A_0, "count", NumberValue.Of(0.0));
		A_0.Location = 194625040818L;
		CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Upvalues[2].Value), A_0, ScriptDatum.FromString("reset"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum Buy(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(1.0));
		return ScriptDatum.Null;
	}

	public static ScriptDatum Close(ScriptContext A_0)
	{
		return ScriptDatum.Null;
	}
}
