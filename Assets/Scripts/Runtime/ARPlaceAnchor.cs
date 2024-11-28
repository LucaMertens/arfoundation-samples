using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ARPlaceAnchor : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The enabled Anchor Manager in the scene.")]
        ARAnchorManager m_AnchorManager;

        [SerializeField]
        [Tooltip("The Scriptable Object Asset that contains the ARRaycastHit event.")]
        ARRaycastHitEventAsset m_RaycastHitEvent;

        Dictionary<TrackableId, ARAnchor> m_AnchorsByTrackableId = new();


        // Prefab to instantiate a line from the anchor to the path point
        [SerializeField]
        private GameObject linePrefab;

        // Prefab to instantiate above the last anchor
        [SerializeField]
        private GameObject pathPointPrefab;

        // The path line renderer
        [SerializeField]
        private LineRenderer pathLineRenderer;


        // The currently selected reference plane
        private ARPlane selectedPlane;

        // The currently selected anchors
        private List<ARAnchor> selectedAnchors = new List<ARAnchor>();




        public ARAnchorManager anchorManager
        {
            get => m_AnchorManager;
            set => m_AnchorManager = value;
        }

        public void RemoveAllAnchors()
        {
            foreach (var anchor in m_AnchorsByTrackableId.Values)
            {
                m_AnchorManager.TryRemoveAnchor(anchor);
            }
            m_AnchorsByTrackableId.Clear();
        }

        // Runs when the reset option is called in the context menu in-editor, or when first created.
        void Reset()
        {
            m_AnchorManager = FindAnyObjectByType<ARAnchorManager>();
        }

        void OnEnable()
        {

            if (m_AnchorManager == null)
                m_AnchorManager = FindAnyObjectByType<ARAnchorManager>();

            if ((m_AnchorManager ? m_AnchorManager.subsystem : null) == null)
            {
                enabled = false;
                Debug.LogWarning($"No XRAnchorSubsystem was found in {nameof(ARPlaceAnchor)}'s {nameof(m_AnchorManager)}, so this script will be disabled.", this);
                return;
            }

            if (m_RaycastHitEvent == null)
            {
                enabled = false;
                Debug.LogWarning($"{nameof(m_RaycastHitEvent)} field on {nameof(ARPlaceAnchor)} component of {name} is not assigned.", this);
                return;
            }

            m_RaycastHitEvent.eventRaised += CreateAnchor;
            m_AnchorManager.trackablesChanged.AddListener(OnAnchorsChanged);
        }

        void OnAnchorsChanged(ARTrackablesChangedEventArgs<ARAnchor> eventArgs)
        {
            // add any anchors that have been added outside our control, such as loading from storage
            foreach (var addedAnchor in eventArgs.added)
            {
                m_AnchorsByTrackableId.Add(addedAnchor.trackableId, addedAnchor);
            }

            // remove any anchors that have been removed outside our control, such as during a session reset
            foreach (var removedAnchor in eventArgs.removed)
            {
                if (removedAnchor.Value != null)
                {
                    m_AnchorsByTrackableId.Remove(removedAnchor.Key);
                    Destroy(removedAnchor.Value.gameObject);
                }
            }
        }

        void OnDisable()
        {
            if (m_RaycastHitEvent != null)
                m_RaycastHitEvent.eventRaised -= CreateAnchor;

            if (m_AnchorManager != null)
                m_AnchorManager.trackablesChanged.AddListener(OnAnchorsChanged);
        }

        void selectPlane(ARPlane plane)
        {
            selectedPlane = plane;
            HandlePathOriginUpdate();
        }

        void selectAnchor(ARAnchor anchor)
        {
            if (selectedAnchors.Count >= 2)
            {
                selectedAnchors.Clear();
            }

            selectedAnchors.Add(anchor);
            HandlePathOriginUpdate();
        }




        // Based on the orientation of the selected plane and the first n points of the path-line, render the path.
        // The plane is just there for orientation in space.
        // The path-line-renderer already contains points. The first n anchors are matched to the first n points of the path-line.
        void HandlePathOriginUpdate()
        {
            if (/* selectedPlane == null || */ selectedAnchors.Count < 2)
            {
                return;
            }


            Vector3[] originalPoints = new Vector3[pathLineRenderer.positionCount];
            pathLineRenderer.GetPositions(originalPoints);

            Vector3 originalDirection = originalPoints[1] - originalPoints[0];
            Vector3 targetDirection = selectedAnchors[1].transform.position - selectedAnchors[0].transform.position;

            // Ensure we're using local space
            pathLineRenderer.useWorldSpace = false;


            // Calculate and apply scale
            float scale = targetDirection.magnitude / originalDirection.magnitude;
            pathLineRenderer.transform.localScale = new Vector3(scale, scale, scale);

            // Calculate and apply rotation
            Quaternion rotation = Quaternion.FromToRotation(originalDirection, targetDirection);
            pathLineRenderer.transform.rotation = rotation;

            // Set the origin of the coordinate system of the path line renderer to be the first anchor
            pathLineRenderer.transform.position = selectedAnchors[0].pose.position - pathLineRenderer.transform.TransformPoint(originalPoints[0]);

            // Assert that the world coordinates of the first point of the path line renderer are the same as the world coordinates of the first anchor
            Debug.Assert(pathLineRenderer.transform.TransformPoint(originalPoints[0]) == selectedAnchors[0].transform.position
                , "" + pathLineRenderer.transform.TransformPoint(originalPoints[0]) + " != " + selectedAnchors[0].transform.position);

            // Set the path line renderer to be active
            pathLineRenderer.gameObject.SetActive(true);
            Debug.Log("Trying to update path");
        }

        /// <summary>
        /// Attempts to attach a new anchor to a hit `ARPlane` if supported.
        /// Otherwise, asynchronously creates a new anchor.
        /// </summary>
        void CreateAnchor(object sender, ARRaycastHit hit)
        {
            if (m_AnchorManager.descriptor.supportsTrackableAttachments && hit.trackable is ARPlane plane)
            {
                selectPlane(plane);
                AttachAnchorToTrackable(plane, hit);
            }
            else
            {
                CreateAnchorAsync(hit);
            }
        }

        void AttachAnchorToTrackable(ARPlane plane, ARRaycastHit hit)
        {
            var anchor = m_AnchorManager.AttachAnchor(plane, hit.pose);
            var arAnchorDebugVisualizer = anchor.GetComponent<ARAnchorDebugVisualizer>();
            if (arAnchorDebugVisualizer != null)
            {
                arAnchorDebugVisualizer.IsAnchorAttachedToTrackable = true;
                arAnchorDebugVisualizer.SetAnchorCreationMethod(true, hit.hitType);
            }
        }

        void CreateBaloon(ARAnchor anchor)
        {
            // Instantiate a new path point above the anchor, using the coordinate system of the anchor
            var pathPoint = Instantiate(pathPointPrefab, anchor.transform.position + new Vector3(0, 2, 0), Quaternion.identity);
            pathPoint.transform.parent = anchor.transform;

            // Draw a new line from the anchor point to the path point
            var line = Instantiate(linePrefab, anchor.transform.position, Quaternion.identity);
            line.transform.SetParent(anchor.transform); // Set the anchor as the parent of the line
            var lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, Vector3.zero); // Start point relative to the anchor
            lineRenderer.SetPosition(1, pathPoint.transform.localPosition); // End point relative to the anchor
            lineRenderer.useWorldSpace = false; // Use local space for the line
        }

        async void CreateAnchorAsync(ARRaycastHit hit)
        {
            var result = await m_AnchorManager.TryAddAnchorAsync(hit.pose);
            if (result.status.IsSuccess())
            {
                var anchor = result.value;
                var arAnchorDebugVisualizer = anchor.GetComponent<ARAnchorDebugVisualizer>();
                arAnchorDebugVisualizer?.SetAnchorCreationMethod(true, hit.hitType);

                selectAnchor(anchor);
                CreateBaloon(anchor);

            }
        }


    }
}
