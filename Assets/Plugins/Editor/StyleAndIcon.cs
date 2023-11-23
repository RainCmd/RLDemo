using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Plugins
{
    internal class EditorStyleAndIconViewer : SearchableEditorWindow
    {
        private class Skin
        {
            public class StyleInfo
            {
                public readonly Skin skin;
                public readonly GUIStyle style;
                private readonly GUIContent simpleness;
                private readonly GUIContent complex;
                public StyleInfo(Skin skin, GUIStyle style)
                {
                    this.skin = skin;
                    this.style = style;
                    simpleness = new GUIContent("", style.name);
                    complex = new GUIContent("", skin.name + ":" + style.name);
                }
                public GUIContent GetContent(bool simpleness)
                {
                    if (simpleness) return this.simpleness;
                    else return complex;
                }
            }
            private readonly List<StyleInfo> styleInfos = new List<StyleInfo>();
            public readonly GUISkin skin;
            public readonly string name;
            public Skin(GUISkin skin, string name)
            {
                this.skin = skin;
                this.name = name;
                foreach (GUIStyle style in skin)
                {
                    styleInfos.Add(new StyleInfo(this, style));
                }
            }
            public StyleInfo this[int index]
            {
                get
                {
                    return styleInfos[index];
                }
            }
            public int Count { get { return styleInfos.Count; } }
        }
        [MenuItem("Window/风格和图标")]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorStyleAndIconViewer>();
            var title = new GUIContent(EditorGUIUtility.IconContent("BuildSettings.Tizen.small"));
            title.text = "风格和图标";
            window.titleContent = title;
        }
        private static Style style_Search = new Style("ToolbarSeachTextField");
        private static Style style_SeachCancelButton = new Style("ToolbarSeachCancelButton");
        private static Style style_SeachCancelButtonEmpty = new Style("ToolbarSeachCancelButtonEmpty");
        private bool loaded = false;
        private List<Skin> Skins = new List<Skin>();
        private List<GUIContent> Icons = new List<GUIContent>();
        private void LoadIconList()
        {
            var textures = EditorAssetBundle.LoadAllAssets<Texture>();
            for (int i = 0; i < textures.Length; i++)
            {
                Icons.Add(new GUIContent(EditorGUIUtility.IconContent(textures[i].name)) { tooltip = textures[i].name });
                EditorUtility.DisplayProgressBar("风格和图标", string.Format("正在加载图标资源...{0}/{1}", i, textures.Length), i / (float)textures.Length);
            }
            EditorUtility.ClearProgressBar();
        }
        private void LoadGUISkinStyle()
        {
            var names = EditorAssetBundle.GetAllAssetNames();
            var sc = EditorAssetBundle.LoadAllAssets<GUISkin>().Length;
            for (int i = 0, si = 0; i < names.Length; i++)
            {
                var skin = EditorAssetBundle.LoadAsset(names[i]) as GUISkin;
                if (skin)
                {
                    EditorUtility.DisplayProgressBar("风格和图标", string.Format("正在加载皮肤资源...{0}/{1}", si, sc), si / (float)sc);
                    Skins.Add(new Skin(skin, names[i]));
                }
            }
            EditorUtility.ClearProgressBar();
        }
        private void Load()
        {
            if (!loaded)
            {
                loaded = true;
                LoadGUISkinStyle();
                LoadIconList();
                UpdateDisplays();
            }
        }

        private Vector2 scrollPosition = Vector2.zero;
        private string search = string.Empty;
        private int selectType = 0;
        private int showType = 2;
        private float itemScale = 1;
        private Vector2 itemSize = new Vector2(32, 32);
        private bool onlyCurrentSkin = false;
        public override void OnEnable()
        {
            base.OnEnable();
            loaded = false;
        }
        private void ShowButton(Rect rect)
        {
            if (selectType == 0)
            {
                rect.x -= 60;
                rect.width += 60;
                if (onlyCurrentSkin != GUI.Toggle(rect, onlyCurrentSkin, new GUIContent("仅当前皮肤", "勾选后仅显示GUI.skin中的样式，并且样式名可直接转换为GUIStyle,否则需要通过BlueSky.EditorTools.GetStyle(string)获取对应样式")))
                {
                    onlyCurrentSkin = !onlyCurrentSkin;
                    UpdateDisplayStyles();
                }
            }
            else
            {
                rect.x -= 48;
                rect.width += 48;
                if (GUI.Button(rect, "导出PNG"))
                {
                    var path = EditorUtility.OpenFolderPanel("导出PNG", "", "");
                    if (!string.IsNullOrEmpty(path))
                    {
                        var mat = new Material(Shader.Find("UI/Default"));
                        var prev = RenderTexture.active;
                        try
                        {
                            var i = 0f;
                            foreach (var item in displayIcons)
                            {
                                EditorUtility.DisplayProgressBar("导出PNG", item.tooltip, i++ / displayIcons.Count);
                                var input = item.image as Texture2D;
                                if (input)
                                {
                                    GL.Clear(true, true, Color.clear);
                                    var tmp = RenderTexture.GetTemporary(input.width, input.height, 0, RenderTextureFormat.ARGB32);
                                    Graphics.Blit(input, tmp, mat);
                                    RenderTexture.active = tmp;

                                    var png = new Texture2D(tmp.width, tmp.height, TextureFormat.ARGB32, false);
                                    png.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                                    var buffer = png.EncodeToPNG();
                                    using (var sw = File.Create(path + "/" + item.tooltip + ".png"))
                                        sw.Write(buffer, 0, buffer.Length);
                                    DestroyImmediate(png);

                                    RenderTexture.ReleaseTemporary(tmp);
                                }
                            }
                        }
                        finally
                        {
                            RenderTexture.active = prev;
                            DestroyImmediate(mat);
                            EditorUtility.ClearProgressBar();
                        }
                    }
                }
            }
        }
        private void OnGUI()
        {
            Load();
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            showType = EditorGUILayout.Popup(showType, new string[] { "显示名称和内容", "只显示名称", "只显示内容" }, GUILayout.Width(85));
            var sv = EditorGUILayout.TextField(search, style_Search);
            if (sv != search)
            {
                search = sv;
                UpdateDisplays();
            }
            if (GUILayout.Button("", string.IsNullOrEmpty(search) ? style_SeachCancelButtonEmpty : style_SeachCancelButton))
            {
                search = "";
                EditorGUI.FocusTextInControl("");
                UpdateDisplays();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            selectType = GUILayout.Toolbar(selectType, new string[] { "GUI样式", "内置图标" });
            if (showType == 2)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("按住ctrl点击，可以复制名称");
                GUILayout.Label(EditorGUIUtility.IconContent("ScaleTool On"), GUILayout.Width(24));
                itemScale = EditorGUILayout.Slider(itemScale, 0.2f, 5);
                itemSize = new Vector2(32, 32) * itemScale;
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            switch (selectType)
            {
                case 0:
                    {
                        switch (showType)
                        {
                            case 0: DrawStyles(); break;
                            case 1: DrawStyles_OnlyName(); break;
                            case 2: DrawStyles_NoName(); break;
                        }
                    }
                    break;
                case 1:
                    {
                        switch (showType)
                        {
                            case 0: DrawIcons(); break;
                            case 1: DrawIcons_OnlyName(); break;
                            case 2: DrawIcons_NoName(); break;
                        }
                    }
                    break;
            }
        }
        private void UpdateDisplays()
        {
            UpdateDisplayStyles();
            UpdateDisplayIcons();
        }
        #region STYLE
        private List<Skin.StyleInfo> displayStyles = new List<Skin.StyleInfo>();
        private void UpdateDisplayStyles()
        {
            var search = this.search.ToUpper();
            displayStyles.Clear();
            if (string.IsNullOrEmpty(search))
            {
                for (int i = 0; i < Skins.Count; i++)
                {
                    if (!onlyCurrentSkin || Skins[i].skin == GUI.skin)
                    {
                        for (int index = 0; index < Skins[i].Count; index++)
                        {
                            displayStyles.Add(Skins[i][index]);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < Skins.Count; i++)
                {
                    if (!onlyCurrentSkin || Skins[i].skin == GUI.skin)
                    {
                        for (int index = 0; index < Skins[i].Count; index++)
                        {
                            if (Skins[i][index].GetContent(onlyCurrentSkin).tooltip.ToUpper().Contains(search))
                            {
                                displayStyles.Add(Skins[i][index]);
                            }
                        }
                    }
                }
            }
        }
        private void DrawStyles()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < displayStyles.Count; i++)
            {
                GUILayout.BeginHorizontal("TextArea");
                EditorGUILayout.TextField(displayStyles[i].GetContent(onlyCurrentSkin).tooltip, EditorStyles.label);
                EditorGUILayout.Space();
                GUILayout.Button(displayStyles[i].GetContent(onlyCurrentSkin).tooltip, displayStyles[i].style);
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        private void DrawStyles_NoName()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            int hc = (int)(position.width / itemSize.x);
            int vc = displayStyles.Count / hc + 1;
            GUILayoutUtility.GetRect(hc * itemSize.x, vc * itemSize.y);
            int index = 0;
            for (int y = 0; y < vc; y++)
            {
                for (int x = 0; x < hc; x++)
                {
                    if (index < displayStyles.Count)
                    {
                        Rect r = new Rect(x * itemSize.x, y * itemSize.y, itemSize.x, itemSize.y);
                        var info = displayStyles[index];
                        if (Event.current.control && r.Contains(Event.current.mousePosition))
                        {
                            EditorGUI.DrawRect(r, new Color(.25f, .25f, .5f, .75f));
                            Repaint();
                        }
                        if (GUI.Button(r, info.GetContent(onlyCurrentSkin), info.style) && Event.current.control)
                        {
                            EditorGUIUtility.systemCopyBuffer = info.GetContent(onlyCurrentSkin).tooltip;
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        break;
                    }
                    index++;
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private void DrawStyles_OnlyName()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < displayStyles.Count; i++)
            {
                EditorGUILayout.TextField(displayStyles[i].GetContent(onlyCurrentSkin).tooltip, EditorStyles.label);
            }
            EditorGUILayout.EndScrollView();
        }
        #endregion STYLE
        #region CONTENT
        private List<GUIContent> displayIcons = new List<GUIContent>();
        private void UpdateDisplayIcons()
        {
            var search = this.search.ToUpper();
            displayIcons.Clear();
            if (string.IsNullOrEmpty(search))
            {
                for (int i = 0; i < Icons.Count; i++)
                {
                    displayIcons.Add(Icons[i]);
                }
            }
            else
            {
                for (int i = 0; i < Icons.Count; i++)
                {
                    if (Icons[i].tooltip.ToUpper().Contains(search))
                    {
                        displayIcons.Add(Icons[i]);
                    }
                }
            }
        }
        private void DrawIcons()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < displayIcons.Count; i++)
            {
                GUILayout.BeginHorizontal("TextArea");
                EditorGUILayout.TextField(displayIcons[i].tooltip, EditorStyles.label);
                EditorGUILayout.Space();
                GUILayout.Label(displayIcons[i]);
                GUILayout.Space(10);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
        private void DrawIcons_NoName()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            int hc = (int)(position.width / itemSize.x);
            int vc = displayIcons.Count / hc + 1;
            GUILayoutUtility.GetRect(hc * itemSize.x, vc * itemSize.y);
            int index = 0;
            for (int y = 0; y < vc; y++)
            {
                for (int x = 0; x < hc; x++)
                {
                    if (index < displayIcons.Count)
                    {
                        Rect r = new Rect(x * itemSize.x, y * itemSize.y, itemSize.x, itemSize.y);
                        if (Event.current.control && r.Contains(Event.current.mousePosition))
                        {
                            EditorGUI.DrawRect(r, new Color(.25f, .25f, .5f, .75f));
                            Repaint();
                        }
                        GUI.Label(r, displayIcons[index]);
                        if (Event.current.control && Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                        {
                            EditorGUIUtility.systemCopyBuffer = displayIcons[index].tooltip;
                            Event.current.Use();
                        }
                    }
                    else
                    {
                        break;
                    }
                    index++;
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private void DrawIcons_OnlyName()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            for (int i = 0; i < displayIcons.Count; i++)
            {
                EditorGUILayout.TextField(displayIcons[i].tooltip, EditorStyles.label);
            }
            GUILayout.EndScrollView();
        }
        #endregion CONTENT

        private static readonly Dictionary<string, GUISkin> editorSkins = new Dictionary<string, GUISkin>();
        /// <summary>
        /// 获取unity内部资源样式
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static GUIStyle GetStyle(string name)
        {
            if (name.IndexOf(':') < 0)
            {
                return name;
            }
            else
            {
                var s = name.Split(':');
                GUISkin skin;
                if (!editorSkins.TryGetValue(s[0], out skin))
                {
                    editorSkins[s[0]] = skin = EditorAssetBundle.LoadAsset<GUISkin>(s[0]);
                }
                if (skin)
                {
                    return skin.GetStyle(s[1]);
                }
                else
                {
                    UnityEngine.Debug.Log("没有找到皮肤：" + name);
                    return null;
                }
            }
        }
        private static AssetBundle editorAssetBundle;
        /// <summary>
        /// 编辑器内部资源
        /// </summary>
        public static AssetBundle EditorAssetBundle
        {
            get
            {
                if (editorAssetBundle == null)
                {
                    editorAssetBundle = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[0]) as AssetBundle;
                }
                return editorAssetBundle;
            }
        }
    }
}
