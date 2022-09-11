using System.Web;
using System.Web.Routing;
using PI2.HttpHandlers;

namespace PI2.RouteHandlers
{
    class WebSocketRouteHandler : IRouteHandler
    {
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new WebSocketHttpHandler();
        }
    }
}