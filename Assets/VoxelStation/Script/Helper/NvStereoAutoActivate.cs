
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

[RequireComponent(typeof(StereoCamera))]

public class NvStereoAutoActivate : MonoBehaviour
	{
#if !UNITY_EDITOR
	StereoCamera m_cam;
#endif


    void OnEnable () 
		{
#if !UNITY_EDITOR
		m_cam = GetComponent<StereoCamera>();
#endif

        int code = NvStereo.NV_InitializeApi();
		if (code != 0) 
			{
			string msg = Marshal.PtrToStringAnsi(NvStereo.NV_GetApiErrorMessage(code));
			Debug.LogWarning(string.Format("NV Initialize: {0} ({1})" + (code == -144? "\nStereoscopic 3D must be enabled at nVidia Control Panel!  Restart Unity 3D after modifying nVidia settings for them to take effect." : ""), code, msg));
			}
		}
		
	
	void OnDisable ()
		{
		int code = NvStereo.NV_ReleaseApi();
		if (code != 0)
			{
			string msg = Marshal.PtrToStringAnsi(NvStereo.NV_GetApiErrorMessage(code));
			Debug.LogWarning(System.String.Format("NV Release: {0} ({1})", code, msg));
			}
		}
		
	
	void Update () 
		{
        // Ensure the NVidia stereoscopic system is enabled and uses the correct settings when 
        // the stereoscopic mode is set to Active in the camera.

#if !UNITY_EDITOR
		NvStereo.NV_EnsureStereoState(m_cam.stereoMode == StereoCamera.StereoModes.Active);
#endif
    }
}
