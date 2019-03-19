using UnityEngine;
using System;
using Cinemachine.Utility;

namespace Cinemachine
{
    /// <summary>Defines a group of target objects, each with a radius and a weight.
    /// The weight is used when calculating the average position of the target group.
    /// Higher-weighted members of the group will count more.
    /// The bounding box is calculated by taking the member positions, weight,
    /// and radii into account.
    /// </summary>
    [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
    [AddComponentMenu("Cinemachine/CinemachineTargetGroup")]
    [SaveDuringPlay]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode]
#endif
    public class CinemachineTargetGroup : MonoBehaviour
    {
        /// <summary>Holds the information that represents a member of the group</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        [Serializable] public struct Target
        {
            /// <summary>The target objects.  This object's position and orientation will contribute to the
            /// group's average position and orientation, in accordance with its weight</summary>
            [Tooltip("The target objects.  This object's position and orientation will contribute to the group's average position and orientation, in accordance with its weight")]
            public Transform target;
            /// <summary>How much weight to give the target when averaging.  Cannot be negative</summary>
            [Tooltip("How much weight to give the target when averaging.  Cannot be negative")]
            public float weight;
            /// <summary>The radius of the target, used for calculating the bounding box.  Cannot be negative</summary>
            [Tooltip("The radius of the target, used for calculating the bounding box.  Cannot be negative")]
            public float radius;
        }

        /// <summary>How the group's position is calculated</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum PositionMode
        {
            ///<summary>Group position will be the center of the group's axis-aligned bounding box</summary>
            GroupCenter,
            /// <summary>Group position will be the weighted average of the positions of the members</summary>
            GroupAverage
        }

        /// <summary>How the group's position is calculated</summary>
        [Tooltip("How the group's position is calculated.  Select GroupCenter for the center of the bounding box, and GroupAverage for a weighted average of the positions of the members.")]
        public PositionMode m_PositionMode = PositionMode.GroupCenter;

        /// <summary>How the group's orientation is calculated</summary>
        [DocumentationSorting(DocumentationSortingAttribute.Level.UserRef)]
        public enum RotationMode
        {
            /// <summary>Manually set in the group's transform</summary>
            Manual,
            /// <summary>Weighted average of the orientation of its members.</summary>
            GroupAverage
        }

        /// <summary>How the group's orientation is calculated</summary>
        [Tooltip("How the group's rotation is calculated.  Select Manual to use the value in the group's transform, and GroupAverage for a weighted average of the orientations of the members.")]
        public RotationMode m_RotationMode = RotationMode.Manual;

        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethod
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
        public UpdateMethod m_UpdateMethod = UpdateMethod.LateUpdate;

        /// <summary>The target objects, together with their weights and radii, that will
        /// contribute to the group's average position, orientation, and size</summary>
        [NoSaveDuringPlay]
        [Tooltip("The target objects, together with their weights and radii, that will contribute to the group's average position, orientation, and size.")]
        public Target[] m_Targets = new Target[0];

        /// <summary>The axis-aligned bounding box of the group, computed using the
        /// targets positions and radii</summary>
        public Bounds BoundingBox { get; private set; }

        /// <summary>The bounding sphere of the group, computed using the
        /// targets positions and radii</summary>
        public BoundingSphere Sphere
        {
            get
            {
                Bounds b = BoundingBox;
                return new BoundingSphere(b.center, ((b.max - b.min) / 2).magnitude);
            }
        }

        /// <summary>Return true if there are no members with weight > 0</summary>
        public bool IsEmpty
        {
            get
            {
                for (int i = 0; i < m_Targets.Length; ++i)
                    if (m_Targets[i].target != null && m_Targets[i].weight > UnityVectorExtensions.Epsilon)
                        return false;
                return true;
            }
        }

        /// <summary>Add a member to the group</summary>
        public void AddMember(Transform t, float weight, float radius)
        {
            int index = 0;
            if (m_Targets == null)
                m_Targets = new Target[1];
            else
            {
                index = m_Targets.Length;
                var oldTargets = m_Targets;
                m_Targets = new Target[index + 1];
                Array.Copy(oldTargets, m_Targets, index);
            }
            m_Targets[index].target = t;
            m_Targets[index].weight = weight;
            m_Targets[index].radius = radius;
        }

        /// <summary>Remove a member from the group</summary>
        public void RemoveMember(Transform t)
        {
            int index = FindMember(t);
            if (index >= 0)
            {
                var oldTargets = m_Targets;
                m_Targets = new Target[m_Targets.Length - 1];
                if (index > 0)
                    Array.Copy(oldTargets, m_Targets, index);
                if (index < oldTargets.Length - 1)
                    Array.Copy(oldTargets, index + 1, m_Targets, index, oldTargets.Length - index - 1);
            }
        }

        /// <summary>Locate a member's index in the group. Returns -1 if not a member</summary>
        public int FindMember(Transform t)
        {
            if (m_Targets != null)
            {
                for (int i = m_Targets.Length-1; i >= 0; --i)
                    if (m_Targets[i].target == t)
                        return i;
            }
            return -1;
        }

        /// <summary>
        /// Get the bounding sphere of a group memebr, with the weight taken into account.
        /// As the member's weight goes to 0, the position lerps to the group average position.
        /// </summary>
        /// <param name="index">Member index</param>
        /// <returns></returns>
        public BoundingSphere GetWeightedBoundsForMember(int index)
        {
            if (index < 0 || index >= m_Targets.Length)
                return Sphere;
            return WeightedMemberBounds(m_Targets[index], mAveragePos, mMaxWeight);
        }

