using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Experimental.Rendering
{
    [ExecuteAlways]
    public class FreeCamera : MonoBehaviour
    {
        public float m_LookSpeedController = 120f;
        public float m_LookSpeedMouse = 10.0f;
        public float m_MoveSpeed = 10.0f;
        public float m_MoveSpeedIncrement = 2.5f;
        public float m_Turbo = 10.0f;

        private static string kMouseX = "Mouse X";
        private static string kMouseY = "Mouse Y";
        private static string kRightStickX = "Controller Right Stick X";
        private static string kRightStickY = "Controller Right Stick Y";
        private static string kVertical = "Vertical";
        private static string kHorizontal = "Horizontal";

        private static string kYAxis = "YAxis";
        private static string kSpeedAxis = "Speed Axis";

        void OnEnable()
        {
            RegisterInputs();
        }

        void RegisterInputs()
        {
#if UNITY_EDITOR
            List<InputManagerEntry> inputEntries = new List<InputManagerEntry>();

            // Add new bindings
            inputEntries.Add(new InputManagerEntry { name = kRightStickX, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fourth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f });
            inputEntries.Add(new InputManagerEntry { name = kRightStickY, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Fifth, sensitivity = 1.0f, gravity = 1.0f, deadZone = 0.2f, invert = true });

            inputEntries.Add(new InputManagerEntry { name = kYAxis, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "page up", altBtnPositive = "joystick button 5", btnNegative = "page down", altBtnNegative = "joystick button 4", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });

            inputEntries.Add(new InputManagerEntry { name = kSpeedAxis, kind = InputManagerEntry.Kind.KeyOrButton, btnPositive = "home", btnNegative = "end", gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });
            inputEntries.Add(new InputManagerEntry { name = kSpeedAxis, kind = InputManagerEntry.Kind.Axis, axis = InputManagerEntry.Axis.Seventh, gravity = 1000.0f, deadZone = 0.001f, sensitivity = 1000.0f });

            InputRegistering.RegisterInputs(inputEntries);
#endif
        }

        void Update()
        {
            // If the debug menu is running, we don't want to conflict with its inputs.
            if (DebugManager.instance.displayRuntimeUI)
                return;

            float inputRotateAxisX = 0.0f;
            float inputRotateAxisY = 0.0f;
            if (Input.GetMouseButton(1))
            {
                inputRotateAxisX = Input.GetAxis(kMouseX) * m_LookSpeedMouse;
                inputRotateAxisY = Input.GetAxis(kMouseY) * m_LookSpeedMouse;
            }
            inputRotateAxisX += (Input.GetAxis(kRightStickX) * m_LookSpeedController * Time.deltaTime);
            inputRotateAxisY += (Input.GetAxis(kRightStickY) * m_LookSpeedController * Time.deltaTime);

            float inputChangeSpeed = Input.GetAxis(kSpeedAxis);
            if (inputChangeSpeed != 0.0f)
            {
                m_MoveSpeed += inputChangeSpeed * m_MoveSpeedIncrement;
                if (m_MoveSpeed < m_MoveSpeedIncrement) m_MoveSpeed = m_MoveSpeedIncrement;
            }

            float inputVertical = Input.GetAxis(kVertical);
            float inputHorizontal = Input.GetAxis(kHorizontal);
            float inputYAxis = Input.GetAxis(kYAxis);

            bool moved = inputRotateAxisX != 0.0f || inputRotateAxisY != 0.0f || inputVertical != 0.0f || inputHorizontal != 0.0f || inputYAxis != 0.0f;
            if (moved)
            {
                float rotationX = transform.localEulerAngles.x;
                float newRotationY = transform.localEulerAngles.y + inputRotateAxisX;

                // Weird clamping code due to weird Euler angle mapping...
                float newRotationX = (rotationX - inputRotateAxisY);
                if (rotationX <= 90.0f && newRotationX >= 0.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 0.0f, 90.0f);
                if (rotationX >= 270.0f)
                    newRotationX = Mathf.Clamp(newRotationX, 270.0f, 360.0f);

                transform.localRotation = Quaternion.Euler(newRotationX, newRotationY, transform.localEulerAngles.z);

                float moveSpeed = Time.deltaTime * m_MoveSpeed;
                if (Input.GetMouseButton(1))
                    moveSpeed *= Input.GetKey(KeyCode.LeftShift) ? m_Turbo : 1.0f;
                else
                    moveSpeed *= Input.GetAxis("Fire1") > 0.0f ? m_Turbo : 1.0f;
                transform.position += transform.forward * moveSpeed * inputVertical;
                transform.position += transform.right * moveSpeed * inputHorizontal;
                transform.position += Vector3.up * moveSpeed * inputYAxis;
            }
        }
    }
}
