using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cinemachine;
using Cinemachine.Editor;
using Cinemachine.Utility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class UpgradeCm2ToCm3Tests : CinemachineFixtureBase
    {
        static IEnumerable<Type> s_AllCinemachineComponents;
        // We ignore fields that don't have proper equality overloads
        static readonly string[] k_IgnoreFieldList = {
            "m_HorizontalRecentering", "m_VerticalRecentering",
            "m_RecenterToTargetHeading", "m_RecenterTarget", "m_HorizontalAxis", "m_VerticalAxis"
        };
        // We ignore components that are post-3.0
        static readonly Type[] k_IgnoreComponentsList = {
            typeof(CinemachineRotationComposer),
            typeof(CinemachinePositionComposer),
            typeof(CinemachineOrbitalFollow),
            typeof(CinemachinePanTilt),
            typeof(CinemachineSplineDolly),
        };
        CinemachineBrain m_Brain;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            var mainCamera = CreateGameObject("MainCamera", typeof(Camera), typeof(CinemachineBrain));
            m_Brain = mainCamera.GetComponent<CinemachineBrain>();
            m_Brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            CinemachineCore.UniformDeltaTimeOverride = 0.1f;
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }
        
        static IEnumerable ConvertTestCases
        {
            get
            {
                s_AllCinemachineComponents = ReflectionHelpers.GetTypesInAllDependentAssemblies((Type t) => 
                    typeof(CinemachineComponentBase).IsAssignableFrom(t) && !t.IsAbstract 
                    && t.GetCustomAttribute<CameraPipelineAttribute>() != null
                    && t.GetCustomAttribute<ObsoleteAttribute>() == null);
                foreach (var cmComponent in s_AllCinemachineComponents)
                {
                    if (k_IgnoreComponentsList.Contains(cmComponent))
                        continue;
                    yield return new TestCaseData(cmComponent).SetName(cmComponent.Name).Returns(null);
                }
            }
        }

#pragma warning disable CS0618
        [UnityTest, TestCaseSource(nameof(ConvertTestCases))]
        public IEnumerator ConvertAllDefaultOnes(Type type)
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));

            yield return null;
            
            var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
            vcam.InvalidateComponentPipeline();
            m_Brain.ManualUpdate(); // ensure pipeline is built

            yield return null;
            
            var componentAdded = AddCinemachineComponent(vcam, type);
            // Copy fields but ignore fields that don't have a proper Equals override.
            var componentValues = CopyPublicFields(componentAdded, k_IgnoreFieldList);

            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(vcamGo);

            yield return null;

            Assert.That(vcamGo.GetComponent<CinemachineVirtualCamera>(), Is.Null);
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null);
            var newComponent = (CinemachineComponentBase) vcamGo.GetComponent(type);
            Assert.That(newComponent, Is.Not.Null);
            Assert.That(PublicFieldsEqual(newComponent, componentValues), Is.True);
            Assert.That(vcamGo.GetComponents<MonoBehaviour>().Length, Is.EqualTo(2));
        }
        
        static CinemachineComponentBase AddCinemachineComponent(CinemachineVirtualCamera vcam, Type t)
        {
            var pipeline = vcam.GetComponentInChildren<CinemachinePipeline>();
            var component = (CinemachineComponentBase) pipeline.gameObject.AddComponent(t);
            vcam.InvalidateComponentPipeline();
            return component;
        }

        static List<Tuple<string,object>> CopyPublicFields(CinemachineComponentBase a, params string[] ignoreList)
        {
            if (a == null)
                return null;

            var values = new List<Tuple<string,object>>();
            var aType = a.GetType();
            var publicFields = aType.GetFields();
            foreach (var pi in publicFields)
            {
                var name = pi.Name;
                if (ignoreList.Contains(name))
                    continue;
                
                var field = aType.GetField(name);
                values.Add(new Tuple<string, object>(name, field.GetValue(a)));
            }

            return values;
        }
        
        static bool PublicFieldsEqual(CinemachineComponentBase a, List<Tuple<string, object>> values)
        {
            if (a == null && (values == null || values.Count == 0))
                return true;

            var aType = a.GetType();

            foreach (var value in values)
            {
                var field = aType.GetField(value.Item1);
                if (field == null)
                    return false;

                var fieldValue = field.GetValue(a);
                if (fieldValue == null && value.Item2 == null)
                    continue;

                if (fieldValue == null || !fieldValue.Equals(value.Item2))
                {
                    Debug.Log($"{a.GetType().Name}.{field.Name}: {fieldValue} != {value.Item2}");
                    return false;
                }
            }

            return true;
        }


        [UnityTest]
        public IEnumerator ConvertTrackedDolly()
        {
            var vcamGo = CreateGameObject("TestVcam", typeof(CinemachineVirtualCamera));

            yield return null;
            
            var vcam = vcamGo.GetComponent<CinemachineVirtualCamera>();
            vcam.AddCinemachineComponent<CinemachineTrackedDolly>();
            vcam.InvalidateComponentPipeline();

            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(vcamGo);

            yield return null;

            Assert.That(vcamGo.GetComponent<CinemachineVirtualCamera>(), Is.Null);
            Assert.That(vcamGo.GetComponent<CinemachineTrackedDolly>(), Is.Null);
            Assert.That(vcamGo.transform.childCount, Is.Zero);
            Assert.That(vcamGo.GetComponent<CmCamera>(), Is.Not.Null);
            Assert.That(vcamGo.GetComponent<CinemachineSplineDolly>(), Is.Not.Null);
            Assert.That(vcamGo.GetComponents<MonoBehaviour>().Length, Is.EqualTo(2));
        } 

        [UnityTest]
        public IEnumerator ConvertFreelook()
        {
            var freelookGo = CreateGameObject("TestVcam", typeof(CinemachineFreeLook));

            yield return null;
            
            CinemachineUpgradeManager.UpgradeSingleObject(freelookGo);
            yield return null;

            Assert.That(freelookGo.GetComponent<CinemachineFreeLook>(), Is.Null);
            Assert.That(freelookGo.transform.childCount, Is.Zero);
            Assert.That(freelookGo.GetComponent<CmCamera>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineOrbitalFollow>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineRotationComposer>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<CinemachineFreeLookModifier>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponent<InputAxisController>(), Is.Not.Null);
            Assert.That(freelookGo.GetComponents<MonoBehaviour>().Length, Is.EqualTo(5));
        }
#pragma warning restore CS0618
    }
}
