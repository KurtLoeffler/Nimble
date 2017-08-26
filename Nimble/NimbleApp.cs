using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nimble
{
	public class NimbleApp
	{
		public NimbleServer server { get; protected set; }
		public Router rootRouter { get; set; }

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

		public void Start()
		{
			server.Start();
		}

		public void Stop()
		{
			server.Stop();
		}

		private void OnHttpRequest(HttpListenerContext httpListenerContext)
		{
			RequestContext context = new RequestContext(this, httpListenerContext);
			
			onInitializeRequest?.Invoke(context);

			if (rootRouter != null)
			{
				string path = context.request.Url.AbsolutePath;
				rootRouter.Evaluate(context, path);
			}

			onFinalizeRequest?.Invoke(context);

			context.Commit();
		}
	}
}

