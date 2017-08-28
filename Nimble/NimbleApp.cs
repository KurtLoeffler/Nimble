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
			OnBeforeStart();
			server.Start();
			OnAfterStart();
		}

		protected virtual void OnBeforeStart()
		{

		}

		protected virtual void OnAfterStart()
		{

		}

		public void Stop()
		{
			OnBeforeStop();
			server.Stop();
			OnAfterStop();
		}

		protected virtual void OnBeforeStop()
		{

		}

		protected virtual void OnAfterStop()
		{

		}

		private void OnHttpRequest(HttpListenerContext httpListenerContext)
		{
			RequestContext context = new RequestContext(this, httpListenerContext);
			
			OnInitializeRequest(context);
			onInitializeRequest?.Invoke(context);

			if (rootRouter != null)
			{
				string path = context.request.Url.AbsolutePath;
				if (!rootRouter.Evaluate(context))
				{
					OnRouteNotFound(context);
				}
			}

			OnFinalizeRequest(context);
			onFinalizeRequest?.Invoke(context);

			context.Commit();
		}

		protected virtual void OnInitializeRequest(RequestContext context)
		{

		}

		protected virtual void OnFinalizeRequest(RequestContext context)
		{

		}

		protected virtual void OnRouteNotFound(RequestContext context)
		{
			context.statusCode = HttpStatusCode.NotFound;
		}
	}
}

