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
		public NimbleApp app { get; internal set; }

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

		public RequestContext(HttpListenerContext httpContext)
		{
			this.httpContext = httpContext;

			statusCode = HttpStatusCode.OK;
			response.AddHeader("Date", DateTime.Now.ToString("r"));
		}

		public bool ValidateRouteVariableType<T>(string key)
		{
			return ValidateRouteVariableType(key, typeof(T));
		}

		public bool ValidateRouteVariableType(string key, Type type)
		{
			if (!RouteVariableIs("id", type))
			{
				app.onRouteVariableValidationFailure?.Invoke(this, key, type);
				return false;
			}
			return true;
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

			}
			return value;
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

		public void ServeStaticFile()
		{
			ServeStaticFile(StaticFileSettings.defaultSettings);
		}

		public void ServeStaticFile(StaticFileSettings staticFileSettings)
		{
			string filename = request.Url.AbsolutePath;
			//Console.WriteLine(filename);
			filename = filename.Substring(1);

			if (string.IsNullOrEmpty(filename))
			{
				foreach (string indexFile in staticFileSettings.indexFiles)
				{
					if (File.Exists(indexFile))
					{
						filename = indexFile;
						break;
					}
				}
			}

			if (File.Exists(filename))
			{
				response.ContentType = staticFileSettings.GetContentTypeForExtension(Path.GetExtension(filename));

				byte[] input = File.ReadAllBytes(filename);

				Write(input);

				response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));
			}
			else
			{
				response.StatusCode = (int)HttpStatusCode.NotFound;
			}
		}
	}
}

