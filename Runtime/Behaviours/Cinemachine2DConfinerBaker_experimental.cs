using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
namespace Cinemachine { 
    
[ExecuteInEditMode]
public class Cinemachine2DConfinerBaker_experimental : CinemachineExtension
{
    private bool IsCacheValid = false;
    private CinemachineVirtualCamera m_vcam;

    [Tooltip("Undersized areas will be replaced by lines. The resolution of this line depends on this parameter.")]
    [Range(4, 100)]
    public int UnderSizedAreaResolution = 10;
    
    public PolygonCollider2D InputConfiner;
    private PolygonCollider2D OutputConfiner;

    private CinemachineConfiner m_confiner;
    
    private bool ClockwiseOrientation = true;
    
    private float DegreeThreshold = 5f;
    private bool SubdivideConfiner = false;
    private float SubdivideConfinerScale = 1;

    private float cameraViewWidth;
    private float cameraViewHeight;
    private float cameraDiagonal;

    private Vector2[] cameraViewDiagonalOffsetsFromMid;
    private Vector2[] cameraViewVerticalAndHorizontalOffsetsFromMid;
    private Vector2[] cameraViewOffsetsFromMid;

    private int RollCount;
    
    public void InvalidateCache()
    {
        IsCacheValid = false;
    }

    private void OnValidate()
    {
        InvalidateCache();
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, 
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (!IsCacheValid || IngredientsChanged())
        {
            Bake();

            if (m_confiner == null)
            {
                m_confiner = GetComponent<CinemachineConfiner>();
                if (m_confiner == null)
                {
                    m_confiner = m_vcam.gameObject.AddComponent<CinemachineConfiner>();
                    m_vcam.AddExtension(m_confiner);
                }
            }

            m_confiner.m_ConfineMode = CinemachineConfiner.Mode.Confine2D;
            m_confiner.m_ConfineScreenEdges = false;
            m_confiner.m_BoundingShape2D = OutputConfiner;
        }
    }
    
    private struct ConfinerIngredients
    {
        public Quaternion CorrectedOrientation;
        public float LensOrthographicSize;
        public float LensAspect;
    }

    private class ConfinerPoint
    {
        public Vector2 point;
        public Vector2 normal;
        public Vector2 edgeNormal;

        public bool removed;
        public Vector2 offset;
        public Vector2 borderPoint;

        public List<int> newPointsSorting;
        public List<NewPoints> newPoints;

        public bool IsInsideKnot;
    }

    private class NewPoints
    {
        public int ID;
        public List<Vector2> points0;
        public List<Vector2> points1;
        public List<Vector2> points2;

        public NewPoints()
        {
            points0 = new List<Vector2>();
            points1 = new List<Vector2>();
            points2 = new List<Vector2>();
        }
        
        public NewPoints(int p1, int p2) 
        {
            points0 = new List<Vector2>();
            points1 = new List<Vector2>(p1);
            points2 = new List<Vector2>(p2);
        }
    }

    private ConfinerIngredients ingredients;

    // Vcam that is being confined is changed -> invalidate
    //     - because the lens settings may change
    private void GatherIngredients()
    {
        ingredients = new ConfinerIngredients();
        ingredients.CorrectedOrientation = m_vcam.State.CorrectedOrientation;
        ingredients.LensOrthographicSize = m_vcam.State.Lens.OrthographicSize;
        ingredients.LensAspect = m_vcam.State.Lens.Aspect;
    }

    private bool IngredientsChanged()
    {
        if (m_vcam == null)
        {
            m_vcam = GetComponent<CinemachineVirtualCamera>();
            return true;
        }
        
        return ingredients.CorrectedOrientation != m_vcam.State.CorrectedOrientation ||
        Math.Abs(ingredients.LensOrthographicSize - m_vcam.State.Lens.OrthographicSize) > 1e-3f ||
        Math.Abs(ingredients.LensAspect - m_vcam.State.Lens.Aspect) > 1e-3f;
    }

