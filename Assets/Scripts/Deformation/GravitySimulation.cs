using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class GravitySimulation
{
    public delegate bool ValidPairCallback(Point pt1, Point pt2);

    private float minSqDist = 1e-3f;
    private float maxSqDist = float.MaxValue;
    private float mergeSqDistance = 0.0f;

    public float gravityConstant = 1.0f;
    public bool  groupSelfInfluence = true;

    public float totalDelta { get; private set; }

    public float minDist
    {
        get { return Mathf.Sqrt(minSqDist); }
        set { minSqDist = value * value; }
    }
    public float maxDist
    {
        get { return Mathf.Sqrt(maxSqDist); }
        set { maxSqDist = value * value; }
    }
    public float mergeDistance
    {
        get { return Mathf.Sqrt(mergeSqDistance); }
        set { mergeSqDistance = value * value; }
    }

    public ValidPairCallback validPairCallback { get; set; }

    public class Point
    {
        public int      index;
        public Vector3  position;
        public Vector3  normal;
        public Vector3  velocity;
        public float    mass;
        public int      externalId;
        public int      groupId;
        public bool     locked;
    }

    class Group
    {
        public int         id;
        public List<Point> points = new();
        public uint        mask;

        public bool selfInfluence => ((mask & (1 << id)) != 0);

        public void BitmaskEnableGroupInfluence(int grpId) { mask = mask | (uint)(1 << grpId); }
        public void BitmaskDisableGroupInfluence(int grpId) { mask = mask & ~(uint)(1 << grpId); }
    }

    List<Group> groups = new();
    List<Point> points = new();

    Dictionary<(int i1, int i2), (float d, float d2, Vector3 v)> distanceCache;

    Dictionary<(int i1, int i2), int> parentGroups = new();
    Dictionary<int, (int i1, int i2)> revParentGroups = new();

    Dictionary<int, int> childPoint = new();

    float _planarAngularTolerance;

    public float planarAngularTolerance
    {
        get { return Mathf.Acos(_planarAngularTolerance) * Mathf.Rad2Deg; }
        set { _planarAngularTolerance = Mathf.Cos(value * Mathf.Deg2Rad); }
    }

    internal int pointCount => points.Count;

    public void EnableInfluence(int grp1, int grp2, bool propagate = true)
    {
        var grp = GetGroup(grp1);
        grp.BitmaskEnableGroupInfluence(grp2);
        if (propagate)
        {
            if (IsMergeGroup(grp2))
            {
                var parents = revParentGroups[grp2];
                EnableInfluence(grp1, parents.i1, true);
                EnableInfluence(grp1, parents.i2, true);
            }
        }
    }

    public void DisableInfluence(int grp1, int grp2, bool propagate = true)
    {
        var grp = GetGroup(grp1);
        grp.BitmaskDisableGroupInfluence(grp2);
        if (propagate)
        {
            if (IsMergeGroup(grp2))
            {
                var parents = revParentGroups[grp2];
                DisableInfluence(grp1, parents.i1, true);
                DisableInfluence(grp1, parents.i2, true);
            }
        }
    }

    public void AddPoint(Vector3 position, Vector3 normal, int gravityGroup, float mass, bool locked)
    {
        Group group = GetGroup(gravityGroup);

        var pt = new Point { index = points.Count, position = position, normal = normal, velocity = Vector3.zero, mass = mass, groupId = gravityGroup, externalId = points.Count, locked = locked };

        points.Add(pt);
        group.points.Add(pt);
    }

    Group GetGroup(int gravityGroup)
    { 
        if (groups.Count <= gravityGroup)
        {
            for (int i = groups.Count; i <= gravityGroup; i++)
            {
                var grp = new Group() { id = groups.Count, mask = 0xFFFFFFFF };
                groups.Add(grp);
                
                if (!groupSelfInfluence) DisableInfluence(grp.id, grp.id);
            }
        }

        return groups[gravityGroup];
    }

    bool IsMergeGroup(int grpId)
    {
        return revParentGroups.ContainsKey(grpId);
    }

    bool IsChildOf(int grpId, int parentId)
    {
        if (revParentGroups.TryGetValue(grpId, out var parent))
        {
            if (parent.i1 == parentId) return true;
            if (parent.i2 == parentId) return true;
            if (IsMergeGroup(parent.i1))
            {
                if (IsChildOf(parent.i1, parentId)) return true;
            }
            if (IsMergeGroup(parent.i2))
            {
                if (IsChildOf(parent.i2, parentId)) return true;
            }
        }

        return false;
    }

    Group GetGroup(int grp1, int grp2)
    {
        // If the same group, preserve group
        if (grp1 == grp2) return groups[grp1];

        if (IsMergeGroup(grp1))
        {
            if (!IsMergeGroup(grp2))
            {
                if (IsChildOf(grp1, grp2))
                {
                    return groups[grp1];
                }
            }
        }
        if (IsMergeGroup(grp2))
        {
            if (!IsMergeGroup(grp1))
            {
                if (IsChildOf(grp2, grp1))
                {
                    return groups[grp2];
                }
            }
        }

        int existingMergeGroup = -1;
        if (parentGroups.TryGetValue((grp1, grp2), out existingMergeGroup))
        {
            return groups[existingMergeGroup];
        }

        var group1 = groups[grp1];
        var group2 = groups[grp2];

        var newGroup = new Group() { id = groups.Count, mask = group1.mask & group2.mask };
        groups.Add(newGroup);
        parentGroups.Add((grp1, grp2), newGroup.id);
        parentGroups.Add((grp2, grp1), newGroup.id);
        revParentGroups.Add(newGroup.id, (grp1, grp2));

        if (!groupSelfInfluence) DisableInfluence(newGroup.id, newGroup.id);
        if (!group1.selfInfluence) DisableInfluence(group1.id, newGroup.id, false);
        if (!group2.selfInfluence) DisableInfluence(group2.id, newGroup.id, false);

        return newGroup;
    }

    public Point GetPoint(int index)
    {
        return points[index];
    }

    void ResetVelocity()
    {
        foreach (var pt in points)
        {
            if (pt != null) 
                pt.velocity = Vector3.zero;
        }
    }

    void BuildCacheDistances()
    {
        if (distanceCache == null) distanceCache = new();

        for (int i = 0; i < points.Count; i++)
        {
            var pt1 = points[i];
            if (pt1 == null) continue;

            distanceCache[(i, i)] = (0.0f, 0.0f, Vector3.zero);

            for (int j = i + 1; j < points.Count; j++)
            {
                var pt2 = points[j];
                if (pt2 == null) continue;

                var v = pt2.position - pt1.position;
                float d2 = Vector3.Dot(v, v);
                float d = Mathf.Sqrt(d2);
                v = v / d;
                distanceCache[(i, j)] = (d, d2, v);
                distanceCache[(j, i)] = (d, d2, -v);
            }
        }
    }
    void UpdateSquareDistances()
    {
        for (int i = 0; i < points.Count; i++)
        {
            var pt1 = points[i];
            if (pt1 == null) continue;

            for (int j = i + 1; j < points.Count; j++)
            {
                var pt2 = points[j];
                if (pt2 == null) continue;

                var v = pt2.position - pt1.position;
                float d2 = Vector3.Dot(v, v);

                var oldValue = distanceCache[(i, j)];

                distanceCache[(i, j)] = (oldValue.d, d2, oldValue.v);
                distanceCache[(j, i)] = (oldValue.d, d2, -oldValue.v);
            }
        }
    }

    void ComputeVelocities()
    {
        for (int i = 0; i < groups.Count; i++)
        {
            // For group i
            var grp1 = groups[i];
            for (int j = 0; j < groups.Count; j++)
            {
                // Check if group j affects group i
                if ((grp1.mask & (uint)(1 << j)) == 0) continue;

                var grp2 = groups[j];

                foreach (var pt1 in grp1.points)
                {
                    foreach (var pt2 in grp2.points)
                    {                        
                        (float d, float d2, Vector3 v) = distanceCache[(pt1.index, pt2.index)];
                        if ((d2 > minSqDist) && (d2 < maxSqDist))
                        {
                            // Check for the plane
                            if (Vector3.Dot(pt1.normal, pt2.normal) > _planarAngularTolerance)
                            {
                                if ((validPairCallback != null) && (!validPairCallback(pt1, pt2))) continue;

                                float str = gravityConstant * pt2.mass / d2;
                                pt1.velocity += v * str;
                            }
                        }
                    }
                }
            }
        }
    }

    void ComputePositions(float deltaTime)
    {
        foreach (var pt in points)
        {
            if ((pt != null) && (!pt.locked))
            {
                pt.position += pt.velocity * deltaTime;
                totalDelta += pt.velocity.sqrMagnitude;
            }
        }
    }

    void MergePoints()
    {
        List<Point> toRemove = new List<Point>();

        foreach (var dc in distanceCache)
        {
            // Only merge smaller index with bigger index, not bigger index with smaller index
            // This makes sure we only merge (A,B) and not (B,A) because it might otherwise
            if (dc.Key.i1 > dc.Key.i2) continue;

            if (dc.Value.d2 < mergeSqDistance)
            {
                var p1 = points[dc.Key.i1];
                var p2 = points[dc.Key.i2];
                // Check if point is already destroyed, or schedulled for destruction
                if ((p1 == null) || (p2 == null) || (p1.index == p2.index)) continue;
                if (toRemove.Find(((p) => (p.index == p1.index) || (p.index == p2.index))) != null) continue;

                // Create new point
                var mergeGroup = GetGroup(p1.groupId, p2.groupId);

                var pt = new Point
                {
                    index = points.Count,
                    position = (p1.position + p2.position) * 0.5f,
                    normal = (p1.normal + p2.normal).normalized,
                    velocity = Vector3.zero,
                    mass = (p1.mass + p2.mass),
                    groupId = mergeGroup.id,
                    externalId = -1,
                    locked = p1.locked | p2.locked
                };

                points.Add(pt);
                mergeGroup.points.Add(pt);

                // Remove previous points
                toRemove.Add(p1);
                toRemove.Add(p2);

                childPoint.Add(p1.index, pt.index);
                childPoint.Add(p2.index, pt.index);
            }
        }

        foreach (var pt in toRemove)
        {
            groups[pt.groupId].points.Remove(pt);
            points[pt.index] = null;

            // Collect keys to remove
            var keysToRemove = new List<(int, int)>();
            foreach (var key in distanceCache.Keys)
            {
                if ((key.i1 == pt.index) || (key.i2 == pt.index))
                {
                    keysToRemove.Add(key);
                }
            }

            // Remove the collected keys
            foreach (var key in keysToRemove)
            {
                distanceCache.Remove(key);
            }
        }
    }

    internal void Step(float deltaTime, int subSteps = 1)
    {
        totalDelta = 0.0f;

        float dt = deltaTime / subSteps;
        for (int i = 0; i < subSteps; i++)
        {
            // Reset velocity - inertia might be used in the future, but for this case I don't believe it is necessary
            ResetVelocity();

            // Cache distances
            BuildCacheDistances();

            // Compute velocity on this step
            ComputeVelocities();

            // Update positions
            ComputePositions(dt);

            if (mergeDistance > 0.0f)
            {
                // Cache distances
                UpdateSquareDistances();

                // Merge points
                MergePoints();
            }
        }
    }

    public bool GetPositionByExternalIndex(int externalId, out Vector3 pos)
    {
        if (childPoint.TryGetValue(externalId, out var nextId))
        {
            return GetPositionByExternalIndex(nextId, out pos);
        }
        
        var pt = points[externalId];
        if (pt != null)
        {
            pos = pt.position;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }
}
