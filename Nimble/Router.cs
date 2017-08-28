using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace Nimble
{
	public class Router
	{
		public Router parent { get; private set; }

		private string _urlPattern;
		public string urlPattern
		{
			get
			{
				return _urlPattern;
			}
			set
			{
				if (_urlPattern == value)
				{
					return;
				}
				_urlPattern = value;

				string escapedPattern = _urlPattern;
				if (escapedPattern.EndsWith("/"))
				{
					escapedPattern = escapedPattern.Substring(0, escapedPattern.Length-1);
				}
				escapedPattern = Regex.Escape(escapedPattern);

				regexPattern = Regex.Replace(escapedPattern, "<([a-zA-Z0-9_]*)>", (m) => {
					string groupName = m.Groups[1].Value;
					return $"(?<{groupName}>.+)";
				});
				regexPattern = Regex.Replace(regexPattern, "\\\\\\{([a-zA-Z0-9_]*)\\}", (m) => {
					string groupName = m.Groups[1].Value;
					return $"(?<{groupName}>[^/]+)";
				});
				
				regexPattern = $"^{regexPattern}/*";
			}
		}
		private string regexPattern;


		public delegate void OnRouteVariableExceptionDelegate(RouteVariableValidationException exception);
		public OnRouteVariableExceptionDelegate onRouteVariableException { get; set; }

		public delegate void OnExecuteDelegate(RequestContext context);
		public OnExecuteDelegate onExecute;

		public ObservableCollection<Router> subRouters { get; private set; } = new ObservableCollection<Router>();

		

		public Router(string urlPattern)
		{
			this.urlPattern = urlPattern;
			subRouters.CollectionChanged += OnSubRoutersChanged;
		}

		private void OnSubRoutersChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (Router item in e.NewItems)
				{
					item.parent = this;
				}
			}
			if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset)
			{
				foreach (Router item in e.OldItems)
				{
					item.parent = null;
				}
				if (e.Action == NotifyCollectionChangedAction.Replace)
				{
					foreach (Router item in e.NewItems)
					{
						item.parent = this;
					}
				}
			}
		}

		public bool Evaluate(RequestContext context)
		{
			Regex regex = new Regex(regexPattern, RegexOptions.Singleline);
			Match match = regex.Match(context.remainingPath);

			if (match.Success)
			{
				string[] groupNames = regex.GetGroupNames();
				if (match.Groups.Count != groupNames.Length)
				{
					throw new Exception("Unexpected group count.");
				}
				for (int i = 0; i < match.Groups.Count; i++)
				{
					string groupName = groupNames[i];
					if (int.TryParse(groupName, out int _))
					{
						continue;
					}
					Group group = match.Groups[i];
					
					context.SetRouteVariable(groupName, group.Value);
				}

				bool isFullMatch = match.Length == context.remainingPath.Length;
				string remainingPath = context.remainingPath.Substring(match.Length);
				if (!isFullMatch)
				{
					if (remainingPath == "/")
					{
						remainingPath = "";
						isFullMatch = true;
					}
				}

				if (!isFullMatch)
				{
					
					foreach (var subRouter in subRouters)
					{
						string previousRemainingPath = context.remainingPath;
						context.remainingPath = remainingPath;
						bool result = subRouter.Evaluate(context);
						context.remainingPath = previousRemainingPath;
						
						if (result)
						{
							return true;
						}
					}
					return false;
				}
				else
				{
					try
					{
						if (!TryExecute(context))
						{
							return false;
						}
					}
					catch (RouteVariableValidationException exception)
					{
						if (onRouteVariableException != null)
						{
							onRouteVariableException?.Invoke(exception);
						}
						else
						{
							Console.WriteLine(exception);
						}
					}
					
					return true;
				}
			}

			return false;
		}

		private bool TryExecute(RequestContext context)
		{
			Router currentRouter = this;
			OnRouteVariableExceptionDelegate onRouteVariableException = null;
			while (currentRouter != null && onRouteVariableException == null)
			{
				onRouteVariableException = currentRouter.onRouteVariableException;
				currentRouter = currentRouter.parent;
			}

			bool resultState = true;
			Router previousRouter = context.currentRouter;
			context.currentRouter = this;
			try
			{
				if (OnValidate(context))
				{
					onExecute?.Invoke(context);
					OnExecute(context);
				}
				else
				{
					resultState = false;
				}
			}
			catch (RouteVariableValidationException exception)
			{
				if (onRouteVariableException != null)
				{
					onRouteVariableException?.Invoke(exception);
				}
				else
				{
					Console.WriteLine(exception);
				}
			}
			context.currentRouter = previousRouter;

			return resultState;
		}

		public virtual bool OnValidate(RequestContext context)
		{
			return true;
		}

		protected virtual void OnExecute(RequestContext context)
		{
			
		}
	}
}
