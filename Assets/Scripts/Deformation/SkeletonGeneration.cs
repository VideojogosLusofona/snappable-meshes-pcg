using UnityEngine;
using SnapMeshPCG;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.ProBuilder;
using static UnityEditor.Searcher.Searcher.AnalyticsEvent;
using System;

public class SkeletonGeneration : MonoBehaviour
{
    public enum SkeletonGenerationType { Straight };

    // Needed for the navmesh generation configuration
    [SerializeField] 
    private NavMeshGeneratorConfig navMeshConfig;
    [SerializeField] 
    private SkeletonGenerationType  genType = SkeletonGenerationType.Straight;
    [SerializeField]
    private int                     maxSteps = 100;
    [SerializeField, Header("Debug Options")]
    private bool displayNavmesh = false;
    [SerializeField]
    private bool displayBoundary = false;
    [SerializeField]
    private bool displayVertices = false;
    [SerializeField]
    private bool displayBorderOverTime = false;
    [SerializeField]
    public bool debugIntersection = false;
    [SerializeField, ShowIf("needDebugTime")]
    private float debugTime = 0.0f;
    [SerializeField, ShowIf("needDebugEdge")]
    private int debugEdge = -1;
    [SerializeField, ShowIf("needDebugVertex")]
    private int debugVertex = -1;

    private bool needDebugTime => displayVertices || displayBorderOverTime;
    private bool needDebugEdge => displayBorderOverTime || debugIntersection;
    private bool needDebugVertex => displayBorderOverTime || debugIntersection;

    private Mesh        navigationMesh;
    private Topology    meshTopology;
    private Boundary    navigationBoundaries;

    private List<DeformationState>  states;

    [Button("Setup Deformation")]
    public void SetupDeformation()
    {
        var navMesh = GetComponent<LocalNavMesh>();
        if (navMesh == null)
        {
            var config = navMeshConfig;
            if (config == null)
            {
                var autoPieceGeneration = GetComponent<AutoPieceGeneration>();
                if ((autoPieceGeneration != null) && (autoPieceGeneration.config != null))
                {
                    config = autoPieceGeneration.config.navMeshProperties;
                }
            }
            if (config == null)
            {
                Debug.Log("<color=#FFFF00>Can't setup deformation - nav mesh properties are not setup!<color=#A0A0A0>");
                return;
            }

            navMesh = gameObject.AddComponent<LocalNavMesh>();
            navMesh.SetNavMeshConfig(config);
        }

        navMesh.Build();

        navigationMesh = navMesh.GetMesh(transform.worldToLocalMatrix);

        meshTopology = new Topology(navigationMesh, Matrix4x4.identity);
        meshTopology.ComputeTriangleNormals();
        navigationBoundaries = meshTopology.GetBoundaries();

        BuildSkeleton();
    }

    [Button("Straight Skeleton Generation: Build")]
    void BuildSkeleton()
    {
        int stepCount = 0;

        StraightSkeletonSetup();
        while ((StraightSkeletonStep() != null) && (stepCount < maxSteps))
        {
            stepCount++;
        }

        if (stepCount == maxSteps)
        {
            Debug.LogWarning("Exceeded maximum number of steps!");
        }
        else
        {
            Debug.Log($"Finished bulding straight skeleton in {stepCount} steps...");
        }
    }

    float straightSkeletonTime = 0.0f;

    [Button("Straight Skeleton Generation: Setup")]
    void StraightSkeletonSetup()
    {
        if (navigationBoundaries == null)
        {
            SetupDeformation();
        }

        var startState = new DeformationState();
        startState.BuildVertices(navigationBoundaries);

        states = new List<DeformationState>
        {
            startState
        };

        straightSkeletonTime = 0.0f;
        debugTime = straightSkeletonTime;

        Debug.Log("<color=#00FF00>Cleared straight skeleton generation...<color=#A0A0A0>");
    }

    [Button("Straight Skeleton Generation: Step")]
    DeformationState StraightSkeletonStep()
    {
        List<DeformationState.Event> events = new();

        var state = GetState(straightSkeletonTime);
        state.GetNextEvent(events);
        if (events.Count == 0)
        {
            Debug.Log("<color=#FFFF00>No event detected!<color=#A0A0A0>");
            return null;
        }

        var newState = state.CreateNewState(events);
        states.Add(newState);

        debugTime = straightSkeletonTime = newState.startTime;

        return newState;
    }

    DeformationState GetState(float t)
    {
        // This assumes the states are placed in ordered fashion
        if (states == null) return null;
        if (states.Count == 0) return null;

        DeformationState currentState = null;
        foreach (var state in states)         
        {
            if (state.startTime > t) return currentState;

            currentState = state;
        }

        return currentState;
    }

    private void OnDrawGizmosSelected()
    {
        if ((displayNavmesh) && (navigationMesh))
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireMesh(navigationMesh);
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);
            Gizmos.DrawMesh(navigationMesh);
        }
        if ((displayBoundary) && (navigationBoundaries != null))
        {
            Gizmos.color = new Color(1, 1, 0, 0.25f);
            int count = navigationBoundaries.Count;

            for (int i = 0; i < count; i++)
            {
                var polyline = navigationBoundaries.Get(i);

                for (int j = 0; j < polyline.Count; j++)
                {
                    Gizmos.DrawLine(polyline[j], polyline[(j + 1) % polyline.Count]);
                }
            }
        }

        if (debugTime < 0) debugTime = 0;
        DeformationState state = GetState(debugTime);
        if (state != null)
        {
            state.OnDrawGizmos(displayVertices, displayBorderOverTime, debugIntersection, debugEdge, debugVertex, debugTime);          
        }
    }
}
