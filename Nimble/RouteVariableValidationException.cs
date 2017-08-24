using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nimble
{

	[Serializable]
	public class RouteVariableValidationException : Exception
	{
		public RequestContext context { get; }
		public string key { get; }
		public Type type { get; }

		public override string Message
		{
			get
			{
				return $"{{{key}}} (\"{context.GetRouteVariable(key)}\") is not convertable to {type}.";
			}
		}

		public RouteVariableValidationException(RequestContext context, string key, Type type)
		{
			this.context = context;
			this.key = key;
			this.type = type;
		}
	}
}
