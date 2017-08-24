using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nimble
{
	public class Router
	{
		public string urlPattern { get; private set; }
		public string escapedPattern { get; private set; }

		public delegate void OnExecuteDelegate(RequestContext context);
		public OnExecuteDelegate onExecute;

		private List<string> groupNames = new List<string>();
		private string regexPattern;

		public Router(string urlPattern) : this(urlPattern, null)
		{

		}

		public Router(string urlPattern, OnExecuteDelegate onExecute)
		{
			this.urlPattern = urlPattern;
			this.onExecute = onExecute;

			escapedPattern = Regex.Escape(urlPattern);

			regexPattern = Regex.Replace(escapedPattern, "\\\\{([a-zA-Z0-9_]*)\\}", (m) => {
				string groupName = m.Groups[1].Value;
				groupNames.Add(groupName);
				return $"(?<{groupName}>[^/]+)";
			});
			regexPattern = $"^{regexPattern}/?$";
		}

		public bool Evaluate(RequestContext context)
		{
			string path = context.request.Url.AbsolutePath;
			//Console.WriteLine(path);

			Match match = Regex.Match(path, regexPattern, RegexOptions.Singleline);

			if (match.Success)
			{
				if (match.Groups.Count != groupNames.Count+1)
				{
					throw new Exception("Unexpected group count.");
				}
				
				for (int i = 1; i < match.Groups.Count; i++)
				{
					Group group = match.Groups[i];
					string groupName = groupNames[i-1];
					context.SetRouteVariable(groupName, group.Value);
				}

				onExecute?.Invoke(context);
				return true;
			}

			return false;
		}
	}
}
