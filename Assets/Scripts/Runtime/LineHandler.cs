using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class PathHandler : MonoBehaviour
    {
        // // Prefab to instantiate a line from the anchor to the path point
        // [SerializeField]
        // private GameObject pathPrefab;

        // The path line renderer
        [SerializeField]
        private LineRenderer pathLineRendererPrefab;

        private LineRenderer pathLineRenderer;


        private Boolean isInitialized = false;



        public void SetRotation(System.Single value)
        {
            if (pathLineRenderer == null || !isInitialized) return;
            if (value < 0 || value > 360)
            {
                Debug.LogError("Rotation value must be between 0 and 360");
                return;
            }
            Debug.Log("Setting rotation to " + value);

            // // Get the first two points to calculate rotation axis
            // Vector3[] points = new Vector3[2];
            // pathLineRenderer.GetPositions(points);

            // // Calculate rotation axis from first two points
            // Vector3 rotationAxis = (points[1] - points[0]).normalized;

            // // Create rotation quaternion
            // Quaternion rotation = Quaternion.AngleAxis(value, rotationAxis);

            // // Set the pivot point to the first point
            // pathLineRenderer.transform.position = points[0];

            // // Apply rotation to the LineRenderer's transform
            // pathLineRenderer.transform.rotation = rotation;

            Vector3[] points = getOriginalPoints(pathLineRendererPrefab);

            // Get the axis of rotation (direction from point 0 to point 1)
            Vector3 rotationAxis = (points[1] - points[0]).normalized;

            // Create rotation quaternion around the axis
            Quaternion rotation = Quaternion.AngleAxis(value, rotationAxis);

            // Rotate all points except the first two
            for (int i = 2; i < pathLineRenderer.positionCount; i++)
            {
                // Translate point to origin relative to first point
                Vector3 pointRelativeToFirst = points[i] - points[0];
                // Apply rotation
                Vector3 rotatedPoint = rotation * pointRelativeToFirst;
                // Translate back
                points[i] = rotatedPoint + points[0];
            }

            // Apply the modified positions
            pathLineRenderer.SetPositions(points);
        }

        public void destroyPath()
        {
            if (pathLineRenderer != null)
            {
                // Destroy the path line renderer
                Destroy(pathLineRenderer.gameObject);
            }
        }

        void OnDisable()
        {
            destroyPath();
        }

        private static Vector3[] getOriginalPoints(LineRenderer lineRenderer)
        {
            Vector3[] points = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(points);
            return points;
        }

        public void UpdatePath(List<ARAnchor> anchors)
        {
            if (anchors.Count < 2) return;

            // Create a new path line renderer if it doesn't exist
            if (pathLineRenderer == null)
            {
                pathLineRenderer = Instantiate(pathLineRendererPrefab);
            }

            Vector3[] originalPoints = getOriginalPoints(pathLineRendererPrefab);

            Vector3 originalDirection = originalPoints[1] - originalPoints[0];
            Vector3 targetDirection = anchors[1].transform.position - anchors[0].transform.position;

            // Ensure we're using local space
            pathLineRenderer.useWorldSpace = false;

            // Calculate and apply scale
            float scale = targetDirection.magnitude / originalDirection.magnitude;
            pathLineRenderer.transform.localScale = new Vector3(scale, scale, scale);

            // Calculate and apply rotation
            Quaternion rotation = Quaternion.FromToRotation(originalDirection, targetDirection);
            pathLineRenderer.transform.rotation = rotation;

            // Set the origin of the coordinate system of the path line renderer to be the first anchor
            pathLineRenderer.transform.position = anchors[0].pose.position - pathLineRenderer.transform.TransformPoint(originalPoints[0]);

            // Assert that the world coordinates of the first point of the path line renderer are the same as the world coordinates of the first anchor
            Debug.Assert(pathLineRenderer.transform.TransformPoint(originalPoints[0]) == anchors[0].transform.position
                , "" + pathLineRenderer.transform.TransformPoint(originalPoints[0]) + " != " + anchors[0].transform.position);

            // Set the path line renderer to be active
            pathLineRenderer.gameObject.SetActive(true);
            isInitialized = true;
            Debug.Log("Trying to update path");
        }


    }
}

