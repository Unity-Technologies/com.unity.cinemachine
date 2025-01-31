#if !CINEMACHINE_NO_CM2_SUPPORT
#pragma warning disable CS0618 // obsolete warnings

using System;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace Unity.Cinemachine.Editor
{
    partial class UpgradeObjectToCm3
    {
        /// <summary>
        /// Search for these types to find GameObjects to upgrade.
        /// The order is important: Referencables first, then NonReferencables for the conversion algorithm.
        /// </summary>
        public readonly List<Type> RootUpgradeComponentTypes = new()
        {
            typeof(CinemachinePath),
            typeof(CinemachineSmoothPath),
            typeof(CinemachineDollyCart),
#if CINEMACHINE_PHYSICS
            typeof(CinemachineCollider),
#endif
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
            typeof(CinemachineConfiner),
#endif
            // FreeLook before vcam because we want to delete the vcam child rigs and not convert them
            typeof(CinemachineFreeLook),
            typeof(CinemachineVirtualCamera),
        };

        /// <summary>
        /// Any component that may be referenced by vcams or freelooks
        /// </summary>
        public static readonly List<Type> Referencables = new()
        {
            typeof(CinemachinePathBase),
            typeof(CinemachineDollyCart),
        };

        public static bool HasReferencableComponent(UnityEngine.GameObject go)
        {
            foreach (var referencable in Referencables)
            {
                var c = go.GetComponentInChildren(referencable);
                if (c != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// After the upgrade is complete, these components should be deleted
        /// </summary>
        public readonly List<Type> ObsoleteComponentTypesToDelete = new()
        {
            typeof(CinemachineVirtualCamera),
            typeof(CinemachineFreeLook),
            typeof(CinemachineComposer),
            typeof(CinemachineGroupComposer),
            typeof(CinemachineTransposer),
            typeof(CinemachineFramingTransposer),
            typeof(CinemachinePOV),
            typeof(CinemachineOrbitalTransposer),
            typeof(CinemachineTrackedDolly),
            typeof(CinemachinePath),
            typeof(CinemachineSmoothPath),
            typeof(CinemachineDollyCart),
            typeof(CinemachinePipeline),
            typeof(Cinemachine3rdPersonFollow),
            typeof(CinemachineSameAsFollowTarget),
#if CINEMACHINE_UNITY_INPUTSYSTEM
            typeof(CinemachineInputProvider),
#endif
#if CINEMACHINE_PHYSICS
            typeof(CinemachineCollider),
#endif
#if CINEMACHINE_PHYSICS || CINEMACHINE_PHYSICS_2D
            typeof(CinemachineConfiner),
#endif
        };

        /// <summary>
        /// Maps class upgrades.
        /// </summary>
        public static readonly Dictionary<Type, Type> ClassUpgradeMap = new()
        {
            { typeof(CinemachineVirtualCamera), typeof(CinemachineCamera) },
            { typeof(CinemachineFreeLook), typeof(CinemachineCamera) },
            { typeof(CinemachineComposer), typeof(CinemachineRotationComposer) },
            { typeof(CinemachineGroupComposer), typeof(CinemachineRotationComposer) },
            { typeof(CinemachineTransposer), typeof(CinemachineFollow) },
            { typeof(CinemachineFramingTransposer), typeof(CinemachinePositionComposer) },
            { typeof(CinemachinePOV), typeof(CinemachinePanTilt) },
            { typeof(CinemachineOrbitalTransposer), typeof(CinemachineOrbitalFollow) },
            { typeof(CinemachineTrackedDolly), typeof(CinemachineSplineDolly) },
            { typeof(CinemachinePath), typeof(SplineContainer) },
            { typeof(CinemachineSmoothPath), typeof(SplineContainer) },
            { typeof(CinemachineDollyCart), typeof(CinemachineSplineCart) },
            { typeof(Cinemachine3rdPersonFollow), typeof(CinemachineThirdPersonFollow) },
            { typeof(CinemachineSameAsFollowTarget), typeof(CinemachineRotateWithFollowTarget) },
#if CINEMACHINE_PHYSICS
            { typeof(CinemachineCollider), typeof(CinemachineDeoccluder) },
#endif
        };

        /// <summary>
        /// Maps API changes.
        /// Some API changes need special care, because type could be different for different properties,
        /// because some components became several separate components.
        /// ManagedReferences also need special care, because instead of simply mapping to a propertyName, we need to map
        /// to the reference id. These are marked as ManagedReference[Propertyname].Value
        /// </summary>
        readonly Dictionary<Type, Dictionary<string, Tuple<string, Type>>> m_APIUpgradeMaps = new()
        {
            {
                typeof(CinemachineFramingTransposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "LookaheadTime", new("Lookahead.Time", typeof(CinemachinePositionComposer)) },
                    { "LookaheadSmoothing", new("Lookahead.Smoothing", typeof(CinemachinePositionComposer)) },
                    { "LookaheadIgnoreY", new("Lookahead.IgnoreY", typeof(CinemachinePositionComposer)) },
                    { "XDamping", new("Damping.x", typeof(CinemachinePositionComposer)) },
                    { "YDamping", new("Damping.y", typeof(CinemachinePositionComposer)) },
                    { "ZDamping", new("Damping.z", typeof(CinemachinePositionComposer)) },
                    { "ScreenX", new("Composition.ScreenPosition.x", typeof(CinemachinePositionComposer)) },
                    { "ScreenY", new("Composition.ScreenPosition.y", typeof(CinemachinePositionComposer)) },
                    { "DeadZoneWidth", new("Composition.DeadZone.Size.x", typeof(CinemachinePositionComposer)) },
                    { "DeadZoneHeight", new("Composition.DeadZone.Size.y", typeof(CinemachinePositionComposer)) },
                    { "SoftZoneWidth", new("Composition.HardLimits.Size.x", typeof(CinemachinePositionComposer)) },
                    { "SoftZoneHeight", new("Composition.HardLimits.Size.y", typeof(CinemachinePositionComposer)) },
                    { "BiasX", new("Composition.HardLimits.Offset.x", typeof(CinemachinePositionComposer)) },
                    { "BiasY", new("Composition.HardLimits.Offset.y", typeof(CinemachinePositionComposer)) },
                    { "GroupFramingSize", new("FramingSize", typeof(CinemachineGroupFraming)) },
                    { "FrameDamping", new("Damping", typeof(CinemachineGroupFraming)) },
                    { "MinimumFOV", new("FovRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaximumFOV", new("FovRange.y", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyIn", new("DollyRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyOut", new("DollyRange.y", typeof(CinemachineGroupFraming)) },
                    { "AdjustmentMode", new("SizeAdjustment", typeof(CinemachineGroupFraming)) }
                }
            },
            {
                typeof(CinemachineOrbitalTransposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "XDamping", new("TrackerSettings.PositionDamping.x", typeof(CinemachineOrbitalFollow)) },
                    { "YDamping", new("TrackerSettings.PositionDamping.y", typeof(CinemachineOrbitalFollow)) },
                    { "ZDamping", new("TrackerSettings.PositionDamping.z", typeof(CinemachineOrbitalFollow)) },
                    { "PitchDamping", new("TrackerSettings.RotationDamping.x", typeof(CinemachineOrbitalFollow)) },
                    { "YawDamping", new("TrackerSettings.RotationDamping.y", typeof(CinemachineOrbitalFollow)) },
                    { "RollDamping", new("TrackerSettings.RotationDamping.z", typeof(CinemachineOrbitalFollow)) },
                    { "BindingMode", new("TrackerSettings.BindingMode", typeof(CinemachineOrbitalFollow)) },
                    { "AngularDampingMode", new("TrackerSettings.AngularDampingMode", typeof(CinemachineOrbitalFollow)) },
                    { "AngularDamping", new("TrackerSettings.QuaternionDamping", typeof(CinemachineOrbitalFollow)) },
                    { "XAxis.Value", new("HorizontalAxis.Value", typeof(CinemachineOrbitalFollow)) },
                    { "YAxis.Value", new("VerticalAxis.Value", typeof(CinemachineOrbitalFollow)) },
                    { "ZAxis.Value", new("RadialAxis.Value", typeof(CinemachineOrbitalFollow)) }
                }
            },
            {
                typeof(CinemachineTransposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "XDamping", new("TrackerSettings.PositionDamping.x", typeof(CinemachineFollow)) },
                    { "YDamping", new("TrackerSettings.PositionDamping.y", typeof(CinemachineFollow)) },
                    { "ZDamping", new("TrackerSettings.PositionDamping.z", typeof(CinemachineFollow)) },
                    { "PitchDamping", new("TrackerSettings.RotationDamping.x", typeof(CinemachineFollow)) },
                    { "YawDamping", new("TrackerSettings.RotationDamping.y", typeof(CinemachineFollow)) },
                    { "RollDamping", new("TrackerSettings.RotationDamping.z", typeof(CinemachineFollow)) },
                    { "BindingMode", new("TrackerSettings.BindingMode", typeof(CinemachineFollow)) },
                    { "AngularDampingMode", new("TrackerSettings.AngularDampingMode", typeof(CinemachineFollow)) },
                    { "AngularDamping", new("TrackerSettings.QuaternionDamping", typeof(CinemachineFollow)) }
                }
            },
            {
                typeof(CinemachineTrackedDolly), new Dictionary<string, Tuple<string, Type>>
                {
                    { "PathOffset.x", new("SplineOffset.x", typeof(CinemachineSplineDolly)) },
                    { "PathOffset.y", new("SplineOffset.y", typeof(CinemachineSplineDolly)) },
                    { "PathOffset.z", new("SplineOffset.z", typeof(CinemachineSplineDolly)) },
                    { "PathPosition", new("SplineSettings.Position", typeof(CinemachineSplineDolly)) },
                    { "XDamping", new("Damping.Position.x", typeof(CinemachineSplineDolly)) },
                    { "YDamping", new("Damping.Position.y", typeof(CinemachineSplineDolly)) },
                    { "ZDamping", new("Damping.Position.z", typeof(CinemachineSplineDolly)) }
                }
            },
            {
                typeof(CinemachineComposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "LookaheadTime", new("Lookahead.Time", typeof(CinemachineRotationComposer)) },
                    { "LookaheadSmoothing", new("Lookahead.Smoothing", typeof(CinemachineRotationComposer)) },
                    { "LookaheadIgnoreY", new("Lookahead.IgnoreY", typeof(CinemachineRotationComposer)) },
                    { "HorizontalDamping", new("Damping.x", typeof(CinemachineRotationComposer)) },
                    { "VerticalDamping", new("Damping.y", typeof(CinemachineRotationComposer)) },
                    { "ScreenX", new("Composition.ScreenPosition.x", typeof(CinemachineRotationComposer)) },
                    { "ScreenY", new("Composition.ScreenPosition.y", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneWidth", new("Composition.DeadZone.Size.x", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneHeight", new("Composition.DeadZone.Size.y", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneWidth", new("Composition.HardLimits.Size.x", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneHeight", new("Composition.HardLimits.Size.y", typeof(CinemachineRotationComposer)) },
                    { "BiasX", new("Composition.HardLimits.Offset.x", typeof(CinemachineRotationComposer)) },
                    { "BiasY", new("Composition.HardLimits.Offset.y", typeof(CinemachineRotationComposer)) }
                }
            },
            {
                typeof(CinemachineGroupComposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "LookaheadTime", new("Lookahead.Time", typeof(CinemachineRotationComposer)) },
                    { "LookaheadSmoothing", new("Lookahead.Smoothing", typeof(CinemachineRotationComposer)) },
                    { "LookaheadIgnoreY", new("Lookahead.IgnoreY", typeof(CinemachineRotationComposer)) },
                    { "HorizontalDamping", new("Damping.x", typeof(CinemachineRotationComposer)) },
                    { "VerticalDamping", new("Damping.y", typeof(CinemachineRotationComposer)) },
                    { "ScreenX", new("Composition.ScreenPosition.x", typeof(CinemachineRotationComposer)) },
                    { "ScreenY", new("Composition.ScreenPosition.y", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneWidth", new("Composition.DeadZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneHeight", new("Composition.DeadZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneWidth", new("Composition.SoftZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneHeight", new("Composition.SoftZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "BiasX", new("Composition.Offset.x", typeof(CinemachineRotationComposer)) },
                    { "BiasY", new("Composition.Offset.y", typeof(CinemachineRotationComposer)) },
                    { "GroupFramingSize", new("FramingSize", typeof(CinemachineGroupFraming)) },
                    { "FrameDamping", new("Damping", typeof(CinemachineGroupFraming)) },
                    { "MinimumFOV", new("FovRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaximumFOV", new("FovRange.y", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyIn", new("DollyRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyOut", new("DollyRange.y", typeof(CinemachineGroupFraming)) },
                    { "AdjustmentMode", new("SizeAdjustment", typeof(CinemachineGroupFraming)) }
                }
            },
            {
                typeof(CinemachinePOV), new Dictionary<string, Tuple<string, Type>>
                {
                    { "HorizontalAxis.Value", new("PanAxis.Value", typeof(CinemachinePanTilt)) },
                    { "VerticalAxis.Value", new("TiltAxis.Value", typeof(CinemachinePanTilt)) }
                }
            },
            {
                typeof(CinemachineFreeLook), new Dictionary<string, Tuple<string, Type>>
                {
                    { "XAxis.Value", new("HorizontalAxis.Value", typeof(CinemachineOrbitalFollow)) },
                    { "YAxis.Value", new("VerticalAxis.Value", typeof(CinemachineOrbitalFollow)) },
                    { "Lens.GateFit", new("Lens.PhysicalProperties.GateFit", typeof(CinemachineCamera)) },
                    { "Lens.LensShift.x", new("Lens.PhysicalProperties.LensShift.x", typeof(CinemachineCamera)) },
                    { "Lens.LensShift.y", new("Lens.PhysicalProperties.LensShift.y", typeof(CinemachineCamera)) },
                    { "Lens.FocusDistance", new("Lens.PhysicalProperties.FocusDistance", typeof(CinemachineCamera)) },
                    { "Lens.Iso", new("Lens.PhysicalProperties.Iso", typeof(CinemachineCamera)) },
                    { "Lens.ShutterSpeed", new("Lens.PhysicalProperties.ShutterSpeed", typeof(CinemachineCamera)) },
                    { "Lens.Aperture", new("Lens.PhysicalProperties.Aperture", typeof(CinemachineCamera)) },
                    { "Lens.BladeCount", new("Lens.PhysicalProperties.BladeCount", typeof(CinemachineCamera)) },
                    { "Lens.Curvature.x", new("Lens.PhysicalProperties.Curvature.x", typeof(CinemachineCamera)) },
                    { "Lens.Curvature.y", new("Lens.PhysicalProperties.Curvature.y", typeof(CinemachineCamera)) },
                    { "Lens.BarrelClipping", new("Lens.PhysicalProperties.BarrelClipping", typeof(CinemachineCamera)) },
                    { "Lens.Anamorphism", new("Lens.PhysicalProperties.Anamorphism", typeof(CinemachineCamera)) },
                }
            },
            {
                typeof(CinemachineVirtualCamera), new Dictionary<string, Tuple<string, Type>>
                {
                    { "Lens.GateFit", new("Lens.PhysicalProperties.GateFit", typeof(CinemachineCamera)) },
                    { "Lens.LensShift.x", new("Lens.PhysicalProperties.LensShift.x", typeof(CinemachineCamera)) },
                    { "Lens.LensShift.y", new("Lens.PhysicalProperties.LensShift.y", typeof(CinemachineCamera)) },
                    { "Lens.FocusDistance", new("Lens.PhysicalProperties.FocusDistance", typeof(CinemachineCamera)) },
                    { "Lens.Iso", new("Lens.PhysicalProperties.Iso", typeof(CinemachineCamera)) },
                    { "Lens.ShutterSpeed", new("Lens.PhysicalProperties.ShutterSpeed", typeof(CinemachineCamera)) },
                    { "Lens.Aperture", new("Lens.PhysicalProperties.Aperture", typeof(CinemachineCamera)) },
                    { "Lens.BladeCount", new("Lens.PhysicalProperties.BladeCount", typeof(CinemachineCamera)) },
                    { "Lens.Curvature.x", new("Lens.PhysicalProperties.Curvature.x", typeof(CinemachineCamera)) },
                    { "Lens.Curvature.y", new("Lens.PhysicalProperties.Curvature.y", typeof(CinemachineCamera)) },
                    { "Lens.BarrelClipping", new("Lens.PhysicalProperties.BarrelClipping", typeof(CinemachineCamera)) },
                    { "Lens.Anamorphism", new("Lens.PhysicalProperties.Anamorphism", typeof(CinemachineCamera)) },
                }
            },
            {
                typeof(Cinemachine3rdPersonFollow), new Dictionary<string, Tuple<string, Type>>
                {
                    { "CameraCollisionFilter", new("AvoidObstacles.CollisionFilter", typeof(CinemachineThirdPersonFollow)) },
                    { "IgnoreTag", new("AvoidObstacles.IgnoreTag", typeof(CinemachineThirdPersonFollow)) },
                    { "CameraRadius", new("AvoidObstacles.CameraRadius", typeof(CinemachineThirdPersonFollow)) },
                    { "DampingIntoCollision", new("AvoidObstacles.DampingIntoCollision", typeof(CinemachineThirdPersonFollow)) },
                    { "DampingFromCollision", new("AvoidObstacles.DampingFromCollision", typeof(CinemachineThirdPersonFollow)) }
                }
            },
            {
                typeof(CinemachineDollyCart), new Dictionary<string, Tuple<string, Type>>
                {
                    { "SplinePosition", new("SplineSettings.Position", typeof(CinemachineSplineCart)) }
                }
            },
#if CINEMACHINE_PHYSICS
            {
                typeof(CinemachineCollider), new Dictionary<string, Tuple<string, Type>>
                {
                    { "AvoidObstacles", new("AvoidObstacles.Enabled", typeof(CinemachineDeoccluder)) },
                    { "DistanceLimit", new("AvoidObstacles.DistanceLimit", typeof(CinemachineDeoccluder)) },
                    { "MinimumOcclusionTime", new("AvoidObstacles.MinimumOcclusionTime", typeof(CinemachineDeoccluder)) },
                    { "CameraRadius", new("AvoidObstacles.CameraRadius", typeof(CinemachineDeoccluder)) },
                    { "Strategy", new("AvoidObstacles.Strategy", typeof(CinemachineDeoccluder)) },
                    { "MaximumEffort", new("AvoidObstacles.MaximumEffort", typeof(CinemachineDeoccluder)) },
                    { "SmoothingTime", new("AvoidObstacles.SmoothingTime", typeof(CinemachineDeoccluder)) },
                    { "Damping", new("AvoidObstacles.Damping", typeof(CinemachineDeoccluder)) },
                    { "DampingWhenOccluded", new("AvoidObstacles.DampingWhenOccluded", typeof(CinemachineDeoccluder)) },
                    { "OptimalTargetDistance", new("ShotQualityEvaluation.OptimalDistance", typeof(CinemachineDeoccluder)) },
                }
            },
#endif
        };
    }
}
#endif
