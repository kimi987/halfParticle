using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace Kimi
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        private static readonly ShaderTagId k_ShaderTagId = new ShaderTagId("DepthOnly");

        private RenderTargetHandle depthAttachmentHandle { get; set; }
        internal RenderTextureDescriptor descriptor { get; set; }
        
        internal bool allocateDepth { get; set; } = true;

        private bool hasAllocateDepth = false;
        internal ShaderTagId shaderTagId { get; set; } = k_ShaderTagId;

        FilteringSettings m_FilteringSettings;
        
        // Constants
        private const int k_DepthBufferBits = 32;
        
        #if UNITY_EDITOR
        public bool debug = false;
        #endif

        public DepthOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DepthOnlyPass));
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
        }
        
        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle depthAttachmentHandle)
        {
            this.depthAttachmentHandle = depthAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = k_DepthBufferBits;

            // Depth-Only pass don't use MSAA
            baseDescriptor.msaaSamples = 1;
            descriptor = baseDescriptor;

            this.allocateDepth = true;
            this.shaderTagId = k_ShaderTagId;
        }
        
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            
            var desc = renderingData.cameraData.cameraTargetDescriptor;

            // When depth priming is in use the camera target should not be overridden so the Camera's MSAA depth attachment is used.
            if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
            {
                ConfigureTarget(renderingData.cameraData.renderer.cameraDepthTarget, descriptor.depthStencilFormat, desc.width, desc.height, 1, true);
            }
            // When not using depth priming the camera target should be set to our non MSAA depth target.
            else
            {
                if (this.allocateDepth)
                {
                    hasAllocateDepth = true;
                    cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
                }
                
                ConfigureTarget(new RenderTargetIdentifier(depthAttachmentHandle.Identifier(), 0, CubemapFace.Unknown, -1), descriptor.depthStencilFormat, desc.width, desc.height, 1, true);
            }

            // Only clear depth here so we don't clear any bound color target. It might be unused by this pass but that doesn't mean we can just clear it. (e.g. in case of overlay cameras + depth priming)
            ConfigureClear(ClearFlag.Depth, Color.black);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler("DepthOnlyPass")))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(this.shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
            }

            #if UNITY_EDITOR
            if (debug)
            {
                if (renderingData.cameraData.renderer.useDepthPriming && (renderingData.cameraData.renderType == CameraRenderType.Base || renderingData.cameraData.clearDepth))
                    Blit(cmd, renderingData.cameraData.renderer.cameraDepthTarget, renderingData.cameraData.renderer.cameraColorTarget);
                else 
                    Blit(cmd, depthAttachmentHandle.id, renderingData.cameraData.renderer.cameraColorTarget);
            }
            #endif
            
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                if (hasAllocateDepth)
                    cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
                hasAllocateDepth = false;
                depthAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }

}
