/*

MODIFIED, REFACTORED AND TRANSLATED INTO C# BY EDY:

This is a heavily modified version of Stereoskopix 3D v027 for supporting
the Active Stereoscopic plugin from Dembeta. The "ActiveStereo" method
communicates with the plugin for triggering the active stereo stereoMode.

The script requires a DUMMY CAMERA: Don't Clear Background, Render
mask = nothing. This camera triggers the OnRenderImage calls
required for the script to run.

The actual cameras capturing the L-R pictures are children of this
one. Use this script together with the StereoCamera script also included.

Information on the original script follows:

.-------------------------------------------------------------------
|  Unity Stereoskopix 3D v027
|-------------------------------------------------------------------
|  This all started when TheLorax began this thread:
|  http://forum.unity3d.com/threads/11775
|-------------------------------------------------------------------
|  There were numerous contributions to the thread from
|  aNTeNNa trEE, InfiniteAlec, Jonathan Czeck, monark and others.
|-------------------------------------------------------------------
|  checco77 of Esimple Studios wrapped the whole thing up
|  in a script & packaged it with a shader, materials, etc.
|  http://forum.unity3d.com/threads/60961
|  Esimple included a copyright & license:
|  Copyright (c) 2010, Esimple Studios All Rights Reserved.
|  License: Distributed under the GNU GENERAL PUBLIC LICENSE (GPL)
| ------------------------------------------------------------------
|  I tweaked everything, added options for Side-by-Side, Over-Under,
|  Swap Left/Right, etc, along with a GUI interface:
|  http://forum.unity3d.com/threads/63874
|-------------------------------------------------------------------
|  Wolfram then pointed me to shaders for interlaced/checkerboard display.
|-------------------------------------------------------------------
|  In this version (v026), I added Wolfram's additional display modes,
|  moved Esimple's anaglyph options into the script (so that only one
|  material is needed), and reorganized the GUI.
|-------------------------------------------------------------------
|  The package consists of
|  1) this script ('stereoskopix3D.js')
|  2) a shader ('stereo3DViewMethods.shader')
|  3) a material ('stereo3DMat')
|  4) a demo scene ('demoScene3D.scene') - WASD or arrow keys travel,
|     L button grab objects, L button lookaround when GUI hidden.
|-------------------------------------------------------------------
|  Instructions: (NOTE: REQUIRES UNITY PRO)
|  1. Drag this script onto your camera.
|  2. Drag 'stereoMat' into the 'Stereo Materials' field.
|  3. Hit 'Play'.
|  4. Adjust parameters with the GUI controls, press the tab key to toggle.
|  5. To save settings from the GUI, copy them down, hit 'Stop',
|     and enter the new settings in the camera inspector.
'-------------------------------------------------------------------
|  Perry Hoberman <hoberman (at) bway.net
|-------------------------------------------------------------------
*/

using UnityEngine;
using UnityEngine.UI;
using VoxelStation.Core;

[RequireComponent(typeof(Camera))]

public class Stereoskopix : MonoBehaviour
{
	public enum StereoMode { Anaglyph, SideBySide, OverUnder, Interlace, Checkerboard, ActiveStereo };
	public enum SideBySideMode { Squeezed, Unsqueezed };
	public enum ConvergencyMode { Parallel, ToedIn };
	public enum RenderCameraMode { LeftRight, LeftOnly, RightOnly, RightLeft };

	public StereoMode stereoMode = StereoMode.Anaglyph;
	public SideBySideMode sideBySideMode = SideBySideMode.Squeezed;
	public ConvergencyMode convergency = ConvergencyMode.Parallel;
	public RenderCameraMode cameraMode = RenderCameraMode.LeftRight;

	public float interaxial = 0.25f;
	public float parallaxDistance = 6.0f;

	public Camera[] uiCameras;
	public Camera backgroundCamera;

	public Material stereoMaterial;

	// Private vars
	private Camera m_leftCam;
	private Camera m_rightCam;
	private Transform m_leftCamRig;
	private Transform m_rightCamRig;

	private RenderTexture m_leftCamTexture;
	private RenderTexture m_rightCamTexture;

	private bool m_stereoEnabled = false;
	private int m_screenWidth = 0;
	private int m_screenHeight = 0;

	new Camera camera;      // Overrides inherited camera in Unity 4. Creates the field in Unity 5.

	void OnEnable()
	{
		// Check for stereo material

		if (stereoMaterial == null)
		{
			Debug.LogError("No Stereo Material Found. Please drag 'stereoMat' into the Stereo Material Field");
			this.enabled = false;
			return;
		}

		// Disable any render in the associated camera. It's used for
		// triggering the OnRenderImage callback only.

		camera = GetComponent<Camera>();
		camera.cullingMask = 0;
		camera.backgroundColor = new Color(0, 0, 0, 0);
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.enabled = false;
		camera.targetTexture = null;

		UpdateView();
	}

	void LateUpdate()
	{
		UpdateView();
	}

	public bool isStereoEnabled()
	{
		return m_stereoEnabled;
	}

