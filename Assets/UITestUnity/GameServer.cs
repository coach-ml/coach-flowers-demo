using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Xamarin.GameTestServer
{
    public static class Utils
    {
        public static HttpMethod ConvertMethod(string method)
        {
            switch (method.ToUpper())
            {
                case "GET":
                    return HttpMethod.Get;
                case "POST":
                    return HttpMethod.Post;
                case "PUT":
                    return HttpMethod.Put;
                case "DELETE":
                    return HttpMethod.Delete;
            }

            throw new Exception("Could not parse method");
        }
    }

	/// <summary>
	/// The webserver itself
	/// </summary>
	public class GameServer
	{
        private readonly int PORT = 8081;
		static GameServer shared;

		public static GameServer Shared {
			get {
				return shared ?? (shared = new GameServer ());
			}
			set {
				shared = value;
			}
		}

		private readonly HttpListener _listener = new HttpListener ();

		public GameServer ()
		{
			if (!HttpListener.IsSupported)
				throw new NotSupportedException ("Http Listener is not supported");

			var prefixes = new [] {
                $"http://*:{PORT}/",
            };

			foreach (string s in prefixes)
				_listener.Prefixes.Add (s);
            
			_listener.Start ();
		}

		/// <summary>
		/// Starts the server.
		/// </summary>
		public void Start ()
		{
			Loom.Init ();
			_listener.Start ();
			ThreadPool.QueueUserWorkItem ((o) =>
			{
				Debug.Log ("Webserver running 1...");
                
				try {
					while (_listener.IsListening) {
						ThreadPool.QueueUserWorkItem ((c) =>
						{
							var ctx = c as HttpListenerContext;
							try {
								byte[] buf = ProcessReponse (ctx);
								if (buf != null) {
									ctx.Response.ContentLength64 = buf.Length;
									ctx.Response.OutputStream.Write (buf, 0, buf.Length);
								}
							} catch (Exception e) {
								Console.WriteLine (e);
								ctx.Response.StatusCode = 500;
							} finally {
								// always close the stream
								ctx.Response.OutputStream.Close ();
							}
						}, _listener.GetContext ());
					}
				} catch (Exception ex) {
					Console.WriteLine (ex);
				}
			});
		}

		byte[] ProcessReponse (HttpListenerContext context)
		{
			var request = context.Request;
			try {
				Debug.Log("Raw URL:" + request.RawUrl);
				var path = string.IsNullOrEmpty(request.Url.Query) ? request.RawUrl : request.RawUrl.Replace(request.Url.Query,"");
				if (path == "/")
					return new byte[0];

				path = path.TrimStart ('/');
				Debug.Log (string.Format("Path: {0}", path));
				var route = Router.GetRoute (path);
				if (!route.SupportsMethod (Utils.ConvertMethod(context.Request.HttpMethod))) {
                    context.Response.StatusCode = 405;
                    var error = "Unsupported method: " + context.Response.StatusCode;
                    Debug.LogError(error);
					return Encoding.ASCII.GetBytes(error);
                }

				context.Response.ContentType = route.ContentType;
				return route.GetResponseBytes (request);
			} catch (Exception ex) {
				Debug.LogError(ex);
				context.Response.StatusCode = 404;
                return Encoding.ASCII.GetBytes(ex.Message);
			}
		}

	}
}
