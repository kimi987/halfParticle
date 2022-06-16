using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    public class DepthOnlyRenderPass : BaseRenderPass
    {
        [SerializeField] public LayerMask _DrawLayerMask;

        [SerializeField] public string _DepthTargetName = "_CameraDepthTexture";

        
#if UNITY_EDITOR
        [SerializeField] public bool isDebug = false;
        public List<string> _showCameras = new List<string>();
#endif

        public DepthOnlyPass _DepthOnlyPass;
        
        RenderTargetHandle m_DepthTexture;
        


        public DepthOnlyRenderPass()
        {
            _RenderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;
            _RenderQueueRange = CRenderQueue.Opaque;
            
            m_DepthTexture.Init(_DepthTargetName);
            
        }
        
        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;

            if (!renderingData.cameraData.requiresDepthTexture)
                return;
            
#if UNITY_EDITOR
            if (!_showCameras.Contains(renderingData.cameraData.camera.name))
                _showCameras.Add(renderingData.cameraData.camera.name);
            
#endif
            
            if (_DepthOnlyPass == null)
            {
                _DepthOnlyPass = new DepthOnlyPass(_RenderPassEvent, GetRenderQueueRange(), _DrawLayerMask);
                _DepthOnlyPass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_DepthTexture);
#if UNITY_EDITOR
                _DepthOnlyPass.debug = isDebug;
#endif
            }
            renderer.EnqueuePass(_DepthOnlyPass);
        }

        public override void Refresh()
        {
            _DepthOnlyPass = null;
        }
    }
  
}
