using Fluid.Values;
using System;

namespace Fluid.Ast.BinaryExpressions
{
    public sealed class FloorDivideBinaryExpression : BinaryExpression
    {
        public FloorDivideBinaryExpression(Expression left, Expression right) : base(left, right)
        {
        }

        internal override FluidValue Evaluate(FluidValue leftValue, FluidValue rightValue)
        {
            if (leftValue is NumberValue && rightValue is NumberValue)
            {
                var rightNumber = rightValue.ToNumberValue();

                if (rightNumber == 0)
                {
                    return NilValue.Instance;
                }

                return NumberValue.Create(Math.Floor(leftValue.ToNumberValue() / rightNumber));
            }

            return NilValue.Instance;
        }
    }
}
