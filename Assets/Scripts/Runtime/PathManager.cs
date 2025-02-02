using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class PathManager : MonoBehaviour
    {
        private LineRenderer pathLineRendererPrefab;


        private LineRenderer pathLineRenderer;


        private Boolean isInitialized = false;

        private List<ARAnchor> lastAnchors = null;

        private float currentRotation = 0f;


        public void changePathModel(LineRenderer newPathLineRendererPrefab)
        {
            destroyPath();
            pathLineRendererPrefab = newPathLineRendererPrefab;
            isInitialized = false;
            if (lastAnchors != null) UpdatePath(lastAnchors);
        }

        public void SetRotation(System.Single value)
        {
            if (pathLineRenderer == null || !isInitialized) return;
            if (value < 0 || value > 360)
            {
                Debug.LogError("Rotation value must be between 0 and 360");
                return;
            }
            Debug.Log("Setting rotation to " + value);


            // Get all current points
            Vector3[] points = new Vector3[pathLineRenderer.positionCount];
            pathLineRenderer.GetPositions(points);

            if (points.Length < 2) return;

            // Define rotation axis as the direction from first to second point
            Vector3 axis = points[1] - points[0];

            // Calculate the rotation difference
            float rotationDifference = value - currentRotation;

            // For each point after the second one
            for (int i = 2; i < points.Length; i++)
            {
                // Get point relative to the first point (rotation pivot)
                Vector3 relativePos = points[i] - points[0];

                // Create rotation quaternion around the axis
                Quaternion rotation = Quaternion.AngleAxis(rotationDifference, axis.normalized);

                // Apply rotation and set new position
                Vector3 newPos = points[0] + (rotation * relativePos);
                pathLineRenderer.SetPosition(i, newPos);
            }

            // Store the current rotation for next time
            currentRotation = value;
        }

        public void destroyPath()
        {
            if (pathLineRenderer != null)
            {
                // Destroy the path line renderer
                Destroy(pathLineRenderer.gameObject);
                pathLineRenderer = null;
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

            lastAnchors = anchors;

            // Create a new path line renderer if it doesn't exist
            if (pathLineRenderer == null)
            {
                pathLineRenderer = Instantiate(pathLineRendererPrefab);
            }

            // Ensure that the path line renderer is not shifted, scaled or rotated
            pathLineRenderer.transform.position = Vector3.zero;
            pathLineRenderer.transform.rotation = Quaternion.identity;
            pathLineRenderer.transform.localScale = Vector3.one;
            pathLineRenderer.useWorldSpace = true;

            Vector3[] originalPoints = getOriginalPoints(pathLineRendererPrefab);

            Vector3 originalDirection = originalPoints[1] - originalPoints[0];
            Vector3 targetDirection = anchors[1].transform.position - anchors[0].transform.position;

            // Calculate and apply scale
            float scale = targetDirection.magnitude / originalDirection.magnitude;

            // Calculate and apply rotation
            Quaternion rotation = Quaternion.FromToRotation(originalDirection, targetDirection);

            // Update all points
            for (int i = 0; i < pathLineRenderer.positionCount; i++)
            {
                // "Offset" from the new origin
                Vector3 relativePos = originalPoints[i] - originalPoints[0];
                // Vector3 newOrigin = anchors[0].transform.position;

                // // Scale and translate the point
                // Vector3 newPos = newOrigin + (relativePos * scale);

                // // Rotate the point
                // newPos = rotation * newPos;

                Vector3 newPos = anchors[0].transform.position + rotation * (relativePos * scale);

                pathLineRenderer.SetPosition(i, newPos);
            }

            // Assert that the world coordinates of the first point of the path line renderer are the same as the world coordinates of the first anchor
            Debug.Assert(pathLineRenderer.GetPosition(0) == anchors[0].transform.position
            , "1st point: " + pathLineRenderer.GetPosition(0) + " != " + anchors[0].transform.position);
            Debug.Assert(pathLineRenderer.GetPosition(1) == anchors[1].transform.position
            , "2nd point: " + pathLineRenderer.GetPosition(1) + " != " + anchors[1].transform.position);

            // Set the path line renderer to be active
            pathLineRenderer.gameObject.SetActive(true);
            isInitialized = true;
            Debug.Log("Trying to update path");
        }


    }
}

