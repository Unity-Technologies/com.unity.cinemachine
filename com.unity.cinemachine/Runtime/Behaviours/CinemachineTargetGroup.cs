using UnityEngine;
using System;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace Unity.Cinemachine
{
    /// <summary>
    /// Interface representing something that can be used as a vcam target.  
    /// It has a transform, a bounding box, and a bounding sphere.
    /// </summary>
    public interface ICinemachineTargetGroup
    {
        /// <summary>
        /// Returns true if object has not been deleted.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Get the MonoBehaviour's Transform
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// The axis-aligned bounding box of the group, computed using the targets positions and radii
        /// </summary>
        Bounds BoundingBox { get; }

        /// <summary>
        /// The bounding sphere of the group, computed using the targets positions and radii
        /// </summary>
        BoundingSphere Sphere { get; }

        /// <summary>
        /// Returns true if the group has no non-zero-weight members
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>The axis-aligned bounding box of the group, in a specific reference frame</summary>
        /// <param name="observer">The frame of reference in which to compute the bounding box</param>
        /// <param name="includeBehind">If true, members behind the observer (negative z) will be included</param>
        /// <returns>The axis-aligned bounding box of the group, in the desired frame of reference</returns>
        Bounds GetViewSpaceBoundingBox(Matrix4x4 observer, bool includeBehind);

        /// <summary>
        /// Get the local-space angular bounds of the group, from a specific point of view.
        /// Also returns the z depth range of the members.
        /// Members behind the observer (negative z) will be ignored.
        /// </summary>
        /// <param name="observer">Point of view from which to calculate, and in whose
        /// space the return values are</param>
        /// <param name="minAngles">The lower bound of the screen angles of the members (degrees)</param>
        /// <param name="maxAngles">The upper bound of the screen angles of the members (degrees)</param>
        /// <param name="zRange">The min and max depth values of the members, relative to the observer</param>
        void GetViewSpaceAngularBounds(
            Matrix4x4 observer, out Vector2 minAngles, out Vector2 maxAngles, out Vector2 zRange);
    }

    /// <summary>Defines a group of target objects, each with a radius and a weight.
    /// The weight is used when calculating the average position of the target group.
    /// Higher-weighted members of the group will count more.
    /// The bounding box is calculated by taking the member positions, weight,
    /// and radii into account.
    /// </summary>
    [AddComponentMenu("Cinemachine/Helpers/Cinemachine Target Group")]
    [SaveDuringPlay]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [HelpURL(Documentation.BaseURL + "manual/CinemachineTargetGroup.html")]
    public class CinemachineTargetGroup : MonoBehaviour, ICinemachineTargetGroup
    {
        /// <summary>Holds the information that represents a member of the group</summary>
        [Serializable] public class Target
        {
            /// <summary>The target object.  This object's position and rotation will contribute to the
            /// group's average position and rotation, in accordance with its weight</summary>
            [Tooltip("The target object.  This object's position and rotation will contribute to the "
                + "group's average position and rotation, in accordance with its weight")]
            [FormerlySerializedAs("target")]
            public Transform Object;
            /// <summary>How much weight to give the target when averaging.  Cannot be negative</summary>
            [Tooltip("How much weight to give the target when averaging.  Cannot be negative")]
            [FormerlySerializedAs("weight")]
            public float Weight = 1;
            /// <summary>The radius of the target, used for calculating the bounding box.  Cannot be negative</summary>
            [Tooltip("The radius of the target, used for calculating the bounding box.  Cannot be negative")]
            [FormerlySerializedAs("radius")]
            public float Radius = 0.5f;
        }

        /// <summary>How the group's position is calculated</summary>
        public enum PositionModes
        {
            ///<summary>Group position will be the center of the group's axis-aligned bounding box</summary>
            GroupCenter,
            /// <summary>Group position will be the weighted average of the positions of the members</summary>
            GroupAverage
        }

        /// <summary>How the group's position is calculated</summary>
        [Tooltip("How the group's position is calculated.  Select GroupCenter for the center of the bounding box, "
            + "and GroupAverage for a weighted average of the positions of the members.")]
        [FormerlySerializedAs("m_PositionMode")]
        public PositionModes PositionMode = PositionModes.GroupCenter;

        /// <summary>How the group's orientation is calculated</summary>
        public enum RotationModes
        {
            /// <summary>Manually set in the group's transform</summary>
            Manual,
            /// <summary>Weighted average of the orientation of its members.</summary>
            GroupAverage
        }

        /// <summary>How the group's orientation is calculated</summary>
        [Tooltip("How the group's rotation is calculated.  Select Manual to use the value in the group's transform, "
            + "and GroupAverage for a weighted average of the orientations of the members.")]
        [FormerlySerializedAs("m_RotationMode")]
        public RotationModes RotationMode = RotationModes.Manual;

        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethods
        {
            /// <summary>Updated in normal MonoBehaviour Update.</summary>
            Update,
            /// <summary>Updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Updated in MonoBehaviour LateUpdate.</summary>
            LateUpdate
        };

        /// <summary>When to update the group's transform based on the position of the group members</summary>
        [Tooltip("When to update the group's transform based on the position of the group members")]
        [FormerlySerializedAs("m_UpdateMethod")]
        public UpdateMethods UpdateMethod = UpdateMethods.LateUpdate;

        /// <summary>The target objects, together with their weights and radii, that will
        /// contribute to the group's average position, orientation, and size</summary>
        [NoSaveDuringPlay]
        [Tooltip("The target objects, together with their weights and radii, that will contribute to the "
            + "group's average position, orientation, and size.")]
        public List<Target> Targets = new ();


        float m_MaxWeight;
        float m_WeightSum;
        Vector3 m_AveragePos;
        Bounds m_BoundingBox;
        BoundingSphere m_BoundingSphere;
        int m_LastUpdateFrame = -1;

        // Caches of valid members so we don't keep checking activeInHierarchy
        List<int> m_ValidMembers = new ();
        List<bool> m_MemberValidity = new ();
        
        void OnValidate()
        {
            var count = Targets.Count;
            for (int i = 0; i < count; ++i)
            {
                Targets[i].Weight = Mathf.Max(0, Targets[i].Weight);
                Targets[i].Radius = Mathf.Max(0, Targets[i].Radius);
            }
        }

        void Reset()
        {
            PositionMode = PositionModes.GroupCenter;
            RotationMode = RotationModes.Manual;
            UpdateMethod = UpdateMethods.LateUpdate;
            Targets.Clear();
        }

        //============================================
        // Legacy support 

        [HideInInspector, SerializeField, NoSaveDuringPlay, FormerlySerializedAs("m_Targets")]
        Target[] m_LegacyTargets;

        void Awake()
        {
            if (m_LegacyTargets != null && m_LegacyTargets.Length > 0)
                Targets.AddRange(m_LegacyTargets);
            m_LegacyTargets = null;
        }

        /// <summary>Obsolete Targets</summary>
        [Obsolete("m_Targets is obsolete.  Please use Targets instead")]
        public Target[] m_Targets
        {
            get => Targets.ToArray();
            set { Targets.Clear(); Targets.AddRange(value); }
        }

        //============================================

        /// <summary>
        /// Get the MonoBehaviour's Transform
        /// </summary>
        public Transform Transform => transform;

        /// <inheritdoc />
        public bool IsValid => this != null;

        /// <summary>The axis-aligned bounding box of the group, computed using the
        /// targets positions and radii</summary>
        public Bounds BoundingBox 
        { 
            get
            {
                if (m_LastUpdateFrame != Time.frameCount)
                    DoUpdate();
                return m_BoundingBox;
            }
            private set => m_BoundingBox = value;
        }

        /// <summary>The bounding sphere of the group, computed using the
        /// targets positions and radii</summary>
        public BoundingSphere Sphere
        { 
            get
            {
                if (m_LastUpdateFrame != Time.frameCount)
                    DoUpdate();
                return m_BoundingSphere;
            }
            private set => m_BoundingSphere = value;
        }

        /// <summary>Return true if there are no members with weight > 0.  This returns the
        /// cached member state and is only valid after a call to DoUpdate().  If members
        /// are added or removed after that call, this will not necessarily return
        /// correct information before the next update.</summary>
        public bool IsEmpty
        {
            get
            {
                if (m_LastUpdateFrame != Time.frameCount)
                    DoUpdate();
                return m_ValidMembers.Count == 0;
            }
        }

        /// <summary>Add a member to the group</summary>
        /// <param name="t">The member to add</param>
        /// <param name="weight">The new member's weight</param>
        /// <param name="radius">The new member's radius</param>
        public void AddMember(Transform t, float weight, float radius)
        {
            Targets.Add(new Target { Object = t, Weight = weight, Radius = radius });
        }

        /// <summary>Remove a member from the group</summary>
        /// <param name="t">The member to remove</param>
        public void RemoveMember(Transform t)
        {
            int index = FindMember(t);
            if (index >= 0)
                Targets.RemoveAt(index);
        }

        /// <summary>Locate a member's index in the group.</summary>
        /// <param name="t">The member to find</param>
        /// <returns>Member index, or -1 if not a member</returns>
        public int FindMember(Transform t)
        {
            var count = Targets.Count;
            for (int i = 0; i < count; ++i)
                if (Targets[i].Object == t)
                    return i;
            return -1;
        }

        /// <summary>
        /// Get the bounding sphere of a group member, with the weight taken into account.
        /// As the member's weight goes to 0, the position interpolates to the group average position.
        /// Note that this result is only valid after DoUpdate has been called. If members
        /// are added or removed after that call or change their weights or active state, 
        /// this will not necessarily return correct information before the next update.
        /// </summary>
        /// <param name="index">Member index</param>
        /// <returns>The weighted bounding sphere</returns>
        public BoundingSphere GetWeightedBoundsForMember(int index)
        {
            if (m_LastUpdateFrame != Time.frameCount)
                DoUpdate();
            if (!IndexIsValid(index) || !m_MemberValidity[index])
                return Sphere;
            return WeightedMemberBoundsForValidMember(Targets[index], m_AveragePos, m_MaxWeight);
        }

        /// <summary>The axis-aligned bounding box of the group, in a specific reference frame.
        /// Note that this result is only valid after DoUpdate has been called. If members
        /// are added or removed after that call or change their weights or active state, 
        /// this will not necessarily return correct information before the next update.</summary>
        /// <param name="observer">The frame of reference in which to compute the bounding box</param>
        /// <param name="includeBehind">If true, members behind the observer (negative z) will be included</param>
        /// <returns>The axis-aligned bounding box of the group, in the desired frame of reference</returns>
        public Bounds GetViewSpaceBoundingBox(Matrix4x4 observer, bool includeBehind)
        {
            if (m_LastUpdateFrame != Time.frameCount)
                DoUpdate();
            var inverseView = observer;
            if (!Matrix4x4.Inverse3DAffine(observer, ref inverseView))
                inverseView = observer.inverse;
            var b = new Bounds(inverseView.MultiplyPoint3x4(m_AveragePos), Vector3.zero);
            if (CachedCountIsValid)
            {
                bool gotOne = false;
                var unit = 2 * Vector3.one;
                var count = m_ValidMembers.Count;
                for (int i = 0; i < count; ++i)
                {
                    var s = WeightedMemberBoundsForValidMember(Targets[m_ValidMembers[i]], m_AveragePos, m_MaxWeight);
                    s.position = inverseView.MultiplyPoint3x4(s.position);
                    if (s.position.z > 0 || includeBehind)
                    {
                        if (gotOne)
                            b.Encapsulate(new Bounds(s.position, s.radius * unit));
                        else
                            b = new Bounds(s.position, s.radius * unit);
                        gotOne = true;
                    }
                }
            }
            return b;
        }

        bool CachedCountIsValid => m_MemberValidity.Count == Targets.Count;
        bool IndexIsValid(int index) => index >= 0 && index < Targets.Count && CachedCountIsValid;

        static BoundingSphere WeightedMemberBoundsForValidMember(Target t, Vector3 avgPos, float maxWeight)
        {
            var pos = t.Object == null ? avgPos : TargetPositionCache.GetTargetPosition(t.Object);
            var w = Mathf.Max(0, t.Weight);
            if (maxWeight > UnityVectorExtensions.Epsilon && w < maxWeight)
                w /= maxWeight;
            else
                w = 1;
            return new BoundingSphere(Vector3.Lerp(avgPos, pos, w), t.Radius * w);
        }

        /// <summary>
        /// Update the group's transform right now, depending on the transforms of the members.
        /// Normally this is called automatically by Update() or LateUpdate().
        /// </summary>
        public void DoUpdate()
        {
            m_LastUpdateFrame = Time.frameCount;

            UpdateMemberValidity();
            m_AveragePos = CalculateAveragePosition();
            BoundingBox = CalculateBoundingBox();
            m_BoundingSphere = CalculateBoundingSphere();

            switch (PositionMode)
            {
                case PositionModes.GroupCenter:
                    transform.position = Sphere.position;
                    break;
                case PositionModes.GroupAverage:
                    transform.position = m_AveragePos;
                    break;
            }

            switch (RotationMode)
            {
                case RotationModes.Manual:
                    break;
                case RotationModes.GroupAverage:
                    transform.rotation = CalculateAverageOrientation();
                    break;
            }
        }

        void UpdateMemberValidity()
        {
            Targets ??= new (); // in case user set it to null
            var count = Targets.Count;
            m_ValidMembers.Clear();
            m_ValidMembers.Capacity = Mathf.Max(m_ValidMembers.Capacity, count);
            m_MemberValidity.Clear();
            m_MemberValidity.Capacity = Mathf.Max(m_MemberValidity.Capacity, count);
            m_WeightSum = m_MaxWeight = 0;
            for (int i = 0; i < count; ++i)
            {
                m_MemberValidity.Add(Targets[i].Object != null 
                        && Targets[i].Weight > UnityVectorExtensions.Epsilon 
                        && Targets[i].Object.gameObject.activeInHierarchy);
                if (m_MemberValidity[i])
                {
                    m_ValidMembers.Add(i);
                    m_MaxWeight = Mathf.Max(m_MaxWeight, Targets[i].Weight);
                    m_WeightSum += Targets[i].Weight;
                }
            }
        }

        // Assumes that UpdateMemberValidity() has been called
        Vector3 CalculateAveragePosition()
        {
            if (m_WeightSum < UnityVectorExtensions.Epsilon)
                return transform.position;

            var pos = Vector3.zero;
            var count = m_ValidMembers.Count;
            for (int i = 0; i < count; ++i)
            {
                var targetIndex = m_ValidMembers[i];
                var weight = Targets[targetIndex].Weight;
                pos += TargetPositionCache.GetTargetPosition(Targets[targetIndex].Object) * weight;
            }
            return pos / m_WeightSum;
        }
        
        // Assumes that UpdateMemberValidity() has been called 
        Bounds CalculateBoundingBox()
        {
            if (m_MaxWeight < UnityVectorExtensions.Epsilon)
                return m_BoundingBox;
            var b = new Bounds(m_AveragePos, Vector3.zero);
            var count = m_ValidMembers.Count;
            for (int i = 0; i < count; ++i)
            {
                var s = WeightedMemberBoundsForValidMember(Targets[m_ValidMembers[i]], m_AveragePos, m_MaxWeight);
                b.Encapsulate(new Bounds(s.position, s.radius * 2 * Vector3.one));
            }
            return b;
        }
        
        /// <summary>
        /// Use Ritter's algorithm for calculating an approximate bounding sphere.
        /// Assumes that UpdateMemberValidity() has been called.
        /// </summary>
        /// <param name="maxWeight">The maximum weight of members in the group</param>
        /// <returns>An approximate bounding sphere.  Will be slightly large.</returns>
        BoundingSphere CalculateBoundingSphere()
        {
            var count = m_ValidMembers.Count;
            if (count == 0 || m_MaxWeight < UnityVectorExtensions.Epsilon)
                return m_BoundingSphere;

            var sphere = WeightedMemberBoundsForValidMember(Targets[m_ValidMembers[0]], m_AveragePos, m_MaxWeight);
            for (int i = 1; i < count; ++i)
            {
                var s = WeightedMemberBoundsForValidMember(Targets[m_ValidMembers[i]], m_AveragePos, m_MaxWeight);
                var distance = (s.position - sphere.position).magnitude + s.radius;
                if (distance > sphere.radius)
                {
                    // Point is outside current sphere: update
                    sphere.radius = (sphere.radius + distance) * 0.5f;
                    sphere.position = (sphere.radius * sphere.position + (distance - sphere.radius) * s.position) / distance;
                }
            }
            return sphere;
        }

        // Assumes that UpdateMemberValidity() has been called
        Quaternion CalculateAverageOrientation()
        {
            if (m_WeightSum > 0.001f)
            {
                var averageForward = Vector3.zero;
                var averageUp = Vector3.zero;
                var count = m_ValidMembers.Count;
                for (int i = 0; i < count; ++i)
                {
                    var targetIndex = m_ValidMembers[i];
                    var scaledWeight = Targets[targetIndex].Weight / m_WeightSum;
                    var rot = TargetPositionCache.GetTargetRotation(Targets[targetIndex].Object);
                    averageForward += rot * Vector3.forward * scaledWeight;
                    averageUp += rot * Vector3.up * scaledWeight;
                }
                if (averageForward.sqrMagnitude > 0.0001f && averageUp.sqrMagnitude > 0.0001f)
                    return Quaternion.LookRotation(averageForward, averageUp);
            }
            return transform.rotation;
        }

        void FixedUpdate()
        {
            if (UpdateMethod == UpdateMethods.FixedUpdate)
                DoUpdate();
        }

        void Update()
        {
            if (!Application.isPlaying || UpdateMethod == UpdateMethods.Update)
                DoUpdate();
        }

        void LateUpdate()
        {
            if (UpdateMethod == UpdateMethods.LateUpdate)
                DoUpdate();
        }

        /// <summary>
        /// Get the local-space angular bounds of the group, from a specific point of view.
        /// Also returns the z depth range of the members.
        /// Note that this result is only valid after DoUpdate has been called. If members
        /// are added or removed after that call or change their weights or active state, 
        /// this will not necessarily return correct information before the next update.
        /// </summary>
        /// <param name="observer">Point of view from which to calculate, and in whose
        /// space the return values are</param>
        /// <param name="minAngles">The lower bound of the screen angles of the members (degrees)</param>
        /// <param name="maxAngles">The upper bound of the screen angles of the members (degrees)</param>
        /// <param name="zRange">The min and max depth values of the members, relative to the observer</param>
        public void GetViewSpaceAngularBounds(
            Matrix4x4 observer, out Vector2 minAngles, out Vector2 maxAngles, out Vector2 zRange)
        {
            if (m_LastUpdateFrame != Time.frameCount)
                DoUpdate();
            var world2local = observer;
            if (!Matrix4x4.Inverse3DAffine(observer, ref world2local))
                world2local = observer.inverse;

            var r = m_BoundingSphere.radius;
            var b = new Bounds() { center = world2local.MultiplyPoint3x4(m_AveragePos), extents = new Vector3(r, r, r) };
            zRange = new Vector2(b.center.z - r, b.center.z + r);
            if (CachedCountIsValid)
            {
                bool haveOne = false;
                var count = m_ValidMembers.Count;
                for (int i = 0; i < count; ++i)
                {
                    var s = WeightedMemberBoundsForValidMember(Targets[m_ValidMembers[i]], m_AveragePos, m_MaxWeight);
                    var p = world2local.MultiplyPoint3x4(s.position);
                    if (p.z < UnityVectorExtensions.Epsilon)
                        continue; // behind us

                    var rN = s.radius / p.z;
                    var rN2 = new Vector3(rN, rN, 0);
                    var pN = p / p.z;
                    if (!haveOne)
                    {
                        b.center = pN;
                        b.extents = rN2;
                        zRange = new Vector2(p.z, p.z);
                        haveOne = true;
                    }
                    else
                    {
                        b.Encapsulate(pN + rN2);
                        b.Encapsulate(pN - rN2);
                        zRange.x = Mathf.Min(zRange.x, p.z);
                        zRange.y = Mathf.Max(zRange.y, p.z);
                    }
                }
            }
            // Don't need the high-precision versions of UnityVectorExtensions.SignedAngle
            var pMin = b.min;
            var pMax = b.max;
            minAngles = new Vector2(
                Vector3.SignedAngle(Vector3.forward, new Vector3(0, pMax.y, 1), Vector3.right),
                Vector3.SignedAngle(Vector3.forward, new Vector3(pMin.x, 0, 1), Vector3.up));
            maxAngles = new Vector2(
                Vector3.SignedAngle(Vector3.forward, new Vector3(0, pMin.y, 1), Vector3.right),
                Vector3.SignedAngle(Vector3.forward, new Vector3(pMax.x, 0, 1), Vector3.up));
        }
    }
}
