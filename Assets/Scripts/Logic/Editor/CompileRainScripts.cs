using RainLanguage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
    private static readonly string dataPath = Application.dataPath;
    private static readonly string scriptsPath = dataPath + "/Scripts/Logic/RainScripts/";
    private static readonly string libraryPath = dataPath + "/Resources/RainLibraries/" + Config.GameName + ".lib.bytes";
    private static readonly string pdbPath = dataPath + "/RainProgramDatabase/" + Config.GameName + ".pdb.bytes";
    private struct LogMsg
    {
        public Action<object, Object> action;
        public string path;
        public string msg;
        public LogMsg(Action<object, Object> action, string path, string msg)
        {
            this.action = action;
            this.path = path;
            this.msg = msg;
        }
    }
    private static readonly Queue<LogMsg> logMsgs = new Queue<LogMsg>();
    private static void EnqueueLogMsg(Action<object, Object> action, string path, string msg)
    {
        lock (logMsgs) logMsgs.Enqueue(new LogMsg(action, path, msg));
    }
    private static void LoadCodeFiles(string path, List<BuildParameter.ICodeFile> files)
    {
        foreach (var file in Directory.GetFiles(path, "*.rain")) files.Add(new CodeFile(file));
        foreach (var dir in Directory.GetDirectories(path)) LoadCodeFiles(dir, files);
    }
    private static void LogErrMsg(RainLanguageAdapter.ErrorMessage msg, Action<object, Object> action, string msgColor)
    {
        var path = msg.Path.Replace(dataPath, "Assets");
        var assetPath = path;
        var detail = msg.Detail;
        var fidx = path.LastIndexOfAny(new char[] { '\\', '/' });
        var sidx = path.LastIndexOf('.');
        path = $"{path.Substring(0, fidx + 1)}<color=#00ccff>{path.Substring(fidx + 1, sidx - fidx - 1)}</color>{path.Substring(sidx)}";
        var detailMsg = $"{path} line:<color=#ffcc00>{detail.line}</color> [{detail.start}, {detail.start + detail.length}]\n错误码:<color=#{msgColor}>{detail.messageType}</color>";
        var emsg = msg.ExteraMsg;
        if (!string.IsNullOrEmpty(emsg)) detailMsg += "\n" + emsg;
        EnqueueLogMsg(action, assetPath, detailMsg);
    }
    private static unsafe void SaveData(RainLanguageAdapter.RainBuffer rb, string path)
    {
        var buffer = new byte[rb.Length];
        var tmp = rb.Data;
        for (int i = 0; i < buffer.Length; i++) buffer[i] = tmp[i];
        using (var fs = File.Create(path))
            fs.Write(buffer, 0, buffer.Length);
    }
    private static string compileState;
    [MenuItem("雨言/编译")]
    private static unsafe void Compile()
    {
        var files = new List<BuildParameter.ICodeFile>();
        LoadCodeFiles(scriptsPath, files);
        var sw = new Stopwatch();
        compileState = "准备编译";
        var thread = new Thread(() =>
        {
            sw.Start();
            compileState = "编译中";
            using (var product = RainLanguageAdapter.BuildProduct(new BuildParameter(Config.GameName, true, files, LoadLibrary, RainErrorLevel.LoggerLevel4)))//不知道为啥，好像偶尔会死循环一样
            {
                compileState = "输出编译信息";
                sw.Stop();
                for (var lvl = 0; lvl < 9; lvl++)
                    for (uint i = 0, cnt = product.GetErrorCount((RainErrorLevel)lvl); i < cnt; i++)
                        using (var msg = product.GetErrorMessage((RainErrorLevel)lvl, i))
                        {
                            if (lvl == 0) LogErrMsg(msg, Debug.LogError, "ff0000");
                            else if (lvl <= (int)RainErrorLevel.WarringLevel4) LogErrMsg(msg, Debug.LogWarning, "ffcc00");
                            else LogErrMsg(msg, Debug.Log, "777777");
                        }
                compileState = "输出编译信息完成";
                if (product.GetErrorCount(RainErrorLevel.Error) > 0)
                    EnqueueLogMsg(Debug.LogError, "", $"<color=#ff0000>雨言编译失败</color>，耗时<color=#ffcc00>{sw.ElapsedMilliseconds}</color>ms");
                else
                {
                    compileState = "保存lib";
                    using (var rb = product.GetLibrary().Serialize()) SaveData(rb, libraryPath);
                    compileState = "保存pdb";
                    using (var rb = product.GetProgramDatabase().Serialize()) SaveData(rb, pdbPath);
                    EnqueueLogMsg(Debug.Log, $"Assets/Resources/RainLibraries/{Config.GameName}.lib.bytes", $"<color=#00ff00>雨言编译成功</color>，耗时<color=#ffcc00>{sw.ElapsedMilliseconds}</color>ms");
                }
            }
            compileState = "编译完成";
        });
        try
        {
            Action log = () =>
            {
                lock (logMsgs)
                    while (logMsgs.Count > 0)
                    {
                        var msg = logMsgs.Dequeue();
                        msg.action(msg.msg, AssetDatabase.LoadAssetAtPath<Object>(msg.path));
                    }
            };
            thread.Start();
            while (!EditorUtility.DisplayCancelableProgressBar("雨言编译中", $"编译状态:{compileState}\t耗时：{sw.ElapsedMilliseconds}ms", 0) && thread.IsAlive)
            {
                log();
                Thread.Sleep(10);
            }
            log();
            if (thread.IsAlive)
            {
                thread.Abort();
                Debug.Log("编译已取消");
            }
            else AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
