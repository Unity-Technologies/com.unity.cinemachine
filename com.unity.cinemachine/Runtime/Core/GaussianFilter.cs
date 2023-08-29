using System;
using UnityEngine;

namespace Unity.Cinemachine
{
    internal abstract class GaussianWindow1d<T>
    {
        protected T[] m_Data;
        protected float[] m_Kernel;
        protected int m_CurrentPos = -1;

        public float Sigma { get; private set; }   // Filter strength: bigger numbers are stronger.  0.5 is minimal.
        public int KernelSize { get { return m_Kernel.Length; } }

        void GenerateKernel(float sigma, int maxKernelRadius)
        {
            // Weight is close to 0 at a distance of sigma*3, so let's just cut it off a little early
            int kernelRadius = Math.Min(maxKernelRadius, Mathf.FloorToInt(Mathf.Abs(sigma) * 2.5f));
            m_Kernel = new float[2 * kernelRadius + 1];
            if (kernelRadius == 0)
                m_Kernel[0] = 1;
            else
            {
                float sum = 0;
                for (int i = -kernelRadius; i <= kernelRadius; ++i)
                {
                    m_Kernel[i + kernelRadius]
                        = (float)(Math.Exp(-(i * i) / (2 * sigma * sigma)) / (2.0 * Math.PI * sigma * sigma));
                    sum += m_Kernel[i + kernelRadius];
                }
                for (int i = -kernelRadius; i <= kernelRadius; ++i)
                    m_Kernel[i + kernelRadius] /= sum;
            }
            Sigma = sigma;
        }

        protected abstract T Compute(int windowPos);

        public GaussianWindow1d(float sigma, int maxKernelRadius = 10)
        {
            GenerateKernel(sigma, maxKernelRadius);
            m_Data = new T[KernelSize];
            m_CurrentPos = -1;
        }

        public void Reset() { m_CurrentPos = -1; }

        public bool IsEmpty() { return m_CurrentPos < 0; }

        public void AddValue(T v)
        {
            if (m_CurrentPos < 0)
            {
                for (int i = 0; i < KernelSize; ++i)
                    m_Data[i] = v;
                m_CurrentPos = Mathf.Min(1, KernelSize-1);
            }
            m_Data[m_CurrentPos] = v;
            if (++m_CurrentPos == KernelSize)
                m_CurrentPos = 0;
        }

        public T Filter(T v)
        {
            if (KernelSize < 3)
                return v;
            AddValue(v);
            return Value();
        }

        /// Returned value will be kernelRadius old
        public T Value() { return Compute(m_CurrentPos); }

        // Direct buffer access
        public int BufferLength { get { return m_Data.Length; } }
        public void SetBufferValue(int index, T value) { m_Data[index] = value; }
        public T GetBufferValue(int index) { return m_Data[index]; }
    }

    internal class GaussianWindow1D_Vector3 : GaussianWindow1d<Vector3>
    {
        public GaussianWindow1D_Vector3(float sigma, int maxKernelRadius = 10)
            : base(sigma, maxKernelRadius) {}

        protected override Vector3 Compute(int windowPos)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < KernelSize; ++i)
            {
                sum += m_Data[windowPos] * m_Kernel[i];
                if (++windowPos == KernelSize)
                    windowPos = 0;
            }
            return sum;
        }
    }

    internal class GaussianWindow1D_Quaternion : GaussianWindow1d<Quaternion>
    {
        public GaussianWindow1D_Quaternion(float sigma, int maxKernelRadius = 10)
            : base(sigma, maxKernelRadius) {}
        protected override Quaternion Compute(int windowPos)
        {
            Quaternion sum = new Quaternion(0, 0, 0, 0);
            Quaternion q = m_Data[m_CurrentPos];
            Quaternion qInverse = Quaternion.Inverse(q);
            for (int i = 0; i < KernelSize; ++i)
            {
                // Make sure the quaternion is in the same hemisphere, or averaging won't work
                float scale = m_Kernel[i];
                Quaternion q2 = qInverse * m_Data[windowPos];
                if (Quaternion.Dot(Quaternion.identity, q2) < 0)
                    scale = -scale;
                sum.x += q2.x * scale;
                sum.y += q2.y * scale;
                sum.z += q2.z * scale;
                sum.w += q2.w * scale;

                if (++windowPos == KernelSize)
                    windowPos = 0;
            }
            return q * Quaternion.Normalize(sum);
        }
    }

    internal class GaussianWindow1D_CameraRotation : GaussianWindow1d<Vector2>
    {
        public GaussianWindow1D_CameraRotation(float sigma, int maxKernelRadius = 10)
            : base(sigma, maxKernelRadius) {}

        protected override Vector2 Compute(int windowPos)
        {
            Vector2 sum = Vector2.zero;
            Vector2 v = m_Data[m_CurrentPos];
            for (int i = 0; i < KernelSize; ++i)
            {
                Vector2 v2 = m_Data[windowPos] - v;
                if (v2.y > 180f)
                    v2.y -= 360f;
                if (v2.y < -180f)
                    v2.y += 360f;
                sum += v2 * m_Kernel[i];
                if (++windowPos == KernelSize)
                    windowPos = 0;
            }
            return v + sum;
        }
    }
}
