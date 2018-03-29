//////////////////////////////////////////////////////////////////////////
//
//  Copyright (C) 2016 VoxelScene, Inc.  All Rights Reserved.
//
//////////////////////////////////////////////////////////////////////////
using UnityEngine;
using UnityEditor;

namespace VoxelStation.Core
{
    [CustomEditor(typeof(VoxelCore))]
    public class VoxelStationEditor : Editor
    {
        private static readonly string INSPECTOR_ICON_PATH = "Assets/VoxelStation/Editor/Icons/";

        private int updateFrameCount = 0;

        private static bool isStereoRigSectionExpanded = true;
        private static bool isGlassesSectionExpanded = false;
        private static bool isStylusSectionExpanded = false;

        private GUIStyle foldoutStyle = null;
        private GUIStyle lineStyle = null;

        private Texture2D cameraIconTexture = null;
        private Texture2D glassesIconTexture = null;
        private Texture2D stylusIconTexture = null;


        private SerializedProperty centerObject;
        private SerializedProperty sceneCamera;
        private SerializedProperty WireDraw;
        private SerializedProperty ipdProperty;
        private SerializedProperty isHeadTrackingEnabled;

        private SerializedProperty enableMouseAutoHideProperty;
        private SerializedProperty mouseAutoHideDelayProperty;

        private SerializedProperty stylusGameObjectProperty;
        private SerializedProperty stylusLengthProperty;
        private SerializedProperty stylusWidthProperty;

        void OnEnable()
        {
            this.LoadIconTextures();
            this.FindSerializedProperties();

            // Ensure only one callback has been registered.
            EditorApplication.update -= OnEditorApplicationUpdate;
            EditorApplication.update += OnEditorApplicationUpdate;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorApplicationUpdate;
        }

        public override void OnInspectorGUI()
        {
            this.InitializeGUIStyles();

            this.serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            this.DrawStereoRigRegion();
            this.DrawGlassesRegion();
            this.DrawStylusRegion();

            this.serializedObject.ApplyModifiedProperties();
        }

        private void DrawStereoRigRegion()
        {
            VoxelCore core = (VoxelCore)this.target;

            isStereoRigSectionExpanded = this.DrawSectionHeader("Stereo Rig", cameraIconTexture, isStereoRigSectionExpanded);
            if (isStereoRigSectionExpanded)
            {
                EditorGUILayout.HelpBox(
                    "CenterObject指相机关注中心点;SceneCamera指当前使用的相机;ViewSize指相机视野大小 ",
                    MessageType.Info);
                EditorGUILayout.PropertyField(this.centerObject, new GUIContent("Center Object"));
                EditorGUILayout.PropertyField(this.sceneCamera, new GUIContent("Scene Camera"));
                EditorGUILayout.PropertyField(this.WireDraw, new GUIContent("WireDraw"));
                EditorGUILayout.Slider(this.ipdProperty, 0.03f, 10f, new GUIContent("View Size"));
                this.DrawToggle("Head Tracking", this.isHeadTrackingEnabled);
                EditorGUILayout.Space();
            }
        }

        private void DrawGlassesRegion()
        {
            VoxelCore core = (VoxelCore)this.target;
            isGlassesSectionExpanded = this.DrawSectionHeader("Glasses", glassesIconTexture, isGlassesSectionExpanded);
            if (isGlassesSectionExpanded)
            {
                // Display pose information (readonly).
                if (core != null)
                {
                    this.DrawPoseInfo("Glass Pose:", core.sceneCamera.position, core.sceneCamera.rotation.eulerAngles);
                }
                else
                {
                    EditorGUILayout.LabelField("Tracker-Space Pose: Unknown");
                }
            }
        }

        private void DrawStylusRegion()
        {
            VoxelCore core = (VoxelCore)this.target;

            isStylusSectionExpanded = this.DrawSectionHeader("Stylus", stylusIconTexture, isStylusSectionExpanded);
            if (isStylusSectionExpanded)
            {
                EditorGUILayout.PropertyField(this.stylusGameObjectProperty, new GUIContent("Stylus"));
                this.stylusLengthProperty.floatValue = EditorGUILayout.FloatField("Length", this.stylusLengthProperty.floatValue);
                this.stylusWidthProperty.floatValue = EditorGUILayout.FloatField("Width", this.stylusWidthProperty.floatValue);
                EditorGUILayout.Space();

                if (core != null)
                {
                    this.DrawPoseInfo("Stylus Pose:", core.pose.Position, core.pose.Rotation.eulerAngles);
                }
                else
                {
                    EditorGUILayout.LabelField("Tracker-Space Pose: Unknown");
                }

                this.DrawToggle("Enable Mouse Auto-Hide", this.enableMouseAutoHideProperty);
                EditorGUILayout.Slider(this.mouseAutoHideDelayProperty, 0.0f, 60.0f, new GUIContent("Mouse Auto-Hide Delay"));
                EditorGUILayout.Space();
            }
        }

