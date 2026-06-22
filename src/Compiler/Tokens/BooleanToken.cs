using System;

namespace AuroraScript.Tokens
{
    internal class BooleanToken : ValueToken
    {
        internal BooleanToken(String value)
        {
            this.Type = ValueType.Boolean;
            this.BoolValue = Boolean.Parse(value);
        }

        internal BooleanToken(Boolean value)
        {
            this.Type = ValueType.Boolean;
            this.BoolValue = value;
        }



        public Boolean BoolValue { get; private set; }

        public override string ToString()
        {
            return BoolValue.ToString();
        }


    }
}
