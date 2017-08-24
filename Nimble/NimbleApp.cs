using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nimble
{
	public class NimbleApp
	{
		public NimbleServer server { get; protected set; }
		public List<Router> routers { get; private set; } = new List<Router>();

		public delegate void RouteVariableValidationFailureDelegate(RequestContext context, string key, Type type);
		public RouteVariableValidationFailureDelegate routeVariableValidationFailure { get; set; }

		public delegate void OnRequestDelegate(RequestContext context);
		public OnRequestDelegate onInitializeRequest { get; set; }
		public OnRequestDelegate onFinalizeRequest { get; set; }

		public string defaultContentType { get; set; }

		private int tempPort;

		public NimbleApp(int port) : this("*", port)
		{

		}

		public NimbleApp(string domainPattern, int port)
		{
			tempPort = port;
			server = new NimbleServer(domainPattern, tempPort);
			server.onHttpRequest += OnHttpRequest;
		}

		private void OnHttpRequest(RequestContext context)
		{
			context.app = this;
			if (defaultContentType != null)
			{
				context.response.ContentType = defaultContentType;
			}

			onInitializeRequest?.Invoke(context);

			foreach (var router in routers)
			{
				if (router.Evaluate(context))
				{
					break;
				}
			}

			onFinalizeRequest?.Invoke(context);
		}
	}
}

