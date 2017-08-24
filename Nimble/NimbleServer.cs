using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nimble
{
	public class NimbleServer
	{
		public delegate void OnHttpRequestDelegate(RequestContext context);
		public event OnHttpRequestDelegate onHttpRequest;

		public delegate void OnStopDelegate();
		public event OnStopDelegate onStop;

		public string domainPattern { get; private set; }
		public int port { get; private set; }

		public bool isStarted
		{
			get
			{
				return listenThread != null;
			}
		}

		private Thread listenThread;
		private HttpListener httpListener;

		public NimbleServer(string domainPattern, int port)
		{
			this.domainPattern = domainPattern;
			this.port = port;
		}

		public void Start()
		{
			if (isStarted)
			{
				throw new Exception($"{nameof(NimbleServer)} already started.");
			}

			listenThread = new Thread(Listen);
			listenThread.IsBackground = true;
			listenThread.Start();
		}

		public void Stop()
		{
			if (!isStarted)
			{
				throw new Exception($"{nameof(NimbleServer)} is not started.");
			}

			listenThread.Abort();
			listenThread = null;
			httpListener.Stop();
			httpListener = null;
			onStop?.Invoke();
		}

		private void Listen()
		{
			httpListener = new HttpListener();
			httpListener.Prefixes.Add($"http://{domainPattern}:{port.ToString()}/");
			httpListener.Start();

			while (true)
			{
				HttpListenerContext context = httpListener.GetContext();
				RequestContext requestContext = new RequestContext(context);

				Task.Run(() => {
					onHttpRequest?.Invoke(requestContext);
					requestContext.Commit();
					requestContext.response.Close();
				});
			}
		}
	}
}
