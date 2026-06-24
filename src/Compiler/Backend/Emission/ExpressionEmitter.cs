using AuroraScript.Compiler.Backend.Lowering;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed class ExpressionEmitter
    {
        private readonly LocalEmitter _locals;

        public ExpressionEmitter(LocalEmitter locals)
        {
            _locals = locals;
        }

        public void Emit(FunctionEmissionContext context, LoweredExpression expression)
        {
            if (expression == null)
            {
                return;
            }

            context.RecordExpression(expression);
            switch (expression)
            {
                case LoweredUnsupportedExpression unsupported:
                    throw context.Unsupported(unsupported);
                case LoweredLiteralExpression:
                    return;
                case LoweredNameExpression name:
                    _locals.EmitName(context, name);
                    return;
                case LoweredBinaryExpression binary:
                    Emit(context, binary.Left);
                    Emit(context, binary.Right);
                    return;
                case LoweredCallExpression call:
                    Emit(context, call.Target);
                    for (var i = 0; i < call.Arguments.Length; i++)
                    {
                        Emit(context, call.Arguments[i]);
                    }
                    return;
                case LoweredLambdaExpression lambda:
                    context.RecordNestedFunction(lambda.Function);
                    return;
                case LoweredAssignmentExpression assignment:
                    Emit(context, assignment.Left);
                    Emit(context, assignment.Right);
                    return;
                case LoweredCompoundExpression compound:
                    Emit(context, compound.Left);
                    Emit(context, compound.Right);
                    return;
                case LoweredUnaryExpression unary:
                    Emit(context, unary.Expression);
                    return;
                case LoweredInExpression inExpression:
                    Emit(context, inExpression.Left);
                    Emit(context, inExpression.Right);
                    return;
                case LoweredGetPropertyExpression property:
                    Emit(context, property.Instance);
                    Emit(context, property.Property);
                    return;
                case LoweredGetElementExpression element:
                    Emit(context, element.Instance);
                    Emit(context, element.Index);
                    return;
                case LoweredSetPropertyExpression property:
                    Emit(context, property.Instance);
                    Emit(context, property.Property);
                    Emit(context, property.Value);
                    return;
                case LoweredSetElementExpression element:
                    Emit(context, element.Instance);
                    Emit(context, element.Index);
                    Emit(context, element.Value);
                    return;
                case LoweredArrayLiteralExpression array:
                    for (var i = 0; i < array.Elements.Length; i++)
                    {
                        Emit(context, array.Elements[i]);
                    }
                    return;
                case LoweredMapExpression map:
                    for (var i = 0; i < map.Entries.Length; i++)
                    {
                        Emit(context, map.Entries[i].Value);
                    }
                    return;
                case LoweredSpreadExpression spread:
                    Emit(context, spread.Expression);
                    return;
                case LoweredNewExpression @new:
                    Emit(context, @new.Expression);
                    return;
            }
        }
    }
}
