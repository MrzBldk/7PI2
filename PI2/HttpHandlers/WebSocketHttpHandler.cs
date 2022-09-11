using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebSockets;

namespace PI2.HttpHandlers
{
    public class WebSocketHttpHandler : IHttpHandler
    {
        private WebSocket webSocket;
        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(ProcessWebSocketRequest);
        }

        private async Task ProcessWebSocketRequest(AspNetWebSocketContext context)
        {
            webSocket = context.WebSocket;

            var buffer = new ArraySegment<byte>(new byte[1024]);
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            Debug.WriteLine(result);

            string mes = DateTime.Now.ToLongTimeString();

            while (webSocket.State == WebSocketState.Open)
            {
                Array.Clear(buffer.Array, 0, buffer.Array.Length);
                System.Text.Encoding.UTF8.GetBytes(mes, 0, mes.Length, buffer.Array, 0);

                await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                Thread.Sleep(2000);

                mes = DateTime.Now.ToLongTimeString();
            }
        }

        public bool IsReusable => false;
    }
}