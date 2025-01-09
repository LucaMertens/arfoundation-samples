using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ModeManager : MonoBehaviour
    {
        [SerializeField] private Canvas moderatorCanvas;
        [SerializeField] private Canvas evaluationCanvas;
        [SerializeField] private ARPlaneManager planeManager;

        private bool inModeratorMode = true;

        void Start()
        {
            Debug.Log("Initializing mode manager");
            SetMode(true);
        }

        public void SwitchToEvalMode()
        {
            Debug.Log("Switching to evaluation mode");
            SetMode(false);
        }

        public void CheckForModeratorModeSwitch()
        {
            // TODO
            if (Input.GetKey(KeyCode.Escape))
            {
                SetMode(true);
            }
        }

        private void SetMode(bool moderatorMode)
        {
            if (inModeratorMode == moderatorMode) return;

            inModeratorMode = moderatorMode;
            moderatorCanvas.enabled = moderatorMode;
            evaluationCanvas.enabled = !moderatorMode;

            // Disable plane and feature point visualization in eval mode
            planeManager.enabled = moderatorMode;


        }

    }
}