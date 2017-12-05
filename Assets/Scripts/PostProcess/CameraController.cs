using UnityEngine;
using System.Collections;

namespace RTGizmos.Utils.CameraUtils.Navigation
{

    [AddComponentMenu("Camera-Control/Scene Camera Controller")]
    public class CameraController : MonoBehaviour
    {
        //
        //Filename: maxCamera.cs
        //
        // original: http://www.unifycommunity.com/wiki/index.php?title=MouseOrbitZoom
        //
        // --01-18-2010 - create temporary target, if none supplied at start

        enum FocusObject
        {
            Target,
            This
        }

        public float Distance
        {
            set
            {
                this.distance = value;
                this.maxDistance = value * this.maxDistanceFactor;
                this.minDistance = value * this.minDistanceFactor;
            }
        }

        public Vector3 targetOffset;
        public float distance = 5.0f;
        public float maxDistanceFactor = 5;
        public float minDistanceFactor = .5f;
        public float xSpeed = 200.0f;
        public float ySpeed = 200.0f;
        public int yMinLimit = -80;
        public int yMaxLimit = 80;
        public int zoomRate = 40;
        public float panSpeed = 0.1f;
        public float moveSpeed = 0.1f;
        public float zoomDampening = 5.0f;


        private float t_xDeg = 0.0f;
        private float t_yDeg = 0.0f;
        private float xDeg = 0.0f;
        private float yDeg = 0.0f;
        private float xDel = 0.0f;
        private float yDel = 0.0f;
        private float zDel = 0.0f;
        private float currentDistance;
        private float desiredDistance;
        private float maxDistance = 2f;
        private float minDistance = .6f;
        private Quaternion rotation;
        private Vector3 position;
        private Transform target;
        private FocusObject focus;

        void Start() { }
        void OnEnable() { Init(); }

        public void Init(Transform target = null)
        {
            //If there is no target, create a temporary target at 'distance' from the cameras current viewpoint
            if (this.target)
            {
                Destroy(this.target.gameObject);
            }

            GameObject go = new GameObject("Cam Target");
            go.transform.position = (target) ? target.transform.position : transform.position + (transform.forward * distance);
            this.target = go.transform;
            if (this.transform.parent)
            {
                this.target.transform.parent = this.transform.parent;
            }

            Distance = Vector3.Distance(transform.position, this.target.position);
            currentDistance = distance;
            desiredDistance = distance;

            //be sure to grab the current rotations as starting points.
            position = transform.position;
            rotation = transform.rotation;

            focus = FocusObject.Target;
        }

        /*
         * Camera logic on LateUpdate to only update after all character movement logic has been handled. 
         */
        void LateUpdate()
        {
            if (Input.GetMouseButtonDown(1))
            {
                focus = FocusObject.This;
            }
            if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonUp(1))
            {
                focus = FocusObject.Target;
            }

            // If Control and Alt and Middle button? ZOOM!
            if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftControl))
            {
                desiredDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * zoomRate * 0.125f * Mathf.Abs(desiredDistance);
            }
            // If middle mouse and left alt are selected? ORBIT
            else if (Input.GetMouseButton(2) && Input.GetKey(KeyCode.LeftAlt))
            {
                xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

                //Clamp the vertical axis for the orbit
                yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);
            }

            // otherwise if middle mouse is selected, we pan by way of transforming the target in screenspace
            else if (Input.GetMouseButton(2))
            {
                //grab the rotation of the camera so we can move in a psuedo local XY space
                target.rotation = transform.rotation;
                xDel += Input.GetAxis("Mouse X") * panSpeed;
                yDel += Input.GetAxis("Mouse Y") * panSpeed;
            }

            // First Person like flying controls
            else if (Input.GetMouseButton(1))
            {
                target.rotation = transform.rotation;
                zDel -= Input.GetAxis("Vertical") * moveSpeed;
                xDel -= Input.GetAxis("Horizontal") * moveSpeed;

                t_xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                t_yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
                t_yDeg = ClampAngle(t_yDeg, yMinLimit, yMaxLimit);
            }

            ////////Orbit Position

            // affect the desired Zoom distance if we roll the scrollwheel
            desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);
            //clamp the zoom min/max
            desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
            // For smoothing of the zoom, lerp distance
            currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

            switch (focus)
            {
                case FocusObject.Target:
                    // apply rotation with lerp.
                    ApplyRotation();
                    break;
                case FocusObject.This:
                    // apply target rotation with lerp.
                    ApplyTargetRotation();
                    break;
            }


            // apply the pan with lerp.
            var xOffset = Mathf.Lerp(0, xDel, Time.deltaTime * zoomDampening);
            xDel -= xOffset;
            var yOffset = Mathf.Lerp(0, yDel, Time.deltaTime * zoomDampening);
            yDel -= yOffset;
            var zOffset = Mathf.Lerp(0, zDel, Time.deltaTime * zoomDampening);
            zDel -= zOffset;

            target.Translate(Vector3.right * -xOffset);
            target.Translate(Vector3.forward * -zOffset);
            target.Translate(transform.up * -yOffset, Space.World);


            // calculate position based on the new currentDistance 
            position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);
            transform.position = position;
        }

        private float AngleWithXZPlane(Transform t)
        {
            var u = t.forward;
            var n = Vector3.up;
            var dot = Vector3.Dot(n, u);
            var a = Mathf.Rad2Deg * Mathf.Asin(Mathf.Abs(dot) / (n.magnitude * u.magnitude)); // Angle Between transform.forward and XZ plane.
            var sign = (dot < 0) ? -1 : 1;
            a *= sign;
            return a;
        }

        private void ApplyRotation()
        {
            var rot_x = Mathf.Lerp(0, xDeg, Time.deltaTime * zoomDampening);
            xDeg -= rot_x;
            var rot_y = Mathf.Lerp(0, yDeg, Time.deltaTime * zoomDampening);
            yDeg -= rot_y;
            var nextStep = AngleWithXZPlane(transform) - rot_y;

            if (Mathf.Clamp(nextStep, yMinLimit, yMaxLimit) != nextStep)
            {
                yDeg = 0;
                rot_y = 0;
            }
            rotation = Quaternion.AngleAxis(rot_x, Vector3.up) * transform.rotation;
            rotation *= Quaternion.AngleAxis(rot_y, Vector3.right);
            transform.rotation = rotation;
        }

        private void ApplyTargetRotation()
        {
            var targetRot_X = Mathf.Lerp(0, t_xDeg, Time.deltaTime * zoomDampening);
            t_xDeg -= targetRot_X;
            var targetRot_Y = Mathf.Lerp(0, t_yDeg, Time.deltaTime * zoomDampening);
            t_yDeg -= targetRot_Y;
            var nextStep = AngleWithXZPlane(transform) - targetRot_Y;

            if (Mathf.Clamp(nextStep, yMinLimit, yMaxLimit) != nextStep)
            {
                t_yDeg = 0;
                targetRot_Y = 0;
            }
            rotation = Quaternion.AngleAxis(targetRot_X, Vector3.up) * transform.rotation;
            rotation *= Quaternion.AngleAxis(targetRot_Y, Vector3.right);
            target.position = position + (rotation * Vector3.forward * currentDistance + targetOffset);
            transform.rotation = rotation;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }
    }
}