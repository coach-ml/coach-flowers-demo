using System;
using System.Drawing;

namespace Xamarin.GameTestServer
{
	public class GameElement
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
        public string Parent { get; set; }
        public string[] Children { get; set; }

        public PointF Location { get; set; }
		public RectangleF Rectangle { get; set;}
		public bool? IsOnScreen { get; set; }

        public static T InitFrom<T>(GameElement elm) where T : GameElement
        {
            var t = Activator.CreateInstance<T>();
            t.Name = elm.Name;
            t.Id = elm.Id;
            t.Type = elm.Type;
            t.Parent = elm.Parent;
            t.Children = elm.Children;
            t.Location = elm.Location;
            t.Rectangle = elm.Rectangle;
            t.IsOnScreen = elm.IsOnScreen;

            return t;
        }
    }
}

