using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using UnityEngine;
using UnityEngine.UI;

namespace Xamarin.GameTestServer.Unity
{
    /// <summary>
    /// Invokes text input on a given element
    /// </summary>
    public class InvokeInputRoute : Route<GameInputField>
    {
        public static readonly string RoutePath = "InvokeInput";

        public static void Enable()
        {
            Router.AddRoute(RoutePath, new InvokeInputRoute());
        }

        public override bool SupportsMethod(HttpMethod method) { return method == HttpMethod.Post; }

        public override GameInputField GetResponse(HttpMethod method, NameValueCollection queryString, string data)
        {
            var objectName = queryString?.Get("name");
            var text = queryString?.Get("text");

            var inputElm = new GameInputField();
            RunInUIThread(() =>
            {
                var input = GameObject.FindObjectsOfType<InputField>().SingleOrDefault(b => b.name == objectName);
                if (input != null)
                {
                    input.text = text;
                    input.onEndEdit.Invoke("");
                    inputElm = input.ToGameElement<GameInputField>();
                }
                else
                {
                    // Fallback to TMP
                    var tmpInput = GameObject.FindObjectsOfType<TMPro.TMP_InputField>().SingleOrDefault(b => b.name == objectName);
                    tmpInput.text = text;
                    tmpInput.onEndEdit.Invoke("");
                    // No return
                }
            });

            return inputElm;
        }
    }
}
