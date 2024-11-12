using NaughtyAttributes;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SnakeClusteringTest : MonoBehaviour
{
    public enum IntersectionMode { None, LOS };

    public bool                                     runSimulationOnStart = true;
    public float                                    distanceTolerance = 1.0f;
    public float                                    angularTolerance = 45.0f;
    public float                                    subAngularTolerance = 5.0f;
    public SnakeClustering.SnakeMode                snakeMode = SnakeClustering.SnakeMode.Normal;
    public SnakeClustering.CreateSnakeMode          createSnakeMode = SnakeClustering.CreateSnakeMode.None;
    public SnakeClustering.ComputeDirectionMode     computeDirectionMode = SnakeClustering.ComputeDirectionMode.Average;
    public SnakeClustering.UpdateSnakePositionMode updatePositionMode = SnakeClustering.UpdateSnakePositionMode.Average;
    public SnakeClustering.UpdateSnakeDirectionMode updateDirectionMode = SnakeClustering.UpdateSnakeDirectionMode.AdjustWithLastMovement;
    [ShowIf("updateDirectionMode", SnakeClustering.UpdateSnakeDirectionMode.AdjustWithLastMovement), Range(0.0f, 1.0f)]
    public float                                    directionInertia = 0.0f;
    public IntersectionMode                         intersectionMode = IntersectionMode.LOS;
    public int                                      runStepsAtStart = 0;
    public int                                      autoMaxSteps = 1000;
    public bool                                     displayDistanceTolerance = false;
    public bool                                     displayDirections = false;
    public bool                                     displayPaths = false;

    [SerializeField] int currentSteps = 0;

    SnakeClustering snakeClustering;

    private void Start()
    {
        if (runSimulationOnStart)
            StartSimulation();
    }

    private void Update()
    {
        if (snakeClustering != null)
        {
            currentSteps++;
            snakeClustering.Step();

            if (snakeClustering.isDone)
            {
                Debug.Log("Simulation done!");
                currentSteps--;
            }
        }
    }

    [Button("Toggle visibility")]
    void ToggleVisibility()
    {
        TestPoint[] allPoints = GetComponentsInChildren<TestPoint>();

        bool b = !allPoints[0].debugRender;

        SetVisibility(b);
    }

    void SetVisibility(bool b)
    {
        TestPoint[] allPoints = GetComponentsInChildren<TestPoint>();

        foreach (var pt in allPoints)
        {
            pt.debugRender = b;
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

        TestPoint[] allPoints = GetComponentsInChildren<TestPoint>();

        snakeClustering = new SnakeClustering();
        snakeClustering.distanceTolerance = distanceTolerance;
        snakeClustering.angularTolerance = angularTolerance;
        snakeClustering.subAngularTolerance = subAngularTolerance;
        snakeClustering.snakeMode = snakeMode;
        snakeClustering.createSnakeMode = createSnakeMode;
        snakeClustering.computeDirectionMode = computeDirectionMode;
        snakeClustering.updateDirectionMode = updateDirectionMode;
        snakeClustering.updatePositionMode = updatePositionMode;
        snakeClustering.directionInertia = directionInertia;
        if (intersectionMode == IntersectionMode.LOS)
        {
            snakeClustering.isSegmentIntersecting = (p1, p2) =>
            {
                Triangle hitInfo = null;
                float hitT = float.MaxValue;
                return !meshOctree.Linecast(p1 + Vector3.up * 0.05f, p2 + Vector3.up * 0.05f, ref hitInfo, ref hitT);
            };
        }

        foreach (var pt in allPoints)
        {
            snakeClustering.AddPoint(pt.transform.position, pt.transform.up);
            if (pt.locked)
            {
                snakeClustering.AddSnake(pt.transform.position, false);
            }
        }

        snakeClustering.ComputeStartupDirections();

        SetVisibility(false);

        for (int i = 0; i < runStepsAtStart; i++)
        {
            StepSimulation();
        }
    }

    [Button("Step Simulation")]
    void StepSimulation()
    {
        if (snakeClustering == null)
        {
            StartSimulation();
        }
        currentSteps++;
        snakeClustering.Step();
        if (snakeClustering.isDone)
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
            if (snakeClustering.isDone)
            {
                break;
            }
        }

        if (currentSteps == autoMaxSteps)
        {
            Debug.LogWarning($"Couldn't finish operation in {autoMaxSteps} steps!");
        }
    }

    [Button("Start and Run Until End")]
    void StartAndRunUntilEnd()
    {
        StartSimulation();
        RunUntilEnd();
    }

    [Button("Stop")]
    void StopSimulation()
    {
        snakeClustering = null;
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

        if (snakeClustering != null)
        {
            Gizmos.color = Color.green;

            int nPoints = snakeClustering.pointCount;
            for (int i = 0; i < nPoints; i++)
            {
                var pt = snakeClustering.GetPoint(i);
                if (pt == null) continue;
                float alpha = 0.5f;
                if (!pt.active) alpha = 0.1f;

                Vector3 pos = pt.position;
                Gizmos.color = new Color(1.0f, 1.0f, 0.0f, alpha);
                Gizmos.DrawSphere(pos, 0.15f);
            }
            int nSnakes = snakeClustering.snakeCount;
            for (int i = 0; i < nSnakes; i++)
            {
                var snake = snakeClustering.GetSnake(i);
                if (snake == null) continue;
                float alpha = 1.0f;
                if (!snake.active) alpha = 0.25f;

                Vector3 pos = snake.currentPosition;
                Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.75f * alpha);
                Gizmos.DrawSphere(pos, 0.10f);
                if (displayDirections)
                {
                    if ((snake.heads != null) && (snake.heads.Count > 0))
                    {
                        foreach (var head in snake.heads)
                        {
                            Gizmos.DrawSphere(head, 0.05f);
                            DebugHelpers.DrawArrow(head, snake.currentDirection, distanceTolerance * 0.9f, distanceTolerance * 0.05f, 45.0f);
                        }
                    }
                    else
                    {
                        DebugHelpers.DrawArrow(snake.currentPosition, snake.currentDirection, distanceTolerance * 0.9f, distanceTolerance * 0.05f, 45.0f);
                    }
                }

                if (displayDistanceTolerance)
                {
                    Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.1f * alpha);
                    Gizmos.DrawSphere(pos, distanceTolerance);
                }

                if (displayPaths)
                {
                    if (snake.previousPoints != null)
                    {
                        Gizmos.color = Colors[i % Colors.Length];
                        for (int j = 0; j < snake.previousPoints.Count - 1; j++)
                        {
                            Gizmos.DrawLine(snake.previousPoints[j], snake.previousPoints[j + 1]);
                        }
                        Gizmos.DrawLine(snake.previousPoints[snake.previousPoints.Count - 1], snake.currentPosition);
                    }
                }
            }
        }
    }
}
