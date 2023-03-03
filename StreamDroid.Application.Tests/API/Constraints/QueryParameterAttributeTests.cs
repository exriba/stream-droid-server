using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Routing;
using StreamDroid.Application.API.Constraints;

namespace StreamDroid.Application.Tests.API.Constraints
{
    public class QueryParameterAttributeTests
    {
        private readonly Mock<IQueryCollection> _mockQueryCollection;
        private readonly ActionConstraintContext _actionConstraintContext;

        public QueryParameterAttributeTests()
        {
            _mockQueryCollection = new Mock<IQueryCollection>();

            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Query).Returns(_mockQueryCollection.Object);

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.Request).Returns(mockRequest.Object);

            _actionConstraintContext = new ActionConstraintContext
            {
                RouteContext = new RouteContext(mockHttpContext.Object)
            };
        }

        [Theory]
        [InlineData("param1", "param2")]
        public void QueryParameterAttribute_Accept_True(string param1, string param2)
        {
            _mockQueryCollection.Setup(x => x.Keys).Returns(new List<string> { param1, param2 });

            var _queryParameterAttribute = new QueryParameterAttribute(param1, param2);
            var result = _queryParameterAttribute.Accept(_actionConstraintContext);

            Assert.True(result);
        }

        [Theory]
        [InlineData("param1", "param2")]
        public void QueryParameterAttribute_Accept_False(string param1, string param2)
        {
            _mockQueryCollection.Setup(x => x.Keys).Returns(new List<string> { param1, param2 });

            var _queryParameterAttribute = new QueryParameterAttribute(param1, param2, "param3");
            var result = _queryParameterAttribute.Accept(_actionConstraintContext);

            Assert.False(result);
        }
    }
}
