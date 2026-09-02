using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Tests.Host;

[AuroraNativeType("Flag")]
public sealed partial class Flag : ScriptObject, INativeTypedDocument
{
    [AuroraExport("value")]
    public bool Value;

    [AuroraExport]
    public Flag(bool value)
    {
        Value = value;
    }

    public void WriteTypedDocument(ref TypedDocumentOutput output)
    {
        output.WriteValue(Value);
    }

    public void ReadTypedDocument(ref TypedDocumentInput input)
    {
        if (input.IsValue)
        {
            Value = ReadBoolean(ref input);
            return;
        }

        if (input.IsMember && input.MemberName == "value" && !input.IsReadOnly)
        {
            Value = ReadBoolean(ref input);
            return;
        }

        throw input.Error("Flag requires a boolean value.");
    }

    private static bool ReadBoolean(ref TypedDocumentInput input)
    {
        if (input.Value.Kind != ValueKind.Boolean)
        {
            throw input.Error("Flag requires a boolean value.");
        }

        return input.Value.Boolean;
    }
}

[AuroraNativeType("State")]
public sealed partial class State : ScriptObject, INativeTypedDocument
{
    [AuroraExport("code")]
    public double Code;

    [AuroraExport]
    public State(double code)
    {
        Code = code;
    }

    public void WriteTypedDocument(ref TypedDocumentOutput output)
    {
        output.WriteValue(Code);
    }

    public void ReadTypedDocument(ref TypedDocumentInput input)
    {
        if (input.IsValue)
        {
            Code = ReadNumber(ref input);
            return;
        }

        if (input.IsMember && input.MemberName == "code" && !input.IsReadOnly)
        {
            Code = ReadNumber(ref input);
            return;
        }

        throw input.Error("State requires a number value.");
    }

    private static double ReadNumber(ref TypedDocumentInput input)
    {
        if (input.Value.Kind != ValueKind.Number || !double.IsFinite(input.Value.Number))
        {
            throw input.Error("State requires a finite number.");
        }

        return input.Value.Number;
    }
}

[AuroraNativeType("User")]
public sealed partial class User : ScriptObject, INativeTypedDocument
{
    [AuroraExport("record")]
    public string Record;

    [AuroraExport]
    public User(string record)
    {
        Record = record;
    }

    public void WriteTypedDocument(ref TypedDocumentOutput output)
    {
        output.WriteValue(Record);
    }

    public void ReadTypedDocument(ref TypedDocumentInput input)
    {
        if (input.IsValue)
        {
            Record = ReadString(ref input);
            return;
        }

        if (input.IsMember && input.MemberName == "record" && !input.IsReadOnly)
        {
            Record = ReadString(ref input);
            return;
        }

        throw input.Error("User requires a string value.");
    }

    private static string ReadString(ref TypedDocumentInput input)
    {
        if (input.Value.Kind != ValueKind.String)
        {
            throw input.Error("User requires a string value.");
        }

        return input.Value.StringText;
    }
}
