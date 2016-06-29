using System;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebTerm
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                var http = (HttpContext)context;

                if (http.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await http.WebSockets.AcceptWebSocketAsync())
                    using (var terminal = new Terminal())
                    {
                        terminal.OnRead = async (data) =>
                        {
                            var sendBuffer = GetBytes(data);

                            if (webSocket != null)
                            {
                                await webSocket.SendAsync(
                                    new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length),
                                    WebSocketMessageType.Text,
                                    true, CancellationToken.None);
                            }
                        };
                        terminal.OnError = async (data) =>
                        {
                            var sendBuffer = GetBytes(data);

                            if (webSocket != null)
                            {
                                await webSocket.SendAsync(
                                    new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length),
                                    WebSocketMessageType.Text,
                                    true, CancellationToken.None);
                            }
                        };

                        terminal.Start();

                        var receiveBuffer = new byte[1024];
                        var received = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                        while (received.MessageType != WebSocketMessageType.Close)
                        {
                            var message = GetString(receiveBuffer.Take(received.Count).ToArray());
                            await terminal.WriteAsync(message);

                            received = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        }

                        await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, CancellationToken.None);
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }

        private static byte[] GetBytes(string str)
        {
            return !string.IsNullOrEmpty(str) ? System.Text.Encoding.UTF8.GetBytes(str) : new byte[0];
        }

        private static string GetString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
