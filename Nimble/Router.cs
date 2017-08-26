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
	public class RouterList : List<Router>
	{
		public new void Add(Router router)
		{
			Console.WriteLine("ADD");
			base.Add(router);
		}
	}

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

				string escapedPattern = Regex.Escape(_urlPattern);

				groupNames.Clear();
				regexPattern = Regex.Replace(escapedPattern, "\\\\{([a-zA-Z0-9_]*)\\}", (m) => {
					string groupName = m.Groups[1].Value;
					groupNames.Add(groupName);
					return $"(?<{groupName}>[^/]+)";
				});
				regexPattern = $"^{regexPattern}/?";
			}
		}
		private string regexPattern;
		private List<string> groupNames = new List<string>();



		public delegate void OnRouteVariableValidationExceptionDelegate(RouteVariableValidationException exception);
		public OnRouteVariableValidationExceptionDelegate onRouteVariableValidationException { get; set; }

		public delegate void OnExecuteDelegate(RequestContext context);
		public OnExecuteDelegate onExecute;

		public ObservableCollection<Router> subRouters { get; private set; } = new ObservableCollection<Router>();

		

		public Router(string urlPattern) : this(urlPattern, null)
		{
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

		public Router(string urlPattern, OnExecuteDelegate onExecute)
		{
			this.urlPattern = urlPattern;
			this.onExecute = onExecute;
		}

		public bool Evaluate(RequestContext context, string path)
		{
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

				bool isFullMatch = match.Length == path.Length;

				if (!isFullMatch)
				{
					string remainingPath = path.Substring(match.Length);

					foreach (var subRouter in subRouters)
					{
						bool result = subRouter.Evaluate(context, remainingPath);
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
						Execute(context);
					}
					catch (RouteVariableValidationException exception)
					{
						if (onRouteVariableValidationException != null)
						{
							onRouteVariableValidationException?.Invoke(exception);
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

		public void Execute(RequestContext context)
		{
			Router currentRouter = this;
			OnRouteVariableValidationExceptionDelegate onRouteVariableValidationException = null;
			while (currentRouter != null && onRouteVariableValidationException == null)
			{
				onRouteVariableValidationException = currentRouter.onRouteVariableValidationException;
				currentRouter = currentRouter.parent;
			}

			Router previousRouter = context.currentRouter;
			context.currentRouter = this;
			try
			{
				onExecute?.Invoke(context);
			}
			catch (RouteVariableValidationException exception)
			{
				if (onRouteVariableValidationException != null)
				{
					onRouteVariableValidationException?.Invoke(exception);
				}
				else
				{
					Console.WriteLine(exception);
				}
			}
			context.currentRouter = previousRouter;
		}
	}
}
