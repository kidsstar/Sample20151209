using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;

namespace Sample.Editor {

    public class PostProcessBuild {

        [PostProcessBuild(200)]
        public static void OnPostProcessBuild(BuildTarget target, string path) {
            if (target != BuildTarget.iOS) {
                return;
            }

            // 適用する .plistmods ファイルを掻き集める
            List<string> files = new List<string>();
            if (Directory.Exists(System.IO.Path.Combine(Application.dataPath, "Sample/PlistMods"))) {
                files.AddRange(System.IO.Directory.GetFiles(System.IO.Path.Combine(Application.dataPath, "Sample/PlistMods"), "*.plistmods", System.IO.SearchOption.AllDirectories));
            }

            // .plistmods ファイルを適用する
            string plistPath = Path.Combine(path, "Info.plist");
            KidsStar.Editor.iOS.PlistMods plistMods = new KidsStar.Editor.iOS.PlistMods(plistPath);
            plistMods.Apply(files);
            plistMods.Save(plistPath);
        }

    }

}