	public void EnableStereo(Camera leftCam, Camera rightCam, Transform leftCameraRig, Transform rightCameraRig)
	{
		if (!leftCam || !rightCam) return;

		m_leftCam = leftCam;
		m_rightCam = rightCam;
		m_leftCamRig = leftCameraRig != null ? leftCameraRig : leftCam.transform;
		m_rightCamRig = rightCameraRig != null ? rightCameraRig : rightCam.transform;

		// Store the camera settings in the associated camera

		camera.projectionMatrix = m_leftCam.projectionMatrix;
		camera.nearClipPlane = m_leftCam.nearClipPlane;
		camera.farClipPlane = m_leftCam.farClipPlane;
		camera.aspect = m_leftCam.aspect;

		// Setup cameras.
		// All cameras except the associated camera are rendered on demand at OnRenderImage

		m_leftCam.targetTexture = m_leftCamTexture;
		m_rightCam.targetTexture = m_rightCamTexture;

		camera.depth = Mathf.Max(m_leftCam.depth, m_rightCam.depth) + 1;

		m_leftCam.enabled = false;
		m_rightCam.enabled = false;

		foreach (Camera cam in uiCameras)
		{
			if (cam != null)
				cam.enabled = false;
		}

		if (backgroundCamera != null)
			backgroundCamera.enabled = false;

		camera.enabled = true;
		m_stereoEnabled = true;
	}

	public void DisableStereo()
	{
		if (!m_stereoEnabled) return;

		m_leftCam.targetTexture = null;
		m_rightCam.targetTexture = null;
		if (m_leftCamTexture) m_leftCamTexture.Release();
		if (m_rightCamTexture) m_rightCamTexture.Release();

		m_leftCam.ResetProjectionMatrix();
		m_rightCam.ResetProjectionMatrix();

		Vector3 localPos = m_leftCamRig.transform.localPosition;
		m_leftCamRig.transform.localPosition = new Vector3(0.0f, localPos.y, localPos.z);

		foreach (Camera cam in uiCameras)
		{
			if (cam != null)
			{
				cam.targetTexture = null;
				cam.enabled = true;
			}
		}

		if (backgroundCamera != null)
		{
			backgroundCamera.targetTexture = null;
			backgroundCamera.enabled = true;
		}

		camera.enabled = false;
		m_stereoEnabled = false;
	}

	private void Update() { }

	void OnPreRender()
	{
		//Clear the previous frame from screen
		GL.Clear(false, true, Color.clear);
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		EnsureRenderTextures();

		// Active stereoscopic mode

		if (stereoMode == StereoMode.ActiveStereo)
		{
			// Left camera

			if (backgroundCamera != null)
			{
				backgroundCamera.targetTexture = m_leftCam.targetTexture;
				backgroundCamera.Render();
			}

			m_leftCam.Render();
			foreach (Camera cam in uiCameras)
			{
				if (cam != null)
				{
					cam.targetTexture = m_leftCam.targetTexture;
					cam.Render();
				}
			}

			Graphics.Blit(m_leftCamTexture, destination);
			GL.IssuePluginEvent(1);

			// Right camera

			if (backgroundCamera != null)
			{
				backgroundCamera.targetTexture = m_rightCam.targetTexture;
				backgroundCamera.Render();
			}

			m_rightCam.Render();
			foreach (Camera cam in uiCameras)
			{
				if (cam != null)
				{
					cam.targetTexture = m_rightCam.targetTexture;
					cam.Render();
				}
			}

			Graphics.Blit(m_rightCamTexture, destination);
			GL.IssuePluginEvent(2);

			return;
		}

		// All other stereoscopic modes.
		// First render all cameras to their respective textures

		if (backgroundCamera != null)
		{
			backgroundCamera.targetTexture = m_leftCam.targetTexture;
			backgroundCamera.Render();
			backgroundCamera.targetTexture = m_rightCam.targetTexture;
			backgroundCamera.Render();
		}

		m_leftCam.Render();
		m_rightCam.Render();

		foreach (Camera cam in uiCameras)
		{
			if (cam != null)
			{
				cam.targetTexture = m_leftCam.targetTexture;
				cam.Render();
				cam.targetTexture = m_rightCam.targetTexture;
				cam.Render();
			}
		}

		// Compose the resulting textures into the destination according to the stereoscopic mode

		RenderTexture.active = destination;
		GL.PushMatrix();
		GL.LoadOrtho();

		switch (stereoMode)
		{
			case StereoMode.Anaglyph:
				stereoMaterial.SetPass(0);
				DrawQuad(0);
				break;

			case StereoMode.SideBySide:
			case StereoMode.OverUnder:
				stereoMaterial.SetPass(1);
				DrawQuad(1);
				stereoMaterial.SetPass(2);
				DrawQuad(2);
				break;

			case StereoMode.Interlace:
			case StereoMode.Checkerboard:
				stereoMaterial.SetPass(3);
				DrawQuad(3);
				break;

			default:
				break;
		}

		GL.PopMatrix();

		if (stereoMode == StereoMode.SideBySide)
			GL.IssuePluginEvent(0);
	}

