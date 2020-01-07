using UnityEngine;
using UnityEngine.UI;
using System.Drawing;
using System;

namespace Xamarin.GameTestServer
{
    static partial class GameElementExtension
    {
        public static GameElement ToGameObject(this object obj)
        {
            var gobj = obj as GameObject;
            if (gobj != null)
            {
                return new GameElement
                {
                    Id = gobj.GetInstanceID().ToString(),
                    Name = gobj.name
                };
            }

            return null;
        }

        private static GameElement GetProperties(UnityEngine.Object go)
        {
            var elm = new GameElement
            {
                Name = go.name,
                Id = go.GetInstanceID().ToString(),
                Type = typeof(GameObject).ToString(),
                IsOnScreen = null
            };

            if (go.GetType().IsSubclassOf(typeof(Component)) || go.GetType() == typeof(Component))
            {
                var gc = (Component)go;
                var tf = gc.GetComponent<RectTransform>();
                if (tf != null)
                {
                    float x = tf.position.x;
                    float y = tf.position.y;
                    var parent = tf.parent?.name;

                    var children = new string[tf.childCount];
                    int i = 0;
                    foreach (Transform child in tf.transform)
                        children[i++] = child.name;

                    elm.Rectangle = new RectangleF(x, y, tf.rect.width, tf.rect.height);
                    elm.Location = new PointF(x, y);
                    elm.Parent = parent;
                    elm.Children = children;
                    elm.IsOnScreen = gc.gameObject.activeSelf;
                }
            }                

            return elm;
        }

        public static T ToGameElement<T>(this UnityEngine.Object obj) where T : GameElement
        {
            return (T)ToGameElement(obj, typeof(T));
        }

        public static GameElement ToGameElement(this UnityEngine.Object obj, Type type)
        {
            var go = GetProperties(obj);
            if (type == typeof(Text) || type == typeof(GameText))
            {
                var text = obj as Text;
                if (text != null)
                {
                    var gt = GameText.InitFrom<GameText>(go);
                    gt.Text = text.text;
                    gt.Type = text.GetType().ToString();

                    return gt;
                }
            }
            else if (type == typeof(Button) || type == typeof(GameButton))
            {
                var btn = obj as Button;
                if (btn != null)
                {
                    var txtComponent = btn.GetComponentInChildren<Text>();
                    string txt = (txtComponent == null) ? "" : txtComponent.text;

                    var gb = GameButton.InitFrom<GameButton>(go);
                    gb.Text = txt;
                    gb.Type = btn.GetType().ToString();

                    return gb;
                }
            }
            else if (type == typeof(InputField) || type == typeof(GameInputField))
            {
                var input = obj as InputField;
                if (input != null)
                {
                    var gi = GameInputField.InitFrom<GameInputField>(go);
                    gi.Text = input.text;
                    gi.Type = input.GetType().ToString();

                    return gi;
                }
            }
            else if (type == typeof(Image) || type == typeof(GameImage))
            {
                var image = obj as Image;
                if (image != null)
                {
                    var gi = GameImage.InitFrom<GameImage>(go);
                    gi.Color = ColorUtility.ToHtmlStringRGB(image.color);
                    gi.Type = image.GetType().ToString();

                    return gi;
                }
            }
            else
            {
                return go;
            }

            throw new Exception("Could not convert GameElement");
        }

        public static RectangleF ToRectangle(this Bounds rect)
        {
            return new RectangleF(rect.min.x, rect.min.y, rect.size.x, rect.size.y);
        }
        public static RectangleF ToRectangle(this Rect rect)
        {
            return new RectangleF(rect.x, rect.y, rect.width, rect.height);
        }
        public static PointF ToPoint(this Vector2 vector)
        {
            return new PointF(vector.x, vector.y);
        }
        public static PointF ToPoint(this Vector3 vector)
        {
            return new PointF(vector.x, vector.y);
        }
    }
}