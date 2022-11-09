//#define DEBUG_HELPERS
#pragma warning disable CS0618 // obsolete warnings

using System;
using System.Linq;
using Cinemachine.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

using Object = UnityEngine.Object;

namespace Cinemachine.Editor
{
    partial class UpgradeObjectToCm3
    {
        /// <summary>
        /// In-place upgrade a GameObject.  Obsolete components are disabled (not deleted), 
        /// and replacement components are added.  GameObject structure may be re-organized
        /// (hidden objects deleted, components moved).
        /// 
        /// In a second pass, call DeleteObsoleteComponents().
        /// </summary>
        /// <param name="go">The GameObject to upgrade</param>
        /// <returns>If the GameObject is not fully upgradable, a clone of the original will be returned.
        /// A null return value means success.</returns>
        public GameObject UpgradeComponents(GameObject go)
        {
            GameObject notUpgradable = null;

            // Is it a DollyCart?
            if (ReplaceComponent<CinemachineDollyCart, CinemachineSplineCart>(go))
            {
                var obsoleteDolly = go.GetComponent<CinemachineDollyCart>();
                var splineCart = go.GetComponent<CinemachineSplineCart>();
                obsoleteDolly.UpgradeToCm3(splineCart);
            }

            // Is it a path?
            if (go.TryGetComponent(out CinemachinePathBase path))
                UpgradePath(path);
            else
            {
                // It's some kind of vcam.  Check for FreeLook first because it has
                // hidden child VirtualCameras and we need to remove them
                if (go.TryGetComponent<CinemachineFreeLook>(out var freelook))
                    notUpgradable = UpgradeFreelook(freelook);
                else if (go.TryGetComponent<CinemachineVirtualCamera>(out var vcam))
                    notUpgradable = UpgradeVcam(vcam);

                // Upgrade the pipeline components (there will be more of these...)
                if (ReplaceComponent<CinemachineComposer, CinemachineRotationComposer>(go))
                     go.GetComponent<CinemachineComposer>().UpgradeToCm3(go.GetComponent<CinemachineRotationComposer>());
                if (ReplaceComponent<CinemachineGroupComposer, CinemachineRotationComposer>(go))
                {
                    var gc = go.GetComponent<CinemachineGroupComposer>();
                    gc.UpgradeToCm3(go.GetComponent<CinemachineRotationComposer>());
                    if (!go.TryGetComponent<CinemachineGroupFraming>(out var _))
                    {
                        var framer = Undo.AddComponent<CinemachineGroupFraming>(go);
                        go.GetComponent<CmCamera>().AddExtension(framer);
                        gc.UpgradeToCm3(framer);
                    }
                }
                if (ReplaceComponent<CinemachineTransposer, CinemachineFollow>(go))
                     go.GetComponent<CinemachineTransposer>().UpgradeToCm3(go.GetComponent<CinemachineFollow>());
                if (ReplaceComponent<CinemachineFramingTransposer, CinemachinePositionComposer>(go))
                {
                    var ft = go.GetComponent<CinemachineFramingTransposer>();
                    ft.UpgradeToCm3(go.GetComponent<CinemachinePositionComposer>());
                    if (ft.FollowTargetAsGroup != null 
                        && ft.m_GroupFramingMode != CinemachineFramingTransposer.FramingMode.None
                        && !go.TryGetComponent<CinemachineGroupFraming>(out var _))
                    {
                        var framer = Undo.AddComponent<CinemachineGroupFraming>(go);
                        go.GetComponent<CmCamera>().AddExtension(framer);
                        ft.UpgradeToCm3(framer);
                    }
                }
                if (ReplaceComponent<CinemachinePOV, CinemachinePanTilt>(go))
                {
                     var pov = go.GetComponent<CinemachinePOV>();
                     pov.UpgradeToCm3(go.GetComponent<CinemachinePanTilt>());
                     ConvertInputAxis(go, "Look X (Pan)", ref pov.m_HorizontalAxis, ref pov.m_HorizontalRecentering);
                     ConvertInputAxis(go, "Look Y (Tilt)", ref pov.m_VerticalAxis, ref pov.m_VerticalRecentering);
                }
                if (ReplaceComponent<CinemachineOrbitalTransposer, CinemachineOrbitalFollow>(go))
                {
                     var orbital = go.GetComponent<CinemachineOrbitalTransposer>();
                     orbital.UpgradeToCm3(go.GetComponent<CinemachineOrbitalFollow>());
                     ConvertInputAxis(go, "Look Orbit X", ref orbital.m_XAxis, ref orbital.m_RecenterToTargetHeading);
                }

                if (ReplaceComponent<Cinemachine3rdPersonFollow, CinemachineThirdPersonFollow>(go)) 
                    go.GetComponent<Cinemachine3rdPersonFollow>().UpgradeToCm3(go.GetComponent<CinemachineThirdPersonFollow>());

                if (ReplaceComponent<CinemachineTrackedDolly, CinemachineSplineDolly>(go))
                {
                    var obsoleteDolly = go.GetComponent<CinemachineTrackedDolly>();
                    var splineDolly = go.GetComponent<CinemachineSplineDolly>();
                    obsoleteDolly.UpgradeToCm3(splineDolly);
                }
            }
            return notUpgradable;
        }

