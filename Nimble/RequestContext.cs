using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nimble
{
	public class RequestContext
	{
		public NimbleApp app { get; private set; }
		public bool hasBeenCommitted { get; private set; }

		/// <summary>
		/// The internal <see cref="HttpListenerContext"/> for this request.
		/// </summary>
		public HttpListenerContext httpContext { get; private set; }

		private MemoryStream responseStream = new MemoryStream();

		/// <summary>
		/// A shortcut to request object.
		/// </summary>
		public HttpListenerRequest request
		{
			get
			{
				return httpContext.Request;
			}
		}

		/// <summary>
		/// A shortcut to the response object.
		/// </summary>
		public HttpListenerResponse response
		{
			get
			{
				return httpContext.Response;
			}
		}

		/// <summary>
		/// A shortcut to the response status code.
		/// </summary>
		public HttpStatusCode statusCode
		{
			get
			{
				return (HttpStatusCode)httpContext.Response.StatusCode;
			}
			set
			{
				httpContext.Response.StatusCode = (int)value;
			}
		}

		public Dictionary<string, string> routeVariables { get; private set; } = new Dictionary<string, string>();

		public RequestContext(NimbleApp app, HttpListenerContext httpContext)
		{
			this.app = app;
			this.httpContext = httpContext;

			if (app.defaultContentType != null)
			{
				response.ContentType = app.defaultContentType;
			}

			statusCode = HttpStatusCode.OK;
			response.AddHeader("Date", DateTime.Now.ToString("r"));
		}

		public bool RouteVariableIs<T>(string key)
		{
			return RouteVariableIs(key, typeof(T));
		}

		public bool RouteVariableIs(string key, Type type)
		{
			string valueString = GetRouteVariable(key);
			if (valueString == null)
			{
				return false;
			}
			try
			{
				Convert.ChangeType(valueString, type, System.Globalization.CultureInfo.InvariantCulture);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public T GetRouteVariable<T>(string key)
		{
			string valueString = GetRouteVariable(key);
			if (valueString == null)
			{
				return default;
			}
			T value = default;
			try
			{
				value = (T)Convert.ChangeType(valueString, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
			}
			catch (Exception)
			{
				throw new RouteVariableValidationException(this, key, typeof(T));
			}
			return value;
		}

		public string GetRouteVariable(string key)
		{
			routeVariables.TryGetValue(key, out string v);
			return v;
		}

		public void SetRouteVariable(string key, object value)
		{
			if (value == null)
			{
				routeVariables.Remove(key);
			}
			else
			{
				routeVariables[key] = value.ToString();
			}
		}

		public void Clear()
		{
			responseStream.Position = 0;
			responseStream.SetLength(0);
		}

		public void Write(string text)
		{
			Write(Encoding.UTF8.GetBytes(text));
		}

		public void Write(byte[] bytes)
		{
			responseStream.Write(bytes, 0, bytes.Length);
		}

		public void Write(Stream stream)
		{
			long pos = stream.Position;
			stream.Position = 0;
			stream.CopyTo(responseStream);
			stream.Position = pos;
		}

		public void Commit()
		{
			if (hasBeenCommitted)
			{
				return;
			}
			hasBeenCommitted = true;
			response.ContentLength64 += responseStream.Length;
			responseStream.WriteTo(response.OutputStream);
			response.OutputStream.Flush();
		}

		public string GetRequestPostData()
		{
			if (!request.HasEntityBody)
			{
				return null;
			}
			using (Stream body = request.InputStream)
			{
				using (StreamReader reader = new StreamReader(body, request.ContentEncoding))
				{
					return reader.ReadToEnd();
				}
			}
		}

		public void ServeStaticFile(string filePath, string cacheControl = null)
		{
			ServeStaticFile(filePath, StaticFileSettings.defaultSettings, cacheControl);
		}

		void ServeStaticFile(string filePath, StaticFileSettings staticFileSettings, string cacheControl = null)
		{
			hasBeenCommitted = true;
			if (filePath.StartsWith("/"))
			{
				filePath = filePath.Substring(1);
			}

			string fullAppPath = Path.GetFullPath(Environment.CurrentDirectory);
			string fullPath = Path.GetFullPath(filePath);
			if (!fullPath.StartsWith(fullAppPath))
			{
				Console.WriteLine($"Path is outside of server root \"{filePath}\"");
				Console.WriteLine(fullAppPath);
				Console.WriteLine(fullPath);
				statusCode = HttpStatusCode.NotFound;
				return;
			}

			string filename = Path.GetFileName(filePath);
			
			if (File.Exists(filePath))
			{
				using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					response.ContentLength64 = stream.Length;
					response.SendChunked = false;
					response.ContentType = staticFileSettings.GetContentTypeForExtension(Path.GetExtension(filePath));
					if (!string.IsNullOrEmpty(cacheControl))
					{
						response.AddHeader("cache-control", cacheControl);
					}
					response.AddHeader("Content-disposition", $"attachment; filename={filename}");
					
					byte[] buffer = new byte[64 * 1024];
					int read;
					using (BinaryWriter binaryWriter = new BinaryWriter(response.OutputStream))
					{
						while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							binaryWriter.Write(buffer, 0, read);
							binaryWriter.Flush();
						}
					}
				}
			}
			else
			{
				statusCode = HttpStatusCode.NotFound;
			}

			response.OutputStream.Close();
		}
	}
}

