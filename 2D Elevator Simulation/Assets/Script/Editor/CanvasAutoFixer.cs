#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSim.Editor
{
    [InitializeOnLoad]
    public class CanvasAutoFixer
    {
        static CanvasAutoFixer()
        {
            // Run this check once whenever the scene hierarchy changes or loads
            EditorApplication.hierarchyChanged += CheckCanvases;
        }

        private static void CheckCanvases()
        {
            if (Application.isPlaying) return;

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            bool fixedAny = false;

            foreach (Canvas canvas in canvases)
            {
                // Fix missing CanvasScaler
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    Debug.Log($"Antigravity: Auto-added missing CanvasScaler to '{canvas.name}'.");
                    EditorUtility.SetDirty(canvas.gameObject);
                    fixedAny = true;
                }

                // Fix resolution scaling
                if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                    Debug.Log($"Antigravity: Auto-fixed CanvasScaler on '{canvas.name}' to scale perfectly with screen (1920x1080).");
                    EditorUtility.SetDirty(canvas.gameObject);
                    fixedAny = true;
                }

                // Fix missing GraphicRaycaster (needed for button clicks)
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"Antigravity: Auto-added missing GraphicRaycaster to '{canvas.name}' so buttons can be clicked.");
                    EditorUtility.SetDirty(canvas.gameObject);
                    fixedAny = true;
                }
            }
            
            if (fixedAny)
            {
                // Clean up and mark scene as needing a save
                EditorApplication.hierarchyChanged -= CheckCanvases;
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }
        }
    }
}
#endif