        /// <summary>
        /// Handles animation clip reference updates for the input animation clip.
        /// Does not support Undo!
        /// </summary>
        public void ProcessAnimationClip(AnimationClip animationClip, Animator trackAnimator)
        {
            var existingEditorBindings = AnimationUtility.GetCurveBindings(animationClip);
            foreach (var previousBinding in existingEditorBindings)
            {
                // if type is not in class upgrade map, then we won't change binding
                if (!ClassUpgradeMap.ContainsKey(previousBinding.type))
                    break;
                    
                var newBinding = previousBinding;
                // upgrade type based on mapping
                newBinding.type = ClassUpgradeMap[previousBinding.type];

                // clean path pointing to old structure where vcam components lived on a hidden child gameObject
                if (previousBinding.path.Contains("cm"))
                {
                    var path = previousBinding.path;

                    //path is either cm only, or someParent/someOtherParent/.../cm. In the second case, we need to remove /cm, thus -1
                    var index = Mathf.Max(0, path.IndexOf("cm") - 1);
                    newBinding.path = path.Substring(0, index);
                }

                if (previousBinding.path.Contains("Rig"))
                {
                    var path = newBinding.path;
                    //path is either xRig only, or someParent/someOtherParent/.../xRig. In the second case, we need to remove /xRig, thus -1
                    newBinding.path = path.Substring(0, Mathf.Max(0, FindRig(path) - 1));

                    static int FindRig(string path)
                    {
                        var rigs = CinemachineFreeLook.RigNames;
                        var index = 0;
                        foreach (var rig in rigs)
                            index = Mathf.Max(index, path.IndexOf(rig));
                        return index;
                    }
                }

                // clean old convention
                if (previousBinding.propertyName.Contains("m_"))
                {
                    var propertyName = previousBinding.propertyName;
                    newBinding.propertyName = propertyName.Replace("m_", string.Empty);
                }

                // Check if previousBinding.type needs an API change
                if (m_APIUpgradeMaps.ContainsKey(previousBinding.type) &&
                    m_APIUpgradeMaps[previousBinding.type].ContainsKey(newBinding.propertyName))
                {
                    var mapping = m_APIUpgradeMaps[previousBinding.type][newBinding.propertyName];
                    newBinding.propertyName = mapping.Item1;
                    // type can differ from previously set type, because some components became several separate ones
                    newBinding.type = mapping.Item2; 
                    
                    // special handling for references
                    if (mapping.Item1.Contains("managedReferences"))
                    {
                        // find property name of the reference
                        var propertyName = mapping.Item1;
                        var start = propertyName.IndexOf("[") + 1;
                        var end = propertyName.IndexOf("]");
                        propertyName = propertyName[start..end];

                        // find animated target component
                        var target = trackAnimator.transform.gameObject.GetComponent(mapping.Item2);

                        // find reference id of the property that's being animated
                        var so = new SerializedObject(target);
                        var prop = so.FindProperty(propertyName);
                        var newPropertyName = newBinding.propertyName;
                        newBinding.propertyName = newPropertyName[..start] + prop.managedReferenceId + newPropertyName[end..];
                    }
                }

                var curve = AnimationUtility.GetEditorCurve(animationClip, previousBinding); //keep existing curves
                AnimationUtility.SetEditorCurve(animationClip, previousBinding, null); //remove previous binding
                AnimationUtility.SetEditorCurve(animationClip, newBinding, curve); //set new binding
            }
        }

