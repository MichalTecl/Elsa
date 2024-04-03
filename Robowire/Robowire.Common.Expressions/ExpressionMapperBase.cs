using System.Linq.Expressions;

namespace Robowire.Common.Expressions
{
    public abstract class ExpressionMapperBase<T>
    {
        public virtual T Map(Expression expression)
        {
            if (expression == null)
            {
                return MapNullExpression();
            }

            if (expression is BinaryExpression) { return MapBinaryExpression((BinaryExpression)expression);  }
            if (expression is BlockExpression) { return MapBlockExpression((BlockExpression)expression);  }
            if (expression is ConditionalExpression) { return MapConditionalExpression((ConditionalExpression)expression); }
            if (expression is InvocationExpression) { return MapInvocationExpression((InvocationExpression)expression); }
            if (expression is LambdaExpression) { return MapLambdaExpression((LambdaExpression)expression); }
            if (expression is MemberExpression) { return MapMemberExpression((MemberExpression)expression); }
            if (expression is MethodCallExpression) { return MapMethodCallExpression((MethodCallExpression)expression); }
            if (expression is ParameterExpression) { return MapParameterExpression((ParameterExpression)expression); }
            if (expression is UnaryExpression) { return MapUnaryExpression((UnaryExpression)expression); }
            if (expression is ConstantExpression) { return MapConstantExpression((ConstantExpression)expression);}

            return MapExpression(expression);
        }

        protected virtual T MapBinaryExpression(BinaryExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapBlockExpression(BlockExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapConditionalExpression(ConditionalExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapInvocationExpression(InvocationExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapLambdaExpression(LambdaExpression expression)
        {
            return Map(expression.Body);
        }

        protected virtual T MapMemberExpression(MemberExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapMethodCallExpression(MethodCallExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapParameterExpression(ParameterExpression expression)
        {
            return MapExpression(expression);
        }

        protected virtual T MapUnaryExpression(UnaryExpression expression)
        {
            return MapExpression(expression);
        }  

        protected virtual T MapConstantExpression(ConstantExpression expression)
        {
            return MapExpression(expression);
        }

        protected abstract T MapNullExpression();

        protected abstract T MapExpression(Expression expression);
    }
}
