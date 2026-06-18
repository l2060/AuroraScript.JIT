using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;

public sealed class UNIT_LIB
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum value = ScriptDatum.FromString("Hello");
		A_0.Module.Define("time", A_0.Global.GetModule("TIMER_LIB"), false, true);
		A_0.Module.Define("md5", A_0.Global.GetModule("MD5_LIB"), false, true);
		A_0.Module.Define("xxx", A_0.Global.GetModule("libs/timer"), false, true);
		A_0.Module.Define("defineTest", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.defineTest), Array.Empty<Upvalue>(), "defineTest"), false, true);
		A_0.Module.Define("testEmpty", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testEmpty), Array.Empty<Upvalue>(), "testEmpty"), false, true);
		A_0.Module.Define("testInlude", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testInlude), Array.Empty<Upvalue>(), "testInlude"), false, true);
		A_0.Module.Define("testHashMap", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testHashMap), Array.Empty<Upvalue>(), "testHashMap"), false, true);
		A_0.Module.Define("testIssue1", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.testIssue1), Array.Empty<Upvalue>(), "testIssue1"), false, true);
		A_0.Module.Define("patchFunc", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.patchFunc), Array.Empty<Upvalue>(), "patchFunc"), false, true);
		A_0.Module.Define("testHotPatch", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testHotPatch), Array.Empty<Upvalue>(), "testHotPatch"), false, true);
		A_0.Module.Define("testIssue2", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testIssue2), Array.Empty<Upvalue>(), "testIssue2"), false, true);
		A_0.Module.Define("input_change", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.input_change), Array.Empty<Upvalue>(), "input_change"), false, true);
		A_0.Module.Define("testInput", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testInput), Array.Empty<Upvalue>(), "testInput"), false, true);
		A_0.Module.Define("testClrType", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testClrType), Array.Empty<Upvalue>(), "testClrType"), false, true);
		A_0.Module.Define("testDatetime", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testDatetime), Array.Empty<Upvalue>(), "testDatetime"), false, true);
		A_0.Module.Define("testProxy", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testProxy), Array.Empty<Upvalue>(), "testProxy"), false, true);
		A_0.Module.Define("testPeculiarity", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.testPeculiarity), Array.Empty<Upvalue>(), "testPeculiarity"), false, true);
		A_0.Module.Define("testJson", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testJson), Array.Empty<Upvalue>(), "testJson"), false, true);
		A_0.Module.Define("replacer", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.replacer), Array.Empty<Upvalue>(), "replacer"), false, true);
		A_0.Module.Define("testDeConstruct", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testDeConstruct), Array.Empty<Upvalue>(), "testDeConstruct"), false, true);
		A_0.Module.Define("testRegex", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testRegex), Array.Empty<Upvalue>(), "testRegex"), false, true);
		A_0.Module.Define("createTestContext", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.createTestContext), Array.Empty<Upvalue>(), "createTestContext"), false, true);
		A_0.Module.Define("formatValue", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.formatValue), Array.Empty<Upvalue>(), "formatValue"), false, true);
		A_0.Module.Define("addFailure", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate4(UNIT_LIB.addFailure), Array.Empty<Upvalue>(), "addFailure"), false, true);
		A_0.Module.Define("expectTrue", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.expectTrue), Array.Empty<Upvalue>(), "expectTrue"), false, true);
		A_0.Module.Define("expectFalse", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(UNIT_LIB.expectFalse), Array.Empty<Upvalue>(), "expectFalse"), false, true);
		A_0.Module.Define("expectEqual", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate4(UNIT_LIB.expectEqual), Array.Empty<Upvalue>(), "expectEqual"), false, true);
		A_0.Module.Define("expectNearlyEqual", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.expectNearlyEqual), Array.Empty<Upvalue>(), "expectNearlyEqual"), false, true);
		A_0.Module.Define("addNote", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.addNote), Array.Empty<Upvalue>(), "addNote"), false, true);
		A_0.Module.Define("isArray", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.isArray), Array.Empty<Upvalue>(), "isArray"), false, true);
		A_0.Module.Define("deepEqual", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.deepEqual), Array.Empty<Upvalue>(), "deepEqual"), false, true);
		A_0.Module.Define("absolute", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.absolute), Array.Empty<Upvalue>(), "absolute"), false, true);
		A_0.Module.Define("executeTest", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.executeTest), Array.Empty<Upvalue>(), "executeTest"), false, true);
		A_0.Module.Define("benchmarkNumbers", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.benchmarkNumbers), Array.Empty<Upvalue>(), "benchmarkNumbers"), false, true);
		A_0.Module.Define("benchmarkArrays", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.benchmarkArrays), Array.Empty<Upvalue>(), "benchmarkArrays"), false, true);
		A_0.Module.Define("benchmarkClosure", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.benchmarkClosure), Array.Empty<Upvalue>(), "benchmarkClosure"), false, true);
		A_0.Module.Define("benchmarkObjects", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.benchmarkObjects), Array.Empty<Upvalue>(), "benchmarkObjects"), false, true);
		A_0.Module.Define("benchmarkStrings", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.benchmarkStrings), Array.Empty<Upvalue>(), "benchmarkStrings"), false, true);
		A_0.Module.Define("factorial", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.factorial), Array.Empty<Upvalue>(), "factorial"), false, true);
		A_0.Module.Define("testAllUnits", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testAllUnits), Array.Empty<Upvalue>(), "testAllUnits"), false, true);
		A_0.Module.Define("test", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.test), Array.Empty<Upvalue>(), "test"), false, true);
		A_0.Module.Define("start_timer", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.start_timer), Array.Empty<Upvalue>(), "start_timer"), false, true);
		A_0.Module.Define("testIterator", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testIterator), Array.Empty<Upvalue>(), "testIterator"), false, true);
		A_0.Module.Define("deepInterruption", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.deepInterruption), Array.Empty<Upvalue>(), "deepInterruption"), false, true);
		A_0.Module.Define("testFor", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.testFor), Array.Empty<Upvalue>(), "testFor"), false, true);
		A_0.Module.Define("testArray", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(UNIT_LIB.testArray), Array.Empty<Upvalue>(), "testArray"), false, true);
		A_0.Module.Define("testDeconstruction", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testDeconstruction), Array.Empty<Upvalue>(), "testDeconstruction"), false, true);
		A_0.Module.Define("testClrFunc", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testClrFunc), Array.Empty<Upvalue>(), "testClrFunc"), false, true);
		A_0.Module.Define("closure1", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.closure1), Array.Empty<Upvalue>(), "closure1"), false, true);
		A_0.Module.Define("testTypeOf", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testTypeOf), Array.Empty<Upvalue>(), "testTypeOf"), false, true);
		A_0.Module.Define("testClosure", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testClosure), Array.Empty<Upvalue>(), "testClosure"), false, true);
		A_0.Module.Define("testMD5", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testMD5), Array.Empty<Upvalue>(), "testMD5"), false, true);
		A_0.Module.Define("testMD5_1000", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testMD5_1000), Array.Empty<Upvalue>(), "testMD5_1000"), false, true);
		A_0.Module.Define("testDraw", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.testDraw), Array.Empty<Upvalue>(), "testDraw"), false, true);
		A_0.Location = 41965271125L;
		A_0.Module.Define("__testCases", new ScriptArray(0), true, true);
		A_0.Location = 114979715157L;
		ScriptObject module = A_0.Module;
		string key = "node";
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "A", ScriptDatum.FromNumber(1.0));
		scriptObject.SetPropertyDatum(A_0, "B", ScriptDatum.FromNumber(2.0));
		scriptObject.SetPropertyDatum(A_0, "C", ScriptDatum.FromNumber(3.0));
		scriptObject.SetPropertyDatum(A_0, "D", ScriptDatum.FromNumber(4.0));
		scriptObject.SetPropertyDatum(A_0, "E", value);
		scriptObject.SetPropertyDatum(A_0, "F", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_32_14), Array.Empty<Upvalue>(), "lambda_32_14")));
		module.Define(key, scriptObject, true, true);
		A_0.Location = 153634420821L;
		ScriptObject module2 = A_0.Module;
		string key2 = "node";
		ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object"));
		string name = "assign";
		ScriptDatum propertyDatum = A_0.Module.GetPropertyDatum(A_0, "node");
		ScriptObject scriptObject2 = new ScriptObject();
		scriptObject2.SetPropertyDatum(A_0, "你好", value);
		module2.Define(key2, ScriptDatum.ToObject(CILHelper.InvokeProperty2(receiver, A_0, name, propertyDatum, ScriptDatum.FromObject(scriptObject2))), true, true);
		A_0.Location = 2150794213461L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("math.arithmetic"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_500_43), Array.Empty<Upvalue>(), "lambda_500_43")));
		A_0.Location = 2223808657493L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("math.bitwise"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_517_40), Array.Empty<Upvalue>(), "lambda_517_40")));
		A_0.Location = 2266758330453L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("string.core"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_527_39), Array.Empty<Upvalue>(), "lambda_527_39")));
		A_0.Location = 2322592905301L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("array.manipulation"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_540_46), Array.Empty<Upvalue>(), "lambda_540_46")));
		A_0.Location = 2425672120405L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("object.behavior"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_564_43), Array.Empty<Upvalue>(), "lambda_564_43")));
		A_0.Location = 2528751335509L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("closure.state"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_588_41), Array.Empty<Upvalue>(), "lambda_588_41")));
		A_0.Location = 2627535583317L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("recursion.factorial"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_611_47), Array.Empty<Upvalue>(), "lambda_611_47")));
		A_0.Location = 2653305387093L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("iteration.patterns"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_617_46), Array.Empty<Upvalue>(), "lambda_617_46")));
		A_0.Location = 2747794667605L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("module.md5"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_639_38), Array.Empty<Upvalue>(), "lambda_639_38")));
		A_0.Location = 2777859438677L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("module.timer"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_646_40), Array.Empty<Upvalue>(), "lambda_646_40")));
		A_0.Location = 2829399046229L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("interop.hostConstants"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_658_49), Array.Empty<Upvalue>(), "lambda_658_49")));
		A_0.Location = 2859463817301L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "defineTest")), A_0, ScriptDatum.FromString("performance.baseline"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_665_48), Array.Empty<Upvalue>(), "lambda_665_48")));
	}

	public static ScriptDatum defineTest(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		A_0.Location = 59145140309L;
		ScriptObject receiver = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "__testCases"));
		string name = "push";
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "name", A_1);
		scriptObject.SetPropertyDatum(A_0, "run", A_2);
		CILHelper.InvokeProperty1(receiver, A_0, name, ScriptDatum.FromObject(scriptObject));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testEmpty(ScriptContext A_0)
	{
		return ScriptDatum.Null;
	}

	public static ScriptDatum testInlude(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("A");
		A_0.Location = 175109257301L;
		ScriptObject collection = CILHelper.CreateObject3("a", ScriptDatum.FromNumber(1.0), "b", ScriptDatum.FromNumber(2.0), "c", ScriptDatum.FromString(""));
		A_0.Location = 179404224597L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.Included(collection, ScriptDatum.FromString("a")));
		A_0.Location = 183699191893L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.Included(StringValue.Of("hello"), ScriptDatum.FromString("el")));
		A_0.Location = 187994159189L;
		ScriptArray scriptArray = new ScriptArray(2);
		scriptArray.SetElementValue(0, scriptDatum);
		scriptArray.SetElementValue(1, ScriptDatum.FromString("B"));
		if (CILHelper.ToBoolean(CILHelper.Included(scriptArray, scriptDatum)))
		{
			A_0.Location = 192289126485L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("111"));
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum testHashMap(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		ScriptDatum value = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("AAA");
		A_0.Location = 218058930261L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "a", scriptDatum);
		scriptObject.SetPropertyDatum(A_0, "b", value);
		ScriptObject value2 = scriptObject;
		A_0.Location = 222353897557L;
		ScriptObject scriptObject2 = new ScriptObject();
		scriptObject2.SetPropertyDatum(A_0, "a", scriptDatum);
		scriptObject2.SetPropertyDatum(A_0, "b", value);
		ScriptObject value3 = scriptObject2;
		A_0.Location = 226648864853L;
		ScriptDatum d = CILHelper.New(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "HashMap")), A_0, Array.Empty<ScriptDatum>());
		A_0.Location = 230943832149L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "set", scriptDatum, ScriptDatum.FromBoolean(true));
		A_0.Location = 235238799445L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "set", scriptDatum2, scriptDatum2);
		A_0.Location = 239533766741L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "set", ScriptDatum.FromObject(value2), ScriptDatum.FromNumber(12345.0));
		A_0.Location = 243828734037L;
		ScriptDatum arg = CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "getOrInsert", ScriptDatum.FromString("F"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_56_44), Array.Empty<Upvalue>(), "lambda_56_44")));
		A_0.Location = 248123701333L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "size"));
		A_0.Location = 252418668629L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		A_0.Location = 256713635925L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "get", scriptDatum));
		A_0.Location = 261008603221L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "has", scriptDatum2));
		A_0.Location = 265303570517L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "has", ScriptDatum.FromObject(value2)));
		A_0.Location = 269598537813L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "has", ScriptDatum.FromObject(value3)));
		A_0.Location = 273893505109L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "delete", scriptDatum);
		A_0.Location = 278188472405L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "clear");
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_56_44(ScriptContext A_0, ScriptDatum A_1)
	{
		return CILHelper.AddStringRight(A_1, "lalala");
	}

	public static ScriptDatum testIssue1(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromBoolean(true);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromBoolean(false);
		A_0.Location = 303958276181L;
		ScriptDatum a = scriptDatum;
		A_0.Location = 308253243477L;
		ScriptDatum scriptDatum4 = scriptDatum2;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum4, b)))
		{
			A_0.Location = 312548210773L;
			if (CILHelper.ToBoolean(a))
			{
				A_0.Location = 316843178069L;
				a = scriptDatum3;
			}
			else
			{
				A_0.Location = 325433112661L;
				a = scriptDatum;
			}
			A_0.Location = 338318014549L;
			CILHelper.IncrementPostfix(ref scriptDatum4);
		}
		A_0.Location = 351202916437L;
		ScriptDatum a2 = scriptDatum2;
		while (CILHelper.ToBoolean(CILHelper.Less(a2, b)))
		{
			A_0.Location = 355497883733L;
			CILHelper.IncrementPostfix(ref a2);
		}
		A_0.Location = 372677752917L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringMiddle(ScriptDatum.FromString("my name is = {"), "debugger", ScriptDatum.FromString("}")));
		A_0.Location = 376972720213L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringRight(CILHelper.AddStringLeft("my age is = {", scriptDatum4), "} year"));
		A_0.Location = 385562654805L;
		ScriptDatum element = CILHelper.GetElement(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "modules")), ScriptDatum.FromString("TIMER_LIB"));
		A_0.Location = 389857622101L;
		CILHelper.InvokeProperty2(A_0.UserState, A_0, "Test", ScriptDatum.FromNumber(123.45), ScriptDatum.FromString("abc"));
		A_0.Location = 394152589397L;
		ScriptArray scriptArray = new ScriptArray(A_1);
		return ScriptDatum.Null;
	}

	public static ScriptDatum patchFunc(ScriptContext A_0)
	{
		A_0.Location = 424217360469L;
		return ScriptDatum.FromString("origin");
	}

	public static ScriptDatum testHotPatch(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("patchFunc result");
		A_0.Location = 445692196949L;
		ScriptDatum arg2 = ScriptDatum.FromString("@module(UNIT_LIB);\r\nexport func patchFunc() {\r\n return \"fixed\";\r\n}\r\n");
		A_0.Location = 471462000725L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg, CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "patchFunc")), A_0));
		A_0.Location = 475756968021L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "HotPatch")), A_0, "incremental", ScriptDatum.FromString("unit"), arg2);
		A_0.Location = 480051935317L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg, CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "patchFunc")), A_0));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testIssue2(ScriptContext A_0)
	{
		A_0.Location = 510116706389L;
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(655.0);
		return ScriptDatum.Null;
	}

	public static ScriptDatum input_change(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 531591542869L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "GIVE")), A_0, ScriptDatum.FromString("esd"), A_1);
		A_0.Location = 535886510165L;
		ScriptDatum scriptDatum = ScriptDatum.FromString("#00ff62ff");
		A_0.Location = 540181477461L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("输入值="), A_1);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testInput(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("购买数量");
		ScriptDatum arg2 = ScriptDatum.FromString("输入一个0-99的值");
		ScriptDatum arg3 = ScriptDatum.FromString("number");
		A_0.Location = 557361346645L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "INPUT_NUMBER")), A_0, arg, arg2, arg3, A_0.Module.GetPropertyDatum(A_0, "input_change"));
		A_0.Location = 565951281237L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "INPUT_NUMBER")), A_0, arg, arg2, arg3, ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.lambda_131_62), Array.Empty<Upvalue>(), "lambda_131_62")));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_131_62(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 570246248533L;
		CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "GIVE")), A_0, ScriptDatum.FromString("esd"), A_1);
		A_0.Location = 574541215829L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("输入值=", A_1));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testClrType(ScriptContext A_0)
	{
		ScriptDatum b = ScriptDatum.FromNumber(1000000.0);
		ScriptDatum d = ScriptDatum.FromString("aaaa");
		A_0.Location = 604605986901L;
		ScriptDatum scriptDatum = CILHelper.New(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "TestObject")), A_0, Array.Empty<ScriptDatum>());
		A_0.Location = 608900954197L;
		ScriptDatum.ToObject(scriptDatum).SetPropertyValue(A_0, "fs", StringValue.Of("ffff"));
		A_0.Location = 613195921493L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			A_0.Location = 617490888789L;
			ScriptDatum.ToObject(scriptDatum).SetPropertyValue(A_0, "Name", ScriptDatum.ToObject(d));
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 634670757973L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", scriptDatum);
		A_0.Location = 638965725269L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Math2")), A_0, "Log10", ScriptDatum.FromNumber(5.0)));
		A_0.Location = 647555659861L;
		CILHelper.InvokeProperty2(A_0.UserState, A_0, "Test", ScriptDatum.FromNumber(123.45), ScriptDatum.FromString("abc"));
		A_0.Location = 651850627157L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "testIssue1")), A_0);
		A_0.Location = 656145594453L;
		CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "testIssue2")), A_0);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testDatetime(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("Current     Time");
		ScriptDatum arg2 = ScriptDatum.FromString("Current UTC Time");
		ScriptDatum arg3 = ScriptDatum.FromString("yyyy-MM-dd HH:mm:ss fff");
		A_0.Location = 677620430933L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg, CILHelper.InvokeProperty0(ScriptDatum.ToObject(CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Date")), A_0, "now")), A_0, "toString"));
		A_0.Location = 681915398229L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2, CILHelper.InvokeProperty0(ScriptDatum.ToObject(CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Date")), A_0, "utcNow")), A_0, "toString"));
		A_0.Location = 690505332821L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg, CILHelper.InvokeProperty1(ScriptDatum.ToObject(CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Date")), A_0, "now")), A_0, "toString", arg3));
		A_0.Location = 694800300117L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2, CILHelper.InvokeProperty1(ScriptDatum.ToObject(CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Date")), A_0, "utcNow")), A_0, "toString", arg3));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testProxy(ScriptContext A_0)
	{
		A_0.Location = 711980169301L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Testing Proxy"));
		A_0.Location = 716275136597L;
		ScriptDatum scriptDatum = CILHelper.New(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Proxy")), A_0, new ScriptDatum[]
		{
			ScriptDatum.FromObject(new ScriptObject()),
			ScriptDatum.FromObject(CILHelper.CreateObject3("get", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.lambda_167_36), Array.Empty<Upvalue>(), "lambda_167_36")), "set", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(UNIT_LIB.lambda_171_43), Array.Empty<Upvalue>(), "lambda_171_43")), "unset", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.lambda_175_38), Array.Empty<Upvalue>(), "lambda_175_38"))))
		});
		A_0.Location = 780699646037L;
		ScriptDatum.ToObject(scriptDatum).SetPropertyValue(A_0, "string", StringValue.Of("Hello, Proxy!"));
		A_0.Location = 784994613333L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(scriptDatum).GetPropertyDatum(A_0, "string"));
		A_0.Location = 789289580629L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", scriptDatum);
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_167_36(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		A_0.Location = 724865071189L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Getting"), A_2);
		A_0.Location = 729160038485L;
		return CILHelper.GetElement(ScriptDatum.ToObject(A_1), A_2);
	}

	public static ScriptDatum lambda_171_43(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 742044940373L;
		CILHelper.InvokeProperty3(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Setting"), A_2, A_3);
		A_0.Location = 746339907669L;
		CILHelper.SetElement(ScriptDatum.ToObject(A_1), A_2, A_3);
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_175_38(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		A_0.Location = 759224809557L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Deleting"), A_2);
		A_0.Location = 763519776853L;
		CILHelper.DeleteElement(A_0, ScriptDatum.ToObject(A_1), A_2);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testPeculiarity(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Location = 815059384405L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(A_0.UserState));
		A_0.Location = 827944286293L;
		ScriptDatum element = CILHelper.GetElement(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "modules")), ScriptDatum.FromString("TIMER_LIB"));
		A_0.Location = 836534220885L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(element).GetPropertyDatum(A_0, "resetCount"));
		A_0.Location = 849419122773L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(new ScriptArray(A_1)));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testJson(ScriptContext A_0)
	{
		A_0.Location = 883778861141L;
		ScriptDatum arg = CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "time")), A_0, "createTimer");
		A_0.Location = 888073828437L;
		ScriptDatum arg2 = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "stringify", arg, ScriptDatum.FromBoolean(true));
		A_0.Location = 892368795733L;
		ScriptDatum arg3 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "parse", arg2);
		A_0.Location = 896663763029L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2, arg3);
		return ScriptDatum.Null;
	}

	public static ScriptDatum replacer(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		ScriptDatum arg6 = CILHelper.GetArg(A_1, 5);
		A_0.Location = 918138599509L;
		ScriptArray scriptArray = new ScriptArray(3);
		scriptArray.SetElementValue(0, arg2);
		scriptArray.SetElementValue(1, arg3);
		scriptArray.SetElementValue(2, arg4);
		return CILHelper.InvokeProperty1(scriptArray, A_0, "join", ScriptDatum.FromString(" - "));
	}

	public static ScriptDatum testDeConstruct(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(4.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(6.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum5 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum6 = ScriptDatum.FromNumber(3.0);
		ScriptDatum scriptDatum7 = ScriptDatum.FromNumber(7.0);
		ScriptDatum scriptDatum8 = ScriptDatum.FromNumber(8.0);
		A_0.Location = 939613435989L;
		ScriptArray scriptArray = new ScriptArray(3);
		scriptArray.SetElementValue(0, scriptDatum);
		scriptArray.SetElementValue(1, scriptDatum2);
		scriptArray.SetElementValue(2, scriptDatum3);
		ScriptArray val = scriptArray;
		A_0.Location = 943908403285L;
		ScriptArray scriptArray2 = new ScriptArray(0);
		scriptArray2.Push(scriptDatum4);
		scriptArray2.Push(scriptDatum5);
		scriptArray2.Push(scriptDatum6);
		CILHelper.SpreadInto(scriptArray2, val);
		scriptArray2.Push(scriptDatum7);
		scriptArray2.Push(scriptDatum8);
		scriptArray2.Push(ScriptDatum.FromNumber(9.0));
		ScriptArray scriptArray3 = scriptArray2;
		A_0.Location = 948203370581L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(scriptArray3));
		A_0.Location = 956793305173L;
		ScriptObject scriptObject = CILHelper.CreateObject3("d", scriptDatum, "e", scriptDatum2, "f", scriptDatum3);
		A_0.Location = 961088272469L;
		ScriptObject scriptObject2 = new ScriptObject();
		scriptObject2.SetPropertyDatum(A_0, "a", scriptDatum4);
		scriptObject2.SetPropertyDatum(A_0, "b", scriptDatum5);
		scriptObject2.SetPropertyDatum(A_0, "c", scriptDatum6);
		scriptObject2.CopyPropertysFrom(scriptObject, false);
		scriptObject2.SetPropertyDatum(A_0, "g", scriptDatum7);
		scriptObject2.SetPropertyDatum(A_0, "h", scriptDatum8);
		scriptObject2.CopyPropertysFrom(scriptArray3, false);
		ScriptObject value = scriptObject2;
		A_0.Location = 965383239765L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(value));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testRegex(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("The quick brown fox jumps over the lazy dog. It barked.");
		A_0.Location = 1004037945429L;
		ScriptRegex value = RegexManager.Resolve("(?<animal>fox|cat) jumps over", "");
		A_0.Location = 1008332912725L;
		ScriptDatum d = scriptDatum;
		A_0.Location = 1012627880021L;
		ScriptDatum scriptDatum2 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "match", ScriptDatum.FromObject(value));
		A_0.Location = 1016922847317L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(scriptDatum2).GetPropertyDatum(A_0, "groups"), scriptDatum2);
		A_0.Location = 1029807749205L;
		A_0.Location = 1034102716501L;
		ScriptRegex value2 = RegexManager.Resolve("[A-Z]", "g");
		A_0.Location = 1038397683797L;
		ScriptDatum arg = CILHelper.InvokeProperty1(ScriptDatum.ToObject(scriptDatum), A_0, "match", ScriptDatum.FromObject(value2));
		A_0.Location = 1042692651093L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg);
		A_0.Location = 1055577552981L;
		ScriptDatum d2 = ScriptDatum.FromString("For more information, see Chapter 3.4.5.1");
		A_0.Location = 1059872520277L;
		ScriptRegex value3 = RegexManager.Resolve("see(chapter\\d+(\\.\\d)*)", "i");
		A_0.Location = 1064167487573L;
		ScriptDatum arg2 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(d2), A_0, "match", ScriptDatum.FromObject(value3));
		A_0.Location = 1072757422165L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg2);
		A_0.Location = 1081347356757L;
		ScriptRegex value4 = RegexManager.Resolve("t(e)(st(\\d?))", "g");
		A_0.Location = 1089937291349L;
		ScriptDatum d3 = ScriptDatum.FromString("test1test2");
		A_0.Location = 1098527225941L;
		ScriptDatum d4 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(d3), A_0, "matchAll", ScriptDatum.FromObject(value4));
		A_0.Location = 1107117160533L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.GetElement(ScriptDatum.ToObject(d4), ScriptDatum.FromNumber(0.0)));
		A_0.Location = 1120002062421L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.GetElement(ScriptDatum.ToObject(d4), ScriptDatum.FromNumber(1.0)));
		A_0.Location = 1132886964309L;
		ScriptDatum arg3 = CILHelper.InvokeProperty2(StringValue.Of("abc12345#$*%"), A_0, "replace", ScriptDatum.FromObject(RegexManager.Resolve("([^\\d]*)(\\d*)([^\\w]*)", "")), A_0.Module.GetPropertyDatum(A_0, "replacer"));
		A_0.Location = 1137181931605L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg3);
		A_0.Location = 1150066833493L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(RegexManager.Resolve("profile\\.json$", "i")));
		A_0.Location = 1154361800789L;
		ScriptDatum arg4 = CILHelper.InvokeProperty1(RegexManager.Resolve("profile\\.json$", "i"), A_0, "test", ScriptDatum.FromString("profile.json"));
		A_0.Location = 1158656768085L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg4);
		return ScriptDatum.Null;
	}

	public static ScriptDatum createTestContext(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 1188721539157L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "name", A_1);
		scriptObject.SetPropertyDatum(A_0, "passed", ScriptDatum.FromBoolean(true));
		scriptObject.SetPropertyDatum(A_0, "checks", ScriptDatum.FromNumber(0.0));
		scriptObject.SetPropertyDatum(A_0, "failures", ScriptDatum.FromObject(new ScriptArray(0)));
		scriptObject.SetPropertyDatum(A_0, "notes", ScriptDatum.FromObject(new ScriptArray(0)));
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum formatValue(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("");
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(0.0);
		ScriptDatum arg = ScriptDatum.FromString(", ");
		ScriptDatum scriptDatum3 = ScriptDatum.FromString(": ");
		A_0.Location = 1231671212117L;
		if (CILHelper.ToBoolean(CILHelper.Equal(A_1, ScriptDatum.Null)))
		{
			A_0.Location = 1235966179413L;
			return ScriptDatum.FromString("null");
		}
		A_0.Location = 1244556114005L;
		ScriptDatum a = CILHelper.TypeOf(A_1);
		A_0.Location = 1248851081301L;
		ScriptDatum scriptDatum4;
		if (!CILHelper.ToBoolean(scriptDatum4 = CILHelper.Equal(a, ScriptDatum.FromString("number"))))
		{
			scriptDatum4 = CILHelper.Equal(a, ScriptDatum.FromString("string"));
		}
		ScriptDatum a2;
		if (!CILHelper.ToBoolean(a2 = scriptDatum4))
		{
			a2 = CILHelper.Equal(a, ScriptDatum.FromString("boolean"));
		}
		if (CILHelper.ToBoolean(a2))
		{
			A_0.Location = 1253146048597L;
			return CILHelper.AddStringLeft("", A_1);
		}
		A_0.Location = 1261735983189L;
		if (CILHelper.ToBoolean(CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "isArray")), A_0, A_1)))
		{
			A_0.Location = 1266030950485L;
			ScriptArray receiver = new ScriptArray(0);
			A_0.Location = 1270325917781L;
			ScriptDatum scriptDatum5 = scriptDatum2;
			while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum5, CILHelper.GetLength(ScriptDatum.ToObject(A_1), A_0))))
			{
				A_0.Location = 1274620885077L;
				CILHelper.InvokeProperty1(receiver, A_0, "push", CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "formatValue")), A_0, CILHelper.GetElement(ScriptDatum.ToObject(A_1), scriptDatum5)));
				CILHelper.IncrementPostfix(ref scriptDatum5);
			}
			A_0.Location = 1283210819669L;
			return CILHelper.AddStringRight(CILHelper.AddStringLeft("[", CILHelper.InvokeProperty1(receiver, A_0, "join", arg)), "]");
		}
		A_0.Location = 1291800754261L;
		if (CILHelper.ToBoolean(CILHelper.Equal(a, ScriptDatum.FromString("object"))))
		{
			A_0.Location = 1296095721557L;
			ScriptArray scriptArray = new ScriptArray(0);
			ScriptEnumerator enumerator = ScriptDatum.ToObject(A_1).GetEnumerator();
			ScriptDatum scriptDatum6;
			while (enumerator.NextValue(out scriptDatum6))
			{
				A_0.Location = 1300390688853L;
				ScriptObject receiver2 = scriptArray;
				string name = "push";
				ScriptDatum arg2 = scriptDatum6;
				A_0.Location = 1304685656149L;
				CILHelper.InvokeProperty1(receiver2, A_0, name, arg2);
			}
			A_0.Location = 1313275590741L;
			CILHelper.InvokeProperty0(scriptArray, A_0, "sort");
			A_0.Location = 1317570558037L;
			ScriptArray receiver3 = new ScriptArray(0);
			A_0.Location = 1321865525333L;
			ScriptDatum scriptDatum7 = scriptDatum2;
			while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum7, CILHelper.GetLength(scriptArray, A_0))))
			{
				A_0.Location = 1326160492629L;
				ScriptDatum element = CILHelper.GetElement(scriptArray, scriptDatum7);
				A_0.Location = 1330455459925L;
				CILHelper.InvokeProperty1(receiver3, A_0, "push", CILHelper.AddStringMiddle(element, ": ", CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "formatValue")), A_0, CILHelper.GetElement(ScriptDatum.ToObject(A_1), element))));
				CILHelper.IncrementPostfix(ref scriptDatum7);
			}
			A_0.Location = 1339045394517L;
			return CILHelper.AddStringRight(CILHelper.AddStringLeft("{", CILHelper.InvokeProperty1(receiver3, A_0, "join", arg)), "}");
		}
		A_0.Location = 1347635329109L;
		if (CILHelper.ToBoolean(CILHelper.Equal(a, ScriptDatum.FromString("function"))))
		{
			A_0.Location = 1351930296405L;
			return ScriptDatum.FromString("[function]");
		}
		A_0.Location = 1360520230997L;
		return CILHelper.AddStringLeft("", A_1);
	}

	public static ScriptDatum addFailure(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3, ScriptDatum A_4)
	{
		A_0.Location = 1377700100181L;
		ScriptObject receiver = ScriptDatum.ToObject(ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "failures"));
		string name = "push";
		string name2 = "message";
		string name3 = "actual";
		ScriptObject function = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "formatValue"));
		A_0.Location = 1386290034773L;
		ScriptDatum value = CILHelper.Invoke1(function, A_0, A_3);
		string name4 = "expected";
		ScriptObject function2 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "formatValue"));
		A_0.Location = 1390585002069L;
		ScriptDatum arg = ScriptDatum.FromObject(CILHelper.CreateObject3(name2, A_2, name3, value, name4, CILHelper.Invoke1(function2, A_0, A_4)));
		A_0.Location = 1377700100181L;
		CILHelper.InvokeProperty1(receiver, A_0, name, arg);
		return ScriptDatum.Null;
	}

	public static ScriptDatum expectTrue(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		A_0.Location = 1412059838549L;
		ScriptDatum.ToObject(arg).SetPropertyValue(A_0, "checks", ScriptDatum.ToObject(CILHelper.Add(ScriptDatum.ToObject(arg).GetPropertyDatum(A_0, "checks"), ScriptDatum.FromNumber(1.0))));
		A_0.Location = 1416354805845L;
		if (CILHelper.ToBoolean(CILHelper.Not(arg2)))
		{
			A_0.Location = 1420649773141L;
			ScriptDatum.ToObject(arg).SetPropertyValue(A_0, "passed", BooleanValue.Of(false));
			A_0.Location = 1424944740437L;
			CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "addFailure")), A_0, arg, arg3, arg4, arg5);
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum expectFalse(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 1446419576917L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectTrue")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			CILHelper.Not(A_2),
			A_3,
			A_2,
			ScriptDatum.FromBoolean(false)
		});
		return ScriptDatum.Null;
	}

	public static ScriptDatum expectEqual(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3, ScriptDatum A_4)
	{
		A_0.Location = 1463599446101L;
		ScriptDatum scriptDatum = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "deepEqual")), A_0, A_2, A_3);
		A_0.Location = 1467894413397L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectTrue")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			scriptDatum,
			A_4,
			A_2,
			A_3
		});
		return ScriptDatum.Null;
	}

	public static ScriptDatum expectNearlyEqual(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		A_0.Location = 1485074282581L;
		ScriptDatum a = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "absolute")), A_0, CILHelper.Subtract(arg2, arg3));
		A_0.Location = 1489369249877L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectTrue")).Invoke(A_0, new ScriptDatum[]
		{
			arg,
			CILHelper.LessEqual(a, arg4),
			CILHelper.AddStringRight(CILHelper.AddStringMiddle(arg5, " (±", arg4), ")"),
			arg2,
			arg3
		});
		return ScriptDatum.Null;
	}

	public static ScriptDatum addNote(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		A_0.Location = 1506549119061L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "notes")), A_0, "push", A_2);
		return ScriptDatum.Null;
	}

	public static ScriptDatum isArray(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 1523728988245L;
		return CILHelper.Equal(CILHelper.TypeOf(A_1), ScriptDatum.FromString("array"));
	}

	public static ScriptDatum deepEqual(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum a = ScriptDatum.FromNumber(1.0);
		ScriptDatum result = ScriptDatum.FromBoolean(true);
		ScriptDatum result2 = ScriptDatum.FromBoolean(false);
		ScriptDatum @null = ScriptDatum.Null;
		A_0.Location = 1540908857429L;
		if (CILHelper.ToBoolean(CILHelper.Equal(A_1, A_2)))
		{
			A_0.Location = 1545203824725L;
			if (CILHelper.ToBoolean(CILHelper.Equal(A_1, scriptDatum)))
			{
				A_0.Location = 1549498792021L;
				return CILHelper.Equal(CILHelper.Divide(a, A_1), CILHelper.Divide(a, A_2));
			}
			A_0.Location = 1558088726613L;
			return result;
		}
		else
		{
			A_0.Location = 1566678661205L;
			ScriptDatum a2 = CILHelper.TypeOf(A_1);
			A_0.Location = 1570973628501L;
			ScriptDatum b = CILHelper.TypeOf(A_2);
			A_0.Location = 1575268595797L;
			if (CILHelper.ToBoolean(CILHelper.NotEqual(a2, b)))
			{
				A_0.Location = 1579563563093L;
				return result2;
			}
			A_0.Location = 1588153497685L;
			ScriptDatum scriptDatum2;
			if (!CILHelper.ToBoolean(scriptDatum2 = CILHelper.Equal(a2, ScriptDatum.FromString("number"))))
			{
				scriptDatum2 = CILHelper.Equal(a2, ScriptDatum.FromString("string"));
			}
			ScriptDatum a3;
			if (!CILHelper.ToBoolean(a3 = scriptDatum2))
			{
				a3 = CILHelper.Equal(a2, ScriptDatum.FromString("boolean"));
			}
			if (CILHelper.ToBoolean(a3))
			{
				A_0.Location = 1592448464981L;
				return CILHelper.Equal(A_1, A_2);
			}
			A_0.Location = 1601038399573L;
			ScriptDatum a4;
			if (!CILHelper.ToBoolean(a4 = CILHelper.Equal(A_1, @null)))
			{
				a4 = CILHelper.Equal(A_2, @null);
			}
			if (CILHelper.ToBoolean(a4))
			{
				A_0.Location = 1605333366869L;
				return CILHelper.Equal(A_1, A_2);
			}
			A_0.Location = 1613923301461L;
			ScriptDatum a5;
			if (CILHelper.ToBoolean(a5 = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "isArray")), A_0, A_1)))
			{
				a5 = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "isArray")), A_0, A_2);
			}
			if (CILHelper.ToBoolean(a5))
			{
				A_0.Location = 1618218268757L;
				if (CILHelper.ToBoolean(CILHelper.NotEqual(CILHelper.GetLength(ScriptDatum.ToObject(A_1), A_0), CILHelper.GetLength(ScriptDatum.ToObject(A_2), A_0))))
				{
					A_0.Location = 1622513236053L;
					return result2;
				}
				A_0.Location = 1631103170645L;
				ScriptDatum scriptDatum3 = scriptDatum;
				while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, CILHelper.GetLength(ScriptDatum.ToObject(A_1), A_0))))
				{
					A_0.Location = 1635398137941L;
					if (CILHelper.ToBoolean(CILHelper.Not(CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "deepEqual")), A_0, CILHelper.GetElement(ScriptDatum.ToObject(A_1), scriptDatum3), CILHelper.GetElement(ScriptDatum.ToObject(A_2), scriptDatum3)))))
					{
						A_0.Location = 1639693105237L;
						return result2;
					}
					CILHelper.IncrementPostfix(ref scriptDatum3);
				}
				A_0.Location = 1652578007125L;
				return result;
			}
			else
			{
				A_0.Location = 1661167941717L;
				if (!CILHelper.ToBoolean(CILHelper.Equal(a2, ScriptDatum.FromString("object"))))
				{
					A_0.Location = 1768542124117L;
					return result2;
				}
				A_0.Location = 1665462909013L;
				ScriptArray scriptArray = new ScriptArray(0);
				A_0.Location = 1669757876309L;
				ScriptArray scriptArray2 = new ScriptArray(0);
				ScriptEnumerator enumerator = ScriptDatum.ToObject(A_1).GetEnumerator();
				ScriptDatum scriptDatum4;
				while (enumerator.NextValue(out scriptDatum4))
				{
					A_0.Location = 1674052843605L;
					ScriptObject receiver = scriptArray;
					string name = "push";
					ScriptDatum arg = scriptDatum4;
					A_0.Location = 1678347810901L;
					CILHelper.InvokeProperty1(receiver, A_0, name, arg);
				}
				ScriptEnumerator enumerator2 = ScriptDatum.ToObject(A_2).GetEnumerator();
				ScriptDatum scriptDatum5;
				while (enumerator2.NextValue(out scriptDatum5))
				{
					A_0.Location = 1686937745493L;
					ScriptObject receiver2 = scriptArray2;
					string name2 = "push";
					ScriptDatum arg2 = scriptDatum5;
					A_0.Location = 1691232712789L;
					CILHelper.InvokeProperty1(receiver2, A_0, name2, arg2);
				}
				A_0.Location = 1699822647381L;
				CILHelper.InvokeProperty0(scriptArray, A_0, "sort");
				A_0.Location = 1704117614677L;
				CILHelper.InvokeProperty0(scriptArray2, A_0, "sort");
				A_0.Location = 1708412581973L;
				if (CILHelper.ToBoolean(CILHelper.NotEqual(CILHelper.GetLength(scriptArray, A_0), CILHelper.GetLength(scriptArray2, A_0))))
				{
					A_0.Location = 1712707549269L;
					return result2;
				}
				A_0.Location = 1721297483861L;
				ScriptDatum scriptDatum6 = scriptDatum;
				while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum6, CILHelper.GetLength(scriptArray, A_0))))
				{
					A_0.Location = 1725592451157L;
					if (CILHelper.ToBoolean(CILHelper.NotEqual(CILHelper.GetElement(scriptArray, scriptDatum6), CILHelper.GetElement(scriptArray2, scriptDatum6))))
					{
						A_0.Location = 1729887418453L;
						return result2;
					}
					A_0.Location = 1738477353045L;
					ScriptDatum element = CILHelper.GetElement(scriptArray, scriptDatum6);
					A_0.Location = 1742772320341L;
					if (CILHelper.ToBoolean(CILHelper.Not(CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "deepEqual")), A_0, CILHelper.GetElement(ScriptDatum.ToObject(A_1), element), CILHelper.GetElement(ScriptDatum.ToObject(A_2), element)))))
					{
						A_0.Location = 1747067287637L;
						return result2;
					}
					CILHelper.IncrementPostfix(ref scriptDatum6);
				}
				A_0.Location = 1759952189525L;
				return result;
			}
		}
	}

	public static ScriptDatum absolute(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 1785721993301L;
		if (CILHelper.ToBoolean(CILHelper.Less(A_1, ScriptDatum.FromNumber(0.0))))
		{
			A_0.Location = 1790016960597L;
			return CILHelper.Negate(A_1);
		}
		A_0.Location = 1798606895189L;
		return A_1;
	}

	public static ScriptDatum executeTest(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("    ");
		ScriptDatum scriptDatum2 = ScriptDatum.FromString(" | actual=");
		ScriptDatum scriptDatum3 = ScriptDatum.FromString(" expected=");
		A_0.Location = 1815786764373L;
		ScriptDatum scriptDatum4 = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "createTestContext")), A_0, ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "name"));
		A_0.Location = 1820081731669L;
		ScriptDatum arg = CILHelper.AddStringLeft("test:", ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "name"));
		A_0.Location = 1824376698965L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "time", arg);
		A_0.Location = 1828671666261L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_1), A_0, "run", scriptDatum4);
		A_0.Location = 1832966633557L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "timeEnd", arg);
		A_0.Location = 1837261600853L;
		if (CILHelper.ToBoolean(ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "passed")))
		{
			A_0.Location = 1841556568149L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringRight(CILHelper.AddStringMiddle(CILHelper.AddStringLeft("[PASS] ", ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "name")), " (", ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "checks")), " checks)"));
		}
		else
		{
			A_0.Location = 1845851535445L;
			ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name = "log";
			ScriptDatum arg2 = CILHelper.AddStringRight(CILHelper.AddStringMiddle(CILHelper.AddStringLeft("[FAIL] ", ScriptDatum.ToObject(A_1).GetPropertyDatum(A_0, "name")), " -> ", CILHelper.GetLength(ScriptDatum.ToObject(ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "failures")), A_0)), " issue(s)");
			A_0.Location = 1850146502741L;
			CILHelper.InvokeProperty1(receiver, A_0, name, arg2);
			A_0.Location = 1854441470037L;
			ScriptDatum scriptDatum5 = ScriptDatum.FromNumber(0.0);
			while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum5, CILHelper.GetLength(ScriptDatum.ToObject(ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "failures")), A_0))))
			{
				A_0.Location = 1858736437333L;
				ScriptDatum element = CILHelper.GetElement(ScriptDatum.ToObject(ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "failures")), scriptDatum5);
				A_0.Location = 1863031404629L;
				CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringMiddle(CILHelper.AddStringMiddle(CILHelper.AddStringLeft("    ", ScriptDatum.ToObject(element).GetPropertyDatum(A_0, "message")), " | actual=", ScriptDatum.ToObject(element).GetPropertyDatum(A_0, "actual")), " expected=", ScriptDatum.ToObject(element).GetPropertyDatum(A_0, "expected")));
				CILHelper.IncrementPostfix(ref scriptDatum5);
			}
		}
		A_0.Location = 1875916306517L;
		return scriptDatum4;
	}

	public static ScriptDatum benchmarkNumbers(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(97.0);
		ScriptDatum b2 = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(1000000.0));
		A_0.Location = 1893096175701L;
		ScriptDatum scriptDatum2 = scriptDatum;
		A_0.Location = 1897391142997L;
		ScriptDatum scriptDatum3 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, b2)))
		{
			A_0.Location = 1901686110293L;
			scriptDatum2 = CILHelper.Modulo(CILHelper.Add(scriptDatum2, scriptDatum3), b);
			CILHelper.IncrementPostfix(ref scriptDatum3);
		}
		A_0.Location = 1910276044885L;
		return scriptDatum2;
	}

	public static ScriptDatum benchmarkArrays(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(200000.0));
		A_0.Location = 1927455914069L;
		ScriptArray scriptArray = new ScriptArray(0);
		A_0.Location = 1931750881365L;
		ScriptDatum scriptDatum2 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum2, b)))
		{
			A_0.Location = 1936045848661L;
			CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum2);
			CILHelper.IncrementPostfix(ref scriptDatum2);
		}
		A_0.Location = 1944635783253L;
		ScriptDatum scriptDatum3 = scriptDatum;
		A_0.Location = 1948930750549L;
		ScriptDatum scriptDatum4 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum4, CILHelper.GetLength(scriptArray, A_0))))
		{
			A_0.Location = 1953225717845L;
			scriptDatum3 = CILHelper.Add(scriptDatum3, CILHelper.GetElement(scriptArray, scriptDatum4));
			CILHelper.IncrementPostfix(ref scriptDatum4);
		}
		A_0.Location = 1961815652437L;
		return scriptDatum3;
	}

	public static ScriptDatum benchmarkClosure(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(500000.0));
		ClosureFunction function = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.makeCounter), Array.Empty<Upvalue>(), "makeCounter");
		A_0.Location = 2009060292693L;
		ScriptDatum d = CILHelper.Invoke0(function, A_0);
		A_0.Location = 2013355259989L;
		ScriptDatum result = scriptDatum;
		A_0.Location = 2017650227285L;
		ScriptDatum a = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			A_0.Location = 2021945194581L;
			result = CILHelper.Invoke0(ScriptDatum.ToObject(d), A_0);
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 2030535129173L;
		return result;
	}

	public static ScriptDatum makeCounter(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue()
		};
		A_0.Location = 1983290488917L;
		array[0].Value = ScriptDatum.FromNumber(0.0);
		A_0.Location = 1987585456213L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_462_26), array, "lambda_462_26"));
	}

	public static ScriptDatum lambda_462_26(ScriptContext A_0)
	{
		A_0.Location = 1991880423509L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 1996175390805L;
		return A_0.Upvalues[0].Value;
	}

	public static ScriptDatum benchmarkObjects(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(7.0);
		ScriptDatum b2 = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(150000.0));
		A_0.Location = 2047714998357L;
		ScriptDatum scriptDatum2 = scriptDatum;
		A_0.Location = 2052009965653L;
		ScriptDatum scriptDatum3 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, b2)))
		{
			A_0.Location = 2056304932949L;
			ScriptObject scriptObject = new ScriptObject();
			scriptObject.SetPropertyDatum(A_0, "index", scriptDatum3);
			scriptObject.SetPropertyDatum(A_0, "value", CILHelper.BitwiseAnd(scriptDatum3, b));
			ScriptObject scriptObject2 = scriptObject;
			A_0.Location = 2060599900245L;
			scriptObject2.SetPropertyValue(A_0, "sum", ScriptDatum.ToObject(CILHelper.Add(scriptObject2.GetPropertyDatum(A_0, "index"), scriptObject2.GetPropertyDatum(A_0, "value"))));
			A_0.Location = 2064894867541L;
			scriptDatum2 = CILHelper.Add(scriptDatum2, scriptObject2.GetPropertyDatum(A_0, "sum"));
			CILHelper.IncrementPostfix(ref scriptDatum3);
		}
		A_0.Location = 2073484802133L;
		return scriptDatum2;
	}

	public static ScriptDatum benchmarkStrings(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("a");
		ScriptDatum b = ScriptDatum.FromNumber(32.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(16.0);
		ScriptDatum a = ScriptDatum.FromString("Combined multiplication and subtraction");
		ScriptDatum scriptDatum3 = ScriptDatum.FromString("sss");
		ScriptDatum b3 = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(80000.0));
		A_0.Location = 2090664671317L;
		ScriptDatum scriptDatum4 = ScriptDatum.FromString("");
		A_0.Location = 2094959638613L;
		ScriptDatum a2 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a2, b3)))
		{
			A_0.Location = 2099254605909L;
			scriptDatum4 = CILHelper.AddStringRight(scriptDatum4, "a");
			A_0.Location = 2103549573205L;
			if (CILHelper.ToBoolean(CILHelper.Greater(CILHelper.GetLength(ScriptDatum.ToObject(scriptDatum4), A_0), b)))
			{
				A_0.Location = 2107844540501L;
				scriptDatum4 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(scriptDatum4), A_0, "substring", CILHelper.Subtract(CILHelper.GetLength(ScriptDatum.ToObject(scriptDatum4), A_0), b2));
			}
			CILHelper.IncrementPostfix(ref a2);
		}
		A_0.Location = 2120729442389L;
		a2 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a2, b3)))
		{
			A_0.Location = 2125024409685L;
			ScriptDatum scriptDatum5 = CILHelper.AddStringRight(a, "sss");
			CILHelper.IncrementPostfix(ref a2);
		}
		A_0.Location = 2137909311573L;
		return CILHelper.GetLength(ScriptDatum.ToObject(scriptDatum4), A_0);
	}

	public static ScriptDatum factorial(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		A_0.Location = 2601765779541L;
		if (CILHelper.ToBoolean(CILHelper.LessEqual(A_1, scriptDatum)))
		{
			A_0.Location = 2606060746837L;
			return scriptDatum;
		}
		A_0.Location = 2614650681429L;
		return CILHelper.Multiply(A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "factorial")), A_0, CILHelper.Subtract(A_1, scriptDatum)));
	}

	public static ScriptDatum testAllUnits(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(1.0);
		A_0.Location = 2945363163221L;
		ScriptArray scriptArray = new ScriptArray(0);
		A_0.Location = 2949658130517L;
		ScriptArray scriptArray2 = new ScriptArray(0);
		A_0.Location = 2953953097813L;
		ScriptDatum scriptDatum2 = scriptDatum;
		A_0.Location = 2958248065109L;
		ScriptDatum scriptDatum3 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, CILHelper.GetLength(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "__testCases")), A_0))))
		{
			A_0.Location = 2966837999701L;
			ScriptDatum scriptDatum4 = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "executeTest")), A_0, CILHelper.GetElement(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "__testCases")), scriptDatum3));
			A_0.Location = 2971132966997L;
			CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum4);
			A_0.Location = 2975427934293L;
			if (CILHelper.ToBoolean(ScriptDatum.ToObject(scriptDatum4).GetPropertyDatum(A_0, "passed")))
			{
				A_0.Location = 2979722901589L;
				scriptDatum2 = CILHelper.Add(scriptDatum2, b);
			}
			else
			{
				A_0.Location = 2984017868885L;
				ScriptObject receiver = scriptArray2;
				string name = "push";
				ScriptDatum arg = scriptDatum4;
				A_0.Location = 2988312836181L;
				CILHelper.InvokeProperty1(receiver, A_0, name, arg);
			}
			CILHelper.IncrementPostfix(ref scriptDatum3);
		}
		A_0.Location = 3001197738069L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "total", CILHelper.GetLength(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "__testCases")), A_0));
		scriptObject.SetPropertyDatum(A_0, "passed", scriptDatum2);
		scriptObject.SetPropertyDatum(A_0, "failed", CILHelper.Subtract(CILHelper.GetLength(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "__testCases")), A_0), scriptDatum2));
		scriptObject.SetPropertyDatum(A_0, "cases", ScriptDatum.FromObject(scriptArray));
		scriptObject.SetPropertyDatum(A_0, "failedCases", ScriptDatum.FromObject(scriptArray2));
		scriptObject.SetPropertyDatum(A_0, "success", CILHelper.Equal(CILHelper.GetLength(scriptArray2, A_0), scriptDatum));
		ScriptObject scriptObject2 = scriptObject;
		A_0.Location = 3035557476437L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("==== AuroraScript unit tests ===="));
		A_0.Location = 3039852443733L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringMiddle(CILHelper.AddStringMiddle(CILHelper.AddStringLeft("Total: ", scriptObject2.GetPropertyDatum(A_0, "total")), ", Passed: ", scriptObject2.GetPropertyDatum(A_0, "passed")), ", Failed: ", scriptObject2.GetPropertyDatum(A_0, "failed")));
		A_0.Location = 3044147411029L;
		return ScriptDatum.FromObject(scriptObject2);
	}

	public static ScriptDatum test(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("time.createTimer");
		A_0.Location = 3061327280213L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "time", arg);
		A_0.Location = 3065622247509L;
		ScriptDatum scriptDatum = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "time")), A_0, "createTimer", ScriptDatum.FromString("unit.timer"), ScriptDatum.FromNumber(128.0));
		A_0.Location = 3069917214805L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "timeEnd", arg);
		A_0.Location = 3074212182101L;
		ScriptDatum.ToObject(scriptDatum).SetPropertyValue(A_0, "start", ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "start_timer")));
		A_0.Location = 3078507149397L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(scriptDatum), A_0, "start");
		A_0.Location = 3082802116693L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(scriptDatum), A_0, "reset");
		A_0.Location = 3087097083989L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(scriptDatum), A_0, "cancel");
		A_0.Location = 3091392051285L;
		return scriptDatum;
	}

	public static ScriptDatum start_timer(ScriptContext A_0)
	{
		A_0.Location = 3108571920469L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("timer start."));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testIterator(ScriptContext A_0)
	{
		ScriptDatum value = ScriptDatum.FromString("Hello");
		ScriptDatum scriptDatum = ScriptDatum.FromString(" = ");
		ScriptDatum d = ScriptDatum.FromString("Hello World!");
		A_0.Location = 3125751789653L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "A", ScriptDatum.FromNumber(1.0));
		scriptObject.SetPropertyDatum(A_0, "B", ScriptDatum.FromNumber(2.0));
		scriptObject.SetPropertyDatum(A_0, "C", ScriptDatum.FromNumber(3.0));
		scriptObject.SetPropertyDatum(A_0, "D", ScriptDatum.FromNumber(4.0));
		scriptObject.SetPropertyDatum(A_0, "E", value);
		scriptObject.SetPropertyDatum(A_0, "F", ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_733_22), Array.Empty<Upvalue>(), "lambda_733_22")));
		ScriptObject scriptObject2 = scriptObject;
		A_0.Location = 3164406495317L;
		ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object"));
		string name = "assign";
		ScriptDatum arg = ScriptDatum.FromObject(scriptObject2);
		ScriptObject scriptObject3 = new ScriptObject();
		scriptObject3.SetPropertyDatum(A_0, "你好", value);
		scriptObject2 = ScriptDatum.ToObject(CILHelper.InvokeProperty2(receiver, A_0, name, arg, ScriptDatum.FromObject(scriptObject3)));
		A_0.Location = 3172996429909L;
		scriptObject2 = ScriptDatum.ToObject(CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object")), A_0, "clone", ScriptDatum.FromObject(scriptObject2)));
		ScriptEnumerator enumerator = scriptObject2.GetEnumerator();
		ScriptDatum scriptDatum2;
		while (enumerator.NextValue(out scriptDatum2))
		{
			A_0.Location = 3177291397205L;
			ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console"));
			string name2 = "log";
			ScriptDatum arg2 = CILHelper.AddStringMiddle(scriptDatum2, " = ", CILHelper.GetElement(scriptObject2, scriptDatum2));
			A_0.Location = 3181586364501L;
			CILHelper.InvokeProperty1(receiver2, A_0, name2, arg2);
		}
		A_0.Location = 3190176299093L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Object")), A_0, "keys", ScriptDatum.FromObject(scriptObject2)));
		ScriptEnumerator enumerator2 = ScriptDatum.ToObject(d).GetEnumerator();
		ScriptDatum arg3;
		while (enumerator2.NextValue(out arg3))
		{
			A_0.Location = 3194471266389L;
			CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg3);
		}
		A_0.Location = 3207356168277L;
		ScriptDatum arg4 = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Array")), A_0, "from", ScriptDatum.FromString("ABCDEFG"), ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(UNIT_LIB.lambda_746_49), Array.Empty<Upvalue>(), "lambda_746_49")));
		A_0.Location = 3211651135573L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", arg4);
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_733_22(ScriptContext A_0)
	{
		A_0.Location = 3151521593429L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("reset"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_746_49(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		return CILHelper.Add(A_1, A_2);
	}

	public static ScriptDatum deepInterruption(ScriptContext A_0)
	{
		ClosureFunction function = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lll), Array.Empty<Upvalue>(), "lll");
		A_0.Location = 3254600808533L;
		CILHelper.Invoke0(function, A_0);
		return ScriptDatum.Null;
	}

	public static ScriptDatum lll(ScriptContext A_0)
	{
		A_0.Location = 3237420939349L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Start testInterruption"));
		A_0.Location = 3241715906645L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "md5")), A_0, "throwMethod");
		A_0.Location = 3246010873941L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("End testInterruption"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testFor(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum b = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(1000000.0));
		A_0.Location = 3271780677717L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			CILHelper.IncrementPostfix(ref a);
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum testArray(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = CILHelper.TryGetArg(A_1, 0, ScriptDatum.FromNumber(1000000.0));
		A_0.Location = 3306140416085L;
		ScriptArray obj = new ScriptArray(0);
		A_0.Location = 3310435383381L;
		ScriptDatum scriptDatum2 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum2, b)))
		{
			A_0.Location = 3314730350677L;
			CILHelper.SetElement(obj, scriptDatum, scriptDatum2);
			CILHelper.IncrementPostfix(ref scriptDatum2);
		}
		return ScriptDatum.Null;
	}

	public static ScriptDatum testDeconstruction(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(2.0);
		A_0.Location = 3344795121749L;
		ScriptArray scriptArray = new ScriptArray(9);
		scriptArray.SetElementValue(0, scriptDatum);
		scriptArray.SetElementValue(1, scriptDatum2);
		scriptArray.SetElementValue(2, ScriptDatum.FromNumber(3.0));
		scriptArray.SetElementValue(3, ScriptDatum.FromNumber(4.0));
		scriptArray.SetElementValue(4, ScriptDatum.FromNumber(5.0));
		scriptArray.SetElementValue(5, ScriptDatum.FromNumber(6.0));
		scriptArray.SetElementValue(6, ScriptDatum.FromNumber(7.0));
		scriptArray.SetElementValue(7, ScriptDatum.FromNumber(8.0));
		scriptArray.SetElementValue(8, ScriptDatum.FromNumber(9.0));
		ScriptArray scriptArray2 = scriptArray;
		A_0.Location = 3349090089045L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "ac", scriptDatum);
		scriptObject.SetPropertyDatum(A_0, "bc", scriptDatum2);
		ScriptDatum scriptDatum3 = ScriptDatum.FromObject(scriptObject.GetPropertyValue(A_0, "k"));
		ScriptDatum scriptDatum4 = ScriptDatum.FromObject(scriptObject.GetPropertyValue(A_0, "l"));
		ScriptDatum scriptDatum5 = ScriptDatum.FromObject(scriptObject.GetPropertyValue(A_0, "ac"));
		ScriptDatum scriptDatum6 = ScriptDatum.FromObject(scriptObject.GetPropertyValue(A_0, "bc"));
		A_0.Location = 3353385056341L;
		ScriptArray scriptArray3 = (ScriptArray)scriptArray2;
		ScriptDatum scriptDatum7;
		scriptArray3.SliceTo(0, scriptArray3.Length - 2, ref scriptDatum7);
		ScriptDatum element = scriptArray3.GetElement(scriptArray3.Length - 2);
		ScriptDatum element2 = scriptArray3.GetElement(scriptArray3.Length - 1);
		A_0.Location = 3357680023637L;
		ScriptArray scriptArray4 = (ScriptArray)scriptArray2;
		ScriptDatum element3 = scriptArray4.GetElement(0);
		ScriptDatum scriptDatum8;
		scriptArray4.SliceTo(1, scriptArray4.Length - 1, ref scriptDatum8);
		ScriptDatum element4 = scriptArray4.GetElement(scriptArray4.Length - 1);
		A_0.Location = 3361974990933L;
		ScriptArray scriptArray5 = (ScriptArray)scriptArray2;
		ScriptDatum element5 = scriptArray5.GetElement(0);
		ScriptDatum element6 = scriptArray5.GetElement(1);
		ScriptDatum scriptDatum9;
		scriptArray5.SliceTo(2, scriptArray5.Length - 0, ref scriptDatum9);
		return ScriptDatum.Null;
	}

	public static ScriptDatum testClrFunc(ScriptContext A_0)
	{
		ScriptDatum b = ScriptDatum.FromNumber(10000.0);
		ScriptDatum d = ScriptDatum.FromString("MK");
		ScriptDatum arg = ScriptDatum.FromNumber(123.0);
		ScriptDatum arg2 = ScriptDatum.FromString("Hello");
		ScriptDatum datum = ScriptDatum.FromString("[");
		ScriptDatum datum2 = ScriptDatum.FromString("-");
		ScriptDatum datum3 = ScriptDatum.FromString("]");
		A_0.Location = 3396334729301L;
		ScriptDatum scriptDatum = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Number")), A_0, ScriptDatum.FromString("055"));
		A_0.Location = 3404924663893L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "fo")).GetPropertyDatum(A_0, "Name"));
		A_0.Location = 3409219631189L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			A_0.Location = 3413514598485L;
			ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "fo")).SetPropertyValue(A_0, "Name", ScriptDatum.ToObject(d));
			A_0.Location = 3417809565781L;
			CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "fo")), A_0, "Say", arg, arg2);
			A_0.Location = 3422104533077L;
			ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "TestObject"));
			string name = "Cat";
			ScriptArray scriptArray = new ScriptArray(3);
			scriptArray.SetElementValue(0, datum);
			scriptArray.SetElementValue(1, datum2);
			scriptArray.SetElementValue(2, datum3);
			ScriptDatum scriptDatum2 = CILHelper.InvokeProperty1(receiver, A_0, name, ScriptDatum.FromObject(scriptArray));
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 3430694467669L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "fo")).GetPropertyDatum(A_0, "Name"));
		A_0.Location = 3434989434965L;
		ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "TestObject"));
		string name2 = "Cat";
		ScriptArray scriptArray2 = new ScriptArray(3);
		scriptArray2.SetElementValue(0, datum);
		scriptArray2.SetElementValue(1, datum2);
		scriptArray2.SetElementValue(2, datum3);
		ScriptDatum arg3 = CILHelper.InvokeProperty1(receiver2, A_0, name2, ScriptDatum.FromObject(scriptArray2));
		A_0.Location = 3439284402261L;
		CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Eat"), arg3);
		A_0.Location = 3443579369557L;
		return ScriptDatum.FromBoolean(true);
	}

	public static ScriptDatum closure1(ScriptContext A_0)
	{
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue(),
			new Upvalue()
		};
		ClosureFunction function = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.makeCounter1), array, "makeCounter1");
		ClosureFunction function2 = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.makeCounter2), array, "makeCounter2");
		A_0.Location = 3460759238741L;
		array[1].Value = ScriptDatum.FromString("123");
		A_0.Location = 3465054206037L;
		array[0].Value = ScriptDatum.FromNumber(0.0);
		A_0.Location = 3529478715477L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "a", CILHelper.Invoke0(function, A_0));
		scriptObject.SetPropertyDatum(A_0, "b", CILHelper.Invoke0(function2, A_0));
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum makeCounter1(ScriptContext A_0)
	{
		A_0.Location = 3473644140629L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_808_26), A_0.Upvalues, "lambda_808_26"));
	}

	public static ScriptDatum lambda_808_26(ScriptContext A_0)
	{
		A_0.Location = 3477939107925L;
		A_0.Upvalues[1].Value = ScriptDatum.FromString("ABC");
		A_0.Location = 3482234075221L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 3486529042517L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "title", A_0.Upvalues[1].Value);
		scriptObject.SetPropertyDatum(A_0, "count", A_0.Upvalues[0].Value);
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum makeCounter2(ScriptContext A_0)
	{
		A_0.Location = 3503708911701L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_815_26), A_0.Upvalues, "lambda_815_26"));
	}

	public static ScriptDatum lambda_815_26(ScriptContext A_0)
	{
		A_0.Location = 3508003878997L;
		A_0.Upvalues[1].Value = ScriptDatum.FromString("XYZ");
		A_0.Location = 3512298846293L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 3516593813589L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "title", A_0.Upvalues[1].Value);
		scriptObject.SetPropertyDatum(A_0, "count", A_0.Upvalues[0].Value);
		return ScriptDatum.FromObject(scriptObject);
	}

	public static ScriptDatum testTypeOf(ScriptContext A_0)
	{
		A_0.Location = 3546658584661L;
		ScriptDatum a = CILHelper.InvokeProperty0(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Date")), A_0, "now");
		A_0.Location = 3550953551957L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "v", ScriptDatum.FromNumber(11.0));
		ScriptObject value = scriptObject;
		A_0.Location = 3555248519253L;
		ScriptArray scriptArray = new ScriptArray(3);
		scriptArray.SetElementValue(0, ScriptDatum.FromNumber(1.0));
		scriptArray.SetElementValue(1, ScriptDatum.FromNumber(2.0));
		scriptArray.SetElementValue(2, ScriptDatum.FromNumber(3.0));
		ScriptArray scriptArray2 = scriptArray;
		A_0.Location = 3559543486549L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(ScriptDatum.FromNumber(123.0)));
		A_0.Location = 3563838453845L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(ScriptDatum.FromString("avc")));
		A_0.Location = 3568133421141L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(ScriptDatum.FromBoolean(true)));
		A_0.Location = 3572428388437L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(ScriptDatum.FromObject(value)));
		A_0.Location = 3576723355733L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(ScriptDatum.FromObject(scriptArray2)));
		A_0.Location = 3581018323029L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(scriptArray2.GetPropertyDatum(A_0, "push")));
		A_0.Location = 3585313290325L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.TypeOf(a));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testClosure(ScriptContext A_0)
	{
		A_0.Location = 3606788126805L;
		ScriptDatum d = CILHelper.Invoke0(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "closure1")), A_0);
		A_0.Location = 3611083094101L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "stringify", CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "a")));
		A_0.Location = 3615378061397L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "stringify", CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "b")));
		A_0.Location = 3619673028693L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "stringify", CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "a")));
		A_0.Location = 3623967995989L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "JSON")), A_0, "stringify", CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "b")));
		return ScriptDatum.Null;
	}

	public static ScriptDatum testMD5(ScriptContext A_0)
	{
		ScriptDatum arg = ScriptDatum.FromString("MD5_SUM");
		A_0.Location = 3641147865173L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "time", arg);
		A_0.Location = 3645442832469L;
		ScriptDatum scriptDatum = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "md5")), A_0, "MD5", ScriptDatum.FromString("12345"));
		A_0.Location = 3649737799765L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "timeEnd", arg);
		A_0.Location = 3654032767061L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.AddStringLeft("\"12345\" md5 is ", scriptDatum));
		A_0.Location = 3658327734357L;
		return scriptDatum;
	}

	public static ScriptDatum testMD5_1000(ScriptContext A_0)
	{
		ScriptDatum b = ScriptDatum.FromNumber(1000.0);
		ScriptDatum arg = ScriptDatum.FromString("12345");
		A_0.Location = 3675507603541L;
		ScriptDatum result = ScriptDatum.FromString("");
		A_0.Location = 3679802570837L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(a, b)))
		{
			A_0.Location = 3684097538133L;
			result = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "md5")), A_0, "MD5", arg);
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 3692687472725L;
		return result;
	}

	public static ScriptDatum testDraw(ScriptContext A_0)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("*");
		ScriptDatum scriptDatum3 = ScriptDatum.FromString(" ");
		A_0.Location = 3722752243797L;
		ScriptDatum d = CILHelper.New(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "StringBuffer")), A_0, new ScriptDatum[]
		{
			ScriptDatum.FromString("\n")
		});
		A_0.Location = 3727047211093L;
		ScriptDatum a = scriptDatum;
		A_0.Location = 3731342178389L;
		ScriptDatum a2 = scriptDatum;
		A_0.Location = 3735637145685L;
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(21.0);
		A_0.Location = 3739932112981L;
		ScriptDatum scriptDatum5 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Math")), A_0, "round", CILHelper.Divide(scriptDatum4, ScriptDatum.FromNumber(2.0)));
		A_0.Location = 3744227080277L;
		ScriptDatum scriptDatum6 = scriptDatum5;
		A_0.Location = 3748522047573L;
		ScriptDatum scriptDatum7 = ScriptDatum.FromNumber(1.0);
		a = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a, scriptDatum4)))
		{
			A_0.Location = 3757111982165L;
			a2 = scriptDatum;
			while (CILHelper.ToBoolean(CILHelper.LessEqual(a2, scriptDatum6)))
			{
				A_0.Location = 3765701916757L;
				ScriptObject receiver = ScriptDatum.ToObject(d);
				string name = "append";
				ScriptDatum arg = scriptDatum2;
				A_0.Location = 3769996884053L;
				CILHelper.InvokeProperty1(receiver, A_0, name, arg);
				CILHelper.IncrementPostfix(ref a2);
			}
			while (CILHelper.ToBoolean(CILHelper.Less(a2, scriptDatum5)))
			{
				A_0.Location = 3782881785941L;
				ScriptObject receiver2 = ScriptDatum.ToObject(d);
				string name2 = "append";
				ScriptDatum arg2 = scriptDatum3;
				A_0.Location = 3787176753237L;
				CILHelper.InvokeProperty1(receiver2, A_0, name2, arg2);
				A_0.Location = 3791471720533L;
				CILHelper.IncrementPostfix(ref a2);
			}
			while (CILHelper.ToBoolean(CILHelper.Less(a2, scriptDatum4)))
			{
				A_0.Location = 3804356622421L;
				ScriptObject receiver3 = ScriptDatum.ToObject(d);
				string name3 = "append";
				ScriptDatum arg3 = scriptDatum2;
				A_0.Location = 3808651589717L;
				CILHelper.InvokeProperty1(receiver3, A_0, name3, arg3);
				A_0.Location = 3812946557013L;
				CILHelper.IncrementPostfix(ref a2);
			}
			A_0.Location = 3821536491605L;
			CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "appendLine");
			A_0.Location = 3825831458901L;
			if (CILHelper.ToBoolean(CILHelper.Equal(scriptDatum6, scriptDatum)))
			{
				scriptDatum7 = CILHelper.Negate(scriptDatum7);
			}
			A_0.Location = 3830126426197L;
			scriptDatum6 = CILHelper.Subtract(scriptDatum6, scriptDatum7);
			A_0.Location = 3834421393493L;
			scriptDatum5 = CILHelper.Add(scriptDatum5, scriptDatum7);
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 3843011328085L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "stringAndRelease"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_32_14(ScriptContext A_0)
	{
		A_0.Location = 140749518933L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("reset"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_500_43(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b = ScriptDatum.FromNumber(100.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(7.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(5.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(3.0);
		A_0.Location = 2155089180757L;
		ScriptDatum scriptDatum3 = scriptDatum;
		A_0.Location = 2159384148053L;
		ScriptDatum scriptDatum4 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.LessEqual(scriptDatum4, b)))
		{
			A_0.Location = 2163679115349L;
			scriptDatum3 = CILHelper.Add(scriptDatum3, scriptDatum4);
			CILHelper.IncrementPostfix(ref scriptDatum4);
		}
		A_0.Location = 2172269049941L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptDatum3, ScriptDatum.FromNumber(5050.0), ScriptDatum.FromString("Sum from 0 to 100 inclusive"));
		A_0.Location = 2176564017237L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Subtract(CILHelper.Multiply(scriptDatum2, ScriptDatum.FromNumber(6.0)), ScriptDatum.FromNumber(4.0)), ScriptDatum.FromNumber(38.0), ScriptDatum.FromString("Combined multiplication and subtraction"));
		A_0.Location = 2180858984533L;
		ScriptDatum arg = CILHelper.Modulo(ScriptDatum.FromNumber(59.0), ScriptDatum.FromNumber(12.0));
		A_0.Location = 2185153951829L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, arg, ScriptDatum.FromNumber(11.0), ScriptDatum.FromString("Modulo remainder check"));
		A_0.Location = 2189448919125L;
		ScriptDatum scriptDatum5 = ScriptDatum.FromNumber(1.0);
		A_0.Location = 2193743886421L;
		ScriptDatum a = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a, b2)))
		{
			A_0.Location = 2198038853717L;
			scriptDatum5 = CILHelper.Multiply(scriptDatum5, b3);
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 2206628788309L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptDatum5, ScriptDatum.FromNumber(243.0), ScriptDatum.FromString("Repeated multiplication"));
		A_0.Location = 2210923755605L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectNearlyEqual")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			CILHelper.Divide(ScriptDatum.FromNumber(22.0), scriptDatum2),
			ScriptDatum.FromNumber(3.142857),
			ScriptDatum.FromNumber(0.0005),
			ScriptDatum.FromString("Fraction approximates PI")
		});
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_517_40(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(32.0);
		ScriptDatum arg = ScriptDatum.FromNumber(8.0);
		A_0.Location = 2228103624789L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.BitwiseAnd(scriptDatum, ScriptDatum.FromNumber(3.0)), scriptDatum2, ScriptDatum.FromString("Bitwise AND"));
		A_0.Location = 2232398592085L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.BitwiseOr(scriptDatum, scriptDatum3), ScriptDatum.FromNumber(7.0), ScriptDatum.FromString("Bitwise OR"));
		A_0.Location = 2236693559381L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.BitwiseXor(scriptDatum, scriptDatum2), ScriptDatum.FromNumber(4.0), ScriptDatum.FromString("Bitwise XOR"));
		A_0.Location = 2240988526677L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.BitwiseNot(scriptDatum2), CILHelper.Negate(scriptDatum3), ScriptDatum.FromString("Bitwise NOT"));
		A_0.Location = 2245283493973L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.LeftShift(scriptDatum2, scriptDatum), scriptDatum4, ScriptDatum.FromString("Left shift"));
		A_0.Location = 2249578461269L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.RightShift(scriptDatum4, scriptDatum3), arg, ScriptDatum.FromString("Right shift"));
		A_0.Location = 2253873428565L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.UnsignedRightShift(scriptDatum4, scriptDatum3), arg, ScriptDatum.FromString("Unsigned right shift"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_527_39(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 2271053297749L;
		ScriptDatum d = ScriptDatum.FromString("Aurora");
		A_0.Location = 2275348265045L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0), ScriptDatum.FromNumber(6.0), ScriptDatum.FromString("length property"));
		A_0.Location = 2279643232341L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "toUpperCase"), ScriptDatum.FromString("AURORA"), ScriptDatum.FromString("toUpperCase"));
		A_0.Location = 2283938199637L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "toLowerCase"), ScriptDatum.FromString("aurora"), ScriptDatum.FromString("toLowerCase"));
		A_0.Location = 2288233166933L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "substring", ScriptDatum.FromNumber(1.0), ScriptDatum.FromNumber(3.0)), ScriptDatum.FromString("ur"), ScriptDatum.FromString("substring extracts range"));
		A_0.Location = 2292528134229L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "indexOf", ScriptDatum.FromString("ro")), ScriptDatum.FromNumber(2.0), ScriptDatum.FromString("indexOf finds substring"));
		A_0.Location = 2296823101525L;
		ScriptDatum scriptDatum = CILHelper.InvokeProperty2(ScriptDatum.ToObject(d), A_0, "replace", ScriptDatum.FromString("Aur"), ScriptDatum.FromString("St"));
		A_0.Location = 2301118068821L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptDatum, ScriptDatum.FromString("Stora"), ScriptDatum.FromString("replace updates prefix"));
		A_0.Location = 2305413036117L;
		ScriptDatum scriptDatum2 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(scriptDatum), A_0, "split", ScriptDatum.FromString("r"));
		A_0.Location = 2309708003413L;
		ScriptObject function = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual"));
		ScriptDatum arg = scriptDatum2;
		ScriptArray scriptArray = new ScriptArray(2);
		scriptArray.SetElementValue(0, ScriptDatum.FromString("Sto"));
		scriptArray.SetElementValue(1, ScriptDatum.FromString("a"));
		CILHelper.Invoke4(function, A_0, A_1, arg, ScriptDatum.FromObject(scriptArray), ScriptDatum.FromString("split produces expected parts"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_540_46(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum arg = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(3.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(4.0);
		A_0.Location = 2326887872597L;
		ScriptArray scriptArray = new ScriptArray(0);
		A_0.Location = 2331182839893L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(scriptArray, A_0), scriptDatum, ScriptDatum.FromString("Empty array length is zero"));
		A_0.Location = 2335477807189L;
		CILHelper.InvokeProperty1(scriptArray, A_0, "push", arg);
		A_0.Location = 2339772774485L;
		CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum2);
		A_0.Location = 2344067741781L;
		CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum3);
		A_0.Location = 2348362709077L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(scriptArray, A_0), scriptDatum3, ScriptDatum.FromString("Push updates length"));
		A_0.Location = 2352657676373L;
		ScriptDatum arg2 = CILHelper.InvokeProperty0(scriptArray, A_0, "pop");
		A_0.Location = 2356952643669L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, arg2, scriptDatum3, ScriptDatum.FromString("Pop returns last element"));
		A_0.Location = 2361247610965L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(scriptArray, A_0), scriptDatum2, ScriptDatum.FromString("Pop reduces length"));
		A_0.Location = 2365542578261L;
		CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum3);
		A_0.Location = 2369837545557L;
		CILHelper.InvokeProperty1(scriptArray, A_0, "push", scriptDatum4);
		A_0.Location = 2374132512853L;
		ScriptDatum d = CILHelper.InvokeProperty2(scriptArray, A_0, "slice", arg, scriptDatum4);
		A_0.Location = 2378427480149L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0), scriptDatum3, ScriptDatum.FromString("Slice returns correct length"));
		A_0.Location = 2382722447445L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetElement(ScriptDatum.ToObject(d), scriptDatum), scriptDatum2, ScriptDatum.FromString("Slice first element"));
		A_0.Location = 2387017414741L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetElement(ScriptDatum.ToObject(d), scriptDatum2), scriptDatum4, ScriptDatum.FromString("Slice last element"));
		A_0.Location = 2391312382037L;
		ScriptDatum scriptDatum5 = scriptDatum;
		A_0.Location = 2395607349333L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromObject(scriptArray));
		A_0.Location = 2399902316629L;
		ScriptDatum scriptDatum6 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum6, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0))))
		{
			A_0.Location = 2404197283925L;
			scriptDatum5 = CILHelper.Add(scriptDatum5, CILHelper.GetElement(ScriptDatum.ToObject(d), scriptDatum6));
			CILHelper.IncrementPostfix(ref scriptDatum6);
		}
		A_0.Location = 2412787218517L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptDatum5, CILHelper.Add(CILHelper.Add(scriptDatum2, scriptDatum3), scriptDatum4), ScriptDatum.FromString("Sum via indexed loop"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_564_43(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(2.0);
		ScriptDatum b = ScriptDatum.FromString("value");
		ScriptDatum scriptDatum3 = ScriptDatum.FromBoolean(true);
		A_0.Location = 2429967087701L;
		ScriptObject scriptObject = new ScriptObject();
		scriptObject.SetPropertyDatum(A_0, "id", scriptDatum);
		string key = "nested";
		ScriptObject scriptObject2 = new ScriptObject();
		scriptObject2.SetPropertyDatum(A_0, "value", scriptDatum2);
		scriptObject.SetPropertyDatum(A_0, key, ScriptDatum.FromObject(scriptObject2));
		ScriptObject scriptObject3 = scriptObject;
		A_0.Location = 2434262054997L;
		scriptObject3.SetPropertyValue(A_0, "name", StringValue.Of("Aurora"));
		A_0.Location = 2438557022293L;
		ScriptDatum.ToObject(scriptObject3.GetPropertyDatum(A_0, "nested")).SetPropertyValue(A_0, "extra", StringValue.Of("script"));
		A_0.Location = 2442851989589L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptObject3.GetPropertyDatum(A_0, "id"), scriptDatum, ScriptDatum.FromString("Direct property access"));
		A_0.Location = 2447146956885L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetProperty2(scriptObject3, A_0, "nested", "value"), scriptDatum2, ScriptDatum.FromString("Nested property access"));
		A_0.Location = 2451441924181L;
		ScriptDatum.ToObject(scriptObject3.GetPropertyDatum(A_0, "nested")).SetPropertyValue(A_0, "value", ScriptDatum.ToObject(CILHelper.Add(CILHelper.GetProperty2(scriptObject3, A_0, "nested", "value"), ScriptDatum.FromNumber(3.0))));
		A_0.Location = 2455736891477L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetProperty2(scriptObject3, A_0, "nested", "value"), ScriptDatum.FromNumber(5.0), ScriptDatum.FromString("Nested property reassignment"));
		A_0.Location = 2460031858773L;
		ScriptArray scriptArray = new ScriptArray(0);
		ScriptEnumerator enumerator = scriptObject3.GetEnumerator();
		ScriptDatum scriptDatum4;
		while (enumerator.NextValue(out scriptDatum4))
		{
			A_0.Location = 2464326826069L;
			ScriptObject receiver = scriptArray;
			string name = "push";
			ScriptDatum arg = scriptDatum4;
			A_0.Location = 2468621793365L;
			CILHelper.InvokeProperty1(receiver, A_0, name, arg);
		}
		A_0.Location = 2477211727957L;
		CILHelper.InvokeProperty0(scriptArray, A_0, "sort");
		A_0.Location = 2481506695253L;
		ScriptObject function = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual"));
		ScriptDatum arg2 = ScriptDatum.FromObject(scriptArray);
		ScriptArray scriptArray2 = new ScriptArray(3);
		scriptArray2.SetElementValue(0, ScriptDatum.FromString("id"));
		scriptArray2.SetElementValue(1, ScriptDatum.FromString("name"));
		scriptArray2.SetElementValue(2, ScriptDatum.FromString("nested"));
		CILHelper.Invoke4(function, A_0, A_1, arg2, ScriptDatum.FromObject(scriptArray2), ScriptDatum.FromString("for-in enumerates object keys"));
		A_0.Location = 2485801662549L;
		CILHelper.DeleteProperty(A_0, ScriptDatum.ToObject(scriptObject3.GetPropertyDatum(A_0, "nested")), "value");
		A_0.Location = 2490096629845L;
		ScriptDatum arg3 = ScriptDatum.FromBoolean(false);
		ScriptEnumerator enumerator2 = ScriptDatum.ToObject(scriptObject3.GetPropertyDatum(A_0, "nested")).GetEnumerator();
		ScriptDatum a;
		while (enumerator2.NextValue(out a))
		{
			A_0.Location = 2494391597141L;
			if (CILHelper.ToBoolean(CILHelper.Equal(a, b)))
			{
				A_0.Location = 2498686564437L;
				arg3 = scriptDatum3;
			}
		}
		A_0.Location = 2515866433621L;
		CILHelper.Invoke3(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectFalse")), A_0, A_1, arg3, ScriptDatum.FromString("delete removes nested property"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_588_41(ScriptContext A_0, ScriptDatum A_1)
	{
		ClosureFunction function = new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(UNIT_LIB.makeCounter), Array.Empty<Upvalue>(), "makeCounter");
		A_0.Location = 2563111073877L;
		ScriptDatum d = CILHelper.Invoke1(function, A_0, ScriptDatum.FromNumber(0.0));
		A_0.Location = 2567406041173L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke0(ScriptDatum.ToObject(d), A_0), ScriptDatum.FromNumber(1.0), ScriptDatum.FromString("Closure increments first call"));
		A_0.Location = 2571701008469L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke0(ScriptDatum.ToObject(d), A_0), ScriptDatum.FromNumber(2.0), ScriptDatum.FromString("Closure increments second call"));
		A_0.Location = 2575995975765L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke0(ScriptDatum.ToObject(d), A_0), ScriptDatum.FromNumber(3.0), ScriptDatum.FromString("Closure increments third call"));
		A_0.Location = 2580290943061L;
		ScriptDatum d2 = CILHelper.Invoke1(function, A_0, ScriptDatum.FromNumber(10.0));
		A_0.Location = 2584585910357L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke0(ScriptDatum.ToObject(d2), A_0), ScriptDatum.FromNumber(11.0), ScriptDatum.FromString("Independent closure maintains own state"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum makeCounter(ScriptContext A_0, ScriptDatum A_1)
	{
		Upvalue[] array = new Upvalue[]
		{
			new Upvalue()
		};
		A_0.Location = 2537341270101L;
		array[0].Value = A_1;
		A_0.Location = 2541636237397L;
		return ScriptDatum.FromObject(new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(UNIT_LIB.lambda_591_26), array, "lambda_591_26"));
	}

	public static ScriptDatum lambda_591_26(ScriptContext A_0)
	{
		A_0.Location = 2545931204693L;
		A_0.Upvalues[0].Value = CILHelper.Add(A_0.Upvalues[0].Value, ScriptDatum.FromNumber(1.0));
		A_0.Location = 2550226171989L;
		return A_0.Upvalues[0].Value;
	}

	public static ScriptDatum lambda_611_47(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		A_0.Location = 2631830550613L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "factorial")), A_0, scriptDatum), scriptDatum, ScriptDatum.FromString("Factorial 1"));
		A_0.Location = 2636125517909L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "factorial")), A_0, ScriptDatum.FromNumber(5.0)), ScriptDatum.FromNumber(120.0), ScriptDatum.FromString("Factorial 5"));
		A_0.Location = 2640420485205L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "factorial")), A_0, ScriptDatum.FromNumber(7.0)), ScriptDatum.FromNumber(5040.0), ScriptDatum.FromString("Factorial 7"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_617_46(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum datum = ScriptDatum.FromString("a");
		ScriptDatum datum2 = ScriptDatum.FromString("b");
		ScriptDatum datum3 = ScriptDatum.FromString("c");
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(3.0);
		A_0.Location = 2657600354389L;
		ScriptDatum d = ScriptDatum.FromString("abc");
		A_0.Location = 2661895321685L;
		ScriptArray scriptArray = new ScriptArray(0);
		ScriptEnumerator enumerator = ScriptDatum.ToObject(d).GetEnumerator();
		ScriptDatum scriptDatum4;
		while (enumerator.NextValue(out scriptDatum4))
		{
			A_0.Location = 2666190288981L;
			ScriptObject receiver = scriptArray;
			string name = "push";
			ScriptDatum arg = scriptDatum4;
			A_0.Location = 2670485256277L;
			CILHelper.InvokeProperty1(receiver, A_0, name, arg);
		}
		A_0.Location = 2679075190869L;
		ScriptObject function = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual"));
		ScriptDatum arg2 = ScriptDatum.FromObject(scriptArray);
		ScriptArray scriptArray2 = new ScriptArray(3);
		scriptArray2.SetElementValue(0, datum);
		scriptArray2.SetElementValue(1, datum2);
		scriptArray2.SetElementValue(2, datum3);
		CILHelper.Invoke4(function, A_0, A_1, arg2, ScriptDatum.FromObject(scriptArray2), ScriptDatum.FromString("for-in iterates string characters"));
		A_0.Location = 2683370158165L;
		ScriptObject scriptObject = CILHelper.CreateObject3("a", scriptDatum, "b", scriptDatum2, "c", scriptDatum3);
		A_0.Location = 2687665125461L;
		ScriptArray scriptArray3 = new ScriptArray(0);
		ScriptEnumerator enumerator2 = scriptObject.GetEnumerator();
		ScriptDatum scriptDatum5;
		while (enumerator2.NextValue(out scriptDatum5))
		{
			A_0.Location = 2691960092757L;
			ScriptObject receiver2 = scriptArray3;
			string name2 = "push";
			ScriptDatum arg3 = scriptDatum5;
			A_0.Location = 2696255060053L;
			CILHelper.InvokeProperty1(receiver2, A_0, name2, arg3);
		}
		A_0.Location = 2704844994645L;
		CILHelper.InvokeProperty0(scriptArray3, A_0, "sort");
		A_0.Location = 2709139961941L;
		ScriptObject function2 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual"));
		ScriptDatum arg4 = ScriptDatum.FromObject(scriptArray3);
		ScriptArray scriptArray4 = new ScriptArray(3);
		scriptArray4.SetElementValue(0, datum);
		scriptArray4.SetElementValue(1, datum2);
		scriptArray4.SetElementValue(2, datum3);
		CILHelper.Invoke4(function2, A_0, A_1, arg4, ScriptDatum.FromObject(scriptArray4), ScriptDatum.FromString("for-in iterates object keys"));
		A_0.Location = 2713434929237L;
		ScriptArray scriptArray5 = new ScriptArray(3);
		scriptArray5.SetElementValue(0, scriptDatum);
		scriptArray5.SetElementValue(1, scriptDatum2);
		scriptArray5.SetElementValue(2, scriptDatum3);
		ScriptArray scriptArray6 = scriptArray5;
		A_0.Location = 2717729896533L;
		ScriptArray scriptArray7 = new ScriptArray(0);
		ScriptEnumerator enumerator3 = scriptArray6.GetEnumerator();
		ScriptDatum scriptDatum6;
		while (enumerator3.NextValue(out scriptDatum6))
		{
			A_0.Location = 2722024863829L;
			ScriptObject receiver3 = scriptArray7;
			string name3 = "push";
			ScriptDatum arg5 = scriptDatum6;
			A_0.Location = 2726319831125L;
			CILHelper.InvokeProperty1(receiver3, A_0, name3, arg5);
		}
		A_0.Location = 2734909765717L;
		ScriptObject function3 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual"));
		ScriptDatum arg6 = ScriptDatum.FromObject(scriptArray7);
		ScriptArray scriptArray8 = new ScriptArray(3);
		scriptArray8.SetElementValue(0, ScriptDatum.FromString("1"));
		scriptArray8.SetElementValue(1, ScriptDatum.FromString("2"));
		scriptArray8.SetElementValue(2, ScriptDatum.FromString("3"));
		CILHelper.Invoke4(function3, A_0, A_1, arg6, ScriptDatum.FromObject(scriptArray8), ScriptDatum.FromString("for-in iterates array indexes as strings"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_639_38(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 2752089634901L;
		ScriptDatum arg = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "md5")), A_0, "MD5", ScriptDatum.FromString("12345"));
		A_0.Location = 2756384602197L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, arg, ScriptDatum.FromString("87c0ee87643a69f47"), ScriptDatum.FromString("MD5 of 12345"));
		A_0.Location = 2760679569493L;
		ScriptDatum arg2 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "md5")), A_0, "MD5", ScriptDatum.FromString("AuroraScript"));
		A_0.Location = 2764974536789L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, arg2, ScriptDatum.FromString("6b30c036a3cb25f3db"), ScriptDatum.FromString("MD5 of AuroraScript"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_646_40(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(64.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("function");
		ScriptDatum @null = ScriptDatum.Null;
		A_0.Location = 2782154405973L;
		ScriptDatum d = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "time")), A_0, "createTimer", ScriptDatum.FromString("unit.timer"), scriptDatum);
		A_0.Location = 2786449373269L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "interval"), scriptDatum, ScriptDatum.FromString("Timer keeps custom interval"));
		A_0.Location = 2790744340565L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectTrue")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			CILHelper.Equal(CILHelper.TypeOf(ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "reset")), scriptDatum2),
			ScriptDatum.FromString("Timer exposes reset function"),
			CILHelper.TypeOf(ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "reset")),
			scriptDatum2
		});
		A_0.Location = 2795039307861L;
		CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "reset");
		A_0.Location = 2799334275157L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "count"), ScriptDatum.FromNumber(0.0), ScriptDatum.FromString("Reset clears counter to zero"));
		A_0.Location = 2803629242453L;
		ScriptDatum arg = CILHelper.InvokeProperty0(ScriptDatum.ToObject(d), A_0, "cancel");
		A_0.Location = 2807924209749L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, arg, ScriptDatum.FromBoolean(true), ScriptDatum.FromString("Cancel returns true"));
		A_0.Location = 2812219177045L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "cancel"), @null, ScriptDatum.FromString("Cancel clears cancel handler"));
		A_0.Location = 2816514144341L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, ScriptDatum.ToObject(d).GetPropertyDatum(A_0, "reset"), @null, ScriptDatum.FromString("Cancel clears reset handler"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_658_49(ScriptContext A_0, ScriptDatum A_1)
	{
		A_0.Location = 2833694013525L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectNearlyEqual")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Math")).GetPropertyDatum(A_0, "PI"),
			ScriptDatum.FromNumber(3.141592653589793),
			ScriptDatum.FromNumber(1E-10),
			ScriptDatum.FromString("PI injected from host")
		});
		A_0.Location = 2837988980821L;
		ScriptDatum b = ScriptDatum.FromNumber(2.0);
		A_0.Location = 2842283948117L;
		ScriptDatum scriptDatum = CILHelper.Multiply(CILHelper.Multiply(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Math")).GetPropertyDatum(A_0, "PI"), b), b);
		A_0.Location = 2846578915413L;
		ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectNearlyEqual")).Invoke(A_0, new ScriptDatum[]
		{
			A_1,
			scriptDatum,
			ScriptDatum.FromNumber(12.566370614359172),
			ScriptDatum.FromNumber(1E-07),
			ScriptDatum.FromString("Area uses host constant")
		});
		return ScriptDatum.Null;
	}

	public static ScriptDatum lambda_665_48(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(10.0);
		ScriptDatum arg = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(3.0);
		A_0.Location = 2863758784597L;
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(1000.0);
		A_0.Location = 2868053751893L;
		ScriptArray scriptArray = new ScriptArray(0);
		A_0.Location = 2872348719189L;
		ScriptDatum a = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(a, scriptDatum4)))
		{
			A_0.Location = 2876643686485L;
			CILHelper.InvokeProperty1(scriptArray, A_0, "push", CILHelper.Modulo(a, scriptDatum2));
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 2885233621077L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.GetLength(scriptArray, A_0), scriptDatum4, ScriptDatum.FromString("Prepared array length matches iterations"));
		A_0.Location = 2889528588373L;
		ScriptDatum scriptDatum5 = scriptDatum;
		A_0.Location = 2893823555669L;
		ScriptDatum scriptDatum6 = scriptDatum;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum6, CILHelper.GetLength(scriptArray, A_0))))
		{
			A_0.Location = 2898118522965L;
			scriptDatum5 = CILHelper.Add(scriptDatum5, CILHelper.GetElement(scriptArray, scriptDatum6));
			CILHelper.IncrementPostfix(ref scriptDatum6);
		}
		A_0.Location = 2906708457557L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, scriptDatum5, ScriptDatum.FromNumber(4500.0), ScriptDatum.FromString("Sum of modular sequence"));
		A_0.Location = 2911003424853L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "benchmarkNumbers")), A_0, scriptDatum2), ScriptDatum.FromNumber(45.0), ScriptDatum.FromString("benchmarkNumbers deterministic check"));
		A_0.Location = 2915298392149L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "benchmarkArrays")), A_0, arg), scriptDatum2, ScriptDatum.FromString("benchmarkArrays deterministic check"));
		A_0.Location = 2919593359445L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "benchmarkClosure")), A_0, scriptDatum3), scriptDatum3, ScriptDatum.FromString("benchmarkClosure deterministic check"));
		A_0.Location = 2923888326741L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "benchmarkObjects")), A_0, arg), ScriptDatum.FromNumber(20.0), ScriptDatum.FromString("benchmarkObjects deterministic check"));
		A_0.Location = 2928183294037L;
		CILHelper.Invoke4(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "expectEqual")), A_0, A_1, CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "benchmarkStrings")), A_0, scriptDatum2), scriptDatum2, ScriptDatum.FromString("benchmarkStrings deterministic check"));
		return ScriptDatum.Null;
	}
}
