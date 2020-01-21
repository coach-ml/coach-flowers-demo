using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UI;

namespace Xamarin.GameTestServer.Unity
{
    /// <summary>
    /// Invokes click event on button
    /// </summary>
    public class InvokeButtonRoute : Route<GameButton>
    {
        public static readonly string RoutePath = "InvokeButton";

        public static void Enable()
        {
            Router.AddRoute(RoutePath, new InvokeButtonRoute());
        }

        public override bool SupportsMethod(HttpMethod method) { return method == HttpMethod.Post; }

        public override GameButton GetResponse(HttpMethod method, NameValueCollection queryString, string data)
        {
            var objectName = queryString?.Get("name");
            var buttonElm = new GameButton();
            RunInUIThread(() =>
            {
                var btn = GameObject.FindObjectsOfType<Button>().SingleOrDefault(b => b.name == objectName);
                btn.onClick.Invoke();
                buttonElm = btn.ToGameElement<GameButton>();
            });
            return buttonElm;
        }
    }
}
