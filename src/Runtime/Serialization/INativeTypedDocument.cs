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
    /// factory to customize construction. Member, element, and scalar writes
    /// receive <see cref="ScriptDatum"/> structs, so number and boolean fields
    /// do not allocate. Serialization writes through <see cref="TypedDocumentOutput"/>,
    /// a ref struct, so the writer itself is not boxed.
    /// <para>
    /// <see cref="WriteTypedDocument"/> chooses the canonical TDoc shape: call
    /// <see cref="TypedDocumentOutput.WriteMember(string, ScriptDatum, bool)"/> for
    /// an object body (<c>Vec2 {x 3,y 4}</c>),
    /// <see cref="TypedDocumentOutput.WriteElement(ScriptDatum)"/> for an array
    /// body (<c>Vec2 [3,4]</c>), or
    /// <see cref="TypedDocumentOutput.WriteValue(ScriptDatum)"/> for a scalar
    /// body (<c>User "a,b,c"</c>, <c>State 1</c>, <c>Flag false</c>).
    /// Do not mix those calls on one write. Deserialize and script literals
    /// report any of the three shapes through <see cref="ReadTypedDocument"/>.
    /// </para>
    /// <para>
    /// Dynamic <see cref="ScriptObject"/> properties are opt-in. An object-body
    /// implementation may call
    /// <see cref="TypedDocumentOutput.WriteDynamicMembers(ScriptObject)"/> and
    /// route accepted extra member input through
    /// <see cref="TypedDocumentInput.DefineDynamicMember(ScriptObject)"/>.
    /// Array and scalar bodies cannot contain named dynamic members.
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
        /// Writes this instance as a TDoc object, array, or scalar body. The
        /// first <see cref="TypedDocumentOutput.WriteMember(string, ScriptDatum, bool)"/>,
        /// <see cref="TypedDocumentOutput.WriteElement(ScriptDatum)"/>, or
        /// <see cref="TypedDocumentOutput.WriteValue(ScriptDatum)"/> call locks
        /// the shape for this write.
        /// </summary>
        void WriteTypedDocument(ref TypedDocumentOutput output);

        /// <summary>
        /// Reads one parsed object member, array element, or scalar value into
        /// this instance. Inspect <see cref="TypedDocumentInput.IsMember"/>,
        /// <see cref="TypedDocumentInput.IsElement"/>, or
        /// <see cref="TypedDocumentInput.IsValue"/>, then get the value from
        /// <see cref="TypedDocumentInput.Value"/>. Unknown names, indexes, or
        /// illegal readonly flags should throw <see cref="TypedDocumentException"/>.
        /// </summary>
        void ReadTypedDocument(ref TypedDocumentInput input);
    }
}
