using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class reproduce_closure
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum index = ScriptDatum.FromNumber(0.0);
		A_0.Module.Define("setup", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.setup), Array.Empty<Upvalue>(), "setup"), false, true);
		A_0.Module.Define("testProxy", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.testProxy), Array.Empty<Upvalue>(), "testProxy"), false, true);
		A_0.Module.Define("testFor", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(reproduce_closure.testFor), Array.Empty<Upvalue>(), "testFor"), false, true);
		A_0.Module.Define("test", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.test), Array.Empty<Upvalue>(), "test"), false, true);
		A_0.Module.Define("closure1", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.closure1), Array.Empty<Upvalue>(), "closure1"), false, true);
		A_0.Module.Define("empty", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(reproduce_closure.empty), Array.Empty<Upvalue>(), "empty"), false, true);
		A_0.Location = 6759498119L;
		A_0.Module.Define("fns", new ScriptArray(0), true, true);
		A_0.Location = 565105246599L;
		CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "empty")), A_0, ScriptDatum.FromNumber(3.0));
		A_0.Location = 573695181191L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "closure1")), A_0);
		A_0.Location = 577990148487L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "test")), A_0);
		A_0.Location = 582285115783L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "closure1")), A_0);
		A_0.Location = 586580083079L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "setup")), A_0);
		A_0.Location = 590875050375L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "testFor")), A_0);
		A_0.Location = 603759952263L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("First call (x+1): ", CILHelper.Invoke0(ScriptDatum.ToObject(CILHelper.GetElement(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "fns")), index)), A_0)));
		A_0.Location = 608054919559L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("Second call (x+10): ", CILHelper.Invoke0(ScriptDatum.ToObject(CILHelper.GetElement(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "fns")), ScriptDatum.FromNumber(1.0))), A_0)));
		A_0.Location = 612349886855L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("Third call (x+1): ", CILHelper.Invoke0(ScriptDatum.ToObject(CILHelper.GetElement(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "fns")), index)), A_0)));
		A_0.Location = 616644854151L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "testProxy")), A_0);
	}

	public static ScriptDatum setup(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("1");
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(3.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(2.0);
		ScriptDatum datum = ScriptDatum.FromNumber(0.0);
		ScriptDatum arg2 = ScriptDatum.FromString("destructuring test passed!");
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue(),
			new Upvalue()
		};
		A_0.Location = 19644400007L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		A_0.Location = 23939367303L;
		ScriptDatum a = scriptDatum;
		A_0.Location = 28234334599L;
		array[0].Value = CILHelper.Add(a, scriptDatum2);
		A_0.Location = 32529301895L;
		array[1].Value = CILHelper.Multiply(array[0].Value, scriptDatum);
		A_0.Location = 41119236487L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "fns")), A_0, "push", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_9_20), array, "lambda_9_20")));
		A_0.Location = 62594072967L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "fns")), A_0, "push", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_14_20), array, "lambda_14_20")));
		A_0.Location = 79773942151L;
		ScriptDatum propertyDatum;
		if (CILHelper.ToBoolean(propertyDatum = A_0.Global.GetPropertyDatum(A_0, "console")))
		{
			propertyDatum = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")).GetPropertyDatum(A_0, "log");
		}
		if (CILHelper.ToBoolean(propertyDatum))
		{
			A_0.Location = 84068909447L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		}
		A_0.Location = 92658844039L;
		ScriptObject scriptObject = CILHelper.CreateObject3("y", scriptDatum3, "a", scriptDatum4, "b", scriptDatum);
		A_0.Location = 96953811335L;
		CILHelper.IncrementPropertyPostfix(scriptObject, "y");
		A_0.Location = 101248778631L;
		scriptObject.SetPropertyValue(A_0, "c", NumberValue.Of(123.0));
		A_0.Location = 105543745927L;
		scriptObject.SetPropertyValue(A_0, "b", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_24_19), array, "lambda_24_19"));
		A_0.Location = 109838713223L;
		ScriptArray scriptArray = new ScriptArray(3);
		scriptArray.SetElementValue(0, datum);
		scriptArray.SetElementValue(1, datum);
		scriptArray.SetElementValue(2, datum);
		ScriptArray val = scriptArray;
		A_0.Location = 114133680519L;
		ScriptArray scriptArray2 = new ScriptArray(0);
		scriptArray2.Push(scriptDatum3);
		scriptArray2.Push(scriptDatum4);
		scriptArray2.Push(scriptDatum);
		scriptArray2.Push(ScriptDatum.FromNumber(4.0));
		scriptArray2.Push(scriptDatum2);
		CILHelper.SpreadInto(scriptArray2, val);
		ScriptArray scriptArray3 = scriptArray2;
		A_0.Location = 118428647815L;
		ScriptObject scriptObject2 = scriptObject;
		ScriptDatum arg3 = ScriptDatum.FromObject(scriptObject2.GetPropertyValue(A_0, "a"));
		ScriptDatum arg4 = ScriptDatum.FromObject(scriptObject2.GetPropertyValue(A_0, "e"));
		A_0.Location = 122723615111L;
		CILHelper.InvokeProperty3(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2, arg3, arg4);
		A_0.Location = 127018582407L;
		ScriptObject scriptObject3 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "testFor"));
		ScriptArray scriptArray4 = new ScriptArray(0);
		scriptArray4.Push(scriptDatum3);
		scriptArray4.Push(scriptDatum4);
		scriptArray4.Push(scriptDatum);
		CILHelper.SpreadInto(scriptArray4, scriptObject);
		scriptObject3.Invoke(A_0, scriptArray4.ToDatumArray());
		A_0.Location = 135608516999L;
		ScriptArray scriptArray5 = (ScriptArray)scriptArray3;
		ScriptDatum element = scriptArray5.GetElement(0);
		ScriptDatum scriptDatum5;
		scriptArray5.SliceTo(1, scriptArray5.Length - 1, ref scriptDatum5);
		ScriptDatum arg5 = scriptDatum5;
		ScriptDatum element2 = scriptArray5.GetElement(scriptArray5.Length - 1);
		A_0.Location = 139903484295L;
		CILHelper.InvokeProperty4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2, element, arg5, element2);
		ScriptEnumerator enumerator = scriptObject.GetEnumerator();
		ScriptDatum scriptDatum6;
		while (enumerator.NextValue(out scriptDatum6))
		{
			A_0.Location = 148493418887L;
			ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name = "log";
			ScriptDatum arg6 = scriptDatum6;
			A_0.Location = 152788386183L;
			CILHelper.InvokeProperty1(receiver, A_0, name, arg6);
		}
		ScriptEnumerator enumerator2 = scriptArray3.GetEnumerator();
		ScriptDatum scriptDatum7;
		while (enumerator2.NextValue(out scriptDatum7))
		{
			A_0.Location = 161378320775L;
			ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name2 = "log";
			ScriptDatum arg7 = scriptDatum7;
			A_0.Location = 165673288071L;
			CILHelper.InvokeProperty1(receiver2, A_0, name2, arg7);
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_9_20(ScriptContext A_0)
	{
		A_0.Location = 45414203783L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 49709171079L;
		return A_0.Upvalues[0].Value;
	}

	public static ScriptDatum lambda_14_20(ScriptContext A_0)
	{
		A_0.Location = 66889040263L;
		A_0.Upvalues[1].Value = CILHelper.Add(A_0.Upvalues[1].Value, ScriptDatum.FromNumber(10.0));
		A_0.Location = 71184007559L;
		return A_0.Upvalues[1].Value;
	}

	public static ScriptDatum lambda_24_19(ScriptContext A_0)
	{
		A_0.Location = 105543745927L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", A_0.Module.GetPropertyDatum(A_0, "fns"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testProxy(ScriptContext A_0)
	{
		A_0.Location = 191443091847L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Testing Proxy"));
		A_0.Location = 200033026439L;
		ScriptObject type = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Proxy"));
		ScriptDatum[] array = new ScriptDatum[2];
		array[0] = ScriptDatum.FromObject(new ScriptObject());
		int num = 1;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "get", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(reproduce_closure.lambda_47_42), Array.Empty<Upvalue>(), "lambda_47_42")));
		scriptObject.SetPropertyDatum(A_0, "set", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate4(reproduce_closure.lambda_51_49), Array.Empty<Upvalue>(), "lambda_51_49")));
		array[num] = ScriptDatum.FromObject(scriptObject);
		ScriptDatum scriptDatum = CILHelper.New(type, A_0, array);
		A_0.Location = 251572633991L;
		ScriptDatum.ToObject(scriptDatum).SetPropertyValue(A_0, "string", StringValue.Of("Hello, Proxy!"));
		A_0.Location = 255867601287L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(scriptDatum).GetPropertyDatum(A_0, "string"));
		A_0.Location = 260162568583L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", scriptDatum);
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_47_42(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 208622961031L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Getting"), A_2);
		A_0.Location = 212917928327L;
		return CILHelper.GetElement(ScriptDatum.ToObject(A_1), A_2);
	}

	public static ScriptDatum lambda_51_49(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3, ScriptDatum A_4)
	{
		A_0.Location = 225802830215L;
		CILHelper.InvokeProperty3(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Setting"), A_2, A_3);
		A_0.Location = 230097797511L;
		CILHelper.SetElement(ScriptDatum.ToObject(A_1), A_2, A_3);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testFor(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum b = ScriptDatum.FromNumber(1000000.0);
		A_0.Location = 281637405063L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(new ScriptArray(A_1)));
		A_0.Location = 285932372359L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			CILHelper.IncrementPostfix(ref a);
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum test(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(100.0);
		ClosureFunction closureFunction = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.makeCounter), Array.Empty<Upvalue>(), "makeCounter");
		A_0.Location = 341766947207L;
		if (CILHelper.ToBoolean(closureFunction))
		{
			A_0.Location = 346061914503L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("1"));
		}
		else
		{
			A_0.Location = 350356881799L;
			ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name = "log";
			ScriptDatum arg = ScriptDatum.FromString("2");
			A_0.Location = 354651849095L;
			CILHelper.InvokeProperty1(receiver, A_0, name, arg);
		}
		A_0.Location = 371831718279L;
		CILHelper.Invoke0(closureFunction, A_0);
		A_0.Location = 376126685575L;
		ScriptDatum d = CILHelper.Invoke0(closureFunction, A_0);
		A_0.Location = 380421652871L;
		ScriptDatum result = scriptDatum;
		A_0.Location = 384716620167L;
		ScriptDatum a = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			A_0.Location = 389011587463L;
			result = CILHelper.Invoke0(ScriptDatum.ToObject(d), A_0);
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 397601522055L;
		ScriptDatum scriptDatum2 = CILHelper.IncrementPostfix(ref result);
		A_0.Location = 401896489351L;
		return result;
	}

	public static ScriptDatum makeCounter(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue()
		};
		A_0.Location = 315997143431L;
		array[0].Value = ScriptDatum.FromNumber(0.0);
		A_0.Location = 320292110727L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_74_22), array, "lambda_74_22"));
	}

	public static ScriptDatum lambda_74_22(ScriptContext A_0)
	{
		A_0.Location = 324587078023L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 328882045319L;
		return A_0.Upvalues[0].Value;
	}

	public static ScriptDatum closure1(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue(),
			new Upvalue()
		};
		ClosureFunction function = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.makeCounter1), array, "makeCounter1");
		ClosureFunction function2 = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.makeCounter2), array, "makeCounter2");
		A_0.Location = 419076358535L;
		array[1].Value = ScriptDatum.FromString("123");
		A_0.Location = 423371325831L;
		array[0].Value = ScriptDatum.FromNumber(0.0);
		A_0.Location = 522155573639L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "a", CILHelper.Invoke0(function, A_0));
		scriptObject.SetPropertyDatum(A_0, "b", CILHelper.Invoke0(function2, A_0));
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum makeCounter1(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			A_0.Upvalues[0],
			A_0.Upvalues[1],
			new Upvalue()
		};
		A_0.Location = 431961260423L;
		array[2].Value = ScriptDatum.FromNumber(10.0);
		A_0.Location = 436256227719L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_101_22), array, "lambda_101_22"));
	}

	public static ScriptDatum lambda_101_22(ScriptContext A_0)
	{
		A_0.Location = 440551195015L;
		CILHelper.IncrementPostfix(ref A_0.Upvalues[2].Value);
		A_0.Location = 444846162311L;
		A_0.Upvalues[1].Value = ScriptDatum.FromString("ABC");
		A_0.Location = 449141129607L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 453436096903L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "title", A_0.Upvalues[1].Value);
		scriptObject.SetPropertyDatum(A_0, "count", A_0.Upvalues[0].Value);
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum makeCounter2(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			A_0.Upvalues[0],
			A_0.Upvalues[1],
			new Upvalue(),
			new Upvalue()
		};
		array[2].Value = ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.ddd), array, "ddd"));
		A_0.Location = 470615966087L;
		array[3].Value = ScriptDatum.FromNumber(20.0);
		A_0.Location = 492090802567L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(reproduce_closure.lambda_114_22), array, "lambda_114_22"));
	}

	public static ScriptDatum ddd(ScriptContext A_0)
	{
		A_0.Location = 479205900679L;
		CILHelper.IncrementPostfix(ref A_0.Upvalues[3].Value);
		A_0.Location = 483500867975L;
		return A_0.Upvalues[3].Value;
	}

	public static ScriptDatum lambda_114_22(ScriptContext A_0)
	{
		A_0.Location = 496385769863L;
		CILHelper.IncrementPostfix(ref A_0.Upvalues[3].Value);
		A_0.Location = 500680737159L;
		A_0.Upvalues[1].Value = ScriptDatum.FromString("XYZ");
		A_0.Location = 504975704455L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 509270671751L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "title", A_0.Upvalues[1].Value);
		scriptObject.SetPropertyDatum(A_0, "ss", CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Upvalues[2].Value), A_0));
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum empty(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(1.0));
		ScriptDatum arg2 = CILHelper.TryGetArg(A_1, 1, ScriptDatum.FromNumber(2.0));
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		A_0.Location = 543630410119L;
		CILHelper.InvokeProperty3(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg, arg2, arg3);
		return ScriptDatum.Null;
	}
}
