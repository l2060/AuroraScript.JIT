using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Tokens;
using System;

namespace AuroraScript.Compiler.Backend.Binding
{
    internal static class FunctionAnnotationBinder
    {
        public static DirectCallDirective ResolveDirectCallDirective(FunctionDeclaration declaration)
        {
            var annotations = declaration?.Annotations;
            if (annotations == null || annotations.Count == 0)
            {
                return DirectCallDirective.Auto;
            }

            var result = DirectCallDirective.Auto;
            var seenDirectCall = false;
            for (var i = 0; i < annotations.Count; i++)
            {
                var annotation = annotations[i];
                var name = annotation.Name?.Value;
                if (StringComparer.Ordinal.Equals(name, "directCall"))
                {
                    result = ResolveDirectCallDirective(annotation, ref seenDirectCall);
                    continue;
                }

                throw new AuroraCompilationException(AuroraCompilationStage.Binding, annotation, $"Unsupported function annotation '@{name}'.");
            }

            return result;
        }

        private static DirectCallDirective ResolveDirectCallDirective(
            FunctionAnnotation annotation,
            ref bool seen)
        {
            if (seen)
            {
                throw new AuroraCompilationException(AuroraCompilationStage.Binding, annotation, "Duplicate @directCall annotation.");
            }
            seen = true;

            if (annotation.Arguments.Count == 0)
            {
                return DirectCallDirective.PreserveClosure;
            }

            if (annotation.Arguments.Count == 1 && annotation.Arguments[0] is BooleanToken boolean)
            {
                return boolean.BoolValue
                    ? DirectCallDirective.PreserveClosure
                    : DirectCallDirective.Disabled;
            }

            throw new AuroraCompilationException(AuroraCompilationStage.Binding, annotation, "@directCall expects no arguments or a single boolean argument.");
        }
    }
}