    private void Bake()
    {
        if (m_vcam == null)
        {
            m_vcam = GetComponent<CinemachineVirtualCamera>();
        }

        GatherIngredients();
        InitializeCameraViewOffsets();

        InitializePointsAndNormals(InputConfiner.GetPath(0), InputConfiner.transform.position,
            out List<Vector2> points, out List<Vector2> pointNormals, out List<Vector2> edgeNormals);

        List<ConfinerPoint> confinerPoints = new List<ConfinerPoint>(points.Count);
        for (int i = 0; i < points.Count; ++i)
        {
            Vector2 point = points[i];
            Vector2 pointNormal = pointNormals[i];

            Vector2 offset = FindOffsetClosestToPointNormal(pointNormal);
            Vector2 reducedOffset = offset;

            confinerPoints.Add(new ConfinerPoint
            {
                point = points[i],
                normal = pointNormals[i],
                edgeNormal = edgeNormals[i],

                removed = false,

                offset = offset,
                borderPoint = point + offset,

                newPoints = new List<NewPoints>(),
            });
        }

        for (int i = 0; i < confinerPoints.Count; ++i) {
            confinerPoints[i].IsInsideKnot = IsPointInsideKnot(confinerPoints, i);
        }
        
        RollCount = RollUntilFirstIsNotKnot(ref confinerPoints);
        if (RollCount >= confinerPoints.Count)
        {
            IsCacheValid = true;
            return;
        }
        
        var knots = FindKnots(in confinerPoints);
        if (!FindOffsetKnots(ref knots, ref confinerPoints))
        {
            IsCacheValid = true;
            return;
        }
        
        DivideEntanglementsIntoSingleAndDoubleKnots(confinerPoints, knots, confinerPoints.Count,
            out List<Intersection> singleKnots, out List<Intersection> doubleKnots);
        OrderDoubleKnots(ref doubleKnots, confinerPoints.Count);

        for (int i = 0; i < singleKnots.Count; ++i)
        {
            DisentangleSingleKnot(ref confinerPoints, ref singleKnots, i);
        }
        
        for (int i = 0; i < doubleKnots.Count; i+=2)
        { 
            DisentangleDoubleKnot(ref confinerPoints, ref doubleKnots, i, i+1);
        }

        List<Vector2> borderPath = new List<Vector2>(confinerPoints.Count);
        for (int cp = 0; cp < confinerPoints.Count; ++cp)
        {
            if (!confinerPoints[cp].removed)
            {
                borderPath.Add(confinerPoints[cp].borderPoint);
            }
            
            if (confinerPoints[cp].newPoints.Count > 1)
            {
                confinerPoints[cp].newPoints.Sort((a, b) =>
                {
                    if (a.ID >= cp && b.ID >= cp)
                    {
                        return a.ID.CompareTo(b.ID);
                    }

                    if (a.ID >= cp)
                    {
                        return (-a.ID).CompareTo(b.ID);
                    }

                    if (b.ID >= cp)
                    {
                        return a.ID.CompareTo(-b.ID);
                    }
                    
                    return a.ID.CompareTo(b.ID);
                });
            }
            
            int selfIndex = -1;
            if (confinerPoints[cp].newPoints.Count <= 0)
            {
                continue;
            }
            for (int np = 0; np < confinerPoints[cp].newPoints.Count; ++np)
            {
                borderPath.AddRange(confinerPoints[cp].newPoints[np].points0);
            }
            for (int np = 0; np < confinerPoints[cp].newPoints.Count; ++np)
            {
                if (confinerPoints[cp].newPoints[np].ID == cp)
                {
                    selfIndex = np;
                    continue;
                }
                borderPath.AddRange(confinerPoints[cp].newPoints[np].points1);
            }
            for (int np = confinerPoints[cp].newPoints.Count - 1; np >= 0; --np)
            {
                if (confinerPoints[cp].newPoints[np].ID == cp)
                {
                    selfIndex = np;
                    continue;
                }
                borderPath.AddRange(confinerPoints[cp].newPoints[np].points2);
            }

            if (selfIndex != -1)
            {
                borderPath.AddRange(confinerPoints[cp].newPoints[selfIndex].points1);
                borderPath.AddRange(confinerPoints[cp].newPoints[selfIndex].points2);
            }
        }

        InitializeOutputConfiner(ref OutputConfiner);
        OutputConfiner.SetPath(0, borderPath);
        IsCacheValid = true;
    }

    private void InitializeOutputConfiner(ref PolygonCollider2D OutputConfiner)
    {
        if (OutputConfiner == null)
        {
            var polygonCollider2Ds = InputConfiner.GetComponentsInChildren<PolygonCollider2D>();
            for (int i = 0; i < polygonCollider2Ds.Length; ++i)
            {
                if (polygonCollider2Ds[i].gameObject.name == "CM_BakedOutputConfiner")
                {
                    OutputConfiner = polygonCollider2Ds[i];
                    return;
                }
            }

            var outputGO = new GameObject("CM_BakedOutputConfiner");
            outputGO.transform.parent = InputConfiner.gameObject.transform;
            OutputConfiner = outputGO.AddComponent<PolygonCollider2D>();
        }
    }

