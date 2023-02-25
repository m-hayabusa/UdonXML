using UnityEngine;
using UnityEditor;

namespace UdonXMLParser
{
    public class CreateUdonXML
    {
        [MenuItem("GameObject/nekomimiStudio/UdonXML", false, 10)]
        public static void Create(MenuCommand menu)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/studio.nekomimi.udonxml/Runtime/UdonXML.prefab");
            GameObject res = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
            GameObjectUtility.SetParentAndAlign(res, (GameObject)menu.context);
            Undo.RegisterCreatedObjectUndo(res, "UdonXML");
            Selection.activeObject = res;
        }
    }
}