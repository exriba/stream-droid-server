using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace StreamDroid.Application.API.Constraints
{
    /// <summary>
    /// Query parameter attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class QueryParameterAttribute : Attribute, IActionConstraint
    {
        public int Order { get; }
        private readonly string[] _parameters;

        public QueryParameterAttribute(params string[] parameters) => _parameters = parameters;

        /// <summary>
        /// Validates that the HTTP request query parameters length matches the given parameters length.
        /// </summary>
        /// <param name="context">action constraint context</param>
        /// <returns><see langword="true"/> if the HTTP request query parameters length matches the given parameters length. Otherwise returns <see langword="false"/>.</returns>
        public bool Accept(ActionConstraintContext context) => context.RouteContext.HttpContext.Request.Query.Keys.Intersect(_parameters).Count() == _parameters.Length;
    }
}
