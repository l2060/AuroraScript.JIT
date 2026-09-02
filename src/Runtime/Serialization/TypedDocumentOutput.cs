namespace AuroraScript.Runtime.Serialization
{
    /// <summary>
    /// Low-allocation TDoc body writer used by <see cref="INativeTypedDocument"/>.
    /// The first member, element, or scalar write locks the body shape.
    /// The struct forwards into the active document writer and must not be stored.
    /// </summary>
    public ref struct TypedDocumentOutput
    {
        private enum BodyKind : byte
        {
            Unspecified = 0,
            Object = 1,
            Array = 2,
            Scalar = 3
        }

        private ref TypedDocumentWriter _writer;
        private int _written;
        private BodyKind _kind;

        internal TypedDocumentOutput(ref TypedDocumentWriter writer)
        {
            _writer = ref writer;
            _written = 0;
            _kind = BodyKind.Unspecified;
        }

        internal int WrittenCount => _written;

        internal void Complete()
        {
            if (_kind == BodyKind.Unspecified)
            {
                _writer.WriteEmptyNativeObject();
                return;
            }

            if (_kind == BodyKind.Scalar)
            {
                return;
            }

            _writer.EndNativeBody(_kind == BodyKind.Array ? ']' : '}', _written);
        }

        /// <summary>Writes a number object member.</summary>
        public void WriteMember(string name, double value, bool readOnly = false)
        {
            WriteMember(name, ScriptDatum.FromNumber(value), readOnly);
        }

        /// <summary>Writes a boolean object member.</summary>
        public void WriteMember(string name, bool value, bool readOnly = false)
        {
            WriteMember(name, ScriptDatum.FromBoolean(value), readOnly);
        }

        /// <summary>Writes a string object member. A null value becomes TDoc <c>null</c>.</summary>
        public void WriteMember(string name, string value, bool readOnly = false)
        {
            WriteMember(
                name,
                value == null ? ScriptDatum.Null : ScriptDatum.FromString(value),
                readOnly);
        }

        /// <summary>Writes one named object member using an existing datum.</summary>
        public void WriteMember(string name, ScriptDatum value, bool readOnly = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            EnsureKind(BodyKind.Object);
            if (_writer.TryWriteNativeMember(name, value, !readOnly, _written == 0))
            {
                _written++;
            }
        }

        /// <summary>Writes a number array element.</summary>
        public void WriteElement(double value)
        {
            WriteElement(ScriptDatum.FromNumber(value));
        }

        /// <summary>Writes a boolean array element.</summary>
        public void WriteElement(bool value)
        {
            WriteElement(ScriptDatum.FromBoolean(value));
        }

        /// <summary>Writes a string array element. A null value becomes TDoc <c>null</c>.</summary>
        public void WriteElement(string value)
        {
            WriteElement(value == null ? ScriptDatum.Null : ScriptDatum.FromString(value));
        }

        /// <summary>Writes one positional array element using an existing datum.</summary>
        public void WriteElement(ScriptDatum value)
        {
            EnsureKind(BodyKind.Array);
            if (_writer.TryWriteNativeElement(value, _written, _written == 0))
            {
                _written++;
            }
        }

        /// <summary>Writes a number as the whole scalar body.</summary>
        public void WriteValue(double value)
        {
            WriteValue(ScriptDatum.FromNumber(value));
        }

        /// <summary>Writes a boolean as the whole scalar body.</summary>
        public void WriteValue(bool value)
        {
            WriteValue(ScriptDatum.FromBoolean(value));
        }

        /// <summary>
        /// Writes a string as the whole scalar body. A null value becomes TDoc
        /// <c>null</c>.
        /// </summary>
        public void WriteValue(string value)
        {
            WriteValue(value == null ? ScriptDatum.Null : ScriptDatum.FromString(value));
        }

        /// <summary>
        /// Writes one scalar body using an existing datum. Only null, boolean,
        /// number, and string are allowed; do not call this more than once.
        /// </summary>
        public void WriteValue(ScriptDatum value)
        {
            if (value.Kind is not (ValueKind.Null or ValueKind.Boolean or ValueKind.Number or ValueKind.String))
            {
                throw new TypedDocumentException(
                    "Native TDoc scalar body requires null, boolean, number, or string.");
            }

            EnsureKind(BodyKind.Scalar);
            _writer.WriteNativeScalar(value);
            _written = 1;
        }

        private void EnsureKind(BodyKind kind)
        {
            if (_kind == BodyKind.Unspecified)
            {
                _kind = kind;
                if (kind == BodyKind.Object)
                {
                    _writer.BeginNativeBody('{');
                }
                else if (kind == BodyKind.Array)
                {
                    _writer.BeginNativeBody('[');
                }

                return;
            }

            if (_kind != kind)
            {
                throw new TypedDocumentException(
                    "Native TDoc output cannot mix object members, array elements, and scalar values.");
            }

            if (_kind == BodyKind.Scalar)
            {
                throw new TypedDocumentException(
                    "Native TDoc scalar body accepts a single value.");
            }
        }
    }
}
