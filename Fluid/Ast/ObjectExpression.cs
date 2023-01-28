using Fluid.Ast.BinaryExpressions;
using Fluid.Values;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Fluid.Ast
{
    public class ObjectExpression : Expression
    {
        public ObjectExpression(List<(string, Expression)> values)
        {
            Values = values ?? new List<(string, Expression)>();
        }

        public List<(string, Expression)> Values { get; }

        // TODO Assert no identical property names.
        public override async ValueTask<FluidValue> EvaluateAsync(TemplateContext context)
        {
            var tasks = new ValueTask<FluidValue>[Values.Count];
            for (int i = 0; i < Values.Count; i++)
            {
                tasks[i] = Values[i].Item2.EvaluateAsync(context);
            }

            foreach (var task in tasks)
            {
                if (!task.IsCompletedSuccessfully)
                {
                    await task;
                }
            }

            var dictionary = new Dictionary<string, FluidValue>(Values.Count);
            for (int i = 0; i < Values.Count; i++)
            {
                dictionary[Values[i].Item1] = tasks[i].Result;
            }

            return FluidValue.Create(dictionary, context.Options);
        }
    }
}
