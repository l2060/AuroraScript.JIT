using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Opt-in TDoc contract for host <see cref="Hosting.AuroraNativeTypeAttribute"/> objects.
    /// Implement this on a native instance type to participate in script
    /// <c>tdoc</c> literals and host serialize/deserialize without CLR reflection.
    /// </summary>
    /// <remarks>
    /// Construction is supplied by the NativeType source generator: a
    /// parameterless constructor when the type has none, otherwise a
    /// <c>CreateTypedDocument()</c> factory. Host code may still write that
    /// factory to customize construction. Member and element writes receive
    /// <see cref="ScriptDatum"/> structs, so number and boolean fields do
    /// not allocate. Serialization writes through <see cref="TypedDocumentOutput"/>,
    /// a ref struct, so the writer itself is not boxed.
    /// <para>
    /// <see cref="WriteTypedDocument"/> chooses the canonical TDoc shape: call
    /// <see cref="TypedDocumentOutput.WriteMember(string, ScriptDatum, bool)"/> for
    /// an object body (<c>Vec2 {x 3,y 4}</c>) or
    /// <see cref="TypedDocumentOutput.WriteElement(ScriptDatum)"/> for an array
    /// body (<c>Vec2 [3,4]</c>). Do not mix both on one write. Deserialize and
    /// script literals report either shape through
    /// <see cref="ReadTypedDocument"/>.
    /// </para>
    /// <para>
    /// The TDoc type-name prefix comes from
    /// <see cref="Hosting.AuroraNativeTypeAttribute"/> through the engine
    /// catalog, so the implementation does not declare its own script name.
    /// </para>
    /// </remarks>
    public interface INativeTypedDocument
    {
        /// <summary>
        /// Writes this instance as a TDoc object or array body. The first
        /// <see cref="TypedDocumentOutput.WriteMember(string, ScriptDatum, bool)"/>
        /// or <see cref="TypedDocumentOutput.WriteElement(ScriptDatum)"/> call
        /// locks the shape for this write.
        /// </summary>
        void WriteTypedDocument(ref TypedDocumentOutput output);

        /// <summary>
        /// Reads one parsed object member or array element into this instance.
        /// Inspect <see cref="TypedDocumentInput.IsMember"/> or
        /// <see cref="TypedDocumentInput.IsElement"/>, then get the value from
        /// <see cref="TypedDocumentInput.Value"/>. Unknown names, indexes, or
        /// illegal readonly flags should throw <see cref="TypedDocumentException"/>.
        /// </summary>
        void ReadTypedDocument(ref TypedDocumentInput input);
    }
}