        /// <summary>
        /// After a GameObject object has been in-place upgraded, call this to delete the
        /// obsolete components that have been disabled.
        /// </summary>
        /// <param name="go">The GameObject being upgraded</param>
        public void DeleteObsoleteComponents(GameObject go)
        {
            foreach (var t in ObsoleteComponentTypesToDelete)
            {
                var components = go.GetComponentsInChildren(t);
                foreach (var c in components) 
                    Undo.DestroyObjectImmediate(c);
            }
            if (PrefabUtility.IsPartOfAnyPrefab(go))
                PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }
 
        /// Disable an obsolete component and add a replacement
        static bool ReplaceComponent<TOld, TNew>(GameObject go) 
            where TOld : MonoBehaviour
            where TNew : MonoBehaviour
        {
            if (go.TryGetComponent<TOld>(out var cOld) && cOld.GetType() == typeof(TOld))
            {
                Undo.RecordObject(cOld, "Upgrader: disable obsolete");
                cOld.enabled = false;
                if (!go.TryGetComponent<TNew>(out var _))
                    Undo.AddComponent<TNew>(go);
                return true;
            }
            return false;
        }

        // Copy all serializable values
        static void CopyValues<T>(T from, T to)
        {
            var json = JsonUtility.ToJson(from);
            JsonUtility.FromJsonOverwrite(json, to);
        }

        static CmCamera UpgradeVcamBaseToCmCamera(CinemachineVirtualCameraBase vcam)
        {
            var go = vcam.gameObject;
            if (!go.TryGetComponent(out CmCamera cmCamera)) // Check if RequireComponent already added CmCamera
            {
                // First disable the old vcamBase, or new one will be rejected
                Undo.RecordObject(vcam, "Upgrader: disable obsolete");
                vcam.enabled = false;

                cmCamera = Undo.AddComponent<CmCamera>(go);
                CopyValues(vcam, cmCamera);

                // Register the extensions with the cmCamera
                foreach (var extension in vcam.gameObject.GetComponents<CinemachineExtension>())
                    cmCamera.AddExtension(extension);
            }
            else if (vcam.enabled) // RequireComponent added CmCamera, it should be enabled iff vcam was enabled
            {
                Undo.RecordObject(vcam, "Upgrader: disable obsolete");
                vcam.enabled = false;
                Undo.RecordObject(cmCamera, "Upgrader: enable upgraded");
                cmCamera.enabled = true;
            }
            return cmCamera;
        }

        static GameObject UpgradeVcam(CinemachineVirtualCamera vcam)
        {
            var go = vcam.gameObject;
            var cmCamera = UpgradeVcamBaseToCmCamera(vcam);        

            cmCamera.Follow = vcam.m_Follow;
            cmCamera.LookAt = vcam.m_LookAt;
            cmCamera.Target.CustomLookAtTarget = vcam.m_Follow != vcam.m_LookAt;
            cmCamera.Lens = vcam.m_Lens;
            cmCamera.Transitions = vcam.Transitions;
                
            // Transfer the component pipeline
            var pipeline = vcam.GetComponentPipeline();
            if (pipeline != null) 
                foreach (var c in pipeline) 
                    if (c != null)
                        CopyValues(c, Undo.AddComponent(go, c.GetType()) as CinemachineComponentBase);

            // Destroy the hidden child object
            var owner = vcam.GetComponentOwner();
            if (owner != null && owner.gameObject != go)
                UnparentAndDestroy(owner);

            return null;
        }

