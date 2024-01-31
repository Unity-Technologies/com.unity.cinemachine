using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu]
public class SpecificAssets : ScriptableObject
{
    public string path;
    public List<SpecialPrefab> specialSampleAssets;
}

[Serializable]
public class SpecialPrefab
{
    
    
    [SerializeField]
    internal GameObject newPrefab;
    [SerializeField]
    internal GameObject oldPrefab;
}

