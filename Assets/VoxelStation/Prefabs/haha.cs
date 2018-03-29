using UnityEditor;
using Babybus.Framework.ExtensionMethods;
using UnityEngine;

class DecryptUtility
{
    [MenuItem("Utility/DecryptFile")]
    static void DecryptFile()
    {
        Debug.Log("haha");
        var inputFile = "D:/Downloads/15e4fdfb-eb07-4dc1-852a-dcb9b6562127";
        var key = "65937b7b4505dfae07d95cd61e7987c994ace247ee7518eecdc711e8bd60bae8a0ed2177b66e9f690627683d697b33fe";

        var unityEditor = typeof(Editor).Assembly;

        var assetStoreUtils = unityEditor.GetType("UnityEditor.AssetStoreUtils");

        assetStoreUtils.Invoke("DecryptFile", inputFile, inputFile + ".unitypackage", key);
    }
}
