﻿using System;
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
				onRouteVariableValidationException = (exception) =>
				{
					exception.context.statusCode = System.Net.HttpStatusCode.BadRequest;
					exception.context.Clear();
					exception.context.Write(JsonConvert.SerializeObject(new { error = exception.Message }));
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
					context.ValidateRouteVariableType<int>("id");

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

			app.Start();

			while (true)
			{
				System.Threading.Thread.Sleep(100);
			}
		}
	}
}
