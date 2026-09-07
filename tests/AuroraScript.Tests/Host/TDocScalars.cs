using AuroraScript.Hosting;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Serialization;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Tests.Host;

[NativeType("Flag")]
public sealed partial class Flag : ScriptObject, INativeTypedDocument
{
    [Export("value")]
    public bool Value;

    [Export]
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

[NativeType("State")]
public sealed partial class State : ScriptObject, INativeTypedDocument
{
    [Export("code")]
    public double Code;

    [Export]
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

[NativeType("User")]
public sealed partial class User : ScriptObject, INativeTypedDocument
{
    [Export("record")]
    public string Record;

    [Export]
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

[NativeType("NativeRecord")]
public sealed partial class NativeRecord : ScriptObject, INativeTypedDocument
{
    [Export("name")]
    public string Name;

    [Export]
    public NativeRecord(string name)
    {
        Name = name;
    }

    public void WriteTypedDocument(ref TypedDocumentOutput output)
    {
        output.WriteMember("name", Name);
        output.WriteDynamicMembers(this);
    }

    public void ReadTypedDocument(ref TypedDocumentInput input)
    {
        if (input.IsMember && input.MemberName == "name")
        {
            if (input.IsReadOnly || input.Value.Kind != ValueKind.String)
            {
                throw input.Error("NativeRecord name requires a writable string.");
            }

            Name = input.Value.StringText;
            return;
        }

        input.DefineDynamicMember(this);
    }
}