        static void UnparentAndDestroy(Transform child)
        {
            if (child != null)
            {
                Undo.SetTransformParent(child, null, "Upgrader: destroy hidden child");
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        static void ConvertInputAxis(
            GameObject go, string name, 
            ref AxisState axis, ref AxisState.Recentering recentering)
        {
            if (!go.TryGetComponent<InputAxisController>(out var iac))
                iac = Undo.AddComponent<InputAxisController>(go);

            // Force creation of missing input controllers
            iac.SynchronizeControllers();

#if CINEMACHINE_UNITY_INPUTSYSTEM
            var provider = go.GetComponent<CinemachineInputProvider>();
            if (provider != null)
            {
                provider.enabled = false;
                iac.AutoEnableInputs = provider.AutoEnableInputs;
            }
#endif
            for (var i = 0; i < iac.Controllers.Count; ++i)
            {
                var c = iac.Controllers[i];
                if (c.Name == name)
                {
#if ENABLE_LEGACY_INPUT_MANAGER
                    c.LegacyInput = axis.m_InputAxisName;
                    c.LegacyGain = axis.m_MaxSpeed;
#endif
#if CINEMACHINE_UNITY_INPUTSYSTEM
                    if (provider != null)
                        c.InputAction = provider.XYAxis;
                    c.Gain = axis.m_MaxSpeed;
                    if (axis.m_SpeedMode == AxisState.SpeedMode.MaxSpeed)
                        c.Gain /= 100; // very approx
#endif
                    c.Control.AccelTime = axis.m_AccelTime;
                    c.Control.DecelTime = axis.m_DecelTime;

                    c.Recentering = new InputAxisRecenteringSettings
                    {
                        Enabled = recentering.m_enabled,
                        Wait = recentering.m_WaitTime,
                        Time = recentering.m_RecenteringTime
                    };
                    break;
                }
            }
        }

        static GameObject UpgradeFreelook(CinemachineFreeLook freelook)
        {
            GameObject notUpgradable = null;

            var topRig = freelook.GetRig(0);
            var middleRig = freelook.GetRig(1);
            var bottomRig = freelook.GetRig(2);
            if (topRig == null || middleRig == null || bottomRig == null)
                return null; // invalid Freelook, nothing to do

            var go = freelook.gameObject;
            if (!IsFreelookUpgradable(freelook))
            {
                notUpgradable = Object.Instantiate(go);
                notUpgradable.SetActive(false);
                notUpgradable.AddComponent<CinemachineDoNotUpgrade>();
                Undo.RegisterCreatedObjectUndo(notUpgradable, "Upgrader: clone of non upgradable");
            }

            var cmCamera = UpgradeVcamBaseToCmCamera(freelook);

            cmCamera.Follow = freelook.m_Follow;
            cmCamera.LookAt = freelook.m_LookAt;
            cmCamera.Target.CustomLookAtTarget = freelook.m_Follow != freelook.m_LookAt;
            cmCamera.Transitions = freelook.Transitions;
                    
            var freeLookModifier = Undo.AddComponent<CinemachineFreeLookModifier>(go);
            ConvertFreelookLens(freelook, cmCamera, freeLookModifier);
            ConvertFreelookBody(freelook, go, freeLookModifier);
            ConvertFreelookAim(freelook, go, freeLookModifier);
            ConvertFreelookNoise(freelook, go, freeLookModifier);

            ConvertInputAxis(go, "Look Orbit X", ref freelook.m_XAxis, ref freelook.m_RecenterToTargetHeading);
            ConvertInputAxis(go, "Look Orbit Y", ref freelook.m_YAxis, ref freelook.m_YAxisRecentering);

            // Destroy the hidden child objects
            UnparentAndDestroy(topRig.GetComponentOwner());
            UnparentAndDestroy(topRig.transform);
            UnparentAndDestroy(middleRig.GetComponentOwner());
            UnparentAndDestroy(middleRig.transform);
            UnparentAndDestroy(bottomRig.GetComponentOwner());
            UnparentAndDestroy(bottomRig.transform);
                    
            return notUpgradable;
        }

        /// <summary>
        /// Differences in these fields will be ignored because the FreeLookModifier
        /// will take care of them
        /// </summary>
        static string[] s_FreelookIgnoreFieldsList = { 
            "m_ScreenX", "m_ScreenY", "m_DeadZoneWidth", "m_DeadZoneHeight", 
            "m_SoftZoneWidth", "m_SoftZoneHeight", "m_BiasX", "m_BiasY", 
            "m_AmplitudeGain", "m_FrequencyGain", };

        static bool IsFreelookUpgradable(CinemachineFreeLook freelook)
        {
            // Freelook is not upgradable if it has:
            // - look at override (parent lookat != child lookat)
            // - different noise profiles on top, mid, bottom ||
            // - different aim component types ||
            // - same aim component types and with different parameters
            var topRig = freelook.GetRig(0);
            var middleRig = freelook.GetRig(1);
            var bottomRig = freelook.GetRig(2);
            var parentLookAt = freelook.LookAt;
            var topNoise = topRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var middleNoise = middleRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var bottomNoise = bottomRig.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var midAim = middleRig.GetCinemachineComponent(CinemachineCore.Stage.Aim);

            return
                parentLookAt == topRig.LookAt && parentLookAt == middleRig.LookAt && parentLookAt == bottomRig.LookAt 
                && IsCompatibleNoise(topNoise, middleNoise) 
                && IsCompatibleNoise(middleNoise, bottomNoise) 
                && PublicFieldsEqual(topRig.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim, s_FreelookIgnoreFieldsList)
                && PublicFieldsEqual(bottomRig.GetCinemachineComponent(CinemachineCore.Stage.Aim), midAim, s_FreelookIgnoreFieldsList);

            static bool IsCompatibleNoise(CinemachineBasicMultiChannelPerlin a, CinemachineBasicMultiChannelPerlin b)
            {
                // This check is conceived with the understanding that if a rig has empty
                // noise then any specific settings differences can be accounted for in
                // the FreeLookModifier by setting amplitude to 0
                return a == null || b == null ||
                    ((a.NoiseProfile == null || b.NoiseProfile == null || a.NoiseProfile == b.NoiseProfile)
                        && a.PivotOffset == b.PivotOffset);
            }

            static bool PublicFieldsEqual(CinemachineComponentBase a, CinemachineComponentBase b, params string[] ignoreList)
            {
                if (a == null && b == null)
                    return true;

                if (a == null || b == null)
                    return false;

                var aType = a.GetType();
                if (aType != b.GetType())
                    return false;

                var publicFields = aType.GetFields();
                foreach (var pi in publicFields)
                {
                    var name = pi.Name;
                    if (ignoreList.Contains(name))
                        continue; // ignore
                            
                    var field = aType.GetField(name);
                    if (!field.GetValue(a).Equals(field.GetValue(b))) 
                    {
#if DEBUG_HELPERS
                        Debug.Log("Rig values differ: " + name);
#endif
                        return false;
                    }
                }
                return true;
            }
        }

        static void ConvertFreelookLens(
            CinemachineFreeLook freelook, 
            CmCamera cmCamera, CinemachineFreeLookModifier freeLookModifier)
        {
            if (freelook.m_CommonLens)
                cmCamera.Lens = freelook.m_Lens;
            else
            {
                cmCamera.Lens = freelook.GetRig(1).m_Lens;
                if (!LensSettings.AreEqual(ref freelook.GetRig(1).m_Lens, ref freelook.GetRig(0).m_Lens)
                    || !LensSettings.AreEqual(ref freelook.GetRig(1).m_Lens, ref freelook.GetRig(2).m_Lens))
                {
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.LensModifier
                    {
                        Top = freelook.GetRig(0).m_Lens,
                        Bottom = freelook.GetRig(2).m_Lens
                    });
                }
            }
        }

        static void ConvertFreelookBody(
            CinemachineFreeLook freelook, 
            GameObject go, CinemachineFreeLookModifier freeLookModifier)
        {
            var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineOrbitalTransposer>();
            var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineOrbitalTransposer>();
            var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineOrbitalTransposer>();
            if (top == null || middle == null || bottom == null)
                return;

            // Use middle rig as template
            var orbital = Undo.AddComponent<CinemachineOrbitalFollow>(go);
            middle.UpgradeToCm3(orbital);

            orbital.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.ThreeRing;
            orbital.Orbits = new Cinemachine3OrbitRig.Settings
            {
                Top = new Cinemachine3OrbitRig.Orbit
                {
                    Height = freelook.m_Orbits[0].m_Height,
                    Radius = Mathf.Max(freelook.m_Orbits[0].m_Radius, 0.01f),
                },
                Center = new Cinemachine3OrbitRig.Orbit
                {
                    Height = freelook.m_Orbits[1].m_Height,
                    Radius = Mathf.Max(freelook.m_Orbits[1].m_Radius, 0.01f),
                },
                Bottom = new Cinemachine3OrbitRig.Orbit
                {
                    Height = freelook.m_Orbits[2].m_Height,
                    Radius = Mathf.Max(freelook.m_Orbits[2].m_Radius, 0.01f),
                },
                SplineCurvature = freelook.m_SplineCurvature,
            };

            // Preserve the 0...1 range for Y axis, in case some script is depending on it
            orbital.VerticalAxis.Range = new Vector2(0, 1);
            orbital.VerticalAxis.Center = 0.5f;
            orbital.VerticalAxis.Wrap = false;
            orbital.VerticalAxis.Value = freelook.m_YAxis.Value;

            // Do we need a modifier?
            var topDamping = new Vector3(top.m_XDamping, top.m_YDamping, top.m_ZDamping);
            var bottomDamping = new Vector3(bottom.m_XDamping, bottom.m_YDamping, bottom.m_ZDamping);
            if (!(orbital.TrackerSettings.PositionDamping - topDamping).AlmostZero()
                || !(orbital.TrackerSettings.PositionDamping - bottomDamping).AlmostZero())
            {
                freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.PositionDampingModifier
                {
                    Damping = new CinemachineFreeLookModifier.TopBottomRigs<Vector3> 
                    {
                        Top = topDamping,
                        Bottom = bottomDamping,
                    }
                });
            }
        }

