#if UNITY_EDITOR
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Data.Audio;
using UnityEditor;

namespace _PawSlidePopGame._Scripts.Utils.UI
{
    [CustomEditor(typeof(UIButtonSound))]
    [CanEditMultipleObjects]
    public class UIButtonSoundEditor : Editor
    {
        private SerializedProperty _soundTypeProp;
        private SerializedProperty _customClipProp;
        private SerializedProperty _volumeProp;
        private SerializedProperty _playVibrationProp;

        private void OnEnable()
        {
            _soundTypeProp = serializedObject.FindProperty("soundType");
            _customClipProp = serializedObject.FindProperty("customClip");
            _volumeProp = serializedObject.FindProperty("volumeScale");
            _playVibrationProp = serializedObject.FindProperty("playVibration");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_soundTypeProp);

            UISoundType type = (UISoundType)_soundTypeProp.enumValueIndex;

            // Chỉ hiện Custom Clip khi chọn chế độ Custom
            if (type == UISoundType.Custom)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("Custom Mode: Kéo Audio riêng biệt vào đây.", MessageType.Info);
                EditorGUILayout.PropertyField(_customClipProp);
                EditorGUILayout.PropertyField(_volumeProp);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(_playVibrationProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif