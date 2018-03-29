
using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;


// This script is the interface between Unity 3D and the plugin's native functions
// inside the DLLs. 


public class NvStereo : MonoBehaviour 
	{
	// NvStereoApi functions. Mostly used from StereoApi.js
	
	[DllImport ("NvStereoApi")]
	public static extern System.IntPtr NV_GetApiLog ();

	[DllImport ("NvStereoApi")]
	public static extern void NV_ClearApiLog ();
	
	[DllImport ("NvStereoApi")]
	public static extern System.IntPtr NV_GetApiErrorMessage (int code);

	[DllImport ("NvStereoApi")]
	public static extern int NV_InitializeApi ();

	[DllImport ("NvStereoApi")]
	public static extern int NV_ReleaseApi ();
	
	[DllImport ("NvStereoApi")]
	public static extern int NV_ActivateStereo ();
	
	[DllImport ("NvStereoApi")]
	public static extern int NV_DeactivateStereo ();

	[DllImport ("NvStereoApi")]
	public static extern int NV_EnsureStereoState (bool bActivated);
	
	
	// NvStereoRenderer fuctions
	
	[DllImport ("NvStereoRenderer")]
	private static extern System.IntPtr NV_GetLog ();

	[DllImport ("NvStereoRenderer")]
	private static extern void NV_ClearLog ();	

	[DllImport ("NvStereoRenderer")]
	private static extern int NV_GetLastResult ();
	
	[DllImport ("NvStereoRenderer")]
	private static extern int NV_GetLastError ();
	
	[DllImport ("NvStereoRenderer")]
	private static extern int NV_GetErrorCount ();

	public bool showApiLog = false;
	public bool showRendererLog = false;


	// ----- API DLL ----------------------------------------	

	private string GetApiLog()
		{
		return Marshal.PtrToStringAnsi(NV_GetApiLog());
		}
		
		
	// ----- Renderer DLL -----------------------------------	
	
	void Awake ()
		{
		// Some function inside the Renderer DLL must be invoked to ensure it to get loaded.
		
		NV_GetLastResult();
		}

	private string GetRendererLog()
		{
		return System.String.Format("{0}\nLast result: {1}\nLast error: 0x{2,00000000:X} (count: {3})", Marshal.PtrToStringAnsi(NV_GetLog()), NV_GetLastResult(), NV_GetLastError(), NV_GetErrorCount());
		}


	// ----- Debug & Log ------------------------------------	

	void OnGUI ()
		{
		string szInfo = (showApiLog? GetApiLog() : "") + "---\n\n" + (showRendererLog? GetRendererLog() : "");		
		
		if (showApiLog || showRendererLog)
			GUI.Label(new Rect(10, 60, Screen.width, Screen.height), szInfo);
		}
	
		
	void OnApplicationQuit ()
		{
		if (showApiLog)
			Debug.Log(GetApiLog());
			
		if (showRendererLog)
			Debug.Log(GetRendererLog());
		
		// Logs are cleared at the end because the plugin gets restarted on application quit.
		// (if cleared on start logs would be empty before sending them to Debug.Log)
		
		NV_ClearApiLog();
		// NV_ClearLog();  // Doesn't need to be cleared - outputs info only on application load (Unity if using the editor) and on resolution change.
		}
	}