        static void ConvertFreelookAim(
            CinemachineFreeLook freelook, 
            GameObject go, CinemachineFreeLookModifier freeLookModifier)
        {
            // We assume that the middle aim is a suitable template
            var template = freelook.GetRig(1).GetCinemachineComponent(CinemachineCore.Stage.Aim);
            if (template == null)
                return;
            var newAim = (CinemachineComponentBase)Undo.AddComponent(go, template.GetType());
            CopyValues(template, newAim);

            // Add modifier if it is a composer
            var middle = newAim as CinemachineComposer;
            var top = freelook.GetRig(0).GetComponentInChildren<CinemachineComposer>();
            var bottom = freelook.GetRig(2).GetComponentInChildren<CinemachineComposer>();
            if (middle != null && top != null && bottom != null)
            {
                var topComposition = top.Composition;
                var middleComposition = middle.Composition;
                var bottomComposition = bottom.Composition;

                if (!ScreenComposerSettings.Approximately(middleComposition, topComposition)
                    || !ScreenComposerSettings.Approximately(middleComposition, bottomComposition))
                {
                    freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.CompositionModifier
                    {
                        Composition = new CinemachineFreeLookModifier.TopBottomRigs<ScreenComposerSettings>
                        {
                            Top = topComposition,
                            Bottom = bottomComposition
                        }
                    });
                }
            }
        }

