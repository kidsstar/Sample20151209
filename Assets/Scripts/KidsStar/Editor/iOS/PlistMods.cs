
using UnityEngine;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MiniJSON;

namespace KidsStar.Editor.iOS {

    public class PlistMods {

        private XmlDocument plist;

        public PlistMods(string path) {
            this.plist = new XmlDocument();
            this.plist.Load(path);
        }

        public void Apply(string pathPlistMods) {
            IDictionary mods = (IDictionary)Json.Deserialize(File.ReadAllText(pathPlistMods));

            // 第1階層が dict ノードであることを前提としている
            IDictionary rootItem = (IDictionary)mods["value"];
            XmlNode rootNode = this.plist.SelectSingleNode("/plist/dict");
            foreach (object key in rootItem.Keys) {
                this.Apply(rootNode, (string)key, (IDictionary)rootItem[key]);
            }
        }

        public void Apply(string[] pathPlistModsList) {
            foreach (string pathPlistMods in pathPlistModsList) {
                this.Apply(pathPlistMods);
            }
        }

        public void Apply(List<string> pathPlistModsList) {
            this.Apply(pathPlistModsList.ToArray());
        }

        public void Save(string path) {
            XmlDocumentType _documentType = this.plist.CreateDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null);
            if (null != this.plist.DocumentType) {
                this.plist.RemoveChild(this.plist.DocumentType);
            }
            this.plist.InsertAfter(_documentType, this.plist.FirstChild);
            this.plist.Save(path);
        }

        private void Apply(XmlNode parent, string key, IDictionary item) {
            // キーがハイフンで始まる場合、削除とみなす
            if (!string.IsNullOrEmpty(key) && Regex.IsMatch(key, "^-")) {
                key = Regex.Replace(key, "^-", string.Empty);
                if (parent.HasKeyNode(key)) {
                    parent.RemoveChild(parent.GetKeyNode(key).NextSibling);
                    parent.RemoveChild(parent.GetKeyNode(key));
                }
                return;
            }

            if (null == item["type"] || null == item["value"]) {
                Debug.LogError("処理対象のノードに type か value が含まれていません");
                return;
            }
            switch ((string)item["type"]) {
                case "bool":
                    this.ApplyScalar(parent, key, (bool)item["value"] ? "true" : "false");
                    break;
                case "integer":
                    this.ApplyScalar(parent, key, "integer", (int)item["value"]);
                    break;
                case "real":
                    this.ApplyScalar(parent, key, "real", (float)item["value"]);
                    break;
                case "string":
                    this.ApplyScalar(parent, key, "string", (string)item["value"]);
                    break;
                case "date":
                    this.ApplyScalar(parent, key, "date", (string)item["value"]);
                    break;
                case "data":
                    this.ApplyScalar(parent, key, "data", (string)item["value"]);
                    break;
                case "array":
                    this.ApplyArray(parent, key, (IList)item["value"]);
                    break;
                case "dict":
                    this.ApplyDict(parent, key, (IDictionary)item["value"]);
                    break;
            }
        }

        private void ApplyScalar(XmlNode parent, string key, string type, object value = null) {
            // キーが空の場合、Array ノードへの挿入と見なす
            if (string.IsNullOrEmpty(key)) {
                if (null == value) {
                    parent.AppendChild(this.plist.CreateElement(type));
                } else {
                    parent.AppendChild(this.plist.CreateSimpleTextNode(type, value.ToString()));
                }
                return;
            }

            if (parent.HasKeyNode(key)) {
                parent.RemoveChild(parent.GetKeyNode(key).NextSibling);
            } else {
                parent.AppendChild(this.plist.CreateKeyNode(key));
            }
            if (null == value) {
                parent.InsertAfter(this.plist.CreateElement(type), parent.GetKeyNode(key));
            } else {
                parent.InsertAfter(this.plist.CreateSimpleTextNode(type, value.ToString()), parent.GetKeyNode(key));
            }
        }

        private void ApplyArray(XmlNode parent, string key, IList itemList) {
            // キーが空の場合、Array ノードへの挿入と見なす
            if (string.IsNullOrEmpty(key)) {
                foreach (object item in itemList) {
                    this.Apply(parent, null, (IDictionary)item);
                }
                return;
            }

            if (!parent.HasKeyNode(key)) {
                // キーがないなら、キーノードと array ノードを作る
                parent.AppendChild(this.plist.CreateKeyNode(key));
                parent.AppendChild(this.plist.CreateElement("array"));
            }
            foreach (object item in itemList) {
                this.Apply(parent.GetKeyNode(key).NextSibling, null, (IDictionary)item);
            }
        }

        private void ApplyDict(XmlNode parent, string key, IDictionary item) {
            // キーが空の場合、Array ノードへの挿入と見なす
            if (string.IsNullOrEmpty(key)) {
                XmlNode dictNode = parent.AppendChild(this.plist.CreateElement("dict"));
                foreach (object k in item.Keys) {
                    this.Apply(dictNode, (string)k, (IDictionary)item[k]);
                }
                return;
            }

            if (!parent.HasKeyNode(key)) {
                // キーがないなら、キーノードと dict ノードを作る
                parent.AppendChild(this.plist.CreateKeyNode(key));
                parent.AppendChild(this.plist.CreateElement("dict"));
            }
            foreach (object k in item.Keys) {
                this.Apply(parent.GetKeyNode(key).NextSibling, (string)k, (IDictionary)item[k]);
            }
        }
    }

    internal static class XmlExtension {

        public static bool HasChildNode(this XmlNode self, string name, bool ignoreCase = true) {
            return null != self.GetChildNode(name, ignoreCase);
        }

        public static XmlNode GetChildNode(this XmlNode self, string name, bool ignoreCase = true) {
            foreach (XmlNode childNode in self.ChildNodes) {
                if (childNode.Name == name || (ignoreCase && childNode.Name.ToLower() == name.ToLower())) {
                    return childNode;
                }
            }
            return null;
        }

        public static bool HasKeyNode(this XmlNode self, string key) {
            return null != self.GetKeyNode(key);
        }

        public static XmlNode GetKeyNode(this XmlNode self, string key) {
            return self.SelectSingleNode(string.Format("./key[.=\"{0}\"]", key));
        }

        public static XmlNode CreateSimpleTextNode(this XmlDocument xml, string name, string text) {
            XmlElement _node = xml.CreateElement(name);
            _node.InnerText = text;
            return _node;
        }

        public static XmlNode CreateKeyNode(this XmlDocument xml, string key) {
            return xml.CreateSimpleTextNode("key", key);
        }

    }
}