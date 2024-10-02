using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;

namespace MyEditorUtilities
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
            if (GUILayout.Button("Snap Position")) { SnapToPosition(); }

            targetRotation = EditorGUILayout.Vector3Field("Target Rotation", targetRotation);
            if (GUILayout.Button("Snap Rotation")) { SnapToRotation(); }

            targetFOV = EditorGUILayout.IntField("Target FOV", targetFOV);
            if (GUILayout.Button("Snap FOV")) { SnapToFOV(); }

            GUI.enabled = cameraData != null;
            //if (GUILayout.Button($"Snap Camera {cameraData == null ? \"(Need a camera)" : \"\"}")) { SnapCamera(); }
            GUI.enabled = true;


            EditorGUILayout.Space(30);


            cameraData  = (Camera)EditorGUILayout.ObjectField("Camera Data", cameraData, typeof(Camera), true);

            if (GUILayout.Button("Copy Camera Data")) { CopyCameraData(); }
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

        private void SnapToPosition()
        {
            if (!TryGetSceneView(out var sceneView)) { return; }

            sceneView.pivot = targetPosition;
            sceneView.Repaint();

        }

        private void SnapToRotation()
        {
            if (!TryGetSceneView(out var sceneView)) { return; }

            sceneView.rotation = Quaternion.Euler(targetRotation);
            sceneView.Repaint();
        }

        private void SnapToFOV()
        {
            if (!TryGetSceneView(out var sceneView)) { return; }

            sceneView.cameraSettings.fieldOfView = targetFOV;
            sceneView.Repaint();
        }
    }
}