	void EnsureRenderTextures()
	{
		if (m_screenWidth != Screen.width || m_screenHeight != Screen.height || m_leftCamTexture == null || m_rightCamTexture == null)
		{
			if (m_leftCamTexture) m_leftCamTexture.Release();
			if (m_rightCamTexture) m_rightCamTexture.Release();

			m_leftCamTexture = new RenderTexture(Screen.width, Screen.height, 24);
			m_rightCamTexture = new RenderTexture(Screen.width, Screen.height, 24);

			stereoMaterial.SetTexture("_LeftTex", m_leftCamTexture);
			stereoMaterial.SetTexture("_RightTex", m_rightCamTexture);

			if (m_stereoEnabled)
			{
				m_leftCam.targetTexture = m_leftCamTexture;
				m_rightCam.targetTexture = m_rightCamTexture;
			}

			m_screenWidth = Screen.width;
			m_screenHeight = Screen.height;
		}
	}
	public Transform centerObject;

	void UpdateView()
	{
		parallaxDistance = Mathf.Abs(centerObject.localPosition.z - transform.localPosition.z);

		if (!m_stereoEnabled)
			return;

		switch (cameraMode)
		{
			case RenderCameraMode.LeftRight:
				m_leftCamRig.position = transform.position + transform.TransformDirection(-interaxial / 2, 0, 0);
				m_rightCamRig.position = transform.position + transform.TransformDirection(interaxial / 2, 0, 0);
				break;
			case RenderCameraMode.LeftOnly:
				m_leftCamRig.position = transform.position + transform.TransformDirection(-interaxial / 2, 0, 0);
				m_rightCamRig.position = transform.position + transform.TransformDirection(-interaxial / 2, 0, 0);
				break;
			case RenderCameraMode.RightOnly:
				m_leftCamRig.position = transform.position + transform.TransformDirection(interaxial / 2, 0, 0);
				m_rightCamRig.position = transform.position + transform.TransformDirection(interaxial / 2, 0, 0);
				break;
			case RenderCameraMode.RightLeft:
				m_leftCamRig.position = transform.position + transform.TransformDirection(interaxial / 2, 0, 0);
				m_rightCamRig.position = transform.position + transform.TransformDirection(-interaxial / 2, 0, 0);
				break;
		}

		if (convergency == ConvergencyMode.ToedIn)
		{
			m_leftCam.projectionMatrix = camera.projectionMatrix;
			m_rightCam.projectionMatrix = camera.projectionMatrix;
			m_leftCamRig.LookAt(transform.position + (transform.TransformDirection(Vector3.forward) * parallaxDistance), transform.up);
			m_rightCamRig.LookAt(transform.position + (transform.TransformDirection(Vector3.forward) * parallaxDistance), transform.up);
		}
		else
		{
			m_leftCamRig.rotation = transform.rotation;
			m_rightCamRig.rotation = transform.rotation;

			switch (cameraMode)
			{
				case RenderCameraMode.LeftRight:
					m_leftCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(true, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					m_rightCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(false, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					break;
				case RenderCameraMode.LeftOnly:
					m_leftCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(true, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					m_rightCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(true, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					break;
				case RenderCameraMode.RightOnly:
					m_leftCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(false, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					m_rightCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(false, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					break;
				case RenderCameraMode.RightLeft:
					m_leftCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(false, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					m_rightCam.projectionMatrix = VoxelStationCore.Matrix.GetProjectionMatrix(true, transform, VoxelCore.Instance.centerObject, VoxelCore.Instance.voxelData.screenAngle, camera, VoxelCore.Instance.viewSize);
					break;
			}
		}
	}

	void DrawQuad(int cam)
	{
		if (stereoMode == StereoMode.Anaglyph)
		{
			GL.Begin(GL.QUADS);
			GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
			GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
			GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
			GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
			GL.End();
		}
		else
		{
			if (stereoMode == StereoMode.SideBySide)
			{
				if (cam == 1)
				{
					GL.Begin(GL.QUADS);
					GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(0.5f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(0.5f, 1.0f, 0.1f);
					GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
					GL.End();
				}
				else
				{
					GL.Begin(GL.QUADS);
					GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.5f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
					GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.5f, 1.0f, 0.1f);
					GL.End();
				}
			}
			else if (stereoMode == StereoMode.OverUnder)
			{
				if (cam == 1)
				{
					GL.Begin(GL.QUADS);
					GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.5f, 0.1f);
					GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.5f, 0.1f);
					GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
					GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
					GL.End();
				}
				else
				{
					GL.Begin(GL.QUADS);
					GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
					GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 0.5f, 0.1f);
					GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 0.5f, 0.1f);
					GL.End();
				}
			}
			else if (stereoMode == StereoMode.Interlace || stereoMode == StereoMode.Checkerboard)
			{
				GL.Begin(GL.QUADS);
				GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.1f);
				GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.1f);
				GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.1f);
				GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.1f);
				GL.End();
			}
		}
	}
}
