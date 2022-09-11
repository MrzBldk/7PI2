using System.Web;

namespace PI2.HttpHandlers
{
    public class IndexHttpHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.WriteFile("static/Index.html");
        }

        public bool IsReusable => false;
    }
}