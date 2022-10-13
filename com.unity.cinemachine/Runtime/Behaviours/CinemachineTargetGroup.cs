using UnityEngine;
using System;
using Cinemachine.Utility;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace Cinemachine
{
    /// <summary>
    /// Interface representing something that can be used as a vcam target.  
    /// It has a transform, a bounding box, and a bounding sphere.
    /// </summary>
    public interface ICinemachineTargetGroup
    {
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
    public class CinemachineTargetGroup : MonoBehaviour, ICinemachineTargetGroup, ISerializationCallbackReceiver
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
            public float Weight;
            /// <summary>The radius of the target, used for calculating the bounding box.  Cannot be negative</summary>
            [Tooltip("The radius of the target, used for calculating the bounding box.  Cannot be negative")]
            [FormerlySerializedAs("radius")]
            public float Radius;
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
        Vector3 m_AveragePos;
        BoundingSphere m_BoundingSphere;
        
        void OnValidate()
        {
            for (int i = 0; i < Targets.Count; ++i)
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

        [SerializeField, FormerlySerializedAs("m_Targets")]
        Target[] m_LegacyTargets;

        /// <summary>Post-Serialization handler - performs legacy upgrade</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_LegacyTargets != null && m_LegacyTargets.Length > 0)
                Targets.AddRange(m_LegacyTargets);
            m_LegacyTargets = null;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

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

        /// <summary>The axis-aligned bounding box of the group, computed using the
        /// targets positions and radii</summary>
        public Bounds BoundingBox { get; private set; }

        /// <summary>The bounding sphere of the group, computed using the
        /// targets positions and radii</summary>
        public BoundingSphere Sphere { get => m_BoundingSphere; }

        /// <summary>Return true if there are no members with weight > 0</summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < Targets.Count; ++i)
                    if (Targets[i].Object != null && Targets[i].Weight > UnityVectorExtensions.Epsilon)
                        return false;
                return true;
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
            for (int i = Targets.Count-1; i >= 0; --i)
                if (Targets[i].Object == t)
                    return i;
            return -1;
        }

        /// <summary>
        /// Get the bounding sphere of a group memebr, with the weight taken into account.
        /// As the member's weight goes to 0, the position lerps to the group average position.
        /// </summary>
        /// <param name="index">Member index</param>
        /// <returns>The weighted bounding sphere</returns>
        public BoundingSphere GetWeightedBoundsForMember(int index)
        {
            if (index < 0 || index >= Targets.Count)
                return Sphere;
            return WeightedMemberBounds(Targets[index], m_AveragePos, m_MaxWeight);
        }

        /// <summary>The axis-aligned bounding box of the group, in a specific reference frame</summary>
        /// <param name="observer">The frame of reference in which to compute the bounding box</param>
        /// <param name="includeBehind">If true, members behind the observer (negative z) will be included</param>
        /// <returns>The axis-aligned bounding box of the group, in the desired frame of reference</returns>
        public Bounds GetViewSpaceBoundingBox(Matrix4x4 observer, bool includeBehind)
        {
            var inverseView = observer;
            if (!Matrix4x4.Inverse3DAffine(observer, ref inverseView))
                inverseView = observer.inverse;
            var b = new Bounds(inverseView.MultiplyPoint3x4(m_AveragePos), Vector3.zero);
            bool gotOne = false;
            var unit = 2 * Vector3.one;
            for (int i = 0; i < Targets.Count; ++i)
            {
                BoundingSphere s = GetWeightedBoundsForMember(i);
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
            return b;
        }

        private static BoundingSphere WeightedMemberBounds(
            Target t, Vector3 avgPos, float maxWeight)
        {
            float w = 0;
            var pos = avgPos;
            if (t.Object != null)
            {
                pos = TargetPositionCache.GetTargetPosition(t.Object);
                w = Mathf.Max(0, t.Weight);
                if (maxWeight > UnityVectorExtensions.Epsilon && w < maxWeight)
                    w /= maxWeight;
                else
                    w = 1;
            }
            return new BoundingSphere(Vector3.Lerp(avgPos, pos, w), t.Radius * w);
        }

        /// <summary>
        /// Update the group's transform right now, depending on the transforms of the members.
        /// Normally this is called automatically by Update() or LateUpdate().
        /// </summary>
        public void DoUpdate()
        {
            m_AveragePos = CalculateAveragePosition(out m_MaxWeight);
            BoundingBox = CalculateBoundingBox(m_AveragePos, m_MaxWeight);
            m_BoundingSphere = CalculateBoundingSphere(m_MaxWeight);

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

        /// <summary>
        /// Use Ritter's algorithm for calculating an approximate bounding sphere
        /// </summary>
        /// <param name="maxWeight">The maximum weight of members in the group</param>
        /// <returns>An approximate bounding sphere.  Will be slightly large.</returns>
        BoundingSphere CalculateBoundingSphere(float maxWeight)
        {
            var sphere = new BoundingSphere { position = transform.position };
            bool gotOne = false;

            for (int i = 0; i < Targets.Count; ++i)
            {
                if (Targets[i].Object == null || Targets[i].Weight < UnityVectorExtensions.Epsilon)
                    continue;
                BoundingSphere s = WeightedMemberBounds(Targets[i], m_AveragePos, maxWeight);
                if (!gotOne)
                {
                    gotOne = true;
                    sphere = s;
                    continue;
                }
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

        Vector3 CalculateAveragePosition(out float maxWeight)
        {
            var pos = Vector3.zero;
            float weight = 0;
            maxWeight = 0;
            for (int i = 0; i < Targets.Count; ++i)
            {
                if (Targets[i].Object != null)
                {
                    weight += Targets[i].Weight;
                    pos += TargetPositionCache.GetTargetPosition(Targets[i].Object) 
                        * Targets[i].Weight;
                    maxWeight = Mathf.Max(maxWeight, Targets[i].Weight);
                }
            }
            if (weight > UnityVectorExtensions.Epsilon)
                pos /= weight;
            else
                pos = transform.position;
            return pos;
        }

        Quaternion CalculateAverageOrientation()
        {
            if (m_MaxWeight <= UnityVectorExtensions.Epsilon)
            {
                return transform.rotation;
            }
            
            float weightedAverage = 0;
            Quaternion r = Quaternion.identity;
            for (int i = 0; i < Targets.Count; ++i)
            {
                if (Targets[i].Object != null)
                {
                    var scaledWeight = Targets[i].Weight / m_MaxWeight;
                    var rot = TargetPositionCache.GetTargetRotation(Targets[i].Object);
                    r *= Quaternion.Slerp(Quaternion.identity, rot, scaledWeight);
                    weightedAverage += scaledWeight;
                }
            }
            return Quaternion.Slerp(Quaternion.identity, r, 1.0f / weightedAverage);
        }

        Bounds CalculateBoundingBox(Vector3 avgPos, float maxWeight)
        {
            Bounds b = new Bounds(avgPos, Vector3.zero);
            if (maxWeight > UnityVectorExtensions.Epsilon)
            {
                for (int i = 0; i < Targets.Count; ++i)
                {
                    if (Targets[i].Object != null)
                    {
                        var s = WeightedMemberBounds(Targets[i], m_AveragePos, maxWeight);
                        b.Encapsulate(new Bounds(s.position, s.radius * 2 * Vector3.one));
                    }
                }
            }
            return b;
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
        /// Get the local-space angular bounds of the group, from a spoecific point of view.
        /// Also returns the z depth range of the members.
        /// </summary>
        /// <param name="observer">Point of view from which to calculate, and in whose
        /// space the return values are</param>
        /// <param name="minAngles">The lower bound of the screen angles of the members (degrees)</param>
        /// <param name="maxAngles">The upper bound of the screen angles of the members (degrees)</param>
        /// <param name="zRange">The min and max depth values of the members, relative to the observer</param>
        public void GetViewSpaceAngularBounds(
            Matrix4x4 observer, out Vector2 minAngles, out Vector2 maxAngles, out Vector2 zRange)
        {
            var world2local = observer;
            if (!Matrix4x4.Inverse3DAffine(observer, ref world2local))
                world2local = observer.inverse;

            zRange = Vector2.zero;
            var b = new Bounds();
            bool haveOne = false;
            for (int i = 0; i < Targets.Count; ++i)
            {
                BoundingSphere s = GetWeightedBoundsForMember(i);
                Vector3 p = world2local.MultiplyPoint3x4(s.position);
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

            // Don't need the high-precision versions of SignedAngle
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
