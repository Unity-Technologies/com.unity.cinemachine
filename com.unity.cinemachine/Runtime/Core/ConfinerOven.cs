#if CINEMACHINE_PHYSICS_2D

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Cinemachine
{
    using IntPoint = Point64;
    using IntRect = Rect64;

    /// <summary>
    /// Responsible for baking confiners via BakeConfiner function.
    /// </summary>
    class ConfinerOven
    {
        // Clipper works with fixed point numbers, so we need to scale the input
        // for appropriate precision.  The scaling factor is dynamically determined
        // according to the bounding box of the input polygon.
        class FloatToIntScaler
        {
            public float FloatToInt(float f) => f * m_FloatToInt;
            public float IntToFloat(long i) => i * m_IntToFloat;
            public float ClipperEpsilon => 0.01f * m_FloatToInt;

            readonly long m_FloatToInt;
            readonly float m_IntToFloat;

            public FloatToIntScaler(Rect polygonBounds)
            {
                const float kMinWorldSize = 100; const float kMaxWorldSize = 10000;
                float size = Mathf.Max(polygonBounds.width, polygonBounds.height);
                var t = Mathf.Max(0, size - kMinWorldSize) / (kMaxWorldSize - kMinWorldSize);
                m_FloatToInt = (long)Mathf.Lerp(100000, 100, t);
                m_IntToFloat = 1f / m_FloatToInt;
            }
        }

        public class BakedSolution
        {
            float m_FrustumSizeIntSpace;

            readonly AspectStretcher m_AspectStretcher;
            readonly bool m_HasBones;
            readonly double m_SqrPolygonDiagonal;

            List<List<IntPoint>> m_OriginalPolygon;
            internal List<List<IntPoint>> m_Solution;

            FloatToIntScaler m_FloatToInt;

            public BakedSolution(
                float aspectRatio, float frustumHeight, bool hasBones, Rect polygonBounds,
                List<List<IntPoint>> originalPolygon, List<List<IntPoint>> solution)
            {
                m_FloatToInt = new FloatToIntScaler(polygonBounds);
                m_AspectStretcher = new AspectStretcher(aspectRatio, polygonBounds.center.x);
                m_FrustumSizeIntSpace = m_FloatToInt.FloatToInt(frustumHeight);
                m_HasBones = hasBones;
                m_OriginalPolygon = originalPolygon;
                m_Solution = solution;

                float polygonSizeX = m_FloatToInt.FloatToInt(polygonBounds.width / aspectRatio);
                float polygonSizeY = m_FloatToInt.FloatToInt(polygonBounds.height);
                m_SqrPolygonDiagonal = polygonSizeX * polygonSizeX + polygonSizeY * polygonSizeY;
            }

            public bool IsValid() => m_Solution != null;

            public Vector2 ConfinePoint(in Vector2 pointToConfine)
            {
                if (m_Solution.Count <= 0)
                    return pointToConfine; // empty confiner -> no need to confine

                Vector2 pInConfinerSpace = m_AspectStretcher.Stretch(pointToConfine);
                var p = new IntPoint(m_FloatToInt.FloatToInt(pInConfinerSpace.x), m_FloatToInt.FloatToInt(pInConfinerSpace.y));
                for (int i = 0; i < m_Solution.Count; ++i)
                {
                    if (Clipper.PointInPolygon(p, m_Solution[i]) != PointInPolygonResult.IsOutside)
                    {
                        return pointToConfine; // inside, no confinement needed
                    }
                }

                // If the poly has bones and if the position to confine is not outside of the original
                // bounding shape, then it is possible that the bone in a neighbouring section
                // is closer than the bone in the correct section of the polygon, if the current section
                // is very large and the neighbouring section is small.  In that case, we'll need to
                // add an extra check when calculating the nearest point.
                bool checkIntersectOriginal = m_HasBones && IsInsideOriginal(p);

                // Confine point
                IntPoint closest = p;
                double minDistance = double.MaxValue;
                for (int i = 0; i < m_Solution.Count; ++i)
                {
                    int numPoints = m_Solution[i].Count;
                    for (int j = 0; j < numPoints; ++j)
                    {
                        IntPoint l1 = m_Solution[i][j];
                        IntPoint l2 = m_Solution[i][(j + 1) % numPoints];

                        IntPoint c = IntPointLerp(l1, l2, ClosestPointOnSegment(p, l1, l2));
                        double diffX = Mathf.Abs(p.X - c.X);
                        double diffY = Mathf.Abs(p.Y - c.Y);
                        double distance = diffX * diffX + diffY * diffY;

                        // penalty for points from which the target is not visible, preferring visibility over proximity
                        if (diffX > m_FrustumSizeIntSpace || diffY > m_FrustumSizeIntSpace)
                        {
                            distance += m_SqrPolygonDiagonal; // penalty is the biggest distance between any two points
                        }

                        if (distance < minDistance && (!checkIntersectOriginal || !DoesIntersectOriginal(p, c)))
                        {
                            minDistance = distance;
                            closest = c;
                        }
                    }
                }

                var result = new Vector2(m_FloatToInt.IntToFloat(closest.X), m_FloatToInt.IntToFloat(closest.Y));
                return m_AspectStretcher.Unstretch(result);

                // local functions
                IntPoint IntPointLerp(IntPoint a, IntPoint b, float lerp)
                {
                    return new IntPoint
                    {
                        X = Mathf.RoundToInt(a.X + (b.X - a.X) * lerp),
                        Y = Mathf.RoundToInt(a.Y + (b.Y - a.Y) * lerp),
                    };
                }

                bool IsInsideOriginal(IntPoint point)
                {
                    for (int p = 0; p < m_OriginalPolygon.Count; p++)
                    {
                        if (Clipper.PointInPolygon(point, m_OriginalPolygon[p]) != PointInPolygonResult.IsOutside)
                            return true;
                    }
                    return false;
                }

                float ClosestPointOnSegment(IntPoint point, IntPoint s0, IntPoint s1)
                {
                    double sX = s1.X - s0.X;
                    double sY = s1.Y - s0.Y;
                    var len2 = sX * sX + sY * sY;
                    if (len2 < m_FloatToInt.ClipperEpsilon)
                        return 0; // degenerate segment

                    double s0pX = point.X - s0.X;
                    double s0pY = point.Y - s0.Y;
                    var dot = s0pX * sX + s0pY * sY;
                    return Mathf.Clamp01((float) (dot / len2));
                }

                bool DoesIntersectOriginal(IntPoint l1, IntPoint l2)
                {
                    double epsilon = m_FloatToInt.ClipperEpsilon;
                    for (int p = 0; p < m_OriginalPolygon.Count; ++p)
                    {
                        var original = m_OriginalPolygon[p];
                        var numPoints = original.Count;
                        for (var i = 0; i < numPoints; ++i)
                            if (FindIntersection(l1, l2, original[i], original[(i + 1) % numPoints], epsilon) == 2)
                                return true;
                    }

                    return false;
                }
            }

#if UNITY_EDITOR
            // Used by inspector to draw the baked path
            List<List<Vector2>> m_Vector2Path;
            public List<List<Vector2>> GetBakedPath()
            {
                // Convert to client space
                var numPaths = m_Solution.Count;
                if (m_Vector2Path == null)
                {
                    m_Vector2Path = new List<List<Vector2>>(numPaths);
                    for (int i = 0; i < numPaths; ++i)
                    {
                        var srcPoly = m_Solution[i];
                        int numPoints = srcPoly.Count;
                        var pathSegment = new List<Vector2>(numPoints);
                        for (var j = 0; j < numPoints; j++)
                        {
                            // Restore the original aspect ratio
                            pathSegment.Add(m_AspectStretcher.Unstretch(
                                new Vector2(m_FloatToInt.IntToFloat(srcPoly[j].X), m_FloatToInt.IntToFloat(srcPoly[j].Y))));
                        }

                        m_Vector2Path.Add(pathSegment);
                    }
                }

                return m_Vector2Path;
            }
#endif
            static int FindIntersection(in IntPoint p1, in IntPoint p2, in IntPoint p3, in IntPoint p4, double epsilon)
            {
                // Get the segments' parameters.
                double dx12 = p2.X - p1.X;
                double dy12 = p2.Y - p1.Y;
                double dx34 = p4.X - p3.X;
                double dy34 = p4.Y - p3.Y;

                // Solve for t1 and t2
                var denominator = dy12 * dx34 - dx12 * dy34;
                var t1 = ((p1.X - p3.X) * dy34 + (p3.Y - p1.Y) * dx34) / denominator;
                if (double.IsInfinity(t1) || double.IsNaN(t1))
                {
                    // The lines are parallel (or close enough to it).
                    if (IntPointDiffSqrMagnitude(p1, p3) < epsilon ||
                        IntPointDiffSqrMagnitude(p1, p4) < epsilon ||
                        IntPointDiffSqrMagnitude(p2, p3) < epsilon ||
                        IntPointDiffSqrMagnitude(p2, p4) < epsilon)
                    {
                        return 2; // they are the same line, or very close parallels
                    }

                    return 0; // no intersection
                }

                var t2 = ((p3.X - p1.X) * dy12 + (p1.Y - p3.Y) * dx12) / -denominator;
                return (t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 < 1) ? 2 : 1; // 2 = segments intersect, 1 = lines intersect

                // local function
                double IntPointDiffSqrMagnitude(IntPoint point1, IntPoint point2)
                {
                    double x = point1.X - point2.X;
                    double y = point1.Y - point2.Y;
                    return x * x + y * y;
                }
            }
        }

        readonly struct AspectStretcher
        {
            public float Aspect { get; }
            readonly float m_InverseAspect;
            readonly float m_CenterX;

            public AspectStretcher(float aspect, float centerX)
            {
                Aspect = aspect;
                m_InverseAspect = 1 / Aspect;
                m_CenterX = centerX;
            }

            public Vector2 Stretch(Vector2 p) => new((p.x - m_CenterX) * m_InverseAspect + m_CenterX, p.y);
            public Vector2 Unstretch(Vector2 p) => new((p.x - m_CenterX) * Aspect + m_CenterX, p.y);
        }

        float m_MinFrustumHeightWithBones;
        float m_SkeletonPadding;

        List<List<IntPoint>> m_OriginalPolygon;
        IntPoint m_MidPoint;
        internal List<List<IntPoint>> m_Skeleton = new();

        FloatToIntScaler m_FloatToInt;

        const int k_MiterLimit = 2; // this is the minimum allowed value.  We want to square the spikes.
        const float k_MaxComputationTimeForFullSkeletonBakeInSeconds = 5f;

        Rect m_PolygonRect;
        AspectStretcher m_AspectStretcher = new(1, 0);

        public ConfinerOven(in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight, float skeletonPadding)
        {
            Initialize(inputPath, aspectRatio, maxFrustumHeight, Mathf.Max(0, skeletonPadding) + 1);
        }

        /// <summary>
        /// Converts and returns a prebaked ConfinerState for the input frustumHeight.
        /// </summary>
        public BakedSolution GetBakedSolution(float frustumHeight)
        {
            // If the user has set a max frustum height, respect it
            if (m_Cache.userSetMaxFrustumHeight > 0)
                frustumHeight = Mathf.Min(m_Cache.userSetMaxFrustumHeight, frustumHeight);

            // Special case: we are shrank to the mid point of the original input confiner area.
            if (State == BakingState.BAKED && frustumHeight >= m_Cache.theoreticalMaxFrustumHeight)
                return new BakedSolution(m_AspectStretcher.Aspect, frustumHeight, false, m_PolygonRect,
                    m_OriginalPolygon, m_Cache.theoreticalMaxCandidate);

            // Inflate with clipper to frustumHeight
            var offsetter = new ClipperOffset(k_MiterLimit);
            offsetter.AddPaths(m_OriginalPolygon, JoinType.Miter, EndType.Polygon);
            var solution = offsetter.Execute(-1f * m_FloatToInt.FloatToInt(frustumHeight));
            if (solution.Count == 0)
                solution = m_Cache.theoreticalMaxCandidate;

            // Add in the skeleton
            var bakedSolution = new List<List<IntPoint>>();
            if (State == BakingState.BAKING || m_Skeleton.Count == 0)
                bakedSolution = solution;
            else
            {
                var c = new Clipper64();
                c.AddSubject(solution);
                c.AddClip(m_Skeleton);
                c.Execute(ClipType.Union, FillRule.EvenOdd, bakedSolution);
            }

            return new BakedSolution(
                m_AspectStretcher.Aspect, frustumHeight,
                m_MinFrustumHeightWithBones < frustumHeight,
                m_PolygonRect, m_OriginalPolygon, bakedSolution);
        }

        struct PolygonSolution
        {
            public List<List<IntPoint>> polygons;
            public float frustumHeight;

            public bool StateChanged(in List<List<IntPoint>> paths)
            {
                if (paths.Count != polygons.Count)
                    return true;
                for (var i = 0; i < paths.Count; ++i)
                {
                    if (paths[i].Count != polygons[i].Count)
                        return true;
                }
                return false;
            }
            public bool IsNull => polygons == null;
        }

        public enum BakingState { BAKING, BAKED, TIMEOUT }
        public BakingState State { get; private set; }

        public float bakeProgress;

        struct BakingStateCache
        {
            public ClipperOffset offsetter;
            public List<PolygonSolution> solutions;
            public PolygonSolution rightCandidate;
            public PolygonSolution leftCandidate;
            public List<List<IntPoint>> userSetMaxCandidate;
            public List<List<IntPoint>> theoreticalMaxCandidate; // theoretical upper bound on the computation
            public float stepSize;
            public float maxFrustumHeight;
            public float userSetMaxFrustumHeight;
            public float theoreticalMaxFrustumHeight;
            public float currentFrustumHeight;

            public float bakeTime;
        }

        BakingStateCache m_Cache;

        void Initialize(in List<List<Vector2>> inputPath, in float aspectRatio, float maxFrustumHeight, float skeletonPadding)
        {
            m_Skeleton.Clear();
            m_Cache.userSetMaxFrustumHeight = maxFrustumHeight;
            m_MinFrustumHeightWithBones = float.MaxValue;
            m_SkeletonPadding = skeletonPadding;

            m_PolygonRect = GetPolygonBoundingBox(inputPath);
            m_AspectStretcher = new AspectStretcher(aspectRatio, m_PolygonRect.center.x);
            m_FloatToInt = new FloatToIntScaler(m_PolygonRect);

            // Don't compute further than what is the theoretical upper-bound (it may be a little bit more)
            m_Cache.theoreticalMaxFrustumHeight = Mathf.Max(m_PolygonRect.width / aspectRatio, m_PolygonRect.height) / 2f;

            // Initialize clipper
            m_OriginalPolygon = new List<List<IntPoint>>(inputPath.Count);
            for (var i = 0; i < inputPath.Count; ++i)
            {
                var srcPath = inputPath[i];
                var numPoints = srcPath.Count;
                var path = new List<IntPoint>(numPoints);
                for (var j = 0; j < numPoints; ++j)
                {
                    // Neutralize the aspect ratio
                    var p = m_AspectStretcher.Stretch(srcPath[j]);
                    path.Add(new IntPoint(m_FloatToInt.FloatToInt(p.x), m_FloatToInt.FloatToInt(p.y)));
                }
                m_OriginalPolygon.Add(path);
            }

            // calculate mid point and use it as the most shrank down version at theoretical max
            m_MidPoint = MidPointOfIntRect(Clipper.GetBounds(m_OriginalPolygon));
            m_Cache.theoreticalMaxCandidate = new List<List<IntPoint>> { new() { m_MidPoint } };

            // Skip the expensive skeleton calculation if it's not wanted
            if (m_Cache.userSetMaxFrustumHeight < 0)
            {
                State = BakingState.BAKED; // if we don't need skeleton, then we don't need to bake
                return;
            }

            m_Cache.offsetter = new ClipperOffset(k_MiterLimit);
            m_Cache.offsetter.AddPaths(m_OriginalPolygon, JoinType.Miter, EndType.Polygon);

            // exact comparison to 0 is intentional!
            m_Cache.maxFrustumHeight = m_Cache.userSetMaxFrustumHeight;
            if (m_Cache.maxFrustumHeight == 0 || m_Cache.maxFrustumHeight > m_Cache.theoreticalMaxFrustumHeight)
            {
                m_Cache.maxFrustumHeight = m_Cache.theoreticalMaxFrustumHeight;
                m_Cache.userSetMaxCandidate = m_Cache.theoreticalMaxCandidate;
            }
            else
            {
                m_Cache.userSetMaxCandidate = new List<List<IntPoint>>(
                    m_Cache.offsetter.Execute(-1 * m_FloatToInt.FloatToInt(m_Cache.userSetMaxFrustumHeight)));
                if (m_Cache.userSetMaxCandidate.Count == 0)
                    m_Cache.userSetMaxCandidate = m_Cache.theoreticalMaxCandidate;
            }
            m_Cache.stepSize = m_Cache.maxFrustumHeight;

            var solution = new List<List<IntPoint>>(m_Cache.offsetter.Execute(0));
            m_Cache.solutions = new List<PolygonSolution>();
            m_Cache.solutions.Add(new PolygonSolution
            {
                polygons = solution,
                frustumHeight = 0,
            });

            m_Cache.rightCandidate = new PolygonSolution();
            m_Cache.leftCandidate = new PolygonSolution
            {
                polygons = solution,
                frustumHeight = 0,
            };
            m_Cache.currentFrustumHeight = 0;

            m_Cache.bakeTime = 0;
            State = BakingState.BAKING;
            bakeProgress = 0;

            // local functions
            Rect GetPolygonBoundingBox(in List<List<Vector2>> polygons)
            {
                float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
                float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
                for (var i = 0; i < polygons.Count; ++i)
                {
                    for (var j = 0; j < polygons[i].Count; ++j)
                    {
                        var p = polygons[i][j];
                        minX = Mathf.Min(minX, p.x);
                        maxX = Mathf.Max(maxX, p.x);
                        minY = Mathf.Min(minY, p.y);
                        maxY = Mathf.Max(maxY, p.y);
                    }
                }
                return new Rect(minX, minY, Mathf.Max(0, maxX - minX), Mathf.Max(0, maxY - minY));
            }

            IntPoint MidPointOfIntRect(IntRect bounds) => new((bounds.left + bounds.right) / 2, (bounds.top + bounds.bottom) / 2);
        }

        /// <summary>
        /// Creates shrinkable polygons from input parameters.
        /// The algorithm is divide and conquer. It iteratively shrinks down the input
        /// polygon towards its shrink directions. If the polygon intersects with itself,
        /// then we divide the polygon into two polygons at the intersection point, and
        /// continue the algorithm on these two polygons separately. We need to keep track of
        /// the connectivity information between sub-polygons.
        /// </summary>
        public void BakeConfiner(float maxComputationTimePerFrameInSeconds)
        {
            if (State != BakingState.BAKING)
                return;

            var startTime = Time.realtimeSinceStartup;
            float minStepSize = m_FloatToInt.IntToFloat(50);

            // Binary search for state changes so we can compute the skeleton
            while (m_Cache.solutions.Count < 1000)
            {
                m_Cache.stepSize = Mathf.Min(m_Cache.stepSize,
                    m_Cache.maxFrustumHeight - m_Cache.leftCandidate.frustumHeight);
#if false
                Debug.Log($"States = {solutions.Count}, "
                          + $"Frustum height = {currentFrustumHeight}, stepSize = {stepSize}");
#endif
                m_Cache.currentFrustumHeight =
                    m_Cache.leftCandidate.frustumHeight + m_Cache.stepSize;

                var candidate =
                    Math.Abs(m_Cache.currentFrustumHeight - m_Cache.maxFrustumHeight) < UnityVectorExtensions.Epsilon
                        ? m_Cache.userSetMaxCandidate
                        : m_Cache.offsetter.Execute(-1f * m_FloatToInt.FloatToInt(m_Cache.currentFrustumHeight));
                if (candidate.Count == 0)
                    candidate = m_Cache.userSetMaxCandidate;

                if (m_Cache.leftCandidate.StateChanged(in candidate))
                {
                    m_Cache.rightCandidate = new PolygonSolution
                    {
                        polygons = new List<List<IntPoint>>(candidate),
                        frustumHeight = m_Cache.currentFrustumHeight,
                    };
                    m_Cache.stepSize = Mathf.Max(m_Cache.stepSize / 2f, minStepSize);
                }
                else
                {
                    m_Cache.leftCandidate = new PolygonSolution
                    {
                        polygons = new List<List<IntPoint>>(candidate),
                        frustumHeight = m_Cache.currentFrustumHeight,
                    };

                    // decrease stepSize if we have a right candidate
                    if (!m_Cache.rightCandidate.IsNull)
                        m_Cache.stepSize = Mathf.Max(m_Cache.stepSize / 2f, minStepSize);
                }

                // if we have a right candidate, and left and right are sufficiently close,
                // then we have located a state change point
                if (!m_Cache.rightCandidate.IsNull && m_Cache.stepSize <= minStepSize)
                {
                    // Add both states: one before the state change and one after
                    m_Cache.solutions.Add(m_Cache.leftCandidate);
                    m_Cache.solutions.Add(m_Cache.rightCandidate);

                    m_Cache.leftCandidate = m_Cache.rightCandidate;
                    m_Cache.rightCandidate = new PolygonSolution();

                    // Back to max step
                    m_Cache.stepSize = m_Cache.maxFrustumHeight;
                }
                else if (m_Cache.rightCandidate.IsNull ||
                         m_Cache.leftCandidate.frustumHeight >= m_Cache.maxFrustumHeight)
                {
                    m_Cache.solutions.Add(m_Cache.leftCandidate);
                    break; // stop searching, because we are at the bound
                }

                // Pause after max time per iteration reached
                var elapsedTime = Time.realtimeSinceStartup - startTime;
                if (elapsedTime > maxComputationTimePerFrameInSeconds)
                {
                    m_Cache.bakeTime += elapsedTime;
                    if (m_Cache.bakeTime > k_MaxComputationTimeForFullSkeletonBakeInSeconds)
                    {
                        State = BakingState.TIMEOUT;
                    }

                    bakeProgress = m_Cache.leftCandidate.frustumHeight / m_Cache.maxFrustumHeight;
                    return;
                }
            }

            ComputeSkeleton(in m_Cache.solutions);

            // Remove useless/empty results
            for (var i = m_Cache.solutions.Count - 1; i >= 0; --i)
                if (m_Cache.solutions[i].polygons.Count == 0)
                    m_Cache.solutions.RemoveAt(i);

            bakeProgress = 1;
            State = BakingState.BAKED;

            // local function
            // TODO: KGB: think about shrinking bones to a point to solve problem of seeing outside of the confiner area.
            void ComputeSkeleton(in List<PolygonSolution> solutions)
            {
                var clipper = new Clipper64();
                var offsetter = new ClipperOffset(k_MiterLimit);
                // At each state change point (1-2, 3-4, ...), collect geometry that gets lost over the transition
                for (var i = 1; i < solutions.Count - 1; i += 2)
                {
                    var prev = solutions[i]; // solution before state change
                    var next = solutions[i+1]; // solution after state change

                    // Grow the larger polygon to inflate marginal regions
                    double step = m_FloatToInt.FloatToInt(m_SkeletonPadding) * (next.frustumHeight - prev.frustumHeight);
                    offsetter.Clear();
                    offsetter.AddPaths(prev.polygons, JoinType.Miter, EndType.Polygon);
                    var expandedPrev = new List<List<IntPoint>>(offsetter.Execute(step));

                    // Grow the smaller polygon to be a bit bigger than the expanded larger one
                    offsetter.Clear();
                    offsetter.AddPaths(next.polygons, JoinType.Miter, EndType.Polygon);
                    var expandedNext = new List<List<IntPoint>>(offsetter.Execute(step * 2));

                    // Compute the difference - this is the lost geometry
                    var solution = new List<List<IntPoint>>();
                    clipper.Clear();
                    clipper.AddSubject(expandedPrev);
                    clipper.AddClip(expandedNext);
                    clipper.Execute(ClipType.Difference, FillRule.EvenOdd, solution);

                    // Add that lost geometry to the skeleton
                    if (solution.Count > 0 && solution[0].Count > 0)
                    {
                        m_Skeleton.AddRange(solution);
                        // Exact comparison is intentional
                        if (m_MinFrustumHeightWithBones == float.MaxValue)
                            m_MinFrustumHeightWithBones = next.frustumHeight;
                    }
                }
            }
        }
    }
}
#endif
