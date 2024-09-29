using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace EditorUtilities
{
    public sealed class CameraSnapper : EditorWindow
    {
        private Vector3 targetPosition;
        private Vector3 targetRotation;
        private int targetFOV;

        private Camera cameraData;

        [MenuItem("Tools/Camera Snapper")]
        public static void ShowWindow()
        {
            GetWindow<CameraSnapper>("Camera Snapper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Snap Camera", EditorStyles.boldLabel);

            targetPosition = EditorGUILayout.Vector3Field("Target Position", targetPosition);

            targetRotation = EditorGUILayout.Vector3Field("Target Rotation", targetRotation);

            targetFOV = EditorGUILayout.IntField("Target FOV", targetFOV);

            if (GUILayout.Button("Snap Camera"))
            {
                SnapCamera();
            }

            EditorGUILayout.Space(30);

            cameraData  = (Camera)EditorGUILayout.ObjectField("Camera Data", cameraData, typeof(Camera), true);

            if (GUILayout.Button("Copy Camera Data"))
            {
                CopyCameraData();
            }
        }

        private void SnapCamera()
        {
            if (!TryGetSceneView(out var sceneView)) { return; }

            sceneView.pivot = targetPosition;
            sceneView.rotation = Quaternion.Euler(targetRotation);
            sceneView.cameraSettings.fieldOfView = targetFOV;
                
            sceneView.Repaint();
        }

        private void CopyCameraData()
        {
            if (cameraData == null) { return; }

            if (!TryGetSceneView(out var sceneView)) { return; }

            sceneView.pivot = cameraData.transform.position;
            sceneView.rotation = cameraData.transform.rotation;
            sceneView.cameraSettings.fieldOfView = cameraData.fieldOfView;
            sceneView.Repaint();
        }

        private bool TryGetSceneView(out SceneView sceneView)
        {
            sceneView = SceneView.lastActiveSceneView;

            return sceneView != null;
        }
    }
}