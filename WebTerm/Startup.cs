
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace WebTerm
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddTransient<WebTerm.Services.ITerminalServiceFactory, WebTerm.Services.TerminalServiceFactory>();
            

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, WebTerm.Services.ITerminalServiceFactory terminalFactory)
        {
            
            loggerFactory.AddConsole();

            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            
            
            app.UseWebSockets();
            app.Use(async (context, next) =>
            {
                var http = (HttpContext)context;

                if (http.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await http.WebSockets.AcceptWebSocketAsync())
                    using (var terminal = terminalFactory.CreateTerminalService())
                    {
                        terminal.OnRead = async (data) =>
                        {
                            var response = new WebTerm.Model.TerminalResponse
                            {
                                Type = WebTerm.Model.TerminalResponseType.Text,
                                Data = data
                            };

                            using (var stream = new System.IO.MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WebTerm.Model.TerminalResponse));

                                serializer.WriteObject(stream, response);
                                var sendBuffer = stream.ToArray();

                                await webSocket?.SendAsync(
                                    new System.ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length),
                                    System.Net.WebSockets.WebSocketMessageType.Text,
                                    true, System.Threading.CancellationToken.None);
                            }
                        };
                        terminal.OnError = async (data) =>
                        {
                            var response = new WebTerm.Model.TerminalResponse
                            {
                                Type = WebTerm.Model.TerminalResponseType.Error,
                                Data = data
                            };

                            using (var stream = new System.IO.MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WebTerm.Model.TerminalResponse));

                                serializer.WriteObject(stream, response);
                                var sendBuffer = stream.ToArray();

                                await webSocket?.SendAsync(
                                    new System.ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length),
                                    System.Net.WebSockets.WebSocketMessageType.Text,
                                    true, System.Threading.CancellationToken.None);
                            }
                        };
                        terminal.OnIdle = async () =>
                        {
                            var response = new WebTerm.Model.TerminalResponse
                            {
                                Type = WebTerm.Model.TerminalResponseType.Idle
                            };

                            using (var stream = new System.IO.MemoryStream())
                            {
                                var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(WebTerm.Model.TerminalResponse));

                                serializer.WriteObject(stream, response);
                                var sendBuffer = stream.ToArray();

                                await webSocket?.SendAsync(
                                    new System.ArraySegment<byte>(sendBuffer, 0, sendBuffer.Length),
                                    System.Net.WebSockets.WebSocketMessageType.Text,
                                    true, System.Threading.CancellationToken.None);
                            }
                        };
                        
                        await terminal.StartAsync();

                        byte[] receiveBuffer = new byte[1024];
                        var received = await webSocket.ReceiveAsync(
                            new System.ArraySegment<byte>(receiveBuffer), System.Threading.CancellationToken.None);

                        while (received.MessageType != System.Net.WebSockets.WebSocketMessageType.Close)
                        {
                            // using System.Linq;
                            // string message = GetString(receiveBuffer.Take(received.Count).ToArray());
                            
                            byte[] dest = new byte[received.Count];
                            System.Array.Copy(receiveBuffer, dest, received.Count);
                            string message = GetString(dest);
                            
                            await terminal.WriteAsync(message);
                            
                            received = await webSocket.ReceiveAsync(
                                new System.ArraySegment<byte>(receiveBuffer), System.Threading.CancellationToken.None);
                        } // Whend 
                        
                        await webSocket.CloseAsync(received.CloseStatus.Value, received.CloseStatusDescription, 
                            System.Threading.CancellationToken.None);
                    }
                }
                else
                {
                    await next();
                }
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();
            
            
            // app.UseCookiePolicy();
            if(false)
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
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
