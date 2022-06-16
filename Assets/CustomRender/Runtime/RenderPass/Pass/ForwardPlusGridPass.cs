using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    //用于绘制网格
    public class ForwardPlusGridPass : ScriptableRenderPass
    {
        private const string mc_profilerTag = "ForwardPlusGridPass";
        private ProfilingSampler m_profilingSampler = new ProfilingSampler(mc_profilerTag);
        private Material m_targetShowLightGridMaterial = null;
        private RenderTargetIdentifier m_colorRenderTargetIdentifier;
        

        public ForwardPlusGridPass(RenderPassEvent _renderPassEvent, Material gridMaterial)
        {
            renderPassEvent = _renderPassEvent;
            m_targetShowLightGridMaterial = gridMaterial;
        }

        public void Setup(RenderTargetIdentifier targetIdentifier)
        {
            m_colorRenderTargetIdentifier = targetIdentifier;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var command = CommandBufferPool.Get(mc_profilerTag);
            using (new ProfilingScope(command, m_profilingSampler))
            {
                command.Blit(m_colorRenderTargetIdentifier, m_colorRenderTargetIdentifier, m_targetShowLightGridMaterial, 0);
                context.ExecuteCommandBuffer(command);
            }
            
            CommandBufferPool.Release(command);
        }
    }
}