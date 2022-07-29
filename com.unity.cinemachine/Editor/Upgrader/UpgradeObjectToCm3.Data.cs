#pragma warning disable CS0618 // obsolete warnings

using System;
using System.Collections.Generic;
using UnityEngine.Splines;

namespace Cinemachine.Editor
{
    partial class UpgradeObjectToCm3
    {
        /// <summary>
        /// Search for these types to find GameObjects to upgrade
        /// </summary>
        public readonly List<Type> RootUpgradeComponentTypes = new()
        {
            // Put the paths first so any vcam references to them will convert
            typeof(CinemachinePath),
            typeof(CinemachineSmoothPath),
            typeof(CinemachineDollyCart),
            // FreeLook before vcam because we want to delete the vcam child rigs and not convert them
            typeof(CinemachineFreeLook),
            typeof(CinemachineVirtualCamera),
        };
        
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
#if CINEMACHINE_UNITY_INPUTSYSTEM
            typeof(CinemachineInputProvider),
#endif
        };
        
        /// <summary>
        /// Maps class upgrades.
        /// </summary>
        readonly Dictionary<Type, Type> m_ClassUpgradeMap = new()
        {
            { typeof(CinemachineVirtualCamera), typeof(CmCamera) },
            { typeof(CinemachineFreeLook), typeof(CmCamera) },
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
#if CINEMACHINE_UNITY_INPUTSYSTEM
            { typeof(CinemachineInputProvider), typeof(InputAxisController) },
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
                    { "LookaheadTime", new Tuple<string, Type>("Lookahead.Time", typeof(CinemachinePositionComposer)) },
                    { "LookaheadSmoothing", new Tuple<string, Type>("Lookahead.Smoothing", typeof(CinemachinePositionComposer)) },
                    { "LookaheadIgnoreY", new Tuple<string, Type>("Lookahead.IgnoreY", typeof(CinemachinePositionComposer)) },
                    { "XDamping", new Tuple<string, Type>("Damping.x", typeof(CinemachinePositionComposer)) },
                    { "YDamping", new Tuple<string, Type>("Damping.y", typeof(CinemachinePositionComposer)) },
                    { "ZDamping", new Tuple<string, Type>("Damping.z", typeof(CinemachinePositionComposer)) },
                    { "ScreenX", new Tuple<string, Type>("Composition.ScreenPosition.x", typeof(CinemachinePositionComposer)) },
                    { "ScreenY", new Tuple<string, Type>("Composition.ScreenPosition.y", typeof(CinemachinePositionComposer)) },
                    { "DeadZoneWidth", new Tuple<string, Type>("Composition.DeadZoneSize.x", typeof(CinemachinePositionComposer)) },
                    { "DeadZoneHeight", new Tuple<string, Type>("Composition.DeadZoneSize.y", typeof(CinemachinePositionComposer)) },
                    { "SoftZoneWidth", new Tuple<string, Type>("Composition.SoftZoneSize.x", typeof(CinemachinePositionComposer)) },
                    { "SoftZoneHeight", new Tuple<string, Type>("Composition.SoftZoneSize.y", typeof(CinemachinePositionComposer)) },
                    { "BiasX", new Tuple<string, Type>("Composition.Bias.x", typeof(CinemachinePositionComposer)) },
                    { "BiasY", new Tuple<string, Type>("Composition.Bias.y", typeof(CinemachinePositionComposer)) },
                    { "GroupFramingSize", new Tuple<string, Type>("FramingSize", typeof(CinemachineGroupFraming)) },
                    { "FrameDamping", new Tuple<string, Type>("Damping", typeof(CinemachineGroupFraming)) },
                    { "MinimumFOV", new Tuple<string, Type>("FovRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaximumFOV", new Tuple<string, Type>("FovRange.y", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyIn", new Tuple<string, Type>("DollyRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyOut", new Tuple<string, Type>("DollyRange.y", typeof(CinemachineGroupFraming)) },
                    { "AdjustmentMode", new Tuple<string, Type>("SizeAdjustment", typeof(CinemachineGroupFraming)) }
                }
            },
            {
                typeof(CinemachineOrbitalTransposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "XDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.x", typeof(CinemachineOrbitalFollow)) },
                    { "YDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.y", typeof(CinemachineOrbitalFollow)) },
                    { "ZDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.z", typeof(CinemachineOrbitalFollow)) },
                    { "PitchDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.x", typeof(CinemachineOrbitalFollow)) },
                    { "YawDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.y", typeof(CinemachineOrbitalFollow)) },
                    { "RollDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.z", typeof(CinemachineOrbitalFollow)) },
                    { "BindingMode", new Tuple<string, Type>("TrackerSettings.BindingMode", typeof(CinemachineOrbitalFollow)) },
                    { "AngularDampingMode", new Tuple<string, Type>("TrackerSettings.AngularDampingMode", typeof(CinemachineOrbitalFollow)) },
                    { "AngularDamping", new Tuple<string, Type>("TrackerSettings.QuaternionDamping", typeof(CinemachineOrbitalFollow)) },
                    { "XAxis.Value", new Tuple<string, Type>("managedReferences[HorizontalAxis].Value", typeof(CinemachineOrbitalFollow)) },
                    { "YAxis.Value", new Tuple<string, Type>("managedReferences[VerticalAxis].Value", typeof(CinemachineOrbitalFollow)) },
                    { "ZAxis.Value", new Tuple<string, Type>("managedReferences[RadialAxis].Value", typeof(CinemachineOrbitalFollow)) },
                }
            },
            {
                typeof(CinemachineTransposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "XDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.x", typeof(CinemachineFollow)) },
                    { "YDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.y", typeof(CinemachineFollow)) },
                    { "ZDamping", new Tuple<string, Type>("TrackerSettings.PositionDamping.z", typeof(CinemachineFollow)) },
                    { "PitchDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.x", typeof(CinemachineFollow)) },
                    { "YawDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.y", typeof(CinemachineFollow)) },
                    { "RollDamping", new Tuple<string, Type>("TrackerSettings.RotationDamping.z", typeof(CinemachineFollow)) },
                    { "BindingMode", new Tuple<string, Type>("TrackerSettings.BindingMode", typeof(CinemachineFollow)) },
                    { "AngularDampingMode", new Tuple<string, Type>("TrackerSettings.AngularDampingMode", typeof(CinemachineFollow)) },
                    { "AngularDamping", new Tuple<string, Type>("TrackerSettings.QuaternionDamping", typeof(CinemachineFollow)) }
                }
            },
            {
                typeof(CinemachineTrackedDolly), new Dictionary<string, Tuple<string, Type>>
                {
                    { "PathOffset.x", new Tuple<string, Type>("SplineOffset.x", typeof(CinemachineSplineDolly)) },
                    { "PathOffset.y", new Tuple<string, Type>("SplineOffset.y", typeof(CinemachineSplineDolly)) },
                    { "PathOffset.z", new Tuple<string, Type>("SplineOffset.z", typeof(CinemachineSplineDolly)) },
                    { "PathPosition", new Tuple<string, Type>("CameraPosition", typeof(CinemachineSplineDolly)) },
                    { "XDamping", new Tuple<string, Type>("Damping.Position.x", typeof(CinemachineSplineDolly)) },
                    { "YDamping", new Tuple<string, Type>("Damping.Position.y", typeof(CinemachineSplineDolly)) },
                    { "ZDamping", new Tuple<string, Type>("Damping.Position.z", typeof(CinemachineSplineDolly)) }
                }
            },
            {
                typeof(CinemachineComposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "LookaheadTime", new Tuple<string, Type>("Lookahead.Time", typeof(CinemachineRotationComposer)) },
                    { "LookaheadSmoothing", new Tuple<string, Type>("Lookahead.Smoothing", typeof(CinemachineRotationComposer)) },
                    { "LookaheadIgnoreY", new Tuple<string, Type>("Lookahead.IgnoreY", typeof(CinemachineRotationComposer)) },
                    { "HorizontalDamping", new Tuple<string, Type>("Damping.x", typeof(CinemachineRotationComposer)) },
                    { "VerticalDamping", new Tuple<string, Type>("Damping.y", typeof(CinemachineRotationComposer)) },
                    { "ScreenX", new Tuple<string, Type>("Composition.ScreenPosition.x", typeof(CinemachineRotationComposer)) },
                    { "ScreenY", new Tuple<string, Type>("Composition.ScreenPosition.y", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneWidth", new Tuple<string, Type>("Composition.DeadZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneHeight", new Tuple<string, Type>("Composition.DeadZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneWidth", new Tuple<string, Type>("Composition.SoftZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneHeight", new Tuple<string, Type>("Composition.SoftZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "BiasX", new Tuple<string, Type>("Composition.Bias.x", typeof(CinemachineRotationComposer)) },
                    { "BiasY", new Tuple<string, Type>("Composition.Bias.y", typeof(CinemachineRotationComposer)) }
                }
            },
            {
                typeof(CinemachineGroupComposer), new Dictionary<string, Tuple<string, Type>>
                {
                    { "LookaheadTime", new Tuple<string, Type>("Lookahead.Time", typeof(CinemachineRotationComposer)) },
                    { "LookaheadSmoothing", new Tuple<string, Type>("Lookahead.Smoothing", typeof(CinemachineRotationComposer)) },
                    { "LookaheadIgnoreY", new Tuple<string, Type>("Lookahead.IgnoreY", typeof(CinemachineRotationComposer)) },
                    { "HorizontalDamping", new Tuple<string, Type>("Damping.x", typeof(CinemachineRotationComposer)) },
                    { "VerticalDamping", new Tuple<string, Type>("Damping.y", typeof(CinemachineRotationComposer)) },
                    { "ScreenX", new Tuple<string, Type>("Composition.ScreenPosition.x", typeof(CinemachineRotationComposer)) },
                    { "ScreenY", new Tuple<string, Type>("Composition.ScreenPosition.y", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneWidth", new Tuple<string, Type>("Composition.DeadZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "DeadZoneHeight", new Tuple<string, Type>("Composition.DeadZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneWidth", new Tuple<string, Type>("Composition.SoftZoneSize.x", typeof(CinemachineRotationComposer)) },
                    { "SoftZoneHeight", new Tuple<string, Type>("Composition.SoftZoneSize.y", typeof(CinemachineRotationComposer)) },
                    { "BiasX", new Tuple<string, Type>("Composition.Bias.x", typeof(CinemachineRotationComposer)) },
                    { "BiasY", new Tuple<string, Type>("Composition.Bias.y", typeof(CinemachineRotationComposer)) },
                    { "GroupFramingSize", new Tuple<string, Type>("FramingSize", typeof(CinemachineGroupFraming)) },
                    { "FrameDamping", new Tuple<string, Type>("Damping", typeof(CinemachineGroupFraming)) },
                    { "MinimumFOV", new Tuple<string, Type>("FovRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaximumFOV", new Tuple<string, Type>("FovRange.y", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyIn", new Tuple<string, Type>("DollyRange.x", typeof(CinemachineGroupFraming)) },
                    { "MaxDollyOut", new Tuple<string, Type>("DollyRange.y", typeof(CinemachineGroupFraming)) },
                    { "AdjustmentMode", new Tuple<string, Type>("SizeAdjustment", typeof(CinemachineGroupFraming)) }
                }
            },
            {
                typeof(CinemachinePOV), new Dictionary<string, Tuple<string, Type>>
                {
                    { "HorizontalAxis.Value", new Tuple<string, Type>("managedReferences[PanAxis].Value", typeof(CinemachinePanTilt))},
                    { "VerticalAxis.Value", new Tuple<string, Type>("managedReferences[TiltAxis].Value", typeof(CinemachinePanTilt))},
                }
            }
        };
    }
}