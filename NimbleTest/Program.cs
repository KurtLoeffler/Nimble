using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			NimbleApp app = new NimbleApp(80)
			{
				defaultContentType = "application/json",
				onInitializeRequest = (context) =>
				{
					Console.WriteLine(context.request.Url.AbsolutePath);
				},
				routeVariableValidationFailure = (context, key, type) =>
				{
					context.statusCode = System.Net.HttpStatusCode.BadRequest;
					context.Write(JsonConvert.SerializeObject(new { error = $"{{{key}}} (\"{context.GetRouteVariable(key)}\") is not convertable to {type}." }));
				}
			};

			var rootRoute = new Router("/")
			{
				onExecute = (context) =>
				{
					context.response.ContentType = "text/html";
					context.Write("<h3>Hello</h3>");
				}
			};
			app.routers.Add(rootRoute);

			var idRoute = new Router("/{id}")
			{
				onExecute = (context) =>
				{
					if (!context.ValidateRouteVariableType<int>("id"))
					{
						return;
					}

					int id = context.GetRouteVariable<int>("id");

					var myPacket = new
					{
						id = id,
						name = "Keen",
						time = DateTime.Now
					};
					context.Write(JsonConvert.SerializeObject(myPacket));
					
				}
			};
			app.routers.Add(idRoute);

			while (true)
			{
				System.Threading.Thread.Sleep(100);
			}
		}
	}
}
