using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
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
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = "cmd.exe";
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.RedirectStandardInput = true;

                        process.OutputDataReceived += async (sender, e) =>
                        {
                            var sendBuffer = GetBytes(e.Data);

                            await webSocket.SendAsync(
                                new ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length), 
                                WebSocketMessageType.Text, 
                                true, 
                                CancellationToken.None);
                        };
                        
                        process.Start();
                        var processWriter = process.StandardInput;
                        process.BeginOutputReadLine();

                        var receiveBuffer = new byte[1024];
                        var received = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);

                        while (received.MessageType != WebSocketMessageType.Close)
                        {
                            var b = new byte[received.Count + 1];
                            Array.Copy(receiveBuffer, 0, b, 0, b.Length);
                            var message = GetString(b);
                            processWriter.WriteLine(message);
                            processWriter.Flush();

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

        static byte[] GetBytes(string str)
        {
            return System.Text.Encoding.UTF8.GetBytes(str);
        }

        static string GetString(byte[] bytes)
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
    }
}
