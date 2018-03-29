//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2016-2017 VoxelSense, Inc.  All Rights Reserved.
//
//////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections;
using System;
using VoxelStationCore;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VoxelStation.Core
{
    public enum ButtonValue
    {
        One = 1,
        Two = 2,
        Three = 3,
    }

    public enum StylusFeature
    {
        ColorRed,
        ColorGreen,
        ColorBlue,
        Vibration
    }

    #region Pose
    public struct Pose
    {
        /// <summary>
        /// The position in meters.
        /// </summary>
        public Vector3 Position { get; set; }

        /// <summary>
        /// The orientation.
        /// </summary>
        public Quaternion Rotation { get; set; }

        /// <summary>
        /// The forward direction.
        /// </summary>
        public Vector3 Direction { get; set; }

    }
    #endregion

    //////////////////////////////////////////////////////////////////
    // Public Enumerations
    //////////////////////////////////////////////////////////////////
    public enum StylusState
    {
        Idle,
        Hover,
        Grab,
    }

    public class StylusEventInfo
    {
        public ButtonValue ButtonID;
        public StylusEventInfo(ButtonValue ButtonID)
        {
            this.ButtonID = ButtonID;
        }
    }

    public class ActionEventInfo
    {
        public GameObject actionObject;
        public Pose pose;
        public float length;

        public ActionEventInfo(GameObject actionObject)
        {
            this.actionObject = actionObject;
        }

        public ActionEventInfo(GameObject actionObject, Pose pose, float length)
        {
            this.actionObject = actionObject;
            this.pose = pose;
            this.length = length;
        }
    }

    

    [ExecuteInEditMode]
    public partial class VoxelCore : MonoBehaviour
    {
        #region 单例
        public static VoxelCore Instance;
        public VoxelCore()
        {

        }
		#endregion

		#region 眼睛与笔相关数据
		

        /// <summary>
        /// 中心平面距离,最小值0.03为主相机近截面的距离
        /// </summary>
        [Range(0.03f, 10)]
        public float        viewSize = 1;
        /// <summary>
        /// 视觉中心点位置
        /// </summary>
        public Transform    centerObject;
        /// <summary>
        /// 当前使用相机
        /// </summary>
        public Transform    sceneCamera = null;

        public Transform    WireDraw;
        /// <summary>
        /// 笔的姿态
        /// </summary>
        public Pose         pose;

		/// <summary>
		/// 立体显示相关左右相机
		/// </summary>
		private Camera      leftCamera;
        private Camera      rightCamera;
        /// <summary>
        /// 笔的两个点对应的数据
        /// </summary>
        private double[]    stylusPositionOne = new double[4] {0.1f,0.1,0.1,0.1 };
        private double[]    stylusPositionTwo = new double[4] {1f,1f,1f,1f };
        /// <summary>
        /// 眼镜标记点数据
        /// </summary>
        private double[]    glassPosition = new double[3];
        #endregion

        #region Mouse
        /// <summary>
        /// Enables the mouse cursor to auto-hide after a specified time in seconds
        /// due to inactivity.
        /// </summary>
        public bool         enableMouseAutoHide = false;
        /// <summary>
        /// The time in seconds of mouse inactivity before its cursor will be hidden
        /// if EnableMouseAutoHide is true.
        /// </summary>
        public float        mouseAutoHideDelay = 5.0f;
        public bool         isHeadTrackingEnabled = true;


        private Vector3     previousMousePosition = Vector3.zero;
        private bool        wasMouseAutoHideEnabled = false;
        private float       mouseAutoHideElapsedTime = 0.0f;
        #endregion

        #region StylusValues
        public delegate void StylusEventHandler(StylusEventInfo info);
        public delegate void ActionEventHandler(ActionEventInfo info);        
        
        public float                    stylusLength = 10f;
        public float                    stylusWidth = 0.002f;
        public GameObject               stylusObject;
        public StylusEventHandler       stylusButtonPressed;
        public StylusEventHandler       stylusButtonReleased;

        /// <summary>
        /// Assign function to UI when the raycaster is hovering on a button
        /// </summary>
        public ActionEventHandler       onStylusHoverUIBegin;
        public ActionEventHandler       onStylusHoverUIEnd;

        /// <summary>
        /// Assign function to do when a button on UI is pressed
        /// </summary>
        public ActionEventHandler       onStylusButtonOnePressedUI;
        public ActionEventHandler       onStylusButtonTwoPressedUI;
        public ActionEventHandler       onStylusButtonThreePressedUI;

        /// <summary>
        /// Assign Callback function for hovering on a grab object
        /// </summary>
        public ActionEventHandler       onStylusHoverObjectBegin;
        public ActionEventHandler       onStylusHoverObjectEnd;

        /// <summary>
        /// Assign callback function for Grab object, update state and on leave grab
        /// </summary>
        public ActionEventHandler       onStylusGrabObjectBegin;
        public ActionEventHandler       onStylusGrabObjectUpdate;
        public ActionEventHandler       onStylusGrabObjectEnd;
        
        private bool                    isButtonOnePressed = false;
        private bool                    wasButtonOnePressed = false;

        private bool                    isButtonTwoPressed = false;
        private bool                    wasButtonTwoPressed = false;

        private bool                    isButtonThreePressed = false;
        private bool                    wasButtonThreePressed = false;

        private LineRenderer            stylusRenderer;
        private GameObject              stylusHead;
        private Vector3                 stylusScaleInital;
        private GameObject              grabObject = null;
        private StylusState             stylusState = StylusState.Idle;

		private float					STYLUS_BEAM_LENGTH = 10f;
		private float					STYLUS_BEAM_WIDTH = 0.002f;
		#endregion

		public VoxelStationData			voxelData = new VoxelStationData();

		private StereoCamera			stereoCameraInstance;
		private GameObject              previousModelSelection;
        private GameObject              _UISelection;

		private ScreenMode				currentScreenMode = ScreenMode.ScreenTilt;

        //////////////////////////////////////////////////////////////////////
        // Unity Monobehaviour Callbacks
        //////////////////////////////////////////////////////////////////////
        public void Awake()
        {
            if (sceneCamera == null)
            {
                Debug.LogError("没有设置当前使用相机！");
                return;
            }

            leftCamera = transform.GetComponentInChildren<StereoCamera>().leftCamera;
            rightCamera = transform.GetComponentInChildren<StereoCamera>().rightCamera;

			STYLUS_BEAM_LENGTH = stylusLength * viewSize;
			STYLUS_BEAM_WIDTH = stylusWidth * viewSize;

			Instance = this;

			//Default Screen mode
			Matrix.SetScreenMode(currentScreenMode);
        }

        // Sent to all game objects before the application is quit
        public void OnApplicationQuit()
        {
            UDPConnect.Instance.StopDevice();
        }

        void Start()
        {
            if (Application.isPlaying)
            {
                //初始化硬件
                UDPConnect.Instance.Startdevice();
            }
            pose = new Pose();
        }

        private bool isFirstRun = true;
		private bool isRotating = false;
		
        // 将更新眼镜和笔数据放在FixedUpdate里面是为了保证数据更新速度大于画线的速度 ，防止线跳动情况发生。但是这样会牺牲一定的帧数
        void Update()
        {
			///if there is no line renderer of stylus then search for it
			if (stylusObject != null && stylusRenderer == null)
				stylusRenderer = stylusObject.transform.GetComponent<LineRenderer>();

			//编辑器状态执行逻辑
			if (Application.isEditor && !Application.isPlaying)
			{
				///Update Stylus length here
				if (stylusRenderer != null)
				{
					if (stylusRenderer.startWidth != stylusWidth || (stylusRenderer.GetPosition(1) != stylusRenderer.transform.position + (stylusRenderer.transform.forward * 0.1f * stylusLength)))
					{
						stylusRenderer.startWidth = stylusWidth;
						stylusRenderer.endWidth = stylusWidth;
						stylusRenderer.SetPosition(0, stylusRenderer.transform.position);
						stylusRenderer.SetPosition(1, stylusRenderer.transform.position + (stylusRenderer.transform.forward * 0.1f * stylusLength));//(we are reciveing direction in 0.1f scale and forward transform is 1)
					}
				}
			}

			if (Application.isPlaying && voxelData.isRecievedData)
			{
				//获取按钮数据
				UpdateButton(voxelData.keyType);
				
				//获取视觉计算后的两个点的数据
				if (leftCamera != null && rightCamera != null)
				{
                    if (stylusObject != null )
                    {
                        stylusHead = stylusObject.transform.GetChild(0).gameObject;
                        stylusScaleInital = stylusObject.transform.localScale;
                    }

                    if (isHeadTrackingEnabled)
					{
                        int isTrackingGlasses = voxelData.isTrackingGlasses;

                        glassPosition = new double[] { voxelData.headPosition[0], voxelData.headPosition[1], voxelData.headPosition[2] };                        

                        if (currentScreenMode == ScreenMode.ScreenTilt)
                            WireDraw.transform.localRotation = Quaternion.Euler(voxelData.screenAngle, 0, 0);

						if (stereoCameraInstance == null)
							stereoCameraInstance = GetComponentInChildren<StereoCamera>();

						if (!isFirstRun)
						{
							if (isTrackingGlasses == 1)
								stereoCameraInstance.stereoMode = StereoCamera.StereoModes.Active;
							else
								stereoCameraInstance.stereoMode = StereoCamera.StereoModes.Disabled;
						}
						else
						{
							isFirstRun = false;
						}

                        UpdateDataToMeter(ref glassPosition);//将单位从毫米换算成米
                        UpdateCameraPose(new Vector3((float)glassPosition[0] - 0.26f, -(float)glassPosition[1] + 0.05f  , (float)glassPosition[2]));//更新眼镜位置
                        
					}

					if(currentScreenMode == ScreenMode.LookAt)
						sceneCamera.LookAt(centerObject);
				}
				
                stylusPositionOne = new double[] { voxelData.penAtPt1[0], voxelData.penAtPt1[1], voxelData.penAtPt1[2], voxelData.penAtPt1[3] };
                stylusPositionTwo = new double[] { voxelData.penAtPt2[0], voxelData.penAtPt2[1], voxelData.penAtPt2[2], voxelData.penAtPt2[3] };

                Vector3 stylusFinalPointOne, stylusFinalPointTwo;

                bool isTherePoint =
                VoxelStationMathSdk.Math.UpdatePenPose(stylusPositionOne, stylusPositionTwo, leftCamera, rightCamera, out stylusFinalPointOne, out stylusFinalPointTwo);//两条射线是否有交点
                
                if (isTherePoint)
                    UpdatePose(stylusFinalPointOne, stylusFinalPointTwo);//更新笔的位置

				//Note: Idle to hover state can be ignored for assigning delegates
				//but hover to grab is a must if a user want to pick a object
				RaycastHit hit = new RaycastHit();
				switch (stylusState)
				{
					case StylusState.Idle:
						{
							// Perform a raycast on the entire scene to determine what the
							// stylus is currently colliding with.
							STYLUS_BEAM_WIDTH = stylusWidth*viewSize;

							if (Physics.Raycast(pose.Position, pose.Direction, out hit))
							{
								// Update the stylus beam length.
								STYLUS_BEAM_LENGTH = hit.distance;

								switch (hit.collider.tag)
								{
									case "UI":
										stylusState = StylusState.Hover;
										if (this.onStylusHoverUIBegin != null)
										{

											//Calls on hover function
											this.onStylusHoverUIBegin(new ActionEventInfo(hit.collider.gameObject));
											_UISelection = hit.collider.gameObject;
										}
										break;

									case "Grab":
										stylusState = StylusState.Hover;
										if (this.onStylusHoverObjectBegin != null)
										{
											previousModelSelection = hit.collider.gameObject;

											this.onStylusHoverObjectBegin(new ActionEventInfo(hit.collider.gameObject));
											previousModelSelection = hit.collider.gameObject;
										}
										break;

									default:
										break;
								}
							}
							else
							{
								STYLUS_BEAM_LENGTH = stylusLength;
							}
						}
						break;

					case StylusState.Hover:
						//PreviousSelection
						if (Physics.Raycast(pose.Position, pose.Direction, out hit))
						{
							// Update the stylus beam length.
							STYLUS_BEAM_LENGTH = hit.distance;

							switch (hit.collider.tag)
							{
								case "UI":
									//If user pointer came directly from model to UI 
									//then call object hover end and reset to idle
									if (previousModelSelection != null && this.onStylusHoverObjectEnd != null)
									{
										this.onStylusHoverObjectEnd(new ActionEventInfo(previousModelSelection));
										previousModelSelection = null;
										stylusState = StylusState.Idle;
									}

									if (isButtonOnePressed && !wasButtonOnePressed && this.onStylusButtonOnePressedUI != null)
									{
										this.onStylusButtonOnePressedUI(new ActionEventInfo(hit.collider.gameObject));
									}
									break;

								case "Grab":
									//if user pointer came from UI to model directly
									//then all end on hover of UI and reset to idle
									if (_UISelection != null && this.onStylusHoverUIEnd != null)
									{
										//Call the function to say the UI hover event is over
										this.onStylusHoverUIEnd(new ActionEventInfo(_UISelection));
										_UISelection = null;
										stylusState = StylusState.Idle;
									}

									//Call the grab function
									if (isButtonOnePressed && !wasButtonOnePressed && this.onStylusGrabObjectBegin != null)
									{
										grabObject = hit.collider.gameObject;
										this.onStylusGrabObjectBegin(new ActionEventInfo(grabObject, pose, STYLUS_BEAM_LENGTH));
										stylusState = StylusState.Grab;
									}
									break;

								default:
									break;

							}

						}
						else
						{
                            if (this.onStylusHoverObjectEnd != null)
                                this.onStylusHoverObjectEnd(new ActionEventInfo(previousModelSelection));
                            stylusState = StylusState.Idle;
                            previousModelSelection = null;
                            STYLUS_BEAM_LENGTH = stylusLength*viewSize;
						}
						break;

					case StylusState.Grab:
						{
							if (grabObject.tag.Equals("Grab") && this.onStylusGrabObjectUpdate != null)
							{
								///Update the grab.
								///this.UpdateGrab(pose.Position, pose.Rotation);
								///calls the update for model
								this.onStylusGrabObjectUpdate(new ActionEventInfo(grabObject, pose, STYLUS_BEAM_LENGTH));
							}

							/// End the grab if the front stylus button was released.
							if (!isButtonOnePressed && wasButtonOnePressed)
							{
								///Calls the end grab delegate to stop movin model
								if (this.onStylusGrabObjectEnd != null)
									this.onStylusGrabObjectEnd(new ActionEventInfo(grabObject));
								stylusState = StylusState.Hover;
								grabObject = null;
							}
						}
						break;

					default:
						break;
				}

				//if we have previously hovered over UI and now the hit.collider is giving empty response then we need to set slection as Idle
				//and notify the call back function
				if (hit.collider == null)
				{
					if (_UISelection != null && this.onStylusHoverUIEnd != null)
					{
						//Call the function to say the UI hover event is over
						this.onStylusHoverUIEnd(new ActionEventInfo(_UISelection));
						_UISelection = null;
						stylusState = StylusState.Idle;
					}

					if (previousModelSelection != null && (stylusState != StylusState.Grab && stylusState != StylusState.Hover))
					{
						if (this.onStylusHoverObjectEnd != null)
							this.onStylusHoverObjectEnd(new ActionEventInfo(previousModelSelection));
						previousModelSelection = null;
						stylusState = StylusState.Idle;
					}
				}

				// Update the stylus beam.
				this.UpdateStylus(pose.Position, pose.Direction);

				// Cache state for next frame.
				wasButtonOnePressed = isButtonOnePressed;
			}
#if !UNITY_EDITOR
            //鼠标隐藏函数，通过操作inspector面板执行
            UpdateMouseAutoHide();
#endif
			//键盘热键相关逻辑
			HotKey();
		}

        /// <summary>
        /// right now we have two options here 
        /// 1)increase/decrease the z scale of the boxes along with xy
        /// 2)keep the zscale of the box same and reduce xy
        /// </summary>
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.matrix = WireDraw.localToWorldMatrix;
            Handles.matrix = WireDraw.localToWorldMatrix;

            float fontSize = 0.3f;//坐标轴的大小
            Vector3 defaultCubeSize = new Vector3(0.565f, 0.34f, 0.3f) * viewSize;

#region PositiveParallexBox
            centerObject.rotation = sceneCamera.rotation;

            Vector3 toadd = new Vector3(0, 0, (defaultCubeSize.z ) / 2 + 0.001f);
            Vector3 origPos = centerObject.position + (centerObject.forward * toadd.z);

            if( voxelData != null && currentScreenMode == ScreenMode.ScreenTilt)
                origPos = Matrix.returnData(origPos.x, origPos.y, origPos.z, (int)voxelData.screenAngle);

            Handles.color = Color.black;
            Handles.Label(WireDraw.InverseTransformPoint(origPos), "Positive Parallex");
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(WireDraw.InverseTransformPoint(origPos), defaultCubeSize );
#endregion

#region NegativeParallexBox
            defaultCubeSize = new Vector3(0.565f, 0.34f, 0.13f)*viewSize;
            toadd = new Vector3(0, 0, (defaultCubeSize.z ) / 2 + 0.001f);
            origPos = centerObject.position - (centerObject.forward * toadd.z);

			if (voxelData != null && currentScreenMode == ScreenMode.ScreenTilt)
				origPos = Matrix.returnData(origPos.x, origPos.y, origPos.z, (int)voxelData.screenAngle);

            Handles.color = Color.black;
            Handles.Label(WireDraw.InverseTransformPoint(origPos), "Negative Parallex");
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(WireDraw.InverseTransformPoint(origPos), defaultCubeSize );
#endregion

#region ScreenZero
            Handles.color = Color.black;
            Handles.Label(WireDraw.InverseTransformPoint(centerObject.position), "Center");
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(WireDraw.InverseTransformPoint(centerObject.position), new Vector2(0.565f, 0.34f) * viewSize) ;
            Gizmos.DrawLine(Vector3.zero, sceneCamera.InverseTransformPoint(centerObject.position));
#endregion

#region 画视觉中心点

            // Draw local right vector.
            Handles.color = Color.red;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(0, centerObject.position, centerObject.rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(0, centerObject.position, centerObject.rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize, EventType.Ignore);
#endif
            // Draw local up vector.
            Handles.color = Color.green;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(0, centerObject.position, centerObject.rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(0, centerObject.position, centerObject.rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize, EventType.Ignore);
#endif

            // Draw local forward vector.
            Handles.color = Color.blue;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(0, centerObject.position, centerObject.rotation * Quaternion.identity, fontSize);
#else
            Handles.ArrowHandleCap(0, centerObject.position, centerObject.rotation * Quaternion.identity, fontSize, EventType.Ignore);
#endif

#endregion

#region 画相机
            Handles.color = Color.yellow;
            Handles.Label(sceneCamera.position, "Camera");
            // Draw local right vector.
            Handles.color = Color.red;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize, EventType.Ignore);
#endif

            // Draw local up vector.
            Handles.color = Color.green;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize, EventType.Ignore);
#endif

            // Draw local forward vector.
            Handles.color = Color.blue;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.identity, fontSize);
#else
            Handles.ArrowHandleCap(1, sceneCamera.position, sceneCamera.rotation * Quaternion.identity, fontSize, EventType.Ignore);
#endif

#endregion

#region 画相机
            Handles.color = Color.yellow;
            Handles.Label(pose.Position, "Stylus");

            // Draw local right vector.
            Handles.color = Color.red;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(2, pose.Position, pose.Rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(2, pose.Position, pose.Rotation * Quaternion.Euler(0.0f, 90.0f, 0.0f), fontSize, EventType.Ignore);
#endif

            // Draw local up vector.
            Handles.color = Color.green;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(2, pose.Position, pose.Rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize);
#else
            Handles.ArrowHandleCap(2, pose.Position, pose.Rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f), fontSize, EventType.Ignore);
#endif

            // Draw local forward vector.
            Handles.color = Color.blue;
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5_0 || UNITY_5_1)
            Handles.ArrowCap(2, pose.Position, pose.Rotation * Quaternion.identity, fontSize);
