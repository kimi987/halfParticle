using Unity.Mathematics;
using UnityEngine;

namespace Kimi
{
    public class Utils
    {
        public static float4 NDCtoViewSpace(float4 p, Matrix4x4 InverseProjection)
        {
            float4 t = math.mul(InverseProjection, p);
            return t / t.w;
        }
        
        public static Plane ComputePlane(float3 p0, float3 p1, float3 p2)
        {
            Plane plane;

            float3 v0 = p1 - p0;
            float3 v2 = p2 - p0;

            plane.m_normal = math.normalize(math.cross(v0, v2));
            plane.m_distance = math.dot(p0, plane.m_normal);
            return plane;
        }
    }
}