        static void ConvertFreelookNoise(
            CinemachineFreeLook freelook, 
            GameObject go, CinemachineFreeLookModifier freeLookModifier)
        {
            // Noise can be on any subset of the rigs
            var top = freelook.GetRig(0).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var middle = freelook.GetRig(1).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var bottom = freelook.GetRig(2).GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            var template = middle != null && middle.NoiseProfile != null 
                ? middle : (top != null && top.NoiseProfile != null? top : bottom);
            if (template == null || template.NoiseProfile == null)
                return;

            var middleNoise = Undo.AddComponent<CinemachineBasicMultiChannelPerlin>(go);
            CopyValues(template, middleNoise);

            var middleSettings = GetNoiseSettings(middle);
            middleNoise.AmplitudeGain = middleSettings.Amplitude;
            middleNoise.FrequencyGain = middleSettings.Frequency;

            var topSettings = GetNoiseSettings(top);
            var bottomSettings = GetNoiseSettings(bottom);
            if (!Mathf.Approximately(topSettings.Amplitude, middleSettings.Amplitude)
                || !Mathf.Approximately(topSettings.Frequency, middleSettings.Frequency)
                || !Mathf.Approximately(bottomSettings.Amplitude, middleSettings.Amplitude)
                || !Mathf.Approximately(bottomSettings.Frequency, middleSettings.Frequency))
            {
                freeLookModifier.Modifiers.Add(new CinemachineFreeLookModifier.NoiseModifier
                {
                    Noise = new CinemachineFreeLookModifier.TopBottomRigs<CinemachineFreeLookModifier.NoiseModifier.NoiseSettings>
                    {
                        Top = topSettings,
                        Bottom = bottomSettings
                    }
                });
            }

            CinemachineFreeLookModifier.NoiseModifier.NoiseSettings GetNoiseSettings(CinemachineBasicMultiChannelPerlin noise)
            {
                var settings = new CinemachineFreeLookModifier.NoiseModifier.NoiseSettings();
                if (noise != null)
                {
                    settings.Amplitude = noise.AmplitudeGain;
                    settings.Frequency = noise.FrequencyGain;
                }
                return settings;
            }
        }

