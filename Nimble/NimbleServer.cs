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
		public delegate void OnHttpRequestDelegate(HttpListenerContext context);
		public event OnHttpRequestDelegate onHttpRequest;

		public delegate void OnStopDelegate();
		public event OnStopDelegate onStop;

		public string domainPattern { get; private set; }
		public int port { get; private set; }

		public bool isStarted
		{
			get
			{
				return cancellationTokenSource != null;
			}
		}
		
		private CancellationTokenSource cancellationTokenSource;
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
			cancellationTokenSource = new CancellationTokenSource();
			Task.Run(() => Listen(cancellationTokenSource.Token), cancellationTokenSource.Token);
		}

		public void Stop()
		{
			if (!isStarted)
			{
				throw new Exception($"{nameof(NimbleServer)} is not started.");
			}

			cancellationTokenSource.Cancel();
			httpListener.Stop();
			httpListener = null;
			cancellationTokenSource = null;

			onStop?.Invoke();
		}

		private void Listen(CancellationToken cancellationToken)
		{
			httpListener = new HttpListener();
			httpListener.Prefixes.Add($"http://{domainPattern}:{port.ToString()}/");
			httpListener.Start();

			while (true)
			{
				try
				{
					HttpListenerContext context = httpListener.GetContext();

					Task.Run(() => {
						onHttpRequest?.Invoke(context);
						context.Response.Close();
					});
				}
				catch (HttpListenerException)
				{

				}

				if (cancellationToken.IsCancellationRequested)
				{
					break;
				}
			}
		}
	}
}
