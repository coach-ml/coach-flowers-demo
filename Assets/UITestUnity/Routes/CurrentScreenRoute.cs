using System.Net;
using System.Net.Http;

namespace Xamarin.GameTestServer.Unity
{
    /// <summary>
    /// Gets current screen
    /// </summary>
	public class CurrentScreenRoute : Route
	{
		public static string RoutePath = "CurrentScreen";
		public static void Enable()
		{
			Router.AddRoute (RoutePath, new CurrentScreenRoute ());
		}

		public override bool SupportsMethod (HttpMethod method)
		{
			return method == HttpMethod.Get;
		}

        public override string GetResponseString(HttpListenerRequest request)
        {
            string screenName = "";
            RunInUIThread(() =>
            {
                screenName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            });

            return screenName;
        }
	}
}

