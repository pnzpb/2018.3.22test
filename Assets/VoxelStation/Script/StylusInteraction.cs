
using UnityEngine;
using UnityEngine.EventSystems;

namespace VoxelStation.Core
{
    public interface StylusFunctionsInterface
    {
        void onStylusHoverUIBegin(ActionEventInfo info);
        void onStylusHoverUIEnd(ActionEventInfo info);
        void onStylusButtonOnePressedUI(ActionEventInfo info);
        void onStylusHoverObjectBegin(ActionEventInfo info);
        void onStylusHoverObjectEnd(ActionEventInfo info);
        void onStylusGrabObjectBegin(ActionEventInfo info);
        void onStylusGrabObjectUpdate(ActionEventInfo info);
        void onStylusGrabObjectEnd(ActionEventInfo info);
    }

    class StylusInteraction : MonoBehaviour, StylusFunctionsInterface
    {
        #region IMPLEMENTED INTERFACE STYLUSFUNCTIONS
        public void onStylusButtonOnePressedUI(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus button one is pressed on UI
          //  Debug.Log("onStylusButtonOnePressedUI");
        }

        public void onStylusGrabObjectBegin(ActionEventInfo info)
        {
            //Debug.Log("onStylusGrabObjecBegin");
            BeginGrab(info.actionObject, info.length, info.pose);
            VoxelCore.Instance.SetStylusFeature(StylusFeature.Vibration, 50);
        }

        public void onStylusGrabObjectEnd(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus grab of object ends
          //  Debug.Log("onStylusGrabObjectEnd");
        }

        public void onStylusGrabObjectUpdate(ActionEventInfo info)
        {
            //Debug.Log("onStylusGrabObjectUpdate");
            UpdateGrab(info.actionObject, info.pose);
        }

        public void onStylusHoverObjectBegin(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus hovers on a object begins
           // Debug.Log("onStylusHoverObjectBegin");
          //  Material m = info.actionObject.GetComponent<Renderer>().material;
          //  Color c = Color.red;
          //  m.color = c;
        }

        public void onStylusHoverObjectEnd(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus hover ends
          //  Debug.Log("onStylusHoverObjectEnd");
         //   Material m = info.actionObject.GetComponent<Renderer>().material;
         //   Color c = Color.white;
         //   m.color = c;
        }

        public void onStylusHoverUIBegin(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus hover on UI begins
         //   Debug.Log("onStylusHoverUIBegin");
            EventSystem.current.SetSelectedGameObject(info.actionObject);
        }

        public void onStylusHoverUIEnd(ActionEventInfo info)
        {
            //TODO: Implement on what happens when stylus hover on UI ends
           // Debug.Log("onStylusHoverUIEnd");
            EventSystem.current.SetSelectedGameObject(null);
        }
        #endregion

        private float initialGrabDistance = 0.0f;
        private Vector3 initialGrabOffset = Vector3.zero;
        private Quaternion initialGrabRotation = Quaternion.identity;

        private void BeginGrab(GameObject hitObject, float hitDistance, Pose pose)
        {
            Vector3 inputEndPosition = pose.Position + (pose.Rotation * (Vector3.forward * hitDistance));            
            initialGrabOffset = Quaternion.Inverse(hitObject.transform.rotation) * (hitObject.transform.position - inputEndPosition);
            initialGrabRotation = Quaternion.Inverse(pose.Rotation) * hitObject.transform.rotation;
            initialGrabDistance = hitDistance;
        }

        private void UpdateGrab(GameObject hitObject, Pose pose)
        {
            Vector3 InputEndPosition = pose.Position + (pose.Rotation * (Vector3.forward * initialGrabDistance));
            // Update the grab object's rotation.
            Quaternion objectRotation = pose.Rotation * initialGrabRotation;
            hitObject.transform.rotation = objectRotation;
            // Update the grab object's position.
            Vector3 objectPosition = InputEndPosition + (objectRotation * initialGrabOffset);
            hitObject.transform.position = objectPosition;
        }

        void Start()
        {
            //Calls on grabs the object of the tag at begin,update & end
            VoxelCore.Instance.onStylusGrabObjectBegin += onStylusGrabObjectBegin;
            VoxelCore.Instance.onStylusGrabObjectUpdate += onStylusGrabObjectUpdate;
            VoxelCore.Instance.onStylusGrabObjectEnd += onStylusGrabObjectEnd;

            //Calls on UI hover begin & end for objects
            VoxelCore.Instance.onStylusHoverObjectBegin += onStylusHoverObjectBegin;
            VoxelCore.Instance.onStylusHoverObjectEnd += onStylusHoverObjectEnd;

            //Calls on UI hover begin & end for UI
            VoxelCore.Instance.onStylusHoverUIBegin += onStylusHoverUIBegin;
            VoxelCore.Instance.onStylusHoverUIEnd += onStylusHoverUIEnd;
            
            //Calls on when hoverig over a button and pressed stylus button one
            VoxelCore.Instance.onStylusButtonOnePressedUI += onStylusButtonOnePressedUI;
        }

        void Update() { }
    }
}
