using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class CompileRainScripts
{
    private class CodeFile : BuildParameter.ICodeFile
    {
        public string Path { get; private set; }
        public string Content
        {
            get
            {
                using (var sr = new StreamReader(Path))
                    return sr.ReadToEnd();
            }
        }
        public CodeFile(string path)
        {
            Path = path;
        }
    }
    private static byte[] LoadLibrary(string name)
    {
        var asset = Resources.Load<TextAsset>("RainLibraries/" + name + ".lib");
        if (asset) return asset.bytes;
        return new byte[0];
    }
    private static string scriptsPath = Application.dataPath + "/Scripts/Logic/RainScripts/";
    private static string libraryPath = Application.dataPath + "/Resources/RainLibraries/" + Config.GameName + ".lib.bytes";
    private static string pdbPath = Application.dataPath + "/RainProgramDatabase/" + Config.GameName + ".pdb.bytes";
    private static void LoadCodeFiles(string path, List<BuildParameter.ICodeFile> files)
    {
        foreach (var file in Directory.GetFiles(path, "*.rain")) files.Add(new CodeFile(file));
        foreach (var dir in Directory.GetDirectories(path)) LoadCodeFiles(dir, files);
    }
    private static void LogErrMsg(RainLanguageAdapter.ErrorMessage msg, System.Action<object, Object> action)
    {
        var code = AssetDatabase.LoadAssetAtPath<Object>(msg.Path.Replace(Application.dataPath, "Assets/"));
        var detail = msg.Detail;
        var detailMsg = string.Format("{0} line:<color=#ffcc00>{1}</color> [{2}, {3}]\n错误码:{4}", msg.Path, detail.line, detail.start, detail.start + detail.length, detail.messageType);
        var emsg = msg.ExteraMsg;
        if (!string.IsNullOrEmpty(emsg)) detailMsg += "\n" + emsg;
        action(detailMsg, code);
    }
    private static unsafe void SaveData(RainLanguageAdapter.RainBuffer rb, string path)
    {
        var buffer = new byte[rb.Length];
        var tmp = rb.Data;
        for (int i = 0; i < buffer.Length; i++) buffer[i] = tmp[i];
        using (var fs = File.Create(path))
            fs.Write(buffer, 0, buffer.Length);
        AssetDatabase.Refresh();
    }
    [MenuItem("雨言/编译")]
    private static unsafe void Compile()
    {
        var files = new List<BuildParameter.ICodeFile>();
        LoadCodeFiles(scriptsPath, files);
        var sw = new Stopwatch();
        using (var product = RainLanguageAdapter.BuildProduct(new BuildParameter(Config.GameName, true, files, LoadLibrary, RainErrorLevel.LoggerLevel4)))
        {
            sw.Stop();
            for (var lvl = 0; lvl < 9; lvl++)
                for (uint i = 0, cnt = product.GetErrorCount((RainErrorLevel)lvl); i < cnt; i++)
                    using (var msg = product.GetErrorMessage((RainErrorLevel)lvl, i))
                    {
                        if (lvl == 0) LogErrMsg(msg, Debug.LogError);
                        else if (lvl <= (int)RainErrorLevel.WarringLevel4) LogErrMsg(msg, Debug.LogWarning);
                        else LogErrMsg(msg, Debug.Log);
                    }
            if (product.GetErrorCount(RainErrorLevel.Error) > 0)
            {
                Debug.LogFormat("<color=#ff0000>雨言编译失败</color>，耗时<color=#ffcc00>{0}</color>ms", sw.ElapsedMilliseconds);
                return;
            }
            using (var rb = product.GetLibrary().Serialize()) SaveData(rb, libraryPath);
            using (var rb = product.GetProgramDatabase().Serialize()) SaveData(rb, pdbPath);

            Debug.LogFormat(Resources.Load<TextAsset>("RainLibraries/" + Config.GameName + ".lib"), "<color=#00ff00>雨言编译成功</color>，耗时<color=#ffcc00>{0}</color>ms", sw.ElapsedMilliseconds);
        }
    }
}
