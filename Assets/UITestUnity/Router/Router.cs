using System.Collections.Generic;

namespace Xamarin.GameTestServer
{
	public static class Router
	{
		static Router()
		{
			UnityRoutes.Enable ();
		}

		static Dictionary<string,Route> routes = new Dictionary<string, Route>();
		public static void AddRoute(string path,Route route)
		{
			routes [path.ToLower()] = route;
		}

		public static Route GetRoute (string path)
		{
			Route route;
			routes.TryGetValue (path.ToLower(), out route);
			return route;
		}
	}
}

