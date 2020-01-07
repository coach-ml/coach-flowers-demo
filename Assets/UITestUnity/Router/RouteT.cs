using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net.Http;

namespace Xamarin.GameTestServer
{
	public abstract class Route<T> : Route
	{
		public abstract T GetResponse (HttpMethod method, NameValueCollection queryString, string data);

		public override string GetResponseString (HttpMethod method, NameValueCollection queryString, string data)
		{
            return JsonConvert.SerializeObject(GetResponse (method, queryString, data));
		}
	}
}