    private bool FindOffsetKnots(ref List<Intersection> knots, ref List<ConfinerPoint> confinerPoints)
    {
        List<int> knotIndex = new List<int>();
        List<int> confinerPointIndex = new List<int>();
        for (int i = 0; i < knots.Count; ++i)
        {
            var intersectionPoint = knots[i].intersectionPoint;
            if (Vector2Equals(confinerPoints[knots[i].s1].borderPoint, intersectionPoint))
            {
                knotIndex.Add(i);
                confinerPointIndex.Add(knots[i].s1);
            }
            if (Vector2Equals(confinerPoints[knots[i].e1].borderPoint, intersectionPoint))
            {
                knotIndex.Add(i);
                confinerPointIndex.Add(knots[i].e1);
            }
            if (Vector2Equals(confinerPoints[knots[i].s2].borderPoint, intersectionPoint))
            {
                knotIndex.Add(i);
                confinerPointIndex.Add(knots[i].s2);
            }
            if (Vector2Equals(confinerPoints[knots[i].e2].borderPoint, intersectionPoint))
            {
                knotIndex.Add(i);
                confinerPointIndex.Add(knots[i].e2);
            }
        }

        if (knotIndex.Count % 2 == 1)
        {
            return false;
        }
        for (int i = 0; i < knotIndex.Count; ++i)
        {
            for (int j = i + 1; j < knotIndex.Count; ++j)
            {
                if (knotIndex[i] >= knots.Count || knotIndex[j] >= knots.Count)
                {
                    continue;
                }
                UnityVectorExtensions.FindIntersection(
                    confinerPoints[confinerPointIndex[i]].point, confinerPoints[confinerPointIndex[i]].borderPoint,
                    confinerPoints[confinerPointIndex[j]].point, confinerPoints[confinerPointIndex[j]].borderPoint,
                    out bool linesIntersect, out bool segmentsIntersect, out Vector2 intersection);

                if (segmentsIntersect)
                {
                    var newIntersection = (confinerPoints[confinerPointIndex[i]].borderPoint +
                                           confinerPoints[confinerPointIndex[j]].borderPoint) / 2f;
                    knots[knotIndex[i]].intersectionPoint = newIntersection;
                    // knots[knotIndex[i]].s2 = knots[knotIndex[j]].s1;
                    // knots[knotIndex[i]].e2 = knots[knotIndex[j]].e1;
                    knots[knotIndex[i]].s1 = knots[knotIndex[j]].s1;
                    knots[knotIndex[i]].e1 = knots[knotIndex[j]].e1;
                    
                    knots.RemoveAt(knotIndex[j]);
                    break;
                }
            }
        }

        return true;
    }

    private int RollUntilFirstIsNotKnot(ref List<ConfinerPoint> confinerPoints)
    {
        int rollCount = 0;

        while (confinerPoints[0].IsInsideKnot && rollCount <= confinerPoints.Count)
        {
            RollByOne(ref confinerPoints);
            ++rollCount;
        }

        return rollCount;
    }

    private void RollByOne(ref List<ConfinerPoint> confinerPoints)
    {
        var first = confinerPoints[0];
        for (int i = 0; i < confinerPoints.Count - 1; ++i)
        {
            confinerPoints[i] = confinerPoints[i + 1];
        }
        confinerPoints[confinerPoints.Count - 1] = first;
    }

