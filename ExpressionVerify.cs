    internal static class ExpressionVerify
    {
        internal static bool CheckExpression(string expression, Dictionary<string, object> parames)
        {
            // 创建一个参数表达式，用于表达式树中的参数
            List<ParameterExpression> parameters = new List<ParameterExpression>();
            foreach (var parameter in parames)
            {
                ParameterExpression param = Expression.Parameter(parameter.Value.GetType(), parameter.Key);
                parameters.Add(param);
            }

            // 使用 DynamicExpressionParser 解析字符串表达式为 Lambda 表达式
            var lambda = DynamicExpressionParser.ParseLambda(parameters.ToArray(), null, expression);

            // 编译表达式树为可执行代码
            var compiledLambda = lambda.Compile();

            // 执行表达式并返回结果
            var args = parames.Select(t => t.Value).ToArray();
            return (bool)compiledLambda.DynamicInvoke(args);
        }
    }
