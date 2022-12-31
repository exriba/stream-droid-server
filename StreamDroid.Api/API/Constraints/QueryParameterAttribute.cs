using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace StreamDroid.Application.API.Constraints
{
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryParameterAttribute : Attribute, IActionConstraint
    {
        public int Order { get; }
        private readonly string[] _parameters;

        public QueryParameterAttribute(params string[] parameters) => _parameters = parameters;

        public bool Accept(ActionConstraintContext context) => context.RouteContext.HttpContext.Request.Query.Keys.Intersect(_parameters).Count() == _parameters.Length;
    }
}