        private void DrawPoseInfo(string label, Vector3 position, Vector3 rotation)
        {
            EditorGUILayout.LabelField(new GUIContent(label));

            // Readonly.
            GUI.enabled = false;
            EditorGUILayout.Vector3Field(new GUIContent("Position"), position);
            EditorGUILayout.Vector3Field(new GUIContent("Rotation"), rotation);
            EditorGUILayout.Space();
            GUI.enabled = true;
        }

        private void OnEditorApplicationUpdate()
        {
            // Only force the inspector to update/repaint if a section
            // with dynamically changing data is expanded (i.e. glasses,
            // stylus, or display).
            if (isGlassesSectionExpanded ||
                isStylusSectionExpanded)
            {
                if (updateFrameCount >= 60)
                {
                    EditorUtility.SetDirty(this.target);
                    updateFrameCount = 0;
                }
                ++updateFrameCount;
            }
        }

        private bool DrawSectionHeader(string name, Texture2D icon, bool isExpanded)
        {
            // Create the divider line.
            GUILayout.Box(GUIContent.none, lineStyle, GUILayout.ExpandWidth(true), GUILayout.Height(1.0f));

            // Create the foldout (AKA expandable section).
            Rect position = GUILayoutUtility.GetRect(40.0f, 2000.0f, 16.0f, 16.0f, foldoutStyle);
            isExpanded = EditorGUI.Foldout(position, isExpanded, new GUIContent(" " + name, icon), true, foldoutStyle);

            return isExpanded;
        }

        private void DrawToggle(string label, SerializedProperty property)
        {
            property.boolValue = EditorGUILayout.Toggle(new GUIContent(label), property.boolValue);
        }

        private void DrawSection(string label, Vector3 position, Vector3 rotation)
        {
            EditorGUILayout.LabelField(new GUIContent(label));

            // Readonly.
            GUI.enabled = false;
            EditorGUILayout.Vector3Field(new GUIContent("Position"), position);
            EditorGUILayout.Vector3Field(new GUIContent("Rotation"), rotation);
            EditorGUILayout.Space();
            GUI.enabled = true;
        }

        private void InitializeGUIStyles()
        {
            if (foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fontStyle = FontStyle.Bold;
                foldoutStyle.fixedWidth = 2000.0f;
            }

            if (lineStyle == null)
            {
                lineStyle = new GUIStyle(GUI.skin.box);
                lineStyle.border.top = 1;
                lineStyle.border.bottom = 1;
                lineStyle.margin.top = 1;
                lineStyle.margin.bottom = 1;
                lineStyle.padding.top = 1;
                lineStyle.padding.bottom = 1;
            }
        }

        private void FindSerializedProperties()
        {
            // Stereo Rig Properties:
            this.centerObject = this.serializedObject.FindProperty("centerObject");
            this.sceneCamera = this.serializedObject.FindProperty("sceneCamera");
            this.WireDraw = this.serializedObject.FindProperty("WireDraw");
            this.ipdProperty = this.serializedObject.FindProperty("viewSize");
            this.isHeadTrackingEnabled = this.serializedObject.FindProperty("isHeadTrackingEnabled");

            // Stylus Properties:
            this.enableMouseAutoHideProperty = this.serializedObject.FindProperty("enableMouseAutoHide");
            this.mouseAutoHideDelayProperty = this.serializedObject.FindProperty("mouseAutoHideDelay");

            this.stylusGameObjectProperty = this.serializedObject.FindProperty("stylusObject");
            this.stylusLengthProperty = this.serializedObject.FindProperty("stylusLength");
            this.stylusWidthProperty = this.serializedObject.FindProperty("stylusWidth");
        }

        private void LoadIconTextures()
        {
            if (cameraIconTexture == null)
            {
                cameraIconTexture = this.LoadIconTexture("CameraIcon.png");
            }

            if (glassesIconTexture == null)
            {
                glassesIconTexture = this.LoadIconTexture("GlassesIcon.png");
            }

            if (stylusIconTexture == null)
            {
                stylusIconTexture = this.LoadIconTexture("StylusIcon.png");
            }
        }

        private Texture2D LoadIconTexture(string iconName)
        {
            return AssetDatabase.LoadAssetAtPath(INSPECTOR_ICON_PATH + iconName, typeof(Texture2D)) as Texture2D;
        }
    }
}