using UnityEngine;
using UnityEditor;

// Configure the Unity Player to work correctly with Active Stereoscopic Plugin for Unity.
[InitializeOnLoad]
public class ConfigureUnityPlayer
{
    public enum vSyncCount
    {
        DontSync,
        EveryVBlank,
        EverySecondVBlank
    }

	static ConfigureUnityPlayer ()
	{
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D9 });
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows, new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D9 });
        PlayerSettings.d3d9FullscreenMode = D3D9FullscreenMode.ExclusiveMode; 
		PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
		PlayerSettings.defaultIsFullScreen = true;

#if UNITY_5_5
		PlayerSettings.apiCompatibilityLevel = ApiCompatibilityLevel.NET_2_0;
#elif UNITY_5_6 || UNITY_2017
		PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Standalone, ApiCompatibilityLevel.NET_2_0);
#endif
		
		QualitySettings.vSyncCount = (int)vSyncCount.DontSync;

        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        AddTag("Grab", tagManager);
        AddTag("UI", tagManager);
    }

    static void AddTag(string tag, SerializedObject tagManager)
    {   
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        
        // First check if it is not already present
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag)) { found = true; break; }
        }

        // if not found, add it
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(0);
            SerializedProperty n = tagsProp.GetArrayElementAtIndex(0);
            n.stringValue = tag;
        }

        tagManager.ApplyModifiedProperties();
    }
}
