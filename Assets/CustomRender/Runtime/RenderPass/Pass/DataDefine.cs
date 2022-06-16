using System;
using Unity.Mathematics;
using UnityEngine;

namespace Kimi
{
    public struct LightData
    {
        public float3 m_worldPos;
        public float m_enabled;
        public float3 m_color;
        public float m_range;
        public float m_lightAttenuation;

        public LightData(float3 worldPos, float enabled, float3 color, float range, float intensity)
        {
            if (range <= 0 || intensity <= 0)
            {
                m_worldPos = worldPos;
                m_enabled = enabled;
                m_color = color;
                m_range = range;
                m_lightAttenuation = 0;
                m_enabled = 0;
                return;
            }

            m_worldPos = worldPos;
            m_enabled = enabled;
            m_color = color;
            m_range = range;
            m_lightAttenuation = intensity / (range * range);
        }
    }

    //截平面定义
    public struct Plane
    {
        public Vector3 m_normal;
        public float m_distance;

        public Plane(ref Vector3 normal, float distance)
        {
            m_normal = normal;
            m_distance = distance;
        }
    }

    public struct Frustum
    {
        public Plane[] planes;

        public Frustum(int count)
        {
            planes = new Plane[count];
        }
    };

    public class FrustumNode
    {
        public int index; //叶子节点的index
        public FrustumNode node_LT;
        public FrustumNode node_RT;
        public FrustumNode node_LB;
        public FrustumNode node_RB;

        public Frustum frustum;

        public FrustumNode(float3 origin, float3 p0, float3 p1, float3 p2, float3 p3, float tileSize, int numFrustumsX)
        {
            index = -1;
            frustum = new Frustum(4)
            {
                planes =
                {
                    [0] = Utils.ComputePlane(origin, p0, p2),
                    [1] = Utils.ComputePlane(origin, p1, p3),
                    [2] = Utils.ComputePlane(origin, p2, p3),
                    [3] = Utils.ComputePlane(origin, p0, p1)
                }
            };

            var l = p3 - p0;

            //判断如果是叶子节点 计算index, 跳出
            if (l.x <= tileSize || l.x <= 2)
            {
                index = (int) (math.ceil(p0.x / tileSize) + math.ceil(p0.y / tileSize) * numFrustumsX);
                return;
            }

            var p_lm = p0 + new float3(0, l.y * 0.5f, 0);
            var p_bm = p0 + new float3(l.x * 0.5f, 0, 0);
            var p_m = p0 + new float3(l.x * 0.5f, l.y * 0.5f, 0);
            var p_tm = p0 + new float3(l.x * 0.5f, l.y, 0);
            var p_rm = p0 + new float3(l.x, l.y * 0.5f, 0);

            node_LT = new FrustumNode(origin, p_lm,
                p_m, p2,
                p_tm, tileSize, numFrustumsX);

            node_RT = new FrustumNode(origin, p_m,
                p_rm, p_tm,
                p3, tileSize, numFrustumsX);

            node_LB = new FrustumNode(origin, p0,
                p_bm, p_lm,
                p_m, tileSize, numFrustumsX);

            node_RB = new FrustumNode(origin, p_bm,
                p1, p_m,
                p_rm, tileSize, numFrustumsX);
        }
    }


    public struct LightIndex
    {
        public int buff; //ffffffff  

        public void Reset()
        {
            buff = 0;
        }

        public void AddIndex(int index)
        {
            var offset = buff & 0x0000000f;

            if (offset >= 7)
            {
                Debug.LogError("当前格子已到达灯光上限");
                return;
            }

            offset += 1;
            var realOffset = offset * 4;
            buff += (((buff >> offset * realOffset) + index) << realOffset);
            buff = (buff >> 4 << 4) + offset;
        }
    }

    public struct LightWeight : IComparable<LightWeight>
    {
        public int m_index;
        public int m_weight;

        public LightWeight(int index, float3 color, int weight)
        {
            m_index = index;
            m_weight = (int) (math.cmax(color) * weight);
        }

        public int CompareTo(LightWeight other)
        {
            return other.m_weight - m_weight;
        }
    };
}