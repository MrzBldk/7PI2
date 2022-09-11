using PI2.RouteHandlers;
using System.Web.Routing;

namespace PI2
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.Add(new Route("index", new IndexRouteHandler()));
            routes.Add(new Route("websocket", new WebSocketRouteHandler()));
        }
    }
}