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

		public int concurrentConnections { get; private set; }
		public int concurrentThreads { get; private set; }

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

			httpListener = new HttpListener();
			httpListener.Prefixes.Add($"http://{domainPattern}:{port.ToString()}/");
			try
			{
				httpListener.Start();
			}
			catch (HttpListenerException)
			{
				Console.WriteLine($"Error starting {nameof(HttpListener)}. On windows the process must be run with admin privileges.");
				throw;
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
			concurrentConnections = 0;
			concurrentThreads = 0;

			while (true)
			{
				try
				{
					HttpListenerContext context = httpListener.GetContext();

					lock (this)
					{
						concurrentConnections++;
					}
					//Console.WriteLine("Begin "+concurrentConnections+" "+concurrentThreads);

					Task.Run(() => {
						lock (this)
						{
							concurrentThreads++;
						}
						onHttpRequest?.Invoke(context);
						context.Response.Close();
						lock (this)
						{
							concurrentConnections--;
							concurrentThreads--;
							//Console.WriteLine("End "+concurrentConnections+" "+concurrentThreads);
						}
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

			concurrentConnections = 0;
			concurrentThreads = 0;
		}
	}
}
