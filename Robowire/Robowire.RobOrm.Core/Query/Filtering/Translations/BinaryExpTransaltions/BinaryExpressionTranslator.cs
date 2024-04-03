using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Robowire.RobOrm.Core.Query.Filtering.Translations.BinaryExpTransaltions
{
    public static class BinaryExpressionTranslator
    {
        private static readonly Dictionary<ExpressionType, string> s_formats = new Dictionary<ExpressionType, string> {
            {ExpressionType.Add, "({0} + {1})"},
            {ExpressionType.AddChecked, "({0} + {1})"},
            {ExpressionType.Divide, "({0} / {1})"},
            {ExpressionType.Modulo, "({0} % {1})"},
            {ExpressionType.Multiply, "({0} * {1})"},
            {ExpressionType.MultiplyChecked, "({0} * {1})"},
            {ExpressionType.Power, "POWER({0}, {1})"},
            {ExpressionType.Subtract, "({0} - {1})"},
            {ExpressionType.SubtractChecked, "({0} - {1})"},
            {ExpressionType.And, "({0} & {1})"},
            {ExpressionType.Or, "({0} | {1})"},
            {ExpressionType.ExclusiveOr, "({0} ^ {1})"},
            {ExpressionType.AndAlso, "({0} AND {1})"},
            {ExpressionType.OrElse, "({0} OR {1})"},
            {ExpressionType.Equal, "({0} = {1})"},
            {ExpressionType.NotEqual, "({0} <> {1})"},
            {ExpressionType.GreaterThanOrEqual, "({0} >= {1})"},
            {ExpressionType.GreaterThan, "({0} > {1})"},
            {ExpressionType.LessThan, "({0} < {1})"},
            {ExpressionType.LessThanOrEqual, "({0} <= {1})"},
            {ExpressionType.Coalesce, "ISNULL({0}, {1})"}};

        public static string Translate(ExpressionType type, string left, string right)
        {
            string format;
            if (!s_formats.TryGetValue(type, out format))
            {
                throw new NotSupportedException($"Operation of type {type} is not supported");
            }

            return string.Format(format, left, right);
        }


    }
}
