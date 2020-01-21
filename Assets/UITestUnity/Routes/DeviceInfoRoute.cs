using UnityEngine;
using System.Collections.Specialized;
using System.Net.Http;

namespace Xamarin.GameTestServer.Unity
{
    public class DeviceInfo
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public float DPI { get; set; }
    }

    /// <summary>
    /// Gets basic device info
    /// </summary>
    public class DeviceInfoRoute : Route<DeviceInfo>
    {
        public static readonly string RoutePath = "DeviceInfo";
        public static void Enable()
        {
            Router.AddRoute(RoutePath, new DeviceInfoRoute());
        }

        public override bool SupportsMethod(HttpMethod method)
        {
            return method == HttpMethod.Get;
        }
        
        public override DeviceInfo GetResponse(HttpMethod method, NameValueCollection queryString, string data)
        {
            var device = new DeviceInfo();
            RunInUIThread(() =>
            {
                device.Height = Screen.height;
                device.Width = Screen.width;
                device.DPI = Screen.dpi;
            });
            
            return device;
        }
    }
}
