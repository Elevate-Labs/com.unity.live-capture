using UnityEditor;
using UnityEngine;

namespace Unity.LiveCapture
{
    /// <summary>
    /// Default Inspector for <see cref="LiveCaptureDevice"/>.
    /// </summary>
    [CustomEditor(typeof(LiveCaptureDevice), true)]
    public class LiveCaptureDeviceEditor : Editor
    {
        static class Contents
        {
            public static readonly GUIContent takeRecorderNotFound = EditorGUIUtility.TrTextContent($"{nameof(TakeRecorder)} not found. " +
                $"Place the device as a child of a {nameof(TakeRecorder)} component in the hierarchy.");
        }

        static readonly string[] s_ExcludeProperties = { "m_Script" };

        /// <summary>
        /// Called when the editor is being initialized.
        /// </summary>
        protected virtual void OnEnable()
        {
        }

        /// <summary>
        /// Makes a custom inspector GUI for <see cref="LiveCaptureDevice"/>.
        /// </summary>
        public override void OnInspectorGUI()
        {
            DoTakeRecorderHelpBox();
            OnDeviceGUI();
        }

        /// <summary>
        /// Draws the Inspector for this device.
        /// </summary>
        protected virtual void OnDeviceGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, s_ExcludeProperties);

            serializedObject.ApplyModifiedProperties();
        }

        void DoTakeRecorderHelpBox()
        {
            var device = target as LiveCaptureDevice;

            if (device.GetTakeRecorder() == null)
            {
                EditorGUILayout.HelpBox(Contents.takeRecorderNotFound.text, MessageType.Warning);
            }
        }
    }
}