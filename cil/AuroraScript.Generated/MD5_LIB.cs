using System;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;

public sealed class MD5_LIB
{
	public static void Initialize(ScriptContext A_0, ScriptDatum[] A_1)
	{
		A_0.Module.Define("throwMethod", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate0(MD5_LIB.throwMethod), Array.Empty<Upvalue>(), "throwMethod"), false, true);
		A_0.Module.Define("RotateLeft", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(MD5_LIB.RotateLeft), Array.Empty<Upvalue>(), "RotateLeft"), false, true);
		A_0.Module.Define("AddUnsigned", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate2(MD5_LIB.AddUnsigned), Array.Empty<Upvalue>(), "AddUnsigned"), false, true);
		A_0.Module.Define("F", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(MD5_LIB.F), Array.Empty<Upvalue>(), "F"), false, true);
		A_0.Module.Define("G", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(MD5_LIB.G), Array.Empty<Upvalue>(), "G"), false, true);
		A_0.Module.Define("H", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(MD5_LIB.H), Array.Empty<Upvalue>(), "H"), false, true);
		A_0.Module.Define("I", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate3(MD5_LIB.I), Array.Empty<Upvalue>(), "I"), false, true);
		A_0.Module.Define("FF", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(MD5_LIB.FF), Array.Empty<Upvalue>(), "FF"), false, true);
		A_0.Module.Define("GG", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(MD5_LIB.GG), Array.Empty<Upvalue>(), "GG"), false, true);
		A_0.Module.Define("HH", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(MD5_LIB.HH), Array.Empty<Upvalue>(), "HH"), false, true);
		A_0.Module.Define("II", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate(MD5_LIB.II), Array.Empty<Upvalue>(), "II"), false, true);
		A_0.Module.Define("ConvertToWordArray", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.ConvertToWordArray), Array.Empty<Upvalue>(), "ConvertToWordArray"), false, true);
		A_0.Module.Define("WordToHex", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.WordToHex), Array.Empty<Upvalue>(), "WordToHex"), false, true);
		A_0.Module.Define("Utf8Encode", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.Utf8Encode), Array.Empty<Upvalue>(), "Utf8Encode"), false, true);
		A_0.Module.Define("WordToHex_str", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.WordToHex_str), Array.Empty<Upvalue>(), "WordToHex_str"), false, true);
		A_0.Module.Define("Utf8Encode_str", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.Utf8Encode_str), Array.Empty<Upvalue>(), "Utf8Encode_str"), false, true);
		A_0.Module.Define("MD5", new ClosureFunction(A_0.Domain, A_0.Module, new ScriptFunctionDelegate1(MD5_LIB.MD5), Array.Empty<Upvalue>(), "MD5"), false, true);
	}

	public static ScriptDatum throwMethod(ScriptContext A_0)
	{
		A_0.Location = 48461369198L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("Start testError"));
		A_0.Location = 52756336494L;
		ScriptDatum scriptDatum = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "xxxx")), A_0, "c", ScriptDatum.FromNumber(1.0));
		A_0.Location = 57051303790L;
		CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "console")), A_0, "log", ScriptDatum.FromString("End testError"));
		return ScriptDatum.Null;
	}

	public static ScriptDatum RotateLeft(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		A_0.Location = 78526140270L;
		return CILHelper.BitwiseOr(CILHelper.LeftShift(A_1, A_2), CILHelper.UnsignedRightShift(A_1, CILHelper.Subtract(ScriptDatum.FromNumber(32.0), A_2)));
	}

	public static ScriptDatum AddUnsigned(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2)
	{
		ScriptDatum b = ScriptDatum.FromNumber(2147483648.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(1073741824.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(1073741823.0);
		A_0.Location = 100000976750L;
		A_0.Location = 104295944046L;
		A_0.Location = 108590911342L;
		A_0.Location = 112885878638L;
		A_0.Location = 117180845934L;
		A_0.Location = 121475813230L;
		ScriptDatum b4 = CILHelper.BitwiseAnd(A_1, b);
		A_0.Location = 125770780526L;
		ScriptDatum b5 = CILHelper.BitwiseAnd(A_2, b);
		A_0.Location = 130065747822L;
		ScriptDatum a = CILHelper.BitwiseAnd(A_1, b2);
		A_0.Location = 134360715118L;
		ScriptDatum b6 = CILHelper.BitwiseAnd(A_2, b2);
		A_0.Location = 138655682414L;
		ScriptDatum a2 = CILHelper.Add(CILHelper.BitwiseAnd(A_1, b3), CILHelper.BitwiseAnd(A_2, b3));
		A_0.Location = 142950649710L;
		if (CILHelper.ToBoolean(CILHelper.BitwiseAnd(a, b6)))
		{
			A_0.Location = 147245617006L;
			return CILHelper.BitwiseXor(CILHelper.BitwiseXor(CILHelper.BitwiseXor(a2, b), b4), b5);
		}
		A_0.Location = 155835551598L;
		if (!CILHelper.ToBoolean(CILHelper.BitwiseOr(a, b6)))
		{
			A_0.Location = 181605355374L;
			return CILHelper.BitwiseXor(CILHelper.BitwiseXor(a2, b4), b5);
		}
		A_0.Location = 160130518894L;
		if (CILHelper.ToBoolean(CILHelper.BitwiseAnd(a2, b2)))
		{
			A_0.Location = 164425486190L;
			return CILHelper.BitwiseXor(CILHelper.BitwiseXor(CILHelper.BitwiseXor(a2, ScriptDatum.FromNumber(3221225472.0)), b4), b5);
		}
		A_0.Location = 168720453486L;
		return CILHelper.BitwiseXor(CILHelper.BitwiseXor(CILHelper.BitwiseXor(a2, b2), b4), b5);
	}

	public static ScriptDatum F(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 203080191854L;
		return CILHelper.BitwiseOr(CILHelper.BitwiseAnd(A_1, A_2), CILHelper.BitwiseAnd(CILHelper.BitwiseNot(A_1), A_3));
	}

	public static ScriptDatum G(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 207375159150L;
		return CILHelper.BitwiseOr(CILHelper.BitwiseAnd(A_1, A_3), CILHelper.BitwiseAnd(A_2, CILHelper.BitwiseNot(A_3)));
	}

	public static ScriptDatum H(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 211670126446L;
		return CILHelper.BitwiseXor(CILHelper.BitwiseXor(A_1, A_2), A_3);
	}

	public static ScriptDatum I(ScriptContext A_0, ScriptDatum A_1, ScriptDatum A_2, ScriptDatum A_3)
	{
		A_0.Location = 215965093742L;
		return CILHelper.BitwiseXor(A_2, CILHelper.BitwiseOr(A_1, CILHelper.BitwiseNot(A_3)));
	}

	public static ScriptDatum FF(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		ScriptDatum arg6 = CILHelper.GetArg(A_1, 5);
		ScriptDatum arg7 = CILHelper.GetArg(A_1, 6);
		A_0.Location = 228849995630L;
		arg = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, arg, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke3(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "F")), A_0, arg2, arg3, arg4), arg5), arg7));
		A_0.Location = 233144962926L;
		return CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "RotateLeft")), A_0, arg, arg6), arg2);
	}

	public static ScriptDatum GG(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		ScriptDatum arg6 = CILHelper.GetArg(A_1, 5);
		ScriptDatum arg7 = CILHelper.GetArg(A_1, 6);
		A_0.Location = 250324832110L;
		arg = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, arg, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke3(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "G")), A_0, arg2, arg3, arg4), arg5), arg7));
		A_0.Location = 254619799406L;
		return CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "RotateLeft")), A_0, arg, arg6), arg2);
	}

	public static ScriptDatum HH(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		ScriptDatum arg6 = CILHelper.GetArg(A_1, 5);
		ScriptDatum arg7 = CILHelper.GetArg(A_1, 6);
		A_0.Location = 271799668590L;
		arg = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, arg, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke3(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "H")), A_0, arg2, arg3, arg4), arg5), arg7));
		A_0.Location = 276094635886L;
		return CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "RotateLeft")), A_0, arg, arg6), arg2);
	}

	public static ScriptDatum II(ScriptContext A_0, ScriptDatum[] A_1)
	{
		ScriptDatum arg = CILHelper.GetArg(A_1, 0);
		ScriptDatum arg2 = CILHelper.GetArg(A_1, 1);
		ScriptDatum arg3 = CILHelper.GetArg(A_1, 2);
		ScriptDatum arg4 = CILHelper.GetArg(A_1, 3);
		ScriptDatum arg5 = CILHelper.GetArg(A_1, 4);
		ScriptDatum arg6 = CILHelper.GetArg(A_1, 5);
		ScriptDatum arg7 = CILHelper.GetArg(A_1, 6);
		A_0.Location = 293274505070L;
		arg = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, arg, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke3(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "I")), A_0, arg2, arg3, arg4), arg5), arg7));
		A_0.Location = 297569472366L;
		return CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "RotateLeft")), A_0, arg, arg6), arg2);
	}

	public static ScriptDatum ConvertToWordArray(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum b = ScriptDatum.FromNumber(8.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(64.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(0.0);
		ScriptDatum b4 = ScriptDatum.FromNumber(4.0);
		A_0.Location = 314749341550L;
		A_0.Location = 319044308846L;
		ScriptDatum length = CILHelper.GetLength(ScriptDatum.ToObject(A_1), A_0);
		A_0.Location = 323339276142L;
		ScriptDatum a = CILHelper.Add(length, b);
		A_0.Location = 327634243438L;
		ScriptDatum a2 = CILHelper.Divide(CILHelper.Subtract(a, CILHelper.Modulo(a, b2)), b2);
		A_0.Location = 331929210734L;
		ScriptDatum a3 = CILHelper.Multiply(CILHelper.Add(a2, b3), ScriptDatum.FromNumber(16.0));
		A_0.Location = 336224178030L;
		ScriptDatum scriptDatum2 = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "Array")), A_0, CILHelper.Subtract(a3, b3));
		A_0.Location = 340519145326L;
		A_0.Location = 344814112622L;
		ScriptDatum scriptDatum3 = scriptDatum;
		ScriptDatum index;
		ScriptDatum scriptDatum4;
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, length)))
		{
			A_0.Location = 349109079918L;
			index = CILHelper.Divide(CILHelper.Subtract(scriptDatum3, CILHelper.Modulo(scriptDatum3, b4)), b4);
			A_0.Location = 357699014510L;
			scriptDatum4 = CILHelper.Multiply(CILHelper.Modulo(scriptDatum3, b4), b);
			A_0.Location = 366288949102L;
			ScriptDatum element = CILHelper.GetElement(ScriptDatum.ToObject(scriptDatum2), index);
			A_0.Location = 370583916398L;
			ScriptDatum a4 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_1), A_0, "charCodeAt", scriptDatum3);
			A_0.Location = 374878883694L;
			ScriptDatum b5 = scriptDatum4;
			A_0.Location = 383468818286L;
			CILHelper.SetElement(ScriptDatum.ToObject(scriptDatum2), index, CILHelper.BitwiseOr(element, CILHelper.LeftShift(a4, b5)));
			A_0.Location = 387763785582L;
			CILHelper.IncrementPostfix(ref scriptDatum3);
		}
		A_0.Location = 396353720174L;
		index = CILHelper.Divide(CILHelper.Subtract(scriptDatum3, CILHelper.Modulo(scriptDatum3, b4)), b4);
		A_0.Location = 400648687470L;
		scriptDatum4 = CILHelper.Multiply(CILHelper.Modulo(scriptDatum3, b4), b);
		A_0.Location = 404943654766L;
		CILHelper.SetElement(ScriptDatum.ToObject(scriptDatum2), index, CILHelper.BitwiseOr(CILHelper.GetElement(ScriptDatum.ToObject(scriptDatum2), index), CILHelper.LeftShift(ScriptDatum.FromNumber(128.0), scriptDatum4)));
		A_0.Location = 409238622062L;
		CILHelper.SetElement(ScriptDatum.ToObject(scriptDatum2), CILHelper.Subtract(a3, ScriptDatum.FromNumber(2.0)), CILHelper.LeftShift(length, ScriptDatum.FromNumber(3.0)));
		A_0.Location = 413533589358L;
		CILHelper.SetElement(ScriptDatum.ToObject(scriptDatum2), CILHelper.Subtract(a3, b3), CILHelper.RightShift(length, ScriptDatum.FromNumber(29.0)));
		A_0.Location = 417828556654L;
		return scriptDatum2;
	}

	public static ScriptDatum WordToHex(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("");
		ScriptDatum b = ScriptDatum.FromNumber(3.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(8.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(255.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("0");
		ScriptDatum arg = ScriptDatum.FromNumber(16.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(2.0);
		A_0.Location = 443598360430L;
		ScriptDatum scriptDatum4 = scriptDatum;
		A_0.Location = 447893327726L;
		A_0.Location = 452188295022L;
		A_0.Location = 456483262318L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.LessEqual(a, b)))
		{
			A_0.Location = 460778229614L;
			ScriptDatum d = CILHelper.BitwiseAnd(CILHelper.RightShift(A_1, CILHelper.Multiply(a, b2)), b3);
			A_0.Location = 469368164206L;
			ScriptDatum d2 = CILHelper.AddStringLeft("0", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "toString", arg));
			A_0.Location = 473663131502L;
			scriptDatum4 = CILHelper.Add(scriptDatum4, CILHelper.InvokeProperty2(ScriptDatum.ToObject(d2), A_0, "substring", CILHelper.Subtract(CILHelper.GetLength(ScriptDatum.ToObject(d2), A_0), scriptDatum3), scriptDatum3));
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 482253066094L;
		return scriptDatum4;
	}

	public static ScriptDatum Utf8Encode(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum b = ScriptDatum.FromNumber(128.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(127.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(2048.0);
		ScriptDatum b4 = ScriptDatum.FromNumber(6.0);
		ScriptDatum b5 = ScriptDatum.FromNumber(192.0);
		ScriptDatum b6 = ScriptDatum.FromNumber(63.0);
		ScriptDatum b7 = ScriptDatum.FromNumber(12.0);
		ScriptDatum b8 = ScriptDatum.FromNumber(224.0);
		A_0.Location = 512317837166L;
		ScriptDatum d = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_1), A_0, "replace", ScriptDatum.FromObject(RegexManager.Resolve("\\r\\n", "g")), ScriptDatum.FromString("\n"));
		A_0.Location = 516612804462L;
		ScriptDatum scriptDatum = ScriptDatum.FromString("");
		A_0.Location = 525202739054L;
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum2, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0))))
		{
			A_0.Location = 529497706350L;
			ScriptDatum scriptDatum3 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "charCodeAt", scriptDatum2);
			A_0.Location = 538087640942L;
			if (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, b)))
			{
				A_0.Location = 542382608238L;
				scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", scriptDatum3));
			}
			else
			{
				ScriptDatum a;
				if (CILHelper.ToBoolean(a = CILHelper.Greater(scriptDatum3, b2)))
				{
					a = CILHelper.Less(scriptDatum3, b3);
				}
				if (CILHelper.ToBoolean(a))
				{
					A_0.Location = 550972542830L;
					ScriptDatum a2 = scriptDatum;
					ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String"));
					string name = "fromCharCode";
					ScriptDatum arg = CILHelper.BitwiseOr(CILHelper.RightShift(scriptDatum3, b4), b5);
					A_0.Location = 555267510126L;
					scriptDatum = CILHelper.Add(a2, CILHelper.InvokeProperty1(receiver, A_0, name, arg));
					A_0.Location = 559562477422L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(scriptDatum3, b6), b)));
				}
				else
				{
					A_0.Location = 568152412014L;
					ScriptDatum a3 = scriptDatum;
					ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String"));
					string name2 = "fromCharCode";
					ScriptDatum arg2 = CILHelper.BitwiseOr(CILHelper.RightShift(scriptDatum3, b7), b8);
					A_0.Location = 572447379310L;
					scriptDatum = CILHelper.Add(a3, CILHelper.InvokeProperty1(receiver2, A_0, name2, arg2));
					A_0.Location = 576742346606L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(CILHelper.RightShift(scriptDatum3, b4), b6), b)));
					A_0.Location = 581037313902L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(scriptDatum3, b6), b)));
				}
			}
			CILHelper.IncrementPostfix(ref scriptDatum2);
		}
		A_0.Location = 593922215790L;
		return scriptDatum;
	}

	public static ScriptDatum WordToHex_str(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromString("");
		ScriptDatum b = ScriptDatum.FromNumber(3.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(8.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(255.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromString("0");
		ScriptDatum arg = ScriptDatum.FromNumber(16.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(2.0);
		A_0.Location = 615397052270L;
		ScriptDatum scriptDatum4 = scriptDatum;
		A_0.Location = 619692019566L;
		A_0.Location = 623986986862L;
		A_0.Location = 628281954158L;
		ScriptDatum a = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.LessEqual(a, b)))
		{
			A_0.Location = 632576921454L;
			ScriptDatum d = CILHelper.BitwiseAnd(CILHelper.RightShift(A_1, CILHelper.Multiply(a, b2)), b3);
			A_0.Location = 641166856046L;
			ScriptDatum d2 = CILHelper.AddStringLeft("0", CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "toString", arg));
			A_0.Location = 645461823342L;
			scriptDatum4 = CILHelper.Add(scriptDatum4, CILHelper.InvokeProperty2(ScriptDatum.ToObject(d2), A_0, "substring", CILHelper.Subtract(CILHelper.GetLength(ScriptDatum.ToObject(d2), A_0), scriptDatum3), scriptDatum3));
			CILHelper.IncrementPostfix(ref a);
		}
		A_0.Location = 654051757934L;
		return scriptDatum4;
	}

	public static ScriptDatum Utf8Encode_str(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum b = ScriptDatum.FromNumber(128.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(127.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(2048.0);
		ScriptDatum b4 = ScriptDatum.FromNumber(6.0);
		ScriptDatum b5 = ScriptDatum.FromNumber(192.0);
		ScriptDatum b6 = ScriptDatum.FromNumber(63.0);
		ScriptDatum b7 = ScriptDatum.FromNumber(12.0);
		ScriptDatum b8 = ScriptDatum.FromNumber(224.0);
		A_0.Location = 671231627118L;
		ScriptDatum d = CILHelper.InvokeProperty2(ScriptDatum.ToObject(A_1), A_0, "replace", ScriptDatum.FromObject(RegexManager.Resolve("\\r\\n", "g")), ScriptDatum.FromString("\n"));
		A_0.Location = 675526594414L;
		ScriptDatum scriptDatum = ScriptDatum.FromString("");
		A_0.Location = 684116529006L;
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(0.0);
		while (CILHelper.ToBoolean(CILHelper.Less(scriptDatum2, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0))))
		{
			A_0.Location = 688411496302L;
			ScriptDatum scriptDatum3 = CILHelper.InvokeProperty1(ScriptDatum.ToObject(d), A_0, "charCodeAt", scriptDatum2);
			A_0.Location = 697001430894L;
			if (CILHelper.ToBoolean(CILHelper.Less(scriptDatum3, b)))
			{
				A_0.Location = 701296398190L;
				scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", scriptDatum3));
			}
			else
			{
				ScriptDatum a;
				if (CILHelper.ToBoolean(a = CILHelper.Greater(scriptDatum3, b2)))
				{
					a = CILHelper.Less(scriptDatum3, b3);
				}
				if (CILHelper.ToBoolean(a))
				{
					A_0.Location = 709886332782L;
					ScriptDatum a2 = scriptDatum;
					ScriptObject receiver = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String"));
					string name = "fromCharCode";
					ScriptDatum arg = CILHelper.BitwiseOr(CILHelper.RightShift(scriptDatum3, b4), b5);
					A_0.Location = 714181300078L;
					scriptDatum = CILHelper.Add(a2, CILHelper.InvokeProperty1(receiver, A_0, name, arg));
					A_0.Location = 718476267374L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(scriptDatum3, b6), b)));
				}
				else
				{
					A_0.Location = 727066201966L;
					ScriptDatum a3 = scriptDatum;
					ScriptObject receiver2 = ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String"));
					string name2 = "fromCharCode";
					ScriptDatum arg2 = CILHelper.BitwiseOr(CILHelper.RightShift(scriptDatum3, b7), b8);
					A_0.Location = 731361169262L;
					scriptDatum = CILHelper.Add(a3, CILHelper.InvokeProperty1(receiver2, A_0, name2, arg2));
					A_0.Location = 735656136558L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(CILHelper.RightShift(scriptDatum3, b4), b6), b)));
					A_0.Location = 739951103854L;
					scriptDatum = CILHelper.Add(scriptDatum, CILHelper.InvokeProperty1(ScriptDatum.ToObject(A_0.Global.GetPropertyDatum(A_0, "String")), A_0, "fromCharCode", CILHelper.BitwiseOr(CILHelper.BitwiseAnd(scriptDatum3, b6), b)));
				}
			}
			CILHelper.IncrementPostfix(ref scriptDatum2);
		}
		A_0.Location = 757130973038L;
		return scriptDatum;
	}

	public static ScriptDatum MD5(ScriptContext A_0, ScriptDatum A_1)
	{
		ScriptDatum scriptDatum = ScriptDatum.FromNumber(7.0);
		ScriptDatum scriptDatum2 = ScriptDatum.FromNumber(12.0);
		ScriptDatum scriptDatum3 = ScriptDatum.FromNumber(5.0);
		ScriptDatum scriptDatum4 = ScriptDatum.FromNumber(9.0);
		ScriptDatum scriptDatum5 = ScriptDatum.FromNumber(14.0);
		ScriptDatum scriptDatum6 = ScriptDatum.FromNumber(4.0);
		ScriptDatum scriptDatum7 = ScriptDatum.FromNumber(11.0);
		ScriptDatum scriptDatum8 = ScriptDatum.FromNumber(16.0);
		ScriptDatum scriptDatum9 = ScriptDatum.FromNumber(6.0);
		ScriptDatum scriptDatum10 = ScriptDatum.FromNumber(10.0);
		ScriptDatum scriptDatum11 = ScriptDatum.FromNumber(15.0);
		ScriptDatum scriptDatum12 = ScriptDatum.FromNumber(0.0);
		ScriptDatum scriptDatum13 = ScriptDatum.FromNumber(3614090360.0);
		ScriptDatum b = ScriptDatum.FromNumber(1.0);
		ScriptDatum scriptDatum14 = ScriptDatum.FromNumber(3905402710.0);
		ScriptDatum b2 = ScriptDatum.FromNumber(2.0);
		ScriptDatum scriptDatum15 = ScriptDatum.FromNumber(606105819.0);
		ScriptDatum b3 = ScriptDatum.FromNumber(3.0);
		ScriptDatum scriptDatum16 = ScriptDatum.FromNumber(3250441966.0);
		ScriptDatum scriptDatum17 = ScriptDatum.FromNumber(4118548399.0);
		ScriptDatum scriptDatum18 = ScriptDatum.FromNumber(1200080426.0);
		ScriptDatum scriptDatum19 = ScriptDatum.FromNumber(2821735955.0);
		ScriptDatum scriptDatum20 = ScriptDatum.FromNumber(4249261313.0);
		ScriptDatum b4 = ScriptDatum.FromNumber(8.0);
		ScriptDatum scriptDatum21 = ScriptDatum.FromNumber(1770035416.0);
		ScriptDatum scriptDatum22 = ScriptDatum.FromNumber(2336552879.0);
		ScriptDatum scriptDatum23 = ScriptDatum.FromNumber(4294925233.0);
		ScriptDatum scriptDatum24 = ScriptDatum.FromNumber(2304563134.0);
		ScriptDatum scriptDatum25 = ScriptDatum.FromNumber(1804603682.0);
		ScriptDatum b5 = ScriptDatum.FromNumber(13.0);
		ScriptDatum scriptDatum26 = ScriptDatum.FromNumber(4254626195.0);
		ScriptDatum scriptDatum27 = ScriptDatum.FromNumber(2792965006.0);
		ScriptDatum scriptDatum28 = ScriptDatum.FromNumber(1236535329.0);
		ScriptDatum scriptDatum29 = ScriptDatum.FromNumber(4129170786.0);
		ScriptDatum scriptDatum30 = ScriptDatum.FromNumber(3225465664.0);
		ScriptDatum scriptDatum31 = ScriptDatum.FromNumber(643717713.0);
		ScriptDatum scriptDatum32 = ScriptDatum.FromNumber(3921069994.0);
		ScriptDatum scriptDatum33 = ScriptDatum.FromNumber(3593408605.0);
		ScriptDatum scriptDatum34 = ScriptDatum.FromNumber(38016083.0);
		ScriptDatum scriptDatum35 = ScriptDatum.FromNumber(3634488961.0);
		ScriptDatum scriptDatum36 = ScriptDatum.FromNumber(3889429448.0);
		ScriptDatum scriptDatum37 = ScriptDatum.FromNumber(568446438.0);
		ScriptDatum scriptDatum38 = ScriptDatum.FromNumber(3275163606.0);
		ScriptDatum scriptDatum39 = ScriptDatum.FromNumber(4107603335.0);
		ScriptDatum scriptDatum40 = ScriptDatum.FromNumber(1163531501.0);
		ScriptDatum scriptDatum41 = ScriptDatum.FromNumber(2850285829.0);
		ScriptDatum scriptDatum42 = ScriptDatum.FromNumber(4243563512.0);
		ScriptDatum scriptDatum43 = ScriptDatum.FromNumber(1735328473.0);
		ScriptDatum scriptDatum44 = ScriptDatum.FromNumber(2368359562.0);
		ScriptDatum scriptDatum45 = ScriptDatum.FromNumber(4294588738.0);
		ScriptDatum scriptDatum46 = ScriptDatum.FromNumber(2272392833.0);
		ScriptDatum scriptDatum47 = ScriptDatum.FromNumber(1839030562.0);
		ScriptDatum scriptDatum48 = ScriptDatum.FromNumber(4259657740.0);
		ScriptDatum scriptDatum49 = ScriptDatum.FromNumber(2763975236.0);
		ScriptDatum scriptDatum50 = ScriptDatum.FromNumber(1272893353.0);
		ScriptDatum scriptDatum51 = ScriptDatum.FromNumber(4139469664.0);
		ScriptDatum scriptDatum52 = ScriptDatum.FromNumber(3200236656.0);
		ScriptDatum scriptDatum53 = ScriptDatum.FromNumber(681279174.0);
		ScriptDatum scriptDatum54 = ScriptDatum.FromNumber(3936430074.0);
		ScriptDatum scriptDatum55 = ScriptDatum.FromNumber(3572445317.0);
		ScriptDatum scriptDatum56 = ScriptDatum.FromNumber(76029189.0);
		ScriptDatum scriptDatum57 = ScriptDatum.FromNumber(3654602809.0);
		ScriptDatum scriptDatum58 = ScriptDatum.FromNumber(3873151461.0);
		ScriptDatum scriptDatum59 = ScriptDatum.FromNumber(530742520.0);
		ScriptDatum scriptDatum60 = ScriptDatum.FromNumber(3299628645.0);
		ScriptDatum scriptDatum61 = ScriptDatum.FromNumber(4096336452.0);
		ScriptDatum scriptDatum62 = ScriptDatum.FromNumber(1126891415.0);
		ScriptDatum scriptDatum63 = ScriptDatum.FromNumber(2878612391.0);
		ScriptDatum scriptDatum64 = ScriptDatum.FromNumber(4237533241.0);
		ScriptDatum scriptDatum65 = ScriptDatum.FromNumber(1700485571.0);
		ScriptDatum scriptDatum66 = ScriptDatum.FromNumber(2399980690.0);
		ScriptDatum scriptDatum67 = ScriptDatum.FromNumber(4293915773.0);
		ScriptDatum scriptDatum68 = ScriptDatum.FromNumber(2240044497.0);
		ScriptDatum scriptDatum69 = ScriptDatum.FromNumber(1873313359.0);
		ScriptDatum scriptDatum70 = ScriptDatum.FromNumber(4264355552.0);
		ScriptDatum scriptDatum71 = ScriptDatum.FromNumber(2734768916.0);
		ScriptDatum scriptDatum72 = ScriptDatum.FromNumber(1309151649.0);
		ScriptDatum scriptDatum73 = ScriptDatum.FromNumber(4149444226.0);
		ScriptDatum scriptDatum74 = ScriptDatum.FromNumber(3174756917.0);
		ScriptDatum scriptDatum75 = ScriptDatum.FromNumber(718787259.0);
		ScriptDatum scriptDatum76 = ScriptDatum.FromNumber(3951481745.0);
		A_0.Location = 778605809518L;
		ScriptDatum scriptDatum77 = ScriptDatum.FromNumber(1732584193.0);
		A_0.Location = 782900776814L;
		ScriptDatum scriptDatum78 = ScriptDatum.FromNumber(4023233417.0);
		A_0.Location = 787195744110L;
		ScriptDatum scriptDatum79 = ScriptDatum.FromNumber(2562383102.0);
		A_0.Location = 791490711406L;
		ScriptDatum scriptDatum80 = ScriptDatum.FromNumber(271733878.0);
		A_0.Location = 795785678702L;
		ScriptDatum scriptDatum81 = scriptDatum;
		ScriptDatum scriptDatum82 = scriptDatum2;
		ScriptDatum scriptDatum83 = ScriptDatum.FromNumber(17.0);
		ScriptDatum scriptDatum84 = ScriptDatum.FromNumber(22.0);
		A_0.Location = 800080645998L;
		ScriptDatum scriptDatum85 = scriptDatum3;
		ScriptDatum scriptDatum86 = scriptDatum4;
		ScriptDatum scriptDatum87 = scriptDatum5;
		ScriptDatum scriptDatum88 = ScriptDatum.FromNumber(20.0);
		A_0.Location = 804375613294L;
		ScriptDatum scriptDatum89 = scriptDatum6;
		ScriptDatum scriptDatum90 = scriptDatum7;
		ScriptDatum scriptDatum91 = scriptDatum8;
		ScriptDatum scriptDatum92 = ScriptDatum.FromNumber(23.0);
		A_0.Location = 808670580590L;
		ScriptDatum scriptDatum93 = scriptDatum9;
		ScriptDatum scriptDatum94 = scriptDatum10;
		ScriptDatum scriptDatum95 = scriptDatum11;
		ScriptDatum scriptDatum96 = ScriptDatum.FromNumber(21.0);
		A_0.Location = 812965547886L;
		ScriptDatum arg = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "Utf8Encode")), A_0, A_1);
		A_0.Location = 817260515182L;
		ScriptDatum d = CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "ConvertToWordArray")), A_0, arg);
		A_0.Location = 821555482478L;
		ScriptDatum a = scriptDatum12;
		while (CILHelper.ToBoolean(CILHelper.Less(a, CILHelper.GetLength(ScriptDatum.ToObject(d), A_0))))
		{
			A_0.Location = 825850449774L;
			ScriptDatum arg2 = scriptDatum77;
			A_0.Location = 830145417070L;
			ScriptDatum arg3 = scriptDatum78;
			A_0.Location = 834440384366L;
			ScriptDatum arg4 = scriptDatum79;
			A_0.Location = 838735351662L;
			ScriptDatum arg5 = scriptDatum80;
			A_0.Location = 847325286254L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum12)),
				scriptDatum81,
				scriptDatum13
			});
			A_0.Location = 851620253550L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b)),
				scriptDatum82,
				scriptDatum14
			});
			A_0.Location = 855915220846L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b2)),
				scriptDatum83,
				scriptDatum15
			});
			A_0.Location = 860210188142L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b3)),
				scriptDatum84,
				scriptDatum16
			});
			A_0.Location = 868800122734L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum6)),
				scriptDatum81,
				scriptDatum17
			});
			A_0.Location = 873095090030L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum3)),
				scriptDatum82,
				scriptDatum18
			});
			A_0.Location = 877390057326L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum9)),
				scriptDatum83,
				scriptDatum19
			});
			A_0.Location = 881685024622L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum)),
				scriptDatum84,
				scriptDatum20
			});
			A_0.Location = 890274959214L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b4)),
				scriptDatum81,
				scriptDatum21
			});
			A_0.Location = 894569926510L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum4)),
				scriptDatum82,
				scriptDatum22
			});
			A_0.Location = 898864893806L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum10)),
				scriptDatum83,
				scriptDatum23
			});
			A_0.Location = 903159861102L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum7)),
				scriptDatum84,
				scriptDatum24
			});
			A_0.Location = 911749795694L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum2)),
				scriptDatum81,
				scriptDatum25
			});
			A_0.Location = 916044762990L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b5)),
				scriptDatum82,
				scriptDatum26
			});
			A_0.Location = 920339730286L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum5)),
				scriptDatum83,
				scriptDatum27
			});
			A_0.Location = 924634697582L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "FF")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum11)),
				scriptDatum84,
				scriptDatum28
			});
			A_0.Location = 933224632174L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b)),
				scriptDatum85,
				scriptDatum29
			});
			A_0.Location = 937519599470L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum9)),
				scriptDatum86,
				scriptDatum30
			});
			A_0.Location = 941814566766L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum7)),
				scriptDatum87,
				scriptDatum31
			});
			A_0.Location = 946109534062L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum12)),
				scriptDatum88,
				scriptDatum32
			});
			A_0.Location = 954699468654L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum3)),
				scriptDatum85,
				scriptDatum33
			});
			A_0.Location = 958994435950L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum10)),
				scriptDatum86,
				scriptDatum34
			});
			A_0.Location = 963289403246L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum11)),
				scriptDatum87,
				scriptDatum35
			});
			A_0.Location = 967584370542L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum6)),
				scriptDatum88,
				scriptDatum36
			});
			A_0.Location = 976174305134L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum4)),
				scriptDatum85,
				scriptDatum37
			});
			A_0.Location = 980469272430L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum5)),
				scriptDatum86,
				scriptDatum38
			});
			A_0.Location = 984764239726L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b3)),
				scriptDatum87,
				scriptDatum39
			});
			A_0.Location = 989059207022L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b4)),
				scriptDatum88,
				scriptDatum40
			});
			A_0.Location = 997649141614L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b5)),
				scriptDatum85,
				scriptDatum41
			});
			A_0.Location = 1001944108910L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b2)),
				scriptDatum86,
				scriptDatum42
			});
			A_0.Location = 1006239076206L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum)),
				scriptDatum87,
				scriptDatum43
			});
			A_0.Location = 1010534043502L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "GG")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum2)),
				scriptDatum88,
				scriptDatum44
			});
			A_0.Location = 1019123978094L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum3)),
				scriptDatum89,
				scriptDatum45
			});
			A_0.Location = 1023418945390L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b4)),
				scriptDatum90,
				scriptDatum46
			});
			A_0.Location = 1027713912686L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum7)),
				scriptDatum91,
				scriptDatum47
			});
			A_0.Location = 1032008879982L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum5)),
				scriptDatum92,
				scriptDatum48
			});
			A_0.Location = 1040598814574L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b)),
				scriptDatum89,
				scriptDatum49
			});
			A_0.Location = 1044893781870L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum6)),
				scriptDatum90,
				scriptDatum50
			});
			A_0.Location = 1049188749166L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum)),
				scriptDatum91,
				scriptDatum51
			});
			A_0.Location = 1053483716462L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum10)),
				scriptDatum92,
				scriptDatum52
			});
			A_0.Location = 1062073651054L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b5)),
				scriptDatum89,
				scriptDatum53
			});
			A_0.Location = 1066368618350L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum12)),
				scriptDatum90,
				scriptDatum54
			});
			A_0.Location = 1070663585646L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b3)),
				scriptDatum91,
				scriptDatum55
			});
			A_0.Location = 1074958552942L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum9)),
				scriptDatum92,
				scriptDatum56
			});
			A_0.Location = 1083548487534L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum4)),
				scriptDatum89,
				scriptDatum57
			});
			A_0.Location = 1087843454830L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum2)),
				scriptDatum90,
				scriptDatum58
			});
			A_0.Location = 1092138422126L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum11)),
				scriptDatum91,
				scriptDatum59
			});
			A_0.Location = 1096433389422L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "HH")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b2)),
				scriptDatum92,
				scriptDatum60
			});
			A_0.Location = 1105023324014L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum12)),
				scriptDatum93,
				scriptDatum61
			});
			A_0.Location = 1109318291310L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum)),
				scriptDatum94,
				scriptDatum62
			});
			A_0.Location = 1113613258606L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum5)),
				scriptDatum95,
				scriptDatum63
			});
			A_0.Location = 1117908225902L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum3)),
				scriptDatum96,
				scriptDatum64
			});
			A_0.Location = 1126498160494L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum2)),
				scriptDatum93,
				scriptDatum65
			});
			A_0.Location = 1130793127790L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b3)),
				scriptDatum94,
				scriptDatum66
			});
			A_0.Location = 1135088095086L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum10)),
				scriptDatum95,
				scriptDatum67
			});
			A_0.Location = 1139383062382L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b)),
				scriptDatum96,
				scriptDatum68
			});
			A_0.Location = 1147972996974L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b4)),
				scriptDatum93,
				scriptDatum69
			});
			A_0.Location = 1152267964270L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum11)),
				scriptDatum94,
				scriptDatum70
			});
			A_0.Location = 1156562931566L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum9)),
				scriptDatum95,
				scriptDatum71
			});
			A_0.Location = 1160857898862L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b5)),
				scriptDatum96,
				scriptDatum72
			});
			A_0.Location = 1169447833454L;
			scriptDatum77 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum6)),
				scriptDatum93,
				scriptDatum73
			});
			A_0.Location = 1173742800750L;
			scriptDatum80 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				scriptDatum79,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum7)),
				scriptDatum94,
				scriptDatum74
			});
			A_0.Location = 1178037768046L;
			scriptDatum79 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				scriptDatum78,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, b2)),
				scriptDatum95,
				scriptDatum75
			});
			A_0.Location = 1182332735342L;
			scriptDatum78 = ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "II")).Invoke(A_0, new ScriptDatum[]
			{
				scriptDatum78,
				scriptDatum79,
				scriptDatum80,
				scriptDatum77,
				CILHelper.GetElement(ScriptDatum.ToObject(d), CILHelper.Add(a, scriptDatum4)),
				scriptDatum96,
				scriptDatum76
			});
			A_0.Location = 1190922669934L;
			scriptDatum77 = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, scriptDatum77, arg2);
			A_0.Location = 1195217637230L;
			scriptDatum78 = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, scriptDatum78, arg3);
			A_0.Location = 1199512604526L;
			scriptDatum79 = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, scriptDatum79, arg4);
			A_0.Location = 1203807571822L;
			scriptDatum80 = CILHelper.Invoke2(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "AddUnsigned")), A_0, scriptDatum80, arg5);
			a = CILHelper.Add(a, scriptDatum8);
		}
		A_0.Location = 1216692473710L;
		ScriptDatum d2 = CILHelper.Add(CILHelper.Add(CILHelper.Add(CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "WordToHex")), A_0, scriptDatum77), CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "WordToHex")), A_0, scriptDatum78)), CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "WordToHex")), A_0, scriptDatum79)), CILHelper.Invoke1(ScriptDatum.ToObject(A_0.Module.GetPropertyDatum(A_0, "WordToHex")), A_0, scriptDatum80));
		A_0.Location = 1220987441006L;
		return CILHelper.InvokeProperty0(ScriptDatum.ToObject(d2), A_0, "toLowerCase");
	}
}
