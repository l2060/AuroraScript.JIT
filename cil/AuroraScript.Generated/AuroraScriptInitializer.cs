using System;
using AuroraScript.Runtime;

public class AuroraScriptInitializer
{
	public static void InitializeDomain(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptGlobal global = A_0.Global;
		global.RegisterModule("l123", -927918079, new ScriptModule("l123", "l123.as"));
		global.RegisterModule("TIMER_LIB", 1351512498, new ScriptModule("TIMER_LIB", "timer.as"));
		global.RegisterModule("test", 529485870, new ScriptModule("test", "test.as"));
		global.RegisterModule("MD5_LIB", 1216728942, new ScriptModule("MD5_LIB", "md5.as"));
		global.RegisterModule("mmmmm1", 1906613181, new ScriptModule("mmmmm1", "mmmmm1.as"));
		global.RegisterModule("MAIN", 1668072333, new ScriptModule("MAIN", "main.as"));
		global.RegisterModule("constant", 1522828447, new ScriptModule("constant", "constant.as"));
		global.RegisterModule("reproduce_closure", -1830436473, new ScriptModule("reproduce_closure", "reproduce_closure.as"));
		global.RegisterModule("libs/timer", 2005083747, new ScriptModule("libs/timer", "libs/timer.as"));
		global.RegisterModule("UNIT_LIB", -984401835, new ScriptModule("UNIT_LIB", "unit.as"));
		l123.Initialize(A_0.With(global.GetModule("l123"), null), A_1);
		TIMER_LIB.Initialize(A_0.With(global.GetModule("TIMER_LIB"), null), A_1);
		test.Initialize(A_0.With(global.GetModule("test"), null), A_1);
		MD5_LIB.Initialize(A_0.With(global.GetModule("MD5_LIB"), null), A_1);
		mmmmm1.Initialize(A_0.With(global.GetModule("mmmmm1"), null), A_1);
		MAIN.Initialize(A_0.With(global.GetModule("MAIN"), null), A_1);
		constant.Initialize(A_0.With(global.GetModule("constant"), null), A_1);
		reproduce_closure.Initialize(A_0.With(global.GetModule("reproduce_closure"), null), A_1);
		libs/timer.Initialize(A_0.With(global.GetModule("libs/timer"), null), A_1);
		UNIT_LIB.Initialize(A_0.With(global.GetModule("UNIT_LIB"), null), A_1);
	}
}
