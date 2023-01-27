using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid.Values;

namespace Fluid.Ast
{
    public sealed class ArrayLiteralExpression : Expression
    {
        public ArrayLiteralExpression(List<Expression> expressions)
        {
            Expressions = expressions;
        }

        public List<Expression> Expressions { get; }

        // TODO Refactor
        public async override ValueTask<FluidValue> EvaluateAsync(TemplateContext context)
        {
            var containsFilter = Expressions.Any(x => x is FilterExpression);
            if (!containsFilter)
            {
                var arrayValue = new ArrayValue(Expressions.Cast<LiteralExpression>().Select(x=> x.Value));
                return arrayValue;
            }


            var tasks = new ValueTask<FluidValue>[Expressions.Count];
            for (int i = 0; i < Expressions.Count; i++)
            {
                tasks[i] = Expressions[i].EvaluateAsync(context);
            }

            foreach (var task in tasks)
            {
                if (!task.IsCompletedSuccessfully)
                {
                    await task;
                }
            }

            return new ArrayValue(tasks.Select(x => x.Result));
        }
    }
}
