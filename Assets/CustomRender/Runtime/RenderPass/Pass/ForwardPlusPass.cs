using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    public class ForwardPlusPass : ScriptableRenderPass
    {
        private int m_maxLightCount = 256;
        private int m_maxLightPerTile = 3;
        private int m_tile_size = 16;
        private Matrix4x4 m_inverseMatrix;
        

        private int m_screen_x = 0;
        private int m_screen_y = 0;
        private float m_screen_size_radio = 0;
        
        private LightData[] m_lightDatas;

        private Frustum[] m_preBuildFrustums;

        private FrustumNode frustumNode;

        private LightIndex[] m_lightIndexes;
        
        private int m_visible_light_count = 0;
        

        //buff
        private ComputeBuffer m_lightListBuffer;
        private ComputeBuffer m_currentLightIndexBuffer;

        public ForwardPlusPass(int tile_size, int max_light_count)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            
            m_tile_size = tile_size;
            
            SetMaxLightCount(max_light_count);
            
            Shader.SetGlobalInt("_TileSize", m_tile_size);
        }

        public void SetMaxLightCount(int max_light_count)
        {
            m_maxLightCount = max_light_count;
            m_lightDatas = new LightData[m_maxLightCount];
        }

        public void SetupQuadTreeFrustums(ref RenderingData renderingData)
        {
            if (m_screen_x == Screen.width && m_screen_y == Screen.height)
                return;
            
            m_screen_x = Screen.width;
            m_screen_y = Screen.height;
            
            Matrix4x4 matrix = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix,
                false);

            m_inverseMatrix = matrix.inverse;
            
            var numFrustumsX = (int)math.ceil((float)m_screen_x / m_tile_size);
            var numFrustumsY = (int)math.ceil((float)m_screen_y / m_tile_size);
            
            var screenSizeRatio = new float2(1.0f / m_screen_x, 1.0f / m_screen_y);
            
            m_lightIndexes = new LightIndex[numFrustumsX * numFrustumsY];
            m_currentLightIndexBuffer?.Dispose();
            var stride = Marshal.SizeOf(typeof(LightIndex));
            m_currentLightIndexBuffer = new ComputeBuffer(numFrustumsX * numFrustumsY,
                stride, ComputeBufferType.Default);
            
            float2 indexs = new float2(0, 0);
            float2[] points = new float2[4];
            
            float2 index_p1 = new float2(numFrustumsX - 1, 0);
            float2 index_p2 = new float2(0, numFrustumsY - 1);
            float2 index_p3 = new float2(numFrustumsX - 1, numFrustumsY - 1);
            float3 origin = new float3(0, 0, 0);
            
            points[0] = indexs * m_tile_size * screenSizeRatio * 2 - 1;
            points[1] = (indexs + index_p1) * m_tile_size * screenSizeRatio * 2 - 1;
            points[2] = (indexs + index_p2) * m_tile_size * screenSizeRatio * 2 - 1;
            points[3]= (indexs + index_p3) * m_tile_size * screenSizeRatio * 2 - 1;

            float4[] viewSpacePoints = new float4[4];

            for (int k = 0; k < 4; k++)
            {
                float4 p = new float4(points[k].x, points[k].y, 0, 1);
                viewSpacePoints[k] = Utils.NDCtoViewSpace(p, m_inverseMatrix);
            }

            frustumNode = new FrustumNode(origin, viewSpacePoints[0].xyz, viewSpacePoints[1].xyz,
                viewSpacePoints[2].xyz, viewSpacePoints[3].xyz, m_tile_size, numFrustumsX);
            // Frustum frustum = new Frustum(4);
            // frustum.planes[0] = Utils.ComputePlane(origin, viewSpacePoints[0].xyz, viewSpacePoints[2].xyz);
            // frustum.planes[1] = Utils.ComputePlane(origin, viewSpacePoints[1].xyz, viewSpacePoints[3].xyz);
            // frustum.planes[2] = Utils.ComputePlane(origin, viewSpacePoints[2].xyz, viewSpacePoints[3].xyz);
            // frustum.planes[3] = Utils.ComputePlane(origin, viewSpacePoints[0].xyz, viewSpacePoints[1].xyz);
            // m_preBuildFrustums[i * numFrustumsX + j] = frustum;
        }

        public void SetupFrustums(ref RenderingData renderingData)
        {
            if (m_screen_x == Screen.width && m_screen_y == Screen.height)
                return;
            m_screen_x = Screen.width;
            m_screen_y = Screen.height;
            
            Matrix4x4 matrix = GL.GetGPUProjectionMatrix(renderingData.cameraData.camera.projectionMatrix,
                false);

            m_inverseMatrix = matrix.inverse;

            var numFrustumsX = (int)math.ceil((float)m_screen_x / m_tile_size);
            var numFrustumsY = (int)math.ceil((float)m_screen_y / m_tile_size);
            
            var screenSizeRatio = new float2(1.0f / m_screen_x, 1.0f / m_screen_y);
            
            m_preBuildFrustums = new Frustum[numFrustumsX * numFrustumsY];
            m_lightIndexes = new LightIndex[m_preBuildFrustums.Length];
            m_currentLightIndexBuffer?.Dispose();
            var stride = Marshal.SizeOf(typeof(LightIndex));
            m_currentLightIndexBuffer = new ComputeBuffer(m_preBuildFrustums.Length,
                stride, ComputeBufferType.Default);
            
            float2 index_p1 = new float2(1, 0);
            float2 index_p2 = new float2(0, 1);
            float2 index_p3 = new float2(1, 1);
            float3 origin = new float3(0, 0, 0);
            
            for (int i = 0; i < numFrustumsY; i++)
            {
                for (int j = 0; j < numFrustumsX; j++)
                {
                    float2[] points = new float2[4];
                    float2 indexs = new float2(j, i);
                    points[0] = indexs * m_tile_size * screenSizeRatio * 2 - 1;
                    points[1] = (indexs + index_p1) * m_tile_size * screenSizeRatio * 2 - 1;
                    points[2] = (indexs + index_p2) * m_tile_size * screenSizeRatio * 2 - 1;
                    points[3]= (indexs + index_p3) * m_tile_size * screenSizeRatio * 2 - 1;

                    

                    float4[] viewSpacePoints = new float4[4];

                    for (int k = 0; k < 4; k++)
                    {
                        float4 p = new float4(points[k].x, points[k].y, 0, 1);
                        viewSpacePoints[k] = Utils.NDCtoViewSpace(p, m_inverseMatrix);
                    }
                    
                    
                    Frustum frustum = new Frustum(4);
                    frustum.planes[0] = Utils.ComputePlane(origin, viewSpacePoints[0].xyz, viewSpacePoints[2].xyz);
                    frustum.planes[1] = Utils.ComputePlane(origin, viewSpacePoints[1].xyz, viewSpacePoints[3].xyz);
                    frustum.planes[2] = Utils.ComputePlane(origin, viewSpacePoints[2].xyz, viewSpacePoints[3].xyz);
                    frustum.planes[3] = Utils.ComputePlane(origin, viewSpacePoints[0].xyz, viewSpacePoints[1].xyz);
                    m_preBuildFrustums[i * numFrustumsX + j] = frustum;
                }
            }
        }
        
        public void SetupLightDatas(ref UnityEngine.Rendering.Universal.LightData
            lightData)
        {
            var visibleLights = lightData.visibleLights;
            m_visible_light_count = 0;
            for (var i = 0; i < visibleLights.Length; i++)
            {
                var visibleLight = visibleLights[i];
                var pos = visibleLight.localToWorldMatrix.GetColumn(3);
                if (lightData.mainLightIndex != i)
                {
                    var col = visibleLight.light.color;
                    m_lightDatas[m_visible_light_count] = new LightData(
                        new float3(pos.x, pos.y, pos.z), 1, new float3(col.r, col.g, col.b), visibleLight.light.range, visibleLight.light.intensity);
                    
                    if ((m_visible_light_count + 1)>= m_maxLightCount)
                        break;
                    
                    m_visible_light_count++;
                }
                else
                {
                    Shader.SetGlobalVector("_MainLightColor", visibleLight.finalColor);
                    Shader.SetGlobalVector("_MainLightPosition", pos.normalized);
                }
            }
            
            if (m_lightListBuffer != null && m_lightListBuffer.count != m_lightDatas.Length)
            {
                m_lightListBuffer?.Dispose();
                var stride = Marshal.SizeOf(typeof(LightData));
                m_lightListBuffer = new ComputeBuffer( m_lightDatas.Length,
                    stride, ComputeBufferType.Default);
            } else if (m_lightListBuffer == null)
            {
                var stride = Marshal.SizeOf(typeof(LightData));
                m_lightListBuffer = new ComputeBuffer( m_lightDatas.Length,
                    stride, ComputeBufferType.Default);
            }
            
            m_lightListBuffer.SetData(m_lightDatas);
            Shader.SetGlobalBuffer("_Lights", m_lightListBuffer);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // SetupFrustums(ref renderingData);
            //四叉树结构的光照分配
            SetupQuadTreeFrustums(ref renderingData);
            SetupLightDatas(ref renderingData.lightData);
            
            var near = renderingData.cameraData.camera.nearClipPlane;
            var far = renderingData.cameraData.camera.farClipPlane;
            var worldToViewM = renderingData.cameraData.camera.worldToCameraMatrix;
            
            //四叉树结构的光照分配
            //for (int i = 0; i < m_visible_light_count; i++)
            //{
               // var lightData = m_lightDatas[i];
               // DoFrustumNodesLight(frustumNode, far, near, worldToViewM, i, ref lightData);
            //}
            
            m_currentLightIndexBuffer.SetData(m_lightIndexes);
            Shader.SetGlobalBuffer("_LightIndexes", m_currentLightIndexBuffer);

            //四叉树结构的光照分配
            m_lightIndexes = new LightIndex[m_lightIndexes.Length];
        }

        public void DoFrustumsLight(float far, float near, Matrix4x4 worldToViewM)
        {
            for (int i = 0; i < m_preBuildFrustums.Length; i++)
            {
                var frustum = m_preBuildFrustums[i];
                List<LightWeight> lightWeightList = new List<LightWeight>();
                var lightIndex = new LightIndex();

                // if (i <= 0)
                // {
                for (int j = 0; j < m_visible_light_count; j++)
                {
                    var lightData = m_lightDatas[j];
                    
                    var weight = CheckFrustumInLightRange(ref frustum, ref lightData, near, far, worldToViewM);
                    if (weight > 0)
                    {
                        //当前的frustum包含该光源信息 权重表示当前光源的重要程度
                        var lightWeight = new LightWeight(j, lightData.m_color * lightData.m_lightAttenuation, weight);
                        lightWeightList.Add(lightWeight);
                    }
                }
                // }
                
                if (lightWeightList.Count > m_maxLightPerTile)
                    lightWeightList.Sort();
                var n = math.min(lightWeightList.Count, m_maxLightPerTile);
                for (int j = 0; j < n; j++)
                {
                    lightIndex.AddIndex(lightWeightList[j].m_index);
                }

                m_lightIndexes[i] = lightIndex;
            }
        }

        public void DoFrustumNodesLight(FrustumNode frustumNode, float far, float near, Matrix4x4 worldToViewM, int lightIndex, ref LightData lightData)
        {
            if (frustumNode == null)
                return;
            var weight = CheckFrustumInLightRange(ref frustumNode.frustum, ref lightData, near, far, worldToViewM);

            if (weight > 0)
            {
                if (frustumNode.index > -1)
                {
                    m_lightIndexes[frustumNode.index].AddIndex(lightIndex);
                }
                else
                {
                    //LT
                    DoFrustumNodesLight(frustumNode.node_LT, far, near, worldToViewM, lightIndex, ref lightData);
                    //LB
                    DoFrustumNodesLight(frustumNode.node_LB, far, near, worldToViewM, lightIndex, ref lightData);
                    //RT
                    DoFrustumNodesLight(frustumNode.node_RT, far, near, worldToViewM, lightIndex, ref lightData);
                    //RB
                    DoFrustumNodesLight(frustumNode.node_RB, far, near, worldToViewM, lightIndex, ref lightData);
                }
            }
        }
        
        public int CheckFrustumInLightRange(ref Frustum frustum, ref LightData lightData, float near, float far, Matrix4x4 worldToViewM)
        {
            float3 viewPos = math.mul(worldToViewM,
                new float4(lightData.m_worldPos.x, lightData.m_worldPos.y, lightData.m_worldPos.z, 1)).xyz;
            
            if (lightData.m_range - viewPos.z < near || -lightData.m_range - viewPos.z > far)
                return 0;
            var range = lightData.m_range * -near / viewPos.z;
            
            viewPos = viewPos * -near / viewPos.z;
            
            
            int index = 0;
            int rangePercent = 0;
            
            var distance_0 = math.abs(math.dot(viewPos, frustum.planes[0].m_normal));
            var distance_1 = math.abs(math.dot(viewPos, frustum.planes[1].m_normal));
            var distance_2 = math.abs(math.dot(viewPos, frustum.planes[2].m_normal));
            var distance_3 = math.abs(math.dot(viewPos, frustum.planes[3].m_normal));
            var distance_LR = math.min(distance_0, distance_1);
            var distance_TB = math.min(distance_2, distance_3);
            
            if (distance_LR > range  ||  distance_TB > range)
                return 0;


            return (int) ((lightData.m_range - math.min(distance_LR, distance_TB)) * 100 / lightData.m_range);
            
            // foreach (var plane in frustum.planes)
            // {
            //
            //     float distance = math.abs(math.dot(viewPos, plane.m_normal) - plane.m_distance);
            //     Debug.LogError("index = " + index);
            //     Debug.LogError("plane.m_normal = " + plane.m_normal);
            //     // Debug.LogError("plane.m_distance = " + plane.m_distance);
            //     // Debug.LogError("distance = " + distance);
            //     index++;
            //     if (distance > lightData.m_range)
            //     {
            //         return 0;
            //     }
            //
            //     var percent = (int) ((distance + lightData.m_range) * 100 / lightData.m_range); 
            //     
            //     if (percent > rangePercent)
            //         rangePercent = percent;
            // }
            //
            // return rangePercent;
        }
        
        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // if (m_lightDatas.Length > 0)
            //     m_lightDatas.Dispose();
            // if (m_lightIndexes.Length > 0)
            //     m_lightIndexes.Dispose();
            //
            // if(m_lightListBuffer != null)
            //     m_lightListBuffer.Dispose();
            // if (m_currentLightIndexBuffer != null)
            //     m_currentLightIndexBuffer.Dispose();    
        }

        public void Dispose()
        {
            
            if(m_lightListBuffer != null)
                m_lightListBuffer.Dispose();
            if (m_currentLightIndexBuffer != null)
                m_currentLightIndexBuffer.Dispose();    
        }
    }
}