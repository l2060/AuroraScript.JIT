using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Describes one object member, array element, or scalar value being read
    /// into an <see cref="INativeTypedDocument"/> instance.
    /// </summary>
    public readonly ref struct TypedDocumentInput
    {
        private readonly ScriptDatum _value;
        private readonly string _path;

        internal TypedDocumentInput(
            string memberName,
            int elementIndex,
            bool readOnly,
            ScriptDatum value,
            string path)
        {
            MemberName = memberName;
            ElementIndex = elementIndex;
            IsReadOnly = readOnly;
            _value = value;
            _path = string.IsNullOrEmpty(path) ? "$" : path;
        }

        /// <summary>Whether this input represents a named object member.</summary>
        public bool IsMember => MemberName != null;

        /// <summary>Whether this input represents a positional array element.</summary>
        public bool IsElement => MemberName == null && ElementIndex >= 0;

        /// <summary>
        /// Whether this input represents the whole scalar body
        /// (<c>User "a,b"</c>, <c>State 1</c>, <c>Flag false</c>).
        /// </summary>
        public bool IsValue => MemberName == null && ElementIndex < 0;

        /// <summary>
        /// Object member name, or <see langword="null"/> for an array element
        /// or scalar value.
        /// </summary>
        public string MemberName { get; }

        /// <summary>
        /// Array index, or -1 for an object member or scalar value.
        /// </summary>
        public int ElementIndex { get; }

        /// <summary>
        /// Whether an object member was declared readonly. Always false for an
        /// array element or scalar value.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>Gets the parsed script value without conversion or boxing.</summary>
        public ScriptDatum Value => _value;

        /// <summary>
        /// Defines this member as an own enumerable dynamic property on the
        /// target, preserving its readonly flag. This is valid only for member
        /// input and lets each NativeType opt into dynamic-property round trips.
        /// </summary>
        public void DefineDynamicMember(ScriptObject target)
        {
            if (target == null) throw new System.ArgumentNullException(nameof(target));
            if (!IsMember)
            {
                throw Error("Only object members can be defined as dynamic properties.");
            }

            target.Define(
                MemberName,
                _value,
                writeable: !IsReadOnly,
                enumerable: true);
        }

        /// <summary>Creates a TDoc error associated with this input's data path.</summary>
        public TypedDocumentException Error(string message)
        {
            return new TypedDocumentException(message, _path);
        }
    }
}