#else
            Handles.ArrowHandleCap(2, pose.Position, pose.Rotation * Quaternion.identity, fontSize, EventType.Ignore);
#endif

#endregion
#endif
        }

        public void ExitExe()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
		    Application.Quit();
#endif
        }

        public static double AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            double sin = vector1.x * vector2.y - vector2.x * vector1.y;
            double cos = vector1.x * vector2.x + vector1.y * vector2.y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }
        
        /// <summary>
        /// Set Stylus Color or Vibration with StylusFeature 
        /// duration in milliseconds the event will take place
        /// </summary>
        public void SetStylusFeature(StylusFeature feature, float duration)
        {   
            //Set_light_vibration cmd like this ("light_vibration,_in_duration_milliseconds,isinterval,_in_interval")
            UDPConnect.Instance.SocketSend("light_vibration,"+ (int)feature +  (uint)duration);
        }

        IEnumerator WaitSeconds(float time)
        {
            yield return new WaitForSeconds(time);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Private Helpers
        //////////////////////////////////////////////////////////////////////////// 

        /// <summary>
        /// 热键相关逻辑
        /// </summary>
        private void HotKey()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                ExitExe();
            if (Input.GetKeyDown(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Delete))
            {
                print("ctrl+alt+delete");
            }
        }

        /// <summary>
        /// 将单位从毫米换算成米
        /// mm - mtr
        /// </summary>
        private void UpdateDataToMeter(ref double[] d)
        {
            for (int i = 0; i < d.Length; i++)
                d[i] = d[i] * 0.001f;
        }

        /// <summary>
        /// 笔按钮状态
        /// </summary>
        public void UpdateButton(int value)
        {
            switch (value)
            {
                case 81: //1 d 前排大按钮按下
                    //Returns status
                    //Capture if necessary
                    SetStylusFeature(StylusFeature.ColorGreen, 50);//red light up for 0.5 second; cxm modified ,2016,12,31
                    SetStylusFeature(StylusFeature.Vibration, 50);

                    CallStylusPressed(ButtonValue.One);
                    isButtonOnePressed = true;
                    
                    break;

                case 83: //1 up 前排大按钮松开
                    CallStylusReleased(ButtonValue.Three);
                    isButtonOnePressed = false;
                    break;

                case 84: //2 d后排左按钮按下
                    CallStylusPressed(ButtonValue.Two);
                    isButtonTwoPressed = true;
                    break;

                case 86: //2 u 后排左按钮松开
                    CallStylusReleased(ButtonValue.Three);
                    isButtonTwoPressed = false;
                    break;

                case 87: //3 u后排右按钮按下
                    CallStylusPressed(ButtonValue.Three);
                    isButtonThreePressed = true;
                    break;

                case 89: //3 u 后排右按钮松开
                    CallStylusReleased(ButtonValue.Three);
                    isButtonThreePressed = false;
                    break;
            }
        }

        /// <summary>
        /// Will call back assigned button press function if any
        /// </summary>
        private void CallStylusPressed(ButtonValue value)
        {
            if (this.stylusButtonPressed != null)
                this.stylusButtonPressed(new StylusEventInfo(value));
		}

        /// <summary>
        /// Will call back assigned button release function if any
        /// </summary>
        private void CallStylusReleased(ButtonValue value)
        {
            if (this.stylusButtonReleased != null)
                this.stylusButtonReleased(new StylusEventInfo(value));
        }

        /// <summary>
        /// 更新位置和角度数据
        /// </summary>
        private void UpdatePose(Vector3 pointOne, Vector3 pointTwo)
        {
            pose.Position = pointOne;
            pose.Direction = (pointOne - pointTwo).normalized;

            Quaternion poseQuaternion = Quaternion.FromToRotation(transform.forward, pose.Direction);

            float rotationAngle = 0.0f;
           
            rotationAngle = (float)voxelData.stylus_Roll;

            pose.Rotation = Quaternion.AngleAxis(-rotationAngle, pose.Direction) * poseQuaternion;
        }

		
        /// <summary>
        /// 更新眼镜的位置 
        /// </summary>
        private void UpdateCameraPose(Vector3 cameraPosition)
        {
			if (currentScreenMode == ScreenMode.LookAt) {
				cameraPosition = new Vector3(cameraPosition.x + 0.26f, -cameraPosition.y + 0.05f, cameraPosition.z);
				if(!isRotating)
					sceneCamera.localPosition = centerObject.localPosition - Matrix.GetCameraPosition(cameraPosition, voxelData.screenAngle, viewSize, transform, centerObject) + new Vector3(0.02249998f, 0, 0) ;
			}				
			else
				sceneCamera.localPosition = Matrix.GetCameraPosition(cameraPosition, voxelData.screenAngle, viewSize);
		}

        private void UpdateMouseAutoHide()
        {
            Vector3 mousePosition = Input.mousePosition;

            if (this.enableMouseAutoHide)
            {
                if (mousePosition != previousMousePosition ||
                    Input.GetMouseButton(0) ||
                    Input.GetMouseButton(1) ||
                    Input.GetMouseButton(2))
                {
                    // If the cursor was previously disabled and the mouse
                    // moved or button was pressed, show the cursor and reset 
                    // the elapsed time before the mouse auto-hides.
                    this.SetMouseCursorVisible(true);
                    mouseAutoHideElapsedTime = 0.0f;
                }
                else if (this.IsMouseCursorVisible())
                {
                    // Update the ekapsed time before the mouse auto-hides.
                    // If the elapsed time falls exceeds the delay, we know 
                    // that the mouse has not been moved.
                    mouseAutoHideElapsedTime += Time.unscaledDeltaTime;
                    if (mouseAutoHideElapsedTime >= this.mouseAutoHideDelay)
                    {
                        this.SetMouseCursorVisible(false);
                    }
                }
            }
            else if (!this.enableMouseAutoHide && wasMouseAutoHideEnabled)
            {
                // Mouse auto-hide was toggled off. Restore the mouse cursor.
                this.SetMouseCursorVisible(true);
            }

            previousMousePosition = mousePosition;
            wasMouseAutoHideEnabled = this.enableMouseAutoHide;
        }

        private void SetMouseCursorVisible(bool isVisible)
        {
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
            Screen.showCursor = isVisible;
#else
            Cursor.visible = isVisible;
#endif
        }

        private bool IsMouseCursorVisible()
        {
#if (UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7)
            return Screen.showCursor;
#else
            return Cursor.visible;
#endif
        }
       
        private void UpdateStylus(Vector3 stylusPosition, Vector3 stylusDirection)
        {

            if (stylusRenderer != null)
            {
                float stylusWidth = this.STYLUS_BEAM_WIDTH;
                float stylusLength = this.STYLUS_BEAM_LENGTH;

                stylusHead.transform.localScale = stylusScaleInital ;
                stylusRenderer.startWidth = stylusWidth;
                stylusRenderer.endWidth = stylusWidth;
                stylusRenderer.SetPosition(0, stylusPosition);
                stylusRenderer.SetPosition(1, stylusPosition + (stylusDirection * stylusLength));
                stylusRenderer.transform.position = stylusPosition + (stylusDirection * stylusLength);
                stylusRenderer.transform.rotation = pose.Rotation;
            }
        }
    }
}