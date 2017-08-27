using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nimble;

namespace NimbleTest
{
	public class Program
	{
		static void Main(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if (arg == "-path" && i < args.Length-1)
				{
					Environment.CurrentDirectory = args[i+1];
					i++;
				}
			}

			//int minWorker, minIOC;
			//ThreadPool.GetMinThreads(out minWorker, out minIOC);
			//ThreadPool.SetMinThreads(30, minIOC);

			Console.WriteLine($"Running server in \"{Environment.CurrentDirectory}\"");

			NimbleApp app = null;
			app = new NimbleApp(80)
			{
				defaultContentType = "application/json",
				onInitializeRequest = (context) =>
				{
					Console.WriteLine($"{context.request.Url.AbsolutePath}");
				},
				onFinalizeRequest = (context) =>
				{
					Console.WriteLine($"done {app.server.concurrentConnections} connections {app.server.concurrentThreads} threads.");
				}
			};

			app.rootRouter = new Router("/")
			{
				onRouteVariableException = (exception) =>
				{
					exception.context.statusCode = System.Net.HttpStatusCode.BadRequest;
					exception.context.Clear();
					exception.context.Write(JsonConvert.SerializeObject(new { error = exception.Message }));
				},
				onExecute = (context) =>
				{
					context.response.ContentType = "text/html";
					context.Write("<h3>Hello</h3>");
				},
				subRouters =
				{
					new Router("favicon.ico")
					{
						onExecute = (context) =>
						{
							context.ServeStaticFile();
						}
					},
					new Router("loaderio-f20d53b48da411ef71d7b647d008884f.txt")
					{
						onExecute = (context) =>
						{
							context.ServeStaticFile();
						}
					},
					new Router("{id}")
					{
						onExecute = (context) =>
						{
							int id = context.GetRouteVariable<int>("id");

							var myPacket = new
							{
								id = id,
								time = DateTime.Now
							};
							context.Write(JsonConvert.SerializeObject(myPacket));
						},
						subRouters =
						{
							new Router("{name}")
							{
								onExecute = (context) =>
								{
									int id = context.GetRouteVariable<int>("id");
									string name = context.GetRouteVariable("name");

									var myPacket = new
									{
										id = id,
										name = name,
										time = DateTime.Now
									};

									string json = JsonConvert.SerializeObject(myPacket);
									context.Write(json);
									//System.Threading.Thread.Sleep(100);
								}
							}
						}
					}
				}
			};

			app.Start();

			while (true)
			{
				System.Threading.Thread.Sleep(100);
			}
		}
	}
}
