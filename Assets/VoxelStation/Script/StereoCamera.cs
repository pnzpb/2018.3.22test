using UnityEngine;

[RequireComponent (typeof(Camera))]
[RequireComponent (typeof(Stereoskopix))]


// This script controls the sterereoscopic settings.
// Stereoscopic cameras must be childen of this GameObject and be referenced here.
//
// This GameObject must have a dummy camera (Don't clear background, culling mask = nothing)
// which is used to intercept the Render calls and communicate with the plugin.
//
// (See the modified version of the Stereoskopix plugin included).


public class StereoCamera : MonoBehaviour
{
	public enum StereoModes
	{
		Disabled,
		Anaglyph,
		Interlaced,
		Active,
		SideBySide
    };

	public Camera       leftCamera;
	public Camera       rightCamera;
	public StereoModes  stereoMode = StereoModes.Disabled;

	public float        eyeDistance = 0.06f;
	public float        parallaxDistance = 3.0f;

	public bool         autoEyeDistance = false;
	public bool         autoEyeParallax = false;

	public LayerMask    autoEyeLayerMask = (1 << 32) - 1;

	public float        autoEyeMinRange = 0.25f;
	public float        autoEyeMaxRange = 0.75f;
	public float        autoEyeDistanceMin = 0.01f;
	public float        autoEyeDistanceMax = 0.06f;
	public float        autoEyeDistanceSpeed = 2.0f;
	public float        autoEyeParallaxMin = 1.0f;
	public float        autoEyeParallaxMax = 3.0f;
	public float        autoEyeParallaxSpeed = 2.0f;

	Stereoskopix        m_stereo;
	Camera              m_camera;


	void OnEnable ()
	{
		m_stereo = GetComponent<Stereoskopix> ();
		m_camera = GetComponent<Camera> ();
	}


	void DoAutoEyeParameters ()
	{
		    RaycastHit hit;
		    bool touch = false;
		    float distanceRatio = 0.0f;

		if (autoEyeDistance || autoEyeParallax) 
        {
			touch = Physics.Raycast (transform.position, transform.forward, out hit, Mathf.Infinity, autoEyeLayerMask);
			distanceRatio = Mathf.InverseLerp (autoEyeMinRange, autoEyeMaxRange, hit.distance);
			if (!touch)
				distanceRatio = 1.0f;
		}

		if (autoEyeDistance) 
        {
            eyeDistance = 0.09f;
		}

		if (autoEyeParallax) {
			    float newParallax = 
                Mathf.Lerp (autoEyeParallaxMin, autoEyeParallaxMax, distanceRatio);
			parallaxDistance = Mathf.Lerp (parallaxDistance, newParallax, autoEyeParallaxSpeed * Time.deltaTime);
		}
	}


	void Update ()
	{
		DoAutoEyeParameters ();

		if (parallaxDistance < 0.2f)
			parallaxDistance = 0.2f;
		if (eyeDistance < 0.0f)
			eyeDistance = 0.0f;
		if (eyeDistance > 0.2f)
			eyeDistance = 0.2f;

		if (stereoMode != StereoModes.Disabled && rightCamera) {
			// Enable stereoscopic 3D mode always as Parallel
			// (uses camera.fieldOfView to calculate camera's parallel frustrums)

			if (!m_stereo.isStereoEnabled () || m_stereo.convergency != Stereoskopix.ConvergencyMode.Parallel) {
				// EnableStereo will disable the children cameras and render them on demand

				m_stereo.DisableStereo ();
				m_stereo.convergency = Stereoskopix.ConvergencyMode.Parallel;
				m_stereo.EnableStereo (leftCamera, rightCamera, null, null);
			}

			// Apply stereoscopic 3D parameters to the Scereoskopix script

			switch (stereoMode) {
			case StereoModes.Anaglyph:
				m_stereo.stereoMode = Stereoskopix.StereoMode.Anaglyph;
				break;
			case StereoModes.Interlaced:
				m_stereo.stereoMode = Stereoskopix.StereoMode.Interlace;
				break;
			case StereoModes.Active:
				m_stereo.stereoMode = Stereoskopix.StereoMode.ActiveStereo;
				break;
			case StereoModes.SideBySide:
				m_stereo.stereoMode = Stereoskopix.StereoMode.SideBySide;
				break;
			}

			m_stereo.parallaxDistance = parallaxDistance;
			m_stereo.interaxial = eyeDistance;
            VoxelStationCore.Matrix.Setinteraxial(eyeDistance);
        } else {
			// Stereoscopic 3D disabled.
			// DisableStereo relocates the left camera at the local center of the GameObject.
			// We use the left camera for non-stereoscopic 3D render.

			m_stereo.DisableStereo ();

			leftCamera.enabled = true;
			leftCamera.fieldOfView = m_camera.fieldOfView;

			if (rightCamera)
				rightCamera.enabled = false;
		}
	}

	public void SetCameraClearFlags (CameraClearFlags flags, Color clearColor)
	{
		leftCamera.clearFlags = flags;
		leftCamera.backgroundColor = clearColor;

		if (rightCamera) {
			rightCamera.clearFlags = flags;
			rightCamera.backgroundColor = clearColor;
		}
	}
}