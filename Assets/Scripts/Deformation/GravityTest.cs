using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GravityTest : MonoBehaviour
{
    [SerializeField] bool   runSimulationOnStart = true;
    [SerializeField] float  gravityConstant = 0.001f;
    [SerializeField] float  planarAngularTolerance = 10.0f;
    [SerializeField] int    nSubsteps = 1;
    [SerializeField] float  minDist = 1e-3f;
    [SerializeField] float  maxDist = float.MaxValue;
    [SerializeField] float  timeStep = 0.01f;
    [SerializeField] float  realtimeTimeStep = 0.1f;
    [SerializeField] int    runStepsAtStart = 0;
    [SerializeField] int    autoMaxSteps = 1000;
    [SerializeField] bool   displayNormals;

    const int maxChains = 4;

    [SerializeField] int    currentSteps = 0;

    GravitySimulation simulation;

    private void Start()
    {
        if (runSimulationOnStart)
            StartSimulation();
    }

    private void Update()
    {
        if (simulation != null)
        {
            currentSteps++;
            simulation.Step(Time.deltaTime * realtimeTimeStep);
            
            if (simulation.totalDelta <= 1e-3f)
            {
                Debug.Log("Simulation done!");
                currentSteps--;
            }
        }
    }

    [Button("Toggle visibility")]
    void ToggleVisibility()
    {
        GravityPointForTesting[] allPoints = GetComponentsInChildren<GravityPointForTesting>();

        bool b = !allPoints[0].debugRender;

        SetVisibility(b);
    }

    void SetVisibility(bool b)
    {
        GravityPointForTesting[] allPoints = GetComponentsInChildren<GravityPointForTesting>();

        foreach (var pt in allPoints)
        {
            pt.debugRender = b;
        }
    }


    [Button("Setup Random Chains")]
    void SetupRandomChains()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Setup Random Chains",
            "Are you sure you want to proceed?",
            "Yes", // Text for the confirmation button
            "No"   // Text for the cancel button
        );

        if (!confirm) return;

        List<GravityPointForTesting> allPoints = new(GetComponentsInChildren<GravityPointForTesting>());

        foreach (var pt in allPoints)
        {
            pt.group = 0;
            pt.ResetLinks();
        }

        float linkProb = 1.0f;
        float decProb = 0.15f;
        for (int i = 0; i < maxChains; i++)
        {
            // Select a random point (initial point)
            var currentPoint = allPoints.Random(false);
            currentPoint.group = i + 1;

            while (currentPoint != null)
            {
                ProbList<GravityPointForTesting> candidates = new();

                for (int j = 0; j < allPoints.Count; j++)
                {
                    float d = Vector3.Distance(allPoints[j].transform.position, currentPoint.transform.position);
                    if (d <= 0.0f)
                    {
                        Debug.LogWarning($"Collapsed points {allPoints[j].name}, {currentPoint.name}");
                    }
                    candidates.Add(allPoints[j], d);
                }

                candidates.ReverseWeights();
                candidates.Normalize();

                var nextPoint = candidates.Get();
                nextPoint.group = i + 1;

                currentPoint.AddLink(nextPoint);
                allPoints.Remove(nextPoint);

                if (Random.Range(0.0f, 1.0f) < linkProb)
                {
                    currentPoint = nextPoint;
                    linkProb -= decProb;
                }
                else
                {
                    currentPoint = null;
                }
            }
        }
    }

    [Button("Start Simulation")]
    void StartSimulation()
    {
        currentSteps = 0;

        var meshOctree = GetComponent<MeshOctreeComponent>();
        if (meshOctree == null)
        {
            meshOctree = gameObject.AddComponent<MeshOctreeComponent>();
        }
        meshOctree.Build();

        GravityPointForTesting[] allPoints = GetComponentsInChildren<GravityPointForTesting>();

        simulation = new GravitySimulation();
        simulation.gravityConstant = gravityConstant;
        simulation.minDist = minDist;
        simulation.maxDist = maxDist;
        simulation.mergeDistance = 0.3f;
        simulation.groupSelfInfluence = false;
        simulation.planarAngularTolerance = planarAngularTolerance;
        simulation.validPairCallback = (p1, p2) => 
        {
            Triangle hitInfo = null;
            float    hitT = float.MaxValue;
            return !meshOctree.Linecast(p1.position + Vector3.up * 0.05f, p2.position + Vector3.up * 0.05f, ref hitInfo, ref hitT);
        };

        foreach (var pt in allPoints)
        {
            simulation.AddPoint(pt.transform.position, pt.transform.up, pt.group, pt.mass, pt.locked);
        }

        SetVisibility(false);

        for (int i = 0; i < runStepsAtStart; i++)
        {
            StepSimulation();
        }
    }

    [Button("Step Simulation")]
    void StepSimulation()
    {
        if (simulation == null)
        {
            StartSimulation();
        }
        currentSteps++;
        simulation.Step(timeStep, Mathf.Max(1, nSubsteps));
        if (simulation.totalDelta < 1e-3)
        {
            Debug.Log("Simulation done!");
            currentSteps--;
        }
    }

    [Button("Run Until End")]
    void RunUntilEnd()
    {
        while (currentSteps < autoMaxSteps)
        {
            StepSimulation();
            if (simulation.totalDelta < 1e-3)
            {
                break;
            }
        }

        if (currentSteps == autoMaxSteps)
        {
            Debug.LogWarning($"Couldn't finish operation in {autoMaxSteps} steps!");
        }
    }

    [Button("Stop")]
    void StopSimulation()
    {
        simulation = null;
        SetVisibility(true);
    }

    public static readonly Color[] Colors = new Color[]
    {
        Color.green,
        Color.red,
        Color.cyan,
        Color.yellow,
        Color.magenta,
        Color.blue,
    };

    private void OnDrawGizmos()
    {
        if (Selection.activeGameObject == null) return;
        if ((Selection.activeGameObject != gameObject) && (!Selection.activeGameObject.transform.IsChildOf(transform))) return;

        if (simulation != null)
        {
            Gizmos.color = Color.green;

            int nPoints = simulation.pointCount;
            for (int i = 0; i < nPoints; i++)
            {
                var pt = simulation.GetPoint(i);
                if (pt == null) continue;
                Vector3 pos = pt.position;
                Gizmos.color = Colors[pt.groupId % Colors.Length];
                Gizmos.DrawSphere(pos, 0.15f);

                if (displayNormals)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(pt.position, pt.position + pt.normal * 1.0f);
                }

                // Convert the world position to screen space
                Vector3 screenPos = Camera.current.WorldToScreenPoint(pos);

                // Apply an offset in screen space (e.g., right and down by 20 pixels)
                screenPos.x += 5;
                screenPos.y -= 5;

                // Convert the modified screen position back to world space
                Vector3 offsetPos = Camera.current.ScreenToWorldPoint(screenPos);

                // Draw the label at the new world position
                Handles.Label(offsetPos, $"{pt.groupId}");
            }

            // Draw segments
            List<GravityPointForTesting> allPoints = new(GetComponentsInChildren<GravityPointForTesting>());

            for (int i = 0; i < allPoints.Count; i++)
            {
                if (simulation.GetPositionByExternalIndex(i, out Vector3 p1))
                {
                    var links = allPoints[i].GetLinks();
                    if ((links != null) && (links.Count > 0))
                    {
                        foreach (var l in links)
                        {
                            int idx = allPoints.IndexOf(l);
                            if (idx != -1)
                            {
                                if (simulation.GetPositionByExternalIndex(idx, out Vector3 p2))
                                {
                                    Gizmos.color = Colors[allPoints[i].group % Colors.Length];
                                    Gizmos.DrawLine(p1, p2);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
