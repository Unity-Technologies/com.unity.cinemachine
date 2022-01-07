#if UNITY_EDITOR
using UnityEditor;
#endif

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Splines;
using UnityEngine;
using UnityEngine.Splines;
using Interpolators = UnityEngine.Splines.Interpolators;

namespace Cinemachine.Samples
{
    /// <summary>
    /// Taken from the splines example package
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer), typeof(MeshRenderer), typeof(MeshFilter))]
    public class RoadSpline : MonoBehaviour
    {
        [SerializeField]
        SplineContainer m_Spline;

        [SerializeField]
        int m_SegmentsPerMeter = 1;

        [SerializeField]
        Mesh m_Mesh;

        [SerializeField]
        float m_TextureScale = 1f;

        public float m_Width = 1f;

        public Spline spline
        {
            get
            {
                if (m_Spline == null)
                    m_Spline = GetComponent<SplineContainer>();
                if (m_Spline == null)
                {
                    Debug.LogError("Cannot loft road mesh because Spline reference is null");
                    return null;
                }
                return m_Spline.Spline;
            }
        }

        public Mesh mesh
        {
            get
            {
                if (m_Mesh != null)
                    return m_Mesh;

                m_Mesh = new Mesh();
                GetComponent<MeshRenderer>().sharedMaterial = Resources.Load<Material>("Track");
                return m_Mesh;
            }
        }

        public int segmentsPerMeter => Mathf.Min(10, Mathf.Max(1, m_SegmentsPerMeter));


        List<Vector3> m_Positions = new List<Vector3>();
        List<Vector3> m_Normals = new List<Vector3>();
        List<Vector2> m_Textures = new List<Vector2>();
        List<int> m_Indices = new List<int>();

        public void OnEnable()
        {
            //Avoid to point to an existing instance when duplicating the GameObject
            if(m_Mesh != null)
                m_Mesh = null;

            Loft();
#if UNITY_EDITOR            
            EditorSplineUtility.afterSplineWasModified += OnAfterSplineWasModified;
            Undo.undoRedoPerformed += Loft;
#endif
        }
        
        public void OnDisable()
        {
#if UNITY_EDITOR
            EditorSplineUtility.afterSplineWasModified -= OnAfterSplineWasModified;
            Undo.undoRedoPerformed -= Loft;
#endif
            
            if(m_Mesh != null)
#if  UNITY_EDITOR
                DestroyImmediate(m_Mesh);
#else
                Destroy(m_Mesh);
#endif
        }

        void OnAfterSplineWasModified(Spline s)
        {
            if(s == spline)
                Loft();
        }

        public void Loft()
        {
            if (spline == null || spline.Count < 2)
                return;
            
            mesh.Clear();

            float length = spline.GetLength();

            if (length < 1)
                return;

            int segments = (int)(segmentsPerMeter * length);
            int vertexCount = segments * 2, triangleCount = (spline.Closed ? segments : segments - 1) * 6;

            m_Positions.Clear();
            m_Normals.Clear();
            m_Textures.Clear();
            m_Indices.Clear();

            m_Positions.Capacity = vertexCount;
            m_Normals.Capacity = vertexCount;
            m_Textures.Capacity = vertexCount;
            m_Indices.Capacity = triangleCount;

            for (int i = 0; i < segments; i++)
            {
                var index = i / (segments - 1f);
                var control = SplineUtility.EvaluatePosition(spline, index);
                var dir = SplineUtility.EvaluateTangent(spline, index);
                var up = SplineUtility.EvaluateUpVector(spline, index);

                var scale = transform.lossyScale;
                //var tangent = math.normalize((float3)math.mul(math.cross(up, dir), new float3(1f / scale.x, 1f / scale.y, 1f / scale.z)));
                var tangent = math.normalize(math.cross(up, dir)) * new float3(1f / scale.x, 1f / scale.y, 1f / scale.z);

                var w = m_Width;

                m_Positions.Add(control - (tangent * w));
                m_Positions.Add(control + (tangent * w));
                m_Normals.Add(Vector3.up);
                m_Normals.Add(Vector3.up);
                m_Textures.Add(new Vector2(0f, index * m_TextureScale));
                m_Textures.Add(new Vector2(1f, index * m_TextureScale));
            }

            for (int i = 0, n = 0; i < triangleCount; i += 6, n += 2)
            {
                m_Indices.Add((n + 2) % vertexCount);
                m_Indices.Add((n + 1) % vertexCount);
                m_Indices.Add((n + 0) % vertexCount);
                m_Indices.Add((n + 2) % vertexCount);
                m_Indices.Add((n + 3) % vertexCount);
                m_Indices.Add((n + 1) % vertexCount);
            }

            mesh.SetVertices(m_Positions);
            mesh.SetNormals(m_Normals);
            mesh.SetUVs(0, m_Textures);
            mesh.subMeshCount = 1;
            mesh.SetIndices(m_Indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);

            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }
    }
}
