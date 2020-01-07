using System;
using UnityEngine;
using System.Collections.Generic;
using System.Net.Http;
using System.Collections.Specialized;

namespace Xamarin.GameTestServer.Unity
{
	public class GameObjectFind : Route<IEnumerable<GameElement>>
	{
        public static readonly string RoutePath = "GameObjectFind";

        public static void Enable()
		{
			Router.AddRoute(RoutePath, new GameObjectFind());
		}

		public override bool SupportsMethod (HttpMethod method) { return method == HttpMethod.Get; }

		public override IEnumerable<GameElement> GetResponse (HttpMethod method, NameValueCollection queryString, string data)
		{
			var typeName = queryString?.Get("type");
            var objectName = queryString?.Get("name");

            Type type = typeof(GameObject);
            if (!string.IsNullOrEmpty(typeName) && typeName != "GameObject")
            {
                type = Type.GetType(string.Format("UnityEngine.UI.{0},UnityEngine.UI", typeName));
            }

			var elements = new List<GameElement>();

            RunInUIThread(() =>
            {
                var objects = GameObject.FindObjectsOfType(type);
                foreach (var obj in objects)
                {
                    if (!string.IsNullOrEmpty(objectName))
                    {
                        if (obj.name == objectName)
                        {
                            var elem = obj.ToGameElement(type);
                            elements.Add(elem);
                        }
                    }
                    else
                    {
                        var elem = obj.ToGameElement(type);
                        elements.Add(elem);
                    }
                }
            });

            return elements;
		}
	}
}