        static void UpgradePath(CinemachinePathBase pathBase)
        {
            var go = pathBase.gameObject;
            if (go.TryGetComponent(out SplineContainer spline))
                return; // already converted

            spline = Undo.AddComponent<SplineContainer>(go);
            var splineRoll = Undo.AddComponent<CinemachineSplineRoll>(go);
            splineRoll.Roll = new SplineData<float>();

            Undo.RecordObject(pathBase, "Upgrader: disable obsolete");
            pathBase.enabled = false;

            switch (pathBase)
            {
                case CinemachinePath path:
                {
                    var waypoints = path.m_Waypoints;
                    spline.Spline = new Spline(waypoints.Length, path.Looped);
                    for (var i = 0; i < waypoints.Length; i++)
                    {
                        spline.Spline.Add(new BezierKnot
                        {
                            Position = waypoints[i].position,
                            Rotation = Quaternion.identity,
                            TangentIn = -waypoints[i].tangent,
                            TangentOut = waypoints[i].tangent,
                        });
                        splineRoll.Roll.Add(new DataPoint<float>(i, waypoints[i].roll));
                    }
                    break;
                }
                case CinemachineSmoothPath smoothPath:
                {
                    var waypoints = smoothPath.m_Waypoints;
                    spline.Spline = new Spline(waypoints.Length, smoothPath.Looped) { EditType = SplineType.CatmullRom };
                    for (var i = 0; i < waypoints.Length; i++)
                    {
                        spline.Spline.Add(new BezierKnot
                        {
                            Position = waypoints[i].position,
                            Rotation = Quaternion.identity,
                        });
                        splineRoll.Roll.Add(new DataPoint<float>(i, waypoints[i].roll));
                    }
                    break;
                }
                default:
                {
                    // GML todo: handle this message properly
                    pathBase.gameObject.AddComponent<CinemachineDoNotUpgrade>();
                    Debug.LogError($"{go.name}: Path type {pathBase.GetType().Name} is not handled by the upgrader");
                    break;
                }
            }
        }
    }
}
#pragma warning restore CS0618