    private bool IsPointInsideKnot(List<ConfinerPoint> confinerPoints, int indexToCheck)
    {
        var p1 = confinerPoints[indexToCheck].borderPoint;
        var p2 = confinerPoints[indexToCheck].point;
        for (int j = 0; j < confinerPoints.Count; ++j)
        {
            for (int i = 0; i < cameraViewOffsetsFromMid.Length; ++i)
            {
                UnityVectorExtensions.FindIntersection(
                    p1, p1 + cameraViewOffsetsFromMid[i],
                    confinerPoints[j].point, confinerPoints[(j + 1) % confinerPoints.Count].point,
                    out bool linesIntersect, out bool segmentsIntersect, out Vector2 intersection);

                if (segmentsIntersect)
                {
                    float distanceSqr = (intersection - p1).sqrMagnitude;
                    if (Mathf.Abs(distanceSqr - cameraViewOffsetsFromMid[i].sqrMagnitude) > 0.5f
                    ) // todo: magnitude is const so precalculate
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
    
    private void DivideEntanglementsIntoSingleAndDoubleKnots(
        in List<ConfinerPoint> confinerPoints, in List<Intersection> knots, int numOfPoints,
        out List<Intersection> singleKnots, out List<Intersection> doubleKnots)
    {
        singleKnots = new List<Intersection>();
        doubleKnots = new List<Intersection>();

        for (int i = 0; i < knots.Count; ++i)
        {
            // DOUBLE KNOTS SHOULD BE CONNECTED BY IsInsideKnot points
            bool isSingleKnot = IsSingleKnot(confinerPoints, knots[i]);
            if (!isSingleKnot && i < knots.Count - 1)
            {
                doubleKnots.Add(knots[i]);
                doubleKnots.Add(knots[i + 1]);
                ++i;
            }
            else
            {
                singleKnots.Add(knots[i]);
            }
        }
    }

    private bool IsSingleKnot(in List<ConfinerPoint> confinerPoints, in Intersection i1)
    {
        bool singleKnot = true;
        int start = CircularIndex(i1.e1 , confinerPoints.Count);
        int end = CircularIndex(i1.s2 , confinerPoints.Count);
        while (start != end)
        {
            if (!confinerPoints[start].IsInsideKnot)
            {
                singleKnot = false;
                break;
            }
            start = (start + 1) % confinerPoints.Count;
        }

        if (singleKnot)
        {
            return true;
        }

        start = i1.e2;
        end = i1.s1;
        while (start != end)
        {
            if (!confinerPoints[start].IsInsideKnot)
            {
                singleKnot = false;
                break;
            }
            start = (start + 1) % confinerPoints.Count;
        }

        return singleKnot;
    }

    private bool AreDoubleKnots(in List<ConfinerPoint> confinerPoints, in Intersection i1, in Intersection i2)
    {
        int start = (i1.e1 + 1) % confinerPoints.Count;
        int end = i2.s1;
        while (start != end)
        {
            if (!confinerPoints[start].IsInsideKnot)
            {
                return false;
            }
            
            start = (start + 1) % confinerPoints.Count;
        }

        return true;
    }
    
    private void OrderDoubleKnots(ref List<Intersection> doubleKnots, int numberOfPoints)
    {
        int cleanIndex = FindCleanIndexForDoubleKnots(doubleKnots, numberOfPoints);
        int doubleKnotStartIndex = 0;
        for (int k = 0; k < doubleKnots.Count; ++k)
        {
            if (doubleKnots[k].s1 > cleanIndex)
            {
                doubleKnotStartIndex = k;
                break;
            }
        }

        RollDoubleKnotsToCorrectOrder(ref doubleKnots, doubleKnotStartIndex);
        int wtf = 3;
    }

    private void RollDoubleKnotsToCorrectOrder(ref List<Intersection> doubleKnots, int startIndex)
    {
        for (int i = 0; i < startIndex; ++i)
        {
            RollByOne(ref doubleKnots);
        }
    }
    private void RollByOne(ref List<Intersection> knots)
    {
        var first = new Intersection
        {
            intersectionPoint = knots[0].intersectionPoint,
            s1 = knots[0].s1,
            e1 = knots[0].e1,
            s2 = knots[0].s2,
            e2 = knots[0].e2,
        };
        for (int i = 0; i < knots.Count - 1; ++i)
        {
            knots[i] = knots[i + 1];
        }
        knots[knots.Count - 1] = first;
    }

    private int FindCleanIndexForDoubleKnots(in List<Intersection> doubleKnots, int numberOfPoints)
    {
        // double knots can be defined by the first not single knot after a single knot must be the start of a double knot.
        // or if index is not part of a not single knot, then the next index part of a knot is a start index

        int cleanIndex = -1;
        for (int p = 0; p < numberOfPoints; ++p)
        {
            bool clean = true;
            for (int k = 0; k < doubleKnots.Count; ++k)
            {
                if (p == doubleKnots[k].s1 ||
                    p == doubleKnots[k].e1 ||
                    p == doubleKnots[k].s2 ||
                    p == doubleKnots[k].e2)
                {
                    clean = false;
                    break;
                }
            }

            if (clean)
            {
                cleanIndex = p;
                return cleanIndex;
            }
        }

        return cleanIndex;
    }

    private bool IsKnotBetween(in List<Intersection> knots, int ignoreKnot, int start, int end, int numOfPoints)
    {
        int index = (start + 1) % numOfPoints;
        while (index != end)
        {
            for (int k = 0; k < knots.Count; ++k)
            {
                if (k == ignoreKnot) continue;
                
                if (knots[k].s1 == index || knots[k].e1 == index || knots[k].s2 == index || knots[k].e2 == index)
                {
                    return true;
                }
            }
            index = (index + 1) % numOfPoints;
        }
        return false;
    }
    
    private class SortBasedOnDistanceFromStartPointComparer : IComparer<Vector2>
    {
        private Vector2 start;
        public int Compare(Vector2 x, Vector2 y)
        {
            float x_dist = (x - start).sqrMagnitude;
            float y_dist = (y - start).sqrMagnitude;
            
            return x_dist.CompareTo(y_dist);
        }
        
        public SortBasedOnDistanceFromStartPointComparer()
        {
            start = Vector2.zero;
        }
        public SortBasedOnDistanceFromStartPointComparer(Vector2 startOfLine)
        {
            start = startOfLine;
        }
    }

    private void SortPointsAlongLine(ref List<Vector2> points, in Vector2 startOfLine)
    {
        points.Sort(new SortBasedOnDistanceFromStartPointComparer(startOfLine));
    }

    private bool Vector2Equals(Vector2 a, Vector2 b, float tolerance = 1e-1f)
    {
        return Math.Abs(a.x - b.x) < tolerance &&
               Math.Abs(a.y - b.y) < tolerance;
    }
    
    private int CircularIndex(int i, int count)
    {
        return i < 0 ? count - ((-i) % count) : i % count;
    }

    private bool CircularIsBetweenOrEqual(int left, int right, int v)
    {
        return left > right ? v <= right || left <= v : left <= v && v <= right;
    }
    private bool CircularIsBetween(int left, int right, int v)
    {
        return left > right ? v < right || left < v : left < v && v < right;
    }
    
    private Vector2 FindOffsetClosestToPointNormal(in Vector2 normal)
    {
        foreach (var offset in cameraViewVerticalAndHorizontalOffsetsFromMid)
        {
            if (Vector2.Angle(offset.normalized, normal.normalized) <= DegreeThreshold)
            {
                return offset;
            }
        }
        
        float minAngle = 360;
        int index = 0;
        for (int i = 0; i < cameraViewDiagonalOffsetsFromMid.Length; ++i)
        {
            Vector2 offset = cameraViewDiagonalOffsetsFromMid[i];
            float angle = Vector2.Angle(offset.normalized, normal.normalized);
            if (angle < minAngle)
            {
                minAngle = angle;
                index = i;
            }
        }

        return cameraViewDiagonalOffsetsFromMid[index];
    }

    private class Intersection
    {
        public Vector2 intersectionPoint;
        // public int s1; // segment1StartIndex
        // public int e1; // segment1EndIndex
        // public int s2; // segment2StartIndex
        // public int e2; // segment2EndIndex

        public int s1;
        public int e1;
        public int s2;
        public int e2;
    }

    private List<Intersection> FindKnots(in List<ConfinerPoint> confinerPoints)
    {
        List<Intersection> _intersections = new List<Intersection>();
        bool entangled = false;
        for (int i = 0; i < confinerPoints.Count - 2; ++i)
        {
            int s1 = i;
            int e1 = s1 + 1;
            for (int j = e1 + 1; j < confinerPoints.Count; ++j)
            {
                int s2 = j;
                int e2 = (j + 1) % confinerPoints.Count;
                if (s1 == s2 || s1 == e2 ||
                    e1 == s2 || e1 == e2)
                {
                    // ignore itself or direct neighbour 
                    continue;
                }
                
                UnityVectorExtensions.FindIntersection(
                    confinerPoints[s1].borderPoint, confinerPoints[e1].borderPoint, 
                    confinerPoints[s2].borderPoint, confinerPoints[e2].borderPoint,
                    out bool lines_intersect, out bool segments_intersect,
                    out Vector2 intersection);

                
                if (segments_intersect)
                {
                    _intersections.Add(new Intersection
                    {
                        intersectionPoint = intersection,
                        s1 = s1,
                        e1 = e1,
                        s2 = s2,
                        e2 = e2,
                    });
                }
            }
        }
        return _intersections;
    }

    private void DisentangleSingleKnot(ref List<ConfinerPoint> confinerPoints, ref List<Intersection> knots, int i1)
    {
        int start = knots[i1].s1;
        int end = knots[i1].e2;
        // if (ReverseOrder)
        // {
        //     // TODO
        // int start = knots[i1].s2;
        // int end = knots[i1].e1;
        // }

        BisectLine(confinerPoints, start, end, knots[i1].intersectionPoint,
            out List<Vector2> line1, out List<Vector2> line2);
        
        MarkPointsRemovedInPathBetweenExclusive(ref confinerPoints, start, end);
        
        // Line1 - Line2 pairwise point swarm average -> midLine
        line2.Reverse();
        var midLine = Midline(line1, line2);

        // i1.s1-> newPoints -> midLine + midline_reversed
        var newPoints = new NewPoints(midLine.Count, midLine.Count);
        for (int i = 0; i < midLine.Count; ++i)
        {
            newPoints.points1.Add(midLine[i]);
        }
        for (int i = 0; i < midLine.Count; ++i)
        {
            newPoints.points2.Add(midLine[midLine.Count - 1 - i]);
        }

        newPoints.ID = start;
        confinerPoints[start].newPoints.Insert(0, newPoints);
    }
  
    private void BisectLine(in List<ConfinerPoint> confinerPoints, 
        in int start, in int end, in Vector2 intersectionPoint,
        out List<Vector2> line1, out List<Vector2> line2)
    {
        if (GetClosestOffsetIntersectionBetweenExclusive(confinerPoints, start, end, 
            out Vector2 point, out int indexP1, out int indexP2))
        {
            // var edgeNormal = confinerPoints[indexP1].edgeNormal;
            // UnityVectorExtensions.FindIntersection(
            //     confinerPoints[indexP1].borderPoint, 
            //     confinerPoints[indexP2].borderPoint,
            //     point, point + edgeNormal,
            //     out bool lines_intersect, out bool segment_intersect, out Vector2 ip);
            
            line1 = GetPointsInPathBetweenExclusive(confinerPoints, start, indexP1);
            line1.Insert(0, intersectionPoint);
            line1.Insert(line1.Count, confinerPoints[indexP1].borderPoint);
            line2 = GetPointsInPathBetweenExclusive(confinerPoints, indexP2, end);
            line2.Insert(0, confinerPoints[indexP2].borderPoint);
            line2.Insert(line2.Count, intersectionPoint);
        }
        else
        {
            var line = GetPointsInPathBetweenExclusive(confinerPoints, start, end);
            line.Insert(0, intersectionPoint);
            line.Insert(line.Count, intersectionPoint);
            BisectLineIntoTwoEqualLengthLines(line, out line1, out line2);
        }
    }

    private bool BisectLineIntoTwoEqualLengthLines(in List<Vector2> line, 
        out List<Vector2> line1, out List<Vector2> line2)
    {
        float lineLength = LineLength(line);
        line1 = new List<Vector2>();
        Vector2 halfPoint = Vector2.zero;
        int i;
        {
            line1.Add(line[0]);

            int line1LastIndex = 0;
            float line1Length = 0;
            for (i = 1; i < line.Count - 1; ++i)
            {
                float length = (line1[line1LastIndex] - line[i]).magnitude;
                if (line1Length + length <= lineLength / 2f)
                {
                    line1Length += length;
                    line1.Add(line[i]);
                    line1LastIndex++;
                    halfPoint = line1[line1LastIndex];
                }
                else
                {
                    float remainingDistance = (lineLength / 2f) - line1Length;
                    line1Length += remainingDistance;
                    Vector2 directionVector = line[i] - line1[line1LastIndex];
                    line1.Add(line1[line1LastIndex] + directionVector.normalized * remainingDistance);
                    line1LastIndex++;
                    halfPoint = line1[line1LastIndex];
                    break;
                }
            }
        }
        
        line2 = new List<Vector2>();
        line2.Add(halfPoint);
        for (int j = i; j < line.Count; ++j)
        {
            line2.Add(line[j]);
        }

        return true;
    }
    
    private float LineLength(in List<Vector2> line)
    {
        float length = 0;
        for (int i = 0; i < line.Count - 1; ++i)
        {
            length += Vector2.Distance(line[i], line[i + 1]);
        }
        return length;
    }

    private void DisentangleDoubleKnot(ref List<ConfinerPoint> confinerPoints,
        ref List<Intersection> intersections, int i1, int i2)
    {
        List<Vector2> newPath = new List<Vector2>();
        int start = intersections[i1].s1;
        int end = intersections[i1].e2;
        int numOfPointsToHalf = 0;
        int iterationCount = 0;
        int maxIteration = Mathf.Abs(end - start);
        int i = start;
        while (i != end)
        {
            bool simpleAdd = true;
            if (i == intersections[i1].s1)
            {
                newPath.Add(intersections[i1].intersectionPoint);
                simpleAdd = false;
            }
            if (i == intersections[i2].s1)
            {
                newPath.Add(confinerPoints[i].borderPoint);
                confinerPoints[i].removed = true;
                newPath.Add(intersections[i2].intersectionPoint);
                numOfPointsToHalf = newPath.Count;
                newPath.Add(intersections[i2].intersectionPoint);
                i = intersections[i2].s2;
                simpleAdd = false;
            }
            if (i == intersections[i1].s2)
            {
                if (CircularIsBetweenOrEqual(intersections[i2].e2, intersections[i1].e2, intersections[i1].s2))
                {
                    // cases:
                    // legend: I=i1, J=i2, s=s1, e=e1, S=s2, E=e2
                    // 
                    //                        e------+
                    //                        |      |
                    //                        |      |
                    //               SE-------J------S
                    //               |  knot  |
                    //               |  knot  |
                    //s--------------I--------es
                    //               |
                    //               |
                    //               E
                    
                    //      s
                    //      |
                    //E-----I-------------S
                    //      |             |
                    //      e-------+     |
                    //              |     |
                    //              |     |
                    //              |     |
                    //        s-----S--J--e-------E
                    newPath.Add(confinerPoints[i].borderPoint);
                    confinerPoints[i].removed = true;
                }
                newPath.Add(intersections[i1].intersectionPoint);
                simpleAdd = false;
                break;
            }
            
            if (simpleAdd)
            {
                newPath.Add(confinerPoints[i].borderPoint);
                confinerPoints[i].removed = true;
            }

            i = CircularIndex(i + 1, confinerPoints.Count);
            ++iterationCount;
            if (iterationCount > maxIteration)
            {
                break;
            }
        }
        
        List<Vector2> line1 = newPath.GetRange(0, numOfPointsToHalf);
        List<Vector2> line2 = newPath.GetRange(numOfPointsToHalf, newPath.Count - numOfPointsToHalf);
        line2.Reverse();
        List<Vector2> midLine = Midline(line1, line2);
        
        // i1.s1-> newPoints -> midLine
        {
            var newPoints1 = new NewPoints(midLine.Count, 0);
            for (i = 0; i < midLine.Count; ++i)
            {
                newPoints1.points1.Add(midLine[i]);
            }

            newPoints1.ID = intersections[i1].s1;
            confinerPoints[intersections[i1].s1].newPoints.Add(newPoints1);
        }
        
        // i2.s2-> newPoints -> midLine_reversed
        {
            var newPoints2 = new NewPoints(0, midLine.Count);
            for (i = 0; i < midLine.Count; ++i)
            {
                newPoints2.points2.Add(midLine[midLine.Count - 1 - i]);
            }

            newPoints2.ID = intersections[i1].s1;
            confinerPoints[intersections[i2].s2].newPoints.Add(newPoints2);
        }
    }
    
    private void FixIntersectionOrder(ref List<Intersection> intersections, int i1, int i2)
    {
        bool swapI2_1 = CircularIsBetween(intersections[i1].s1, intersections[i2].s1, intersections[i1].e1);
        bool swapI2_2 = CircularIsBetween(intersections[i1].s1, intersections[i2].e1, intersections[i1].e1);
        bool swapI2_3 = CircularIsBetween(intersections[i1].s1, intersections[i2].e1, intersections[i2].s1);
        bool swapI2_4 = CircularIsBetween(intersections[i1].e1, intersections[i2].e1, intersections[i2].s1);
        bool swapI2 = !(swapI2_1 && swapI2_2 && swapI2_3 && swapI2_4);
        bool swapI1_1 = CircularIsBetween(intersections[i1].s1, intersections[i1].e2, intersections[i1].e1);
        bool swapI1_2 = CircularIsBetween(intersections[i1].s1, intersections[i1].e2, intersections[i2].s1);
        bool swapI1_3 = CircularIsBetween(intersections[i1].s1, intersections[i1].e2, intersections[i2].e1);
        bool swapI1_4 = CircularIsBetween(intersections[i1].s1, intersections[i1].e2, intersections[i2].s2);
        bool swapI1 = !(swapI1_1 && swapI1_2 && swapI1_3 && swapI1_4);

        if (swapI1)
        {
            var i = intersections[i1];
            (i.s1, i.s2) = (i.s2, i.s1);
            (i.e1, i.e2) = (i.e2, i.e1);
            intersections[i1] = i;
        }
        if (swapI2)
        {
            var i = intersections[i2];
            (i.s1, i.s2) = (i.s2, i.s1);
            (i.e1, i.e2) = (i.e2, i.e1);
            intersections[i2] = i;
        }
    }
 
    private List<Vector2> Midline(in List<Vector2> line1, in List<Vector2> line2)
    {
        List<Vector2> midLine = new List<Vector2>(UnderSizedAreaResolution);

        float line1Length = LineLength(line1);
        float line1SubsegmentLength = line1Length / UnderSizedAreaResolution;
        float line2Length = LineLength(line2);
        float line2SubsegmentLength = line2Length / UnderSizedAreaResolution;

        var line1Subdivided = SubdivideLine(line1, line1SubsegmentLength, UnderSizedAreaResolution);
        var line2Subdivided = SubdivideLine(line2, line2SubsegmentLength, UnderSizedAreaResolution);

        if (line1Subdivided.Count <= 0)
        {
            midLine.AddRange(line2Subdivided);
        }
        else if (line2Subdivided.Count <= 0)
        {
            midLine.AddRange(line1Subdivided);
        }
        else
        {
            for (int i = 0; i <= UnderSizedAreaResolution; ++i)
            {
                midLine.Add((line1Subdivided[i] + line2Subdivided[i]) / 2f);
            }
        }
        
        
        return midLine;
    }

    private List<Vector2> SubdivideLine(in List<Vector2> line, in float segmentLength, in int subdivisionCount = 0)
    {
        List<Vector2> subdividedLine = new List<Vector2>(subdivisionCount);
        if (line == null || line.Count <= 0)
        {
            return subdividedLine;
        }
        subdividedLine.Add(line[0]);
        
        float leftOver = 0;
        for (int i = 0; i < line.Count - 1; ++i)
        {
            Vector2 startingPoint = line[i];
            while (true)
            {
                var direction = line[i + 1] - startingPoint;
                if (direction.magnitude >= (segmentLength - leftOver))
                {
                    startingPoint += direction.normalized * (segmentLength - leftOver);
                    subdividedLine.Add(startingPoint);
                    leftOver = 0;
                }
                else
                {
                    leftOver += direction.magnitude;
                    break;
                }
            }
        }
        
        if (leftOver > segmentLength / 4f)
        {
            subdividedLine.Add(line[line.Count-1]);
        }
        return subdividedLine;
    }
    
    private bool GetClosestOffsetIntersectionBetweenExclusive(
        in List<ConfinerPoint> confinerPoints, in int start, in int end,
        out Vector2 intersection, out int indexP1, out int indexP2)
    {
        intersection = new Vector2();
        indexP1 = 0;
        indexP2 = 0;
        if (start > end)
        {
            for (int i = start + 1; i < confinerPoints.Count; ++i)
            {
                indexP1 = i;
                indexP2 = (i + 1) % confinerPoints.Count;
                UnityVectorExtensions.FindIntersection(
                    confinerPoints[indexP1].point, confinerPoints[indexP1].borderPoint,
                    confinerPoints[indexP2].point, confinerPoints[indexP2].borderPoint,
                    out bool lines_intersect, out bool segment_intersect, out intersection);

                if (segment_intersect)
                {
                    return true;
                }
            }
            for (int i = 0; i < end - 1; ++i)
            {
                indexP1 = i;
                indexP2 = (i + 1) % confinerPoints.Count;
                UnityVectorExtensions.FindIntersection(
                    confinerPoints[indexP1].point, confinerPoints[indexP1].borderPoint,
                    confinerPoints[indexP2].point, confinerPoints[indexP2].borderPoint,
                    out bool lines_intersect, out bool segment_intersect, out intersection);
                
                if (segment_intersect)
                {
                    return true;
                }
            }
        }
        else
        {
            for (int i = start + 1; i < end - 1; ++i)
            {
                indexP1 = i;
                indexP2 = (i + 1) % confinerPoints.Count;
                UnityVectorExtensions.FindIntersection(
                    confinerPoints[indexP1].point, confinerPoints[indexP1].borderPoint,
                    confinerPoints[indexP2].point, confinerPoints[indexP2].borderPoint,
                    out bool lines_intersect, out bool segment_intersect, out intersection);
                
                if (segment_intersect)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private List<Vector2> GetPointsInPathBetweenExclusive(in List<ConfinerPoint> confinerPoints, 
        in int start, in int end)
    {
        List<Vector2> points = new List<Vector2>();
        if (start > end)
        {
            for (int i = start + 1; i < confinerPoints.Count; ++i)
            {
                points.Add(confinerPoints[i].borderPoint);
            }
            for (int i = 0; i <= end - 1; ++i)
            {
                points.Add(confinerPoints[i].borderPoint);
            }
        }
        else
        {
            for (int i = start + 1; i <= end - 1; ++i)
            {
                points.Add(confinerPoints[i].borderPoint);
            }
        }
        return points;
    }
    
    private void MarkPointsRemovedInPathBetweenExclusive(ref List<ConfinerPoint> confinerPoints, 
        in int start, in int end)
    {
        if (start > end)
        {
            for (int i = start + 1; i < confinerPoints.Count; ++i)
            {
                confinerPoints[i].removed = true;
            }
            for (int i = 0; i <= end - 1; ++i)
            {
                confinerPoints[i].removed = true;
            }
        }
        else
        {
            for (int i = start + 1; i <= end - 1; ++i)
            {
                confinerPoints[i].removed = true;
            }
        }
    }
    
    private Vector2 AverageVectors(in List<Vector2> vectors)
    {
        Vector2 average = Vector2.zero;
        for (int i = 0; i < vectors.Count; ++i)
        {
            average += vectors[i];
        }
        return average / vectors.Count;
    }
    
    private Vector2 Vector2SelectMin(in Vector2 a, in Vector2 b)
    {
        return a.sqrMagnitude < b.sqrMagnitude ? a : b;
    }

    private void InitializeCameraViewOffsets()
    {
        Quaternion rot = Quaternion.Inverse(ingredients.CorrectedOrientation);
        float dy = ingredients.LensOrthographicSize;
        float dx = dy * ingredients.LensAspect;
        
        Vector2 up = (rot * Vector3.up) * dy;
        Vector2 right = (rot * Vector3.right) * dx;
        Vector2 down = -up;
        Vector2 left = -right;
        Vector2 diagonalUpRight = (up + right);
        Vector2 diagonalUpLeft = (up + left);
        Vector2 diagonalDownRight = (down + right);
        Vector2 diagonalDownLeft = (down + left);
        
        cameraViewDiagonalOffsetsFromMid = new[] {diagonalUpRight, diagonalUpLeft, diagonalDownRight, diagonalDownLeft};
        cameraViewVerticalAndHorizontalOffsetsFromMid = new[] {up, down, left, right};
        cameraViewOffsetsFromMid = new[]
            {up, down, left, right, diagonalUpRight, diagonalUpLeft, diagonalDownRight, diagonalDownLeft};

        cameraViewWidth = right.magnitude * 2;
        cameraViewHeight = up.magnitude * 2;
        cameraDiagonal = diagonalUpRight.magnitude * 2;
    }

    private bool IsOrientationClockwise(Vector2[] path)
    {
        float sum = 0;
        for (int i = 0; i < path.Length; ++i)
        {
            var p1 = path[i];
            var p2 = path[(i + 1) % path.Length];

            sum += (p2.x - p1.x) * (p2.y + p1.y);
        }

        return sum > 0;
    }

    private void InitializePointsAndNormals(Vector2[] path, Vector2 pathOffset,
        out List<Vector2> points, out List<Vector2> pointNormals, out List<Vector2> edgeNormals)
    {

        ClockwiseOrientation = IsOrientationClockwise(path);
        
        points = new List<Vector2>(path.Length);
        for (int i = 0; i < path.Length; ++i)
        {
            points.Add(path[i] + pathOffset);
        }
        if (SubdivideConfiner)
        {

            int index = 0;
            float divisonSize = Mathf.Min(cameraViewWidth, cameraViewHeight) * SubdivideConfinerScale;
            while (index < points.Count)
            {
                var thisPoint = points[index];
                var nextPoint = points[(index + 1) % points.Count];
                
                float edgeLength = (nextPoint - thisPoint).magnitude;
                int divisionAmount = Mathf.CeilToInt(edgeLength / divisonSize);
                for (int d = 1; d < divisionAmount; ++d)
                {
                    points.Insert(index + d, Vector2.Lerp(thisPoint, nextPoint, (float)d / divisionAmount));
                }

                ++index;
            }
        }
        
        ComputeNormals(points, out pointNormals, out edgeNormals);
    }

    private void ComputeNormals(in List<Vector2> points, out List<Vector2> pointNormals, out List<Vector2> edgeNormals)
    {
        edgeNormals = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; ++i)
        {
            Vector2 edge = points[(i + 1) % points.Count] - points[i];
            Vector2 normal = ClockwiseOrientation ? new Vector2(edge.y, -edge.x) : new Vector2(-edge.y, edge.x);
            edgeNormals.Add(normal.normalized);
        }
        
        pointNormals = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; ++i)
        {
            int prevIndex = i == 0 ? points.Count - 1 : i - 1;
            Vector2 normal = (edgeNormals[i] + edgeNormals[prevIndex]) / 2f;

            if (edgeNormals[i] != edgeNormals[prevIndex])
            {
                // corner
            }
            
            pointNormals.Add(normal.normalized);
        }
    }
}
}