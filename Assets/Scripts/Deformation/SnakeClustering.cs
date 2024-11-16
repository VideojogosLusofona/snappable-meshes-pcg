using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class SnakeClustering 
{
    public enum SnakeMode { Normal, Hydra };
    public enum UpdateSnakePositionMode { Closest, FurthestInDirection, Average, AverageBox, Directional };
    public enum UpdateSnakeDirectionMode { AdjustWithLastMovement, AdjustWithHistory, Recompute };
    public enum ComputeDirectionMode { Average, Weighted };
    public enum CreateSnakeMode { None, Random, ClosestToSnake, SnakeEndpoints };

    internal class Point
    {
        public bool active;
        public Vector3 position;
        public Vector3 normal;
    }

    public class Snake
    {
        public bool             active;
        public Vector3          currentPosition;
        public Vector3          currentDirection;
        public List<Vector3>    heads;
        public List<Vector3>    previousPoints;

        public void SetNewPosition(Vector3 newPos)
        {
            if (previousPoints == null) previousPoints = new();
            previousPoints.Add(currentPosition);
            currentPosition = newPos;
        }

        public (float, int) GetDistance(Vector3 position)
        {
            float minDist = float.MaxValue;
            int index = -1;
            for (int i = 0; i < previousPoints.Count; i++)
            {
                var p = previousPoints[i];
                float d = Vector3.Distance(p, position);
                if (d < minDist)
                {
                    minDist = d;
                    index = i;
                }
            }

            return (minDist, index);
        }

        public bool GetDistanceEndpoints(Vector3 position, float angularToleranceRadians, out float distance, out int index)
        {
            Vector3 snakeDir = avgDirection;
            distance = float.MaxValue;
            index = -1;

            if (snakeDir.sqrMagnitude < 1e-3) return false;

            float cosTolerance = Mathf.Cos(angularToleranceRadians);

            Vector3 toStart = (previousPoints[0] - position).normalized;
            bool    canExtendStart = Vector3.Dot(toStart, snakeDir) > cosTolerance;
            Vector3 fromEnd = (position - previousPoints.Last()).normalized;
            bool    canExtendEnd = Vector3.Dot(fromEnd, snakeDir) > cosTolerance;

            if (!canExtendStart && !canExtendEnd) return false;
            if (canExtendStart)
            {
                float distanceStart = Vector3.Distance(previousPoints[0], position);
                if (!canExtendEnd) 
                {
                    distance = distanceStart;
                    index = 0;
                    return true;
                }
                else
                {
                    float distanceEnd = Vector3.Distance(previousPoints.Last(), position);
                    if (distanceEnd < distanceStart)
                    {
                        distance = distanceEnd;
                        index = previousPoints.Count - 1;
                    }
                    else
                    {
                        distance = distanceStart;
                        index = 0;
                    }
                    return true;
                }
            }
            else
            {
                distance = Vector3.Distance(previousPoints.Last(), position);
                index = previousPoints.Count - 1;
                return true;
            }
        }

        public void Invert()
        {
            previousPoints.Reverse();
            heads = null;
        }

        public Vector3 avgDirection
        {
            get
            {
                Vector3 dir = Vector3.zero;
                if ((previousPoints == null) || (previousPoints.Count < 2)) return dir;

                for (int i = 0; i < previousPoints.Count - 1; i++)
                {
                    dir = dir + (previousPoints[i + 1] - previousPoints[i]);
                }

                return dir.normalized;
            }
        }
    };

    public delegate bool IsSegmentIntersecting(Vector3 pt1, Vector3 pt2);

    public float distanceTolerance { get; set; }
    public float angularTolerance
    {
        get { return _angularTolerance; }
        set { _angularTolerance = value; _cosAngularTolerance = Mathf.Cos(value * Mathf.Deg2Rad); }
    }
    public IsSegmentIntersecting isSegmentIntersecting { get; set; }
    public SnakeMode snakeMode
    {
        get { return _snakeMode; }
        set { _snakeMode = value; }
    }
    public UpdateSnakeDirectionMode updateDirectionMode
    {
        get { return _updateDirectionMode; }
        set { _updateDirectionMode = value; }
    }
    public UpdateSnakePositionMode updatePositionMode
    {
        get { return _updatePositionMode; }
        set { _updatePositionMode = value; }
    }
    public float directionInertia { get; set; }
    public ComputeDirectionMode computeDirectionMode
    {
        get { return _computeDirectionMode; }
        set { _computeDirectionMode = value; }
    }
    public CreateSnakeMode createSnakeMode
    {
        get { return _createSnakeMode; }
        set { _createSnakeMode = value; }
    }

    private SnakeMode                   _snakeMode = SnakeMode.Normal;
    private UpdateSnakePositionMode     _updatePositionMode = UpdateSnakePositionMode.Average;
    private UpdateSnakeDirectionMode    _updateDirectionMode = UpdateSnakeDirectionMode.AdjustWithLastMovement;
    private ComputeDirectionMode        _computeDirectionMode = ComputeDirectionMode.Average;
    private CreateSnakeMode             _createSnakeMode = CreateSnakeMode.None;
    private float                       _cosAngularTolerance;
    private float                       _angularTolerance;

    private List<Point>     points = new();
    private List<Snake>     snakes = new();

    public bool isDone
    {
        get
        {
            foreach (var point in points)
            {
                if (point.active)
                {
                    if (_createSnakeMode != CreateSnakeMode.None) return false;

                    return !isSnakeActive;
                }
            }
            return true;
        }
    }
    public bool isSnakeActive
    {
        get
        {
            foreach (var snake in snakes)
            {
                if (snake.active) return true;
            }
            return false;
        }
    }
    public int pointCount => points.Count;
    public int snakeCount => snakes.Count;
    internal Point GetPoint(int index) => points[index];
    internal Snake GetSnake(int index) => snakes[index];


    public void Step()
    {
        List<Snake> disableSnakes = null;

        foreach (var snake in snakes)
        {
            if (!snake.active) continue;

            var food = GetPointsForSnake(snake);
            if (food.Count == 0)
            {
                // Dead snake
                if (disableSnakes == null) disableSnakes = new();
                disableSnakes.Add(snake);

                // Add current point to previous points
                snake.previousPoints.Add(snake.currentPosition);
            }
            else
            {
                // Move snake to the average position, and mark points as used
                Vector3 prevPos = snake.currentPosition;
                Vector3 newPos = Vector3.zero;
                if (_updatePositionMode == UpdateSnakePositionMode.Closest)
                {
                    float minDist = float.MaxValue;
                    foreach (var p in food)
                    {
                        float d = Vector3.Distance(p.position, snake.currentPosition);
                        if ((d > 1e-3) && (d < minDist))
                        {
                            minDist = d;
                            newPos = p.position;
                        }
                        p.active = false;
                    }
                    snake.SetNewPosition(newPos);
                }
                else if (_updatePositionMode == UpdateSnakePositionMode.FurthestInDirection)
                {
                    float maxDist = 0.0f;
                    foreach (var p in food)
                    {
                        float d = Vector3.Dot(p.position - snake.currentPosition, snake.currentDirection);
                        if (d > maxDist)
                        {
                            maxDist = d;
                            newPos = p.position;
                        }
                        p.active = false;
                    }
                    snake.SetNewPosition(newPos);
                }
                else if (_updatePositionMode == UpdateSnakePositionMode.Average)
                {
                    foreach (var p in food)
                    {
                        newPos += p.position;
                        p.active = false;
                    }
                    newPos /= food.Count;
                    snake.SetNewPosition(newPos);
                }
                else if (_updatePositionMode == UpdateSnakePositionMode.AverageBox)
                {
                    Vector3 pMin = food[0].position;
                    Vector3 pMax = food[0].position;
                    foreach (var p in food)
                    {
                        pMin.x = Mathf.Min(pMin.x, p.position.x);
                        pMin.y = Mathf.Min(pMin.y, p.position.y);
                        pMin.z = Mathf.Min(pMin.z, p.position.z);
                        pMax.x = Mathf.Max(pMax.x, p.position.x);
                        pMax.y = Mathf.Max(pMax.y, p.position.y);
                        pMax.z = Mathf.Max(pMax.z, p.position.z);
                        p.active = false;
                    }
                    newPos = (pMin + pMax) * 0.5f;
                    snake.SetNewPosition(newPos);
                }
                else if (_updatePositionMode == UpdateSnakePositionMode.Directional)
                {
                    float avgDist = 0.0f;
                    foreach (var p in food)
                    {
                        Vector3 toFood = p.position - snake.currentPosition;
                        avgDist += Vector3.Dot(toFood, snake.currentDirection);
                        p.active = false;
                    }
                    avgDist /= food.Count;
                    snake.SetNewPosition(snake.currentPosition + snake.currentDirection * avgDist);
                }

                if (_snakeMode == SnakeMode.Hydra)
                {
                    snake.heads = new();
                    foreach (var p in food)
                    {
                        snake.heads.Add(p.position);
                    }
                }

                if (_updateDirectionMode == UpdateSnakeDirectionMode.Recompute)
                {
                    snake.currentDirection = Vector3.zero;
                    ComputeDirection(snake);
                }
                else if (_updateDirectionMode == UpdateSnakeDirectionMode.AdjustWithLastMovement)
                {
                    var newDir = (newPos - prevPos).normalized;
                    snake.currentDirection = Vector3.Lerp(snake.currentDirection, newDir, directionInertia).normalized;
                }
                else if (_updateDirectionMode == UpdateSnakeDirectionMode.AdjustWithHistory)
                {
                    var newDir = (newPos - prevPos).normalized;
                    newDir = (snake.currentDirection * snake.previousPoints.Count + newDir).normalized;
                    snake.currentDirection = Vector3.Lerp(snake.currentDirection, newDir, directionInertia).normalized;
                }
            }
        }

        if (disableSnakes != null)
        {
            foreach (var snake in disableSnakes)
            {
                snake.active = false;
            }
        }

        if ((!isSnakeActive) && (!isDone))
        {
            List<Point> allActivePoints = new List<Point>();
            foreach (var pt in points)
            {
                if (pt.active) allActivePoints.Add(pt);
            }

            if (createSnakeMode == CreateSnakeMode.ClosestToSnake)
            {
                // Find active point closest to a snake
                (var newPoint, Snake snake, int index) = GetClosestSnake(allActivePoints);
                newPoint.active = false;
                if ((index == 0) || (index == snake.previousPoints.Count - 1))
                {
                    if (index == 0) snake.Invert();     // Invert snake so that we begin from the same start point as before

                    snake.currentPosition = newPoint.position;
                    snake.active = true;
                    snake.currentDirection = Vector2.zero;
                }
                else
                {
                    // Create a new snake from here
                    snake = AddSnake(newPoint.position, true);
                }

                ComputeDirection(snake);
            }
            else if (createSnakeMode == CreateSnakeMode.SnakeEndpoints)
            {
                // Find active point closest to a snake
                (var newPoint, Snake snake, int index) = GetClosestSnakeEndpoints(allActivePoints);
                if ((snake != null) && ((index == 0) || (index == snake.previousPoints.Count - 1)))
                {
                    newPoint.active = false;
                    if (index == 0) snake.Invert();     // Invert snake so that we begin from the same start point as before

                    snake.currentPosition = newPoint.position;
                    snake.active = true;
                    snake.currentDirection = Vector2.zero;
                }
                else
                {
                    // Couldn't find a valid point at the endpoints, find closest point
                    (newPoint, snake, index) = GetClosestSnake(allActivePoints);
                    newPoint.active = false;

                    // Create a new snake from here
                    snake = AddSnake(newPoint.position, true);
                }

                ComputeDirection(snake);
            }
            else if (createSnakeMode == CreateSnakeMode.Random)
            {
                var newPoint = allActivePoints.Random(true);
                newPoint.active = false;
                var snake = AddSnake(newPoint.position, true);
                ComputeDirection(snake);
            }
        }
    }

    public void AddPoint(Vector3 position, Vector3 normal)
    {
        points.Add(new Point
        {
            active = true,
            position = position,
            normal = normal
        });
    }

    public Snake AddSnake(Vector3 position, bool allow_duplicates)
    {
        if (!allow_duplicates)
        {
            foreach (var snake in snakes)
            {
                if (Vector3.Distance(snake.currentPosition, position) < 1e-3) return null;
            }
        }

        snakes.Add(new Snake
        {
            active = true,
            currentPosition = position
        });

        return snakes.Last();
    }

    public void ComputeStartupDirections()
    {
        foreach (var snake in snakes)
        {
            ComputeDirection(snake);
        }
    }

    void ComputeDirection(Snake snake)
    {
        if (!snake.active) return;
        if (snake.currentDirection.sqrMagnitude > 1e-3) return;

        Vector3 finalDirection = Vector3.zero;

        if ((snake.heads != null) && (snake.heads.Count > 0))
        {
            List<Vector3> headDir = new();
            foreach (var head in snake.heads)
            {
                // Get all points in range
                var pointsInRange = GetActivePointsInRange(points, head);

                // For each point, check how many points are within the cone given by the angle tolerance
                // Points at the position of the snake head are considered as in any direction
                int maxCount = 0;
                foreach (var p in pointsInRange)
                {
                    if (!p.active) continue;
                    if (GetDirection(head, p.position, out Vector3 direction))
                    {
                        var pointsInAngle = GetActivePointsInCone(pointsInRange, head, direction, false);
                        if (pointsInAngle.Count > maxCount)
                        {
                            float maxDistance = 0.0f;
                            if (_computeDirectionMode == ComputeDirectionMode.Weighted)
                            {
                                foreach (var tmp in pointsInAngle)
                                {
                                    maxDistance = Mathf.Max(maxDistance, Vector3.Distance(head, tmp.position));
                                }
                                // We want to weight what's closest to be more influencial, so we need to invert the distance, hence
                                // the need for maxDistance. Multiply by two guarantees that something that's on the very edge doesn't have zero influence
                                maxDistance *= 1.1f;
                            }

                            maxCount = pointsInAngle.Count;
                            finalDirection = Vector3.zero;
                            foreach (var tmp in pointsInAngle)
                            {
                                if (GetDirection(snake.currentPosition, tmp.position, out Vector3 d))
                                {
                                    if (_computeDirectionMode == ComputeDirectionMode.Weighted) finalDirection += d * (maxDistance - Vector3.Distance(head, tmp.position));
                                    else finalDirection += d;
                                }
                            }
                            finalDirection.Normalize();

                            headDir.Add(finalDirection);
                        }
                    }
                }
            }

            finalDirection = Vector3.zero;
            foreach (var d in headDir) finalDirection += d;
            finalDirection.Normalize();

        }
        else
        {
            // Get all points in range
            var pointsInRange = GetActivePointsInRange(points, snake.currentPosition);

            // For each point, check how many points are within the cone given by the angle tolerance
            // Points at the position of the snake head are considered as in any direction
            int maxCount = 0;
            foreach (var p in pointsInRange)
            {
                if (!p.active) continue;
                if (GetDirection(snake.currentPosition, p.position, out Vector3 direction))
                {
                    var pointsInAngle = GetActivePointsInCone(pointsInRange, snake.currentPosition, direction, false);
                    if (pointsInAngle.Count > maxCount)
                    {
                        float maxDistance = 0.0f;
                        if (computeDirectionMode == ComputeDirectionMode.Weighted)
                        {
                            foreach (var tmp in pointsInAngle)
                            {
                                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(snake.currentPosition, tmp.position));
                            }
                            // We want to weight what's closest to be more influencial, so we need to invert the distance, hence
                            // the need for maxDistance. Multiply by two guarantees that something that's on the very edge doesn't have zero influence
                            maxDistance *= 1.1f;
                        }

                        maxCount = pointsInAngle.Count;
                        finalDirection = Vector3.zero;
                        foreach (var tmp in pointsInAngle)
                        {
                            if (GetDirection(snake.currentPosition, tmp.position, out Vector3 d))
                            {
                                if (_computeDirectionMode == ComputeDirectionMode.Weighted) finalDirection += d * (maxDistance - Vector3.Distance(snake.currentPosition, tmp.position));
                                else finalDirection += d;
                            }
                        }
                        finalDirection.Normalize();
                    }
                }
            }
        }

        snake.active = (finalDirection.magnitude > 1e-3);
        if (!snake.active)
        {
            if ((snake.previousPoints == null) || (Vector3.Distance(snake.previousPoints.Last(), snake.currentPosition) > 1e-3))
            {
                if (snake.previousPoints == null) snake.previousPoints = new();
                snake.previousPoints.Add(snake.currentPosition);
            }
        }
        snake.currentDirection = finalDirection;
    }

    private bool GetDirection(Vector3 currentPosition, Vector3 targetPosition, out Vector3 direction)
    {
        direction = Vector3.zero;
        Vector3 delta = targetPosition - currentPosition;
        float   distance = delta.magnitude;
        if (distance < 1e-3) return false;

        direction = delta / distance;

        return true;
    }

    private List<Point> GetActivePointsInRange(List<Point> pointSet, Vector3 position)
    {
        List<Point> ret = new();

        foreach (var point in pointSet)
        {
            if (!point.active) continue;
            float d = Vector3.Distance(point.position, position);
            if (d < distanceTolerance)
            {
                ret.Add(point);
            }
        }

        return ret;
    }

    private List<Point> GetActivePointsInCone(List<Point> pointSet, Vector3 position, Vector3 testDirection, bool considerSelf)
    {
        List<Point> ret = new();
        foreach (var point in pointSet)
        {
            if (!point.active) continue;
            if (GetDirection(position, point.position, out Vector3 thisDirection))
            {
                if (Vector3.Dot(testDirection, thisDirection) > _cosAngularTolerance)
                {
                    ret.Add(point);
                }
            }
            else
            {
                if (considerSelf) ret.Add(point);
            }

        }
        return ret;
    }

    private List<Point> GetPointsForSnake(Snake snake)
    {
        if ((snake.heads != null) && (snake.heads.Count > 0) && (snakeMode == SnakeMode.Hydra))
        {
            List<Point> ret = new();

            foreach (var head in snake.heads)
            {
                var tmp = GetActivePointsInRange(points, head);
                tmp = GetActivePointsInCone(tmp, head, snake.currentDirection, true);

                ret.AddRange(tmp);
            }

            return ret;
        }
        else
        {
            var ret = GetActivePointsInRange(points, snake.currentPosition);
            ret = GetActivePointsInCone(ret, snake.currentPosition, snake.currentDirection, true);
            return ret;
        }
    }

    private (Point point, Snake snake, int index) GetClosestSnake(List<Point> pointSet)
    {
        float minDist = float.MaxValue;
        Point retPoint = null;
        Snake retSnake = null;
        int   retIndex = -1;
        foreach (var p in pointSet)
        {
            foreach (var snake in snakes)
            {
                (float d, int index) = snake.GetDistance(p.position);
                if (d < minDist)
                {
                    minDist = d;
                    retPoint = p;
                    retSnake = snake;
                    retIndex = index;
                }
            }
        }

        return (retPoint, retSnake, retIndex);
    }

    private (Point point, Snake snake, int index) GetClosestSnakeEndpoints(List<Point> pointSet)
    {
        float minDist = float.MaxValue;
        Point retPoint = null;
        Snake retSnake = null;
        int retIndex = -1;
        foreach (var p in pointSet)
        {
            foreach (var snake in snakes)
            {
                float   d = float.MaxValue;
                int     index = -1;

                if (snake.GetDistanceEndpoints(p.position, _cosAngularTolerance, out d, out index))
                {
                    if (d < minDist)
                    {
                        minDist = d;
                        retPoint = p;
                        retSnake = snake;
                        retIndex = index;
                    }
                }
            }
        }

        return (retPoint, retSnake, retIndex);
    }
}
