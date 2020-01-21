using System;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using UnityEngine;
using System.Net.Http;
using System.Threading;

namespace Xamarin.GameTestServer
{
	public abstract class Route
	{
		public abstract bool SupportsMethod (HttpMethod method);

		public virtual string ContentType {
			get {
				return "application/json";
			}
		}
        
		public virtual string GetResponseString (HttpMethod method, NameValueCollection queryString, string data)
		{
			throw new Exception ("You need to provide either GetResponseString or GetResponseBytes");
		}

		public virtual string GetResponseString (HttpListenerRequest request)
		{
			return null;
		}

		public virtual byte[] GetResponseBytes (HttpListenerRequest request)
		{
			var responseString = GetResponseString (request);
			if (responseString != null) {
				return Encoding.UTF8.GetBytes (responseString);
			}

            var method = Utils.ConvertMethod(request.HttpMethod);

			string data;
			using (var reader = new StreamReader (request.InputStream))
				data = reader.ReadToEnd ();

			//Unity does not handle query strings properly. So we need to get it our self.
			var queryString = CreateQueryString(request.Url.Query);

			var responseData = GetResponseBytes(method, queryString, data);

			if (responseData != null)
				return responseData;

			responseString = GetResponseString (method, queryString,data);
			return Encoding.UTF8.GetBytes (responseString);
		}

		public virtual byte[] GetResponseBytes(HttpMethod method, NameValueCollection queryString, string data)
		{
			return null;
		}

		public static NameValueCollection CreateQueryString (string query)
		{
			if (query == null || query.Length == 0) {
				return new NameValueCollection (1);
			}
			
			var query_string = new NameValueCollection ();
			if (query [0] == '?')
				query = query.Substring (1);
			string [] components = query.Split ('&');
			foreach (string kv in components) {
				int pos = kv.IndexOf ('=');
				if (pos == -1) {
					query_string.Add (null, WWW.UnEscapeURL (kv));
				} else {
					string key =  WWW.UnEscapeURL(kv.Substring (0, pos));
					string val = WWW.UnEscapeURL (kv.Substring (pos + 1));
					
					query_string.Add (key, val);
				}
			}
			return query_string;
		}
        
        public void RunInUIThread(Action t)
        {
            bool completed = false;
            Loom.QueueOnMainThread(() => {
                try
                {
                    t.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
                finally
                {
                    completed = true;
                }
            });
            while (!completed)
            {
                Thread.Sleep(100);
            }
        }
    }
}

