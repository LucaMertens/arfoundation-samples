using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ModeManager : MonoBehaviour
    {
        [SerializeField] private Canvas moderatorCanvas;
        [SerializeField] private Canvas evaluationCanvas;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARPointCloudManager pointCloudManager;
        [SerializeField] private XROrigin xROrigin;
        [SerializeField] private PathManager pathManager;
        [SerializeField] private TMP_Text pathTitle;
        [SerializeField] private List<LineRenderer> pathLineRendererPrefabs;
        private int currentPathIndex = 0;

        private bool inModeratorMode = true;


        void Start()
        {
            Debug.Log("Initializing mode manager");
            SetMode(true);
            changePathModelAndText(pathLineRendererPrefabs[currentPathIndex]);
            OnEnable(); // Just to be sure
        }

        void OnEnable()
        {
            InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
        }

        private void Update()
        {
            int touchCount = InputSystem.EnhancedTouch.Touch.activeTouches.Count;
            if (touchCount > 0) Debug.Log("Touch count: " + touchCount);
            if (!inModeratorMode && (touchCount == 6))
            {
                SwitchToModeratorMode();
                // Check if all 6 touches started in this frame
                // bool allTouchesStarted = true;
                // for (int i = 0; i < touchCount; i++) {
                //     if (Input.GetTouch(i).phase != TouchPhase.Began) {
                //         allTouchesStarted = false;
                //         break;
                //     }
                // }

                // if (allTouchesStarted) {
                //     SetMode(true);
                // }
            }
        }

        private void changePathModelAndText(LineRenderer newPathLineRendererPrefab)
        {
            pathManager.changePathModel(newPathLineRendererPrefab);
            pathTitle.text = newPathLineRendererPrefab.name;
        }

        public void SwitchToEvalMode()
        {
            Debug.Log("Switching to evaluation mode");
            SetMode(false);
        }

        public void SwitchToModeratorMode()
        {
            Debug.Log("Switching to moderator mode");
            SetMode(true);
        }

        public void SelectNextPath()
        {
            if (pathLineRendererPrefabs.Count <= 1) return;
            Debug.Log("Switching to next path");
            currentPathIndex = (currentPathIndex + 1) % pathLineRendererPrefabs.Count;
            changePathModelAndText(pathLineRendererPrefabs[currentPathIndex]);
        }

        private void SetMode(bool moderatorMode)
        {
            if (inModeratorMode == moderatorMode) return;

            inModeratorMode = moderatorMode;
            moderatorCanvas.enabled = moderatorMode;
            evaluationCanvas.enabled = !moderatorMode;

            // Remove all planes and feature points

            // foreach (var plane in planeManager.trackables)
            // {
            //     plane.gameObject.SetActive(moderatorMode);
            // }

            // foreach (var pointCloud in pointCloudManager.trackables)
            // {
            //     pointCloud.gameObject.SetActive(moderatorMode);
            // }

            xROrigin.TrackablesParent.gameObject.SetActive(moderatorMode);

            // Disable plane and feature point visualization in eval mode
            // planeManager.enabled = moderatorMode;
            pointCloudManager.enabled = moderatorMode;


        }

    }
}