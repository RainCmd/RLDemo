using UnityEngine;

namespace Plugins
{
    /// <summary>
    /// <see cref="UnityEngine.GUIStyle"/>封装
    /// <para>可以在声明字段时直接设置style名称，在实际调用时再创建<see cref="UnityEngine.GUIStyle"/></para>
    /// </summary>
    public class Style
    {
        /// <summary>
        /// style名称
        /// </summary>
        public readonly string name;
        private bool _alignment = false;
        private TextAnchor alignment;
        private bool _fontSize = false;
        private int fontSize;
        private GUIStyle style;
        /// <summary>
        /// GUIStyle
        /// </summary>
        public GUIStyle GUIStyle
        {
            get
            {
                if (style == null)
                {
                    if (name.IndexOf(':') < 0)
                    {
                        style = new GUIStyle(name);
                    }
                    else
                    {
                        style = new GUIStyle(EditorStyleAndIconViewer.GetStyle(name));
                    }
                    if (_alignment) style.alignment = alignment;
                    if (_fontSize) style.fontSize = fontSize;
                }
                return style;
            }
        }
        /// <summary>
        /// 创建style
        /// </summary>
        /// <param name="name">style名</param>
        public Style(string name)
        {
            this.name = name;
        }
        /// <summary>
        /// 创建style
        /// </summary>
        /// <param name="name">style名</param>
        /// <param name="alignment">文字对齐</param>
        public Style(string name, TextAnchor alignment)
        {
            this.name = name;
            this.alignment = alignment;
            _alignment = true;
        }
        /// <summary>
        /// 创建style
        /// </summary>
        /// <param name="name">style名</param>
        /// <param name="fontSize">字体大小</param>
        public Style(string name, int fontSize)
        {
            this.name = name;
            this.fontSize = fontSize;
            _fontSize = true;
        }
        /// <summary>
        /// 创建style
        /// </summary>
        /// <param name="name">style名</param>
        /// <param name="alignment">文字对齐</param>
        /// <param name="fontSize">字体大小</param>
        public Style(string name, TextAnchor alignment, int fontSize)
        {
            this.name = name;
            this.alignment = alignment;
            _alignment = true;
            this.fontSize = fontSize;
            _fontSize = true;
        }
        /// <summary>
        /// 绘制style
        /// </summary>
        /// <param name="position"></param>
        public void Draw(Rect position)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle.Draw(position, false, false, false, false);
            }
        }
        /// <summary>
        /// 绘制style
        /// </summary>
        /// <param name="position"></param>
        /// <param name="text"></param>
        /// <param name="isHover"></param>
        /// <param name="isActive"></param>
        /// <param name="on"></param>
        /// <param name="hasKeyboardFocus"></param>
        public void Draw(Rect position, string text, bool isHover = false, bool isActive = false, bool on = false, bool hasKeyboardFocus = false)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle.Draw(position, text, isHover, isActive, on, hasKeyboardFocus);
            }
        }
        /// <summary>
        /// GUIStyle的默认转换
        /// </summary>
        /// <param name="style"></param>
        public static implicit operator GUIStyle(Style style)
        {
            if (style == null) return GUIStyle.none;
            return style.GUIStyle;
        }
    }
}
