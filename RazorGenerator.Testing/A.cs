namespace RazorGenerator.Testing
{
    using System.Collections;
    using System.Collections.Specialized;
    using System.Web;
    using Moq;
    
    public partial class A
    {
        public static HttpContextBuilder HttpContext = new HttpContextBuilder();
    }

    public class HttpContextBuilder
    {
        private HttpRequestBase _request;
        private HttpResponseBase _response;

        public HttpContextBuilder()
        {
            _request = GetDefaultRequest();

            _response = GetDefaultResponse();
        }

        public HttpContextBuilder With(HttpRequestBase request)
        {
            _request = request;
            return this;
        }

        public HttpContextBuilder With(HttpResponseBase response)
        {
            _response = response;
            return this;
        }

        public HttpContextBase Build()
        {
            var mockHttpContext = new Mock<HttpContextBase>(MockBehavior.Loose);
            mockHttpContext.Setup(m => m.Items).Returns(new Hashtable());
            mockHttpContext.Setup(m => m.Request).Returns(_request);
            mockHttpContext.Setup(m => m.Response).Returns(_response);

            return mockHttpContext.Object;
        }

        private static HttpResponseBase GetDefaultResponse()
        {
            var mockResponse = new Mock<HttpResponseBase>(MockBehavior.Loose);
            mockResponse.Setup(m => m.ApplyAppPathModifier(It.IsAny<string>()))
                .Returns<string>(virtualPath => virtualPath);
            mockResponse.Setup(m => m.Cookies).Returns(new HttpCookieCollection());
            return mockResponse.Object;
        }

        private static HttpRequestBase GetDefaultRequest()
        {
            var mockRequest = new Mock<HttpRequestBase>(MockBehavior.Loose);
            mockRequest.Setup(m => m.IsLocal).Returns(false);
            mockRequest.Setup(m => m.ApplicationPath).Returns("/");
            mockRequest.Setup(m => m.ServerVariables).Returns(new NameValueCollection());
            mockRequest.Setup(m => m.RawUrl).Returns(string.Empty);
            mockRequest.Setup(m => m.Cookies).Returns(new HttpCookieCollection());
            return mockRequest.Object;
        }
    }
}