        /// <summary>The axis-aligned bounding box of the group, in a specific reference frame</summary>
        /// <param name="observer">The frame of reference in which to compute the bounding box</param>
        /// <returns>The axis-aligned bounding box of the group, in the desired frame of reference</returns>
        public Bounds GetViewSpaceBoundingBox(Matrix4x4 observer)
        {
            Matrix4x4 inverseView = observer.inverse;
            Bounds b = new Bounds(inverseView.MultiplyPoint3x4(mAveragePos), Vector3.zero);
            for (int i = 0; i < m_Targets.Length; ++i)
            {
                BoundingSphere s = GetWeightedBoundsForMember(i);
                s.position = inverseView.MultiplyPoint3x4(s.position);
                b.Encapsulate(new Bounds(s.position, s.radius * 2 * Vector3.one));
            }
            return b;
        }

        private static BoundingSphere WeightedMemberBounds(Target t, Vector3 avgPos, float maxWeight)
        {
            float w = 0;
            Vector3 pos = avgPos;
            if (t.target != null)
            {
                pos = t.target.position;
                w = Mathf.Max(0, t.weight);
                if (maxWeight > UnityVectorExtensions.Epsilon && w < maxWeight)
                    w /= maxWeight;
                else
                    w = 1;
            }
            return new BoundingSphere(Vector3.Lerp(avgPos, pos, w), t.radius * w);
        }

        private float mMaxWeight;
        private Vector3 mAveragePos;

        void DoUpdate()
        {
            if (IsEmpty)
                return;

            mAveragePos = CalculateAveragePosition(out mMaxWeight);
            BoundingBox = CalculateBoundingBox(mAveragePos, mMaxWeight);

            switch (m_PositionMode)
            {
                case PositionMode.GroupCenter:
                    transform.position = BoundingBox.center;
                    break;
                case PositionMode.GroupAverage:
                    transform.position = mAveragePos;
                    break;
            }

            switch (m_RotationMode)
            {
                case RotationMode.Manual:
                    break;
                case RotationMode.GroupAverage:
                    transform.rotation = CalculateAverageOrientation();
                    break;
            }
        }

        Vector3 CalculateAveragePosition(out float maxWeight)
        {
            Vector3 pos = Vector3.zero;
            float weight = 0;
            maxWeight = 0;
            for (int i = 0; i < m_Targets.Length; ++i)
            {
                if (m_Targets[i].target != null)
                {
                    weight += m_Targets[i].weight;
                    pos += m_Targets[i].target.position * m_Targets[i].weight;
                    maxWeight = Mathf.Max(maxWeight, m_Targets[i].weight);
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
            Quaternion r = Quaternion.identity;
            for (int i = 0; i < m_Targets.Length; ++i)
            {
                if (m_Targets[i].target != null)
                {
                    float w = m_Targets[i].weight;
                    Quaternion q = m_Targets[i].target.rotation;
                    // This is probably bogus
                    r = new Quaternion(r.x + q.x * w, r.y + q.y * w, r.z + q.z * w, r.w + q.w * w);
                }
            }
            return r.Normalized();
        }

        Bounds CalculateBoundingBox(Vector3 avgPos, float maxWeight)
        {
            Bounds b = new Bounds(avgPos, Vector3.zero);
            if (maxWeight > UnityVectorExtensions.Epsilon)
            {
                for (int i = 0; i < m_Targets.Length; ++i)
                {
                    if (m_Targets[i].target != null)
                    {
                        BoundingSphere s = WeightedMemberBounds(m_Targets[i], mAveragePos, maxWeight);
                        b.Encapsulate(new Bounds(s.position, s.radius * 2 * Vector3.one));
                    }
                }
            }
            return b;
        }

        private void OnValidate()
        {
            for (int i = 0; i < m_Targets.Length; ++i)
            {
                m_Targets[i].weight = Mathf.Max(0, m_Targets[i].weight);
                m_Targets[i].radius = Mathf.Max(0, m_Targets[i].radius);
            }
        }

        void FixedUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.FixedUpdate)
                DoUpdate();
        }

        void Update()
        {
            if (!Application.isPlaying || m_UpdateMethod == UpdateMethod.Update)
                DoUpdate();
        }

        void LateUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.LateUpdate)
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
            Matrix4x4 inverseView = observer.inverse;
            minAngles = Vector2.zero;
            maxAngles = Vector2.zero;
            zRange = Vector3.zero;
            for (int i = 0; i < m_Targets.Length; ++i)
            {
                BoundingSphere s = GetWeightedBoundsForMember(i);
                Vector3 p = inverseView.MultiplyPoint3x4(s.position);

                // Add the radius
                float r = p.magnitude;
                Vector2 extraA = Vector2.zero;
                if (r > UnityVectorExtensions.Epsilon && s.radius > UnityVectorExtensions.Epsilon)
                    extraA = Mathf.Atan2(s.radius, r) * Mathf.Rad2Deg * Vector2.one;
                Vector2 a = Quaternion.identity.GetCameraRotationToTarget(p, Vector3.up);
                if (i == 0)
                {
                    minAngles = a - extraA;
                    maxAngles = a + extraA;
                    zRange = new Vector2(p.z - s.radius, p.z + s.radius);
                }
                else
                {
                    minAngles = Vector3.Min(minAngles, a - extraA);
                    maxAngles = Vector3.Max(maxAngles, a + extraA);
                    zRange.x = Mathf.Min(zRange.x, p.z - s.radius);
                    zRange.y = Mathf.Max(zRange.y, p.z + s.radius);
                }
            }
        }
    }
}
