using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Kimi
{
    public class ForwardPlusRenderPass : BaseRenderPass
    {
        public ForwardPlusPass _ForwardPlusPass;
        public ForwardPlusGridPass _ForwardPlusGridPass;
        
        //复制图像用于debug
        private CopyColorPass m_copyColorPass = null;
        private RenderTargetHandle m_GridMainRT;
        
        public Shader samplingPS;

        public Shader blitPS;
        
        [Range(1, 256)]
        [SerializeField] public int _TileSize = 16;
        [Range(1, 256)]
        [SerializeField] public int _MaxLightCount = 64;

        [SerializeField] public bool _Debug = false;

        [DrawIf("_Debug", true)]
        [SerializeField]public Material _ShowGridMaterial;
        
        [DrawIf("_Debug", true)]
        [SerializeField]public RenderPassEvent _ShowGridRenderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        
        [DrawIf("_Debug", true)]
        [Range(0, 1)]
        [SerializeField]public float _ShowGridAlpha;

   

        
        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;

            if (_ForwardPlusPass == null)
                _ForwardPlusPass = new ForwardPlusPass(_TileSize, _MaxLightCount);
            renderer.EnqueuePass(_ForwardPlusPass);

            if (_Debug)
            {
                InitDebugCopyColorPass(ref renderer);
                Downsampling downsamplingMethod =
                    UniversalRenderPipeline.asset.opaqueDownsampling;
                m_copyColorPass.Setup(renderer.cameraColorTarget, m_GridMainRT, downsamplingMethod); 
                renderer.EnqueuePass(m_copyColorPass);
            } else if (m_copyColorPass != null)
                m_copyColorPass = null;
            

            if (_Debug && _ShowGridMaterial != null)
            {
                if (_ForwardPlusGridPass == null)
                {
                    _ForwardPlusGridPass = new ForwardPlusGridPass(_ShowGridRenderPassEvent, _ShowGridMaterial);
                }

                _ForwardPlusGridPass.Setup(renderer.cameraColorTarget);
                renderer.EnqueuePass(_ForwardPlusGridPass);
            }
        }

        public void InitDebugCopyColorPass(ref ScriptableRenderer renderer)
        {
            if (m_copyColorPass == null)
            {
                m_GridMainRT.Init("_GridMainRT");
                Material samplingMaterial = CoreUtils.CreateEngineMaterial(
                    samplingPS);
                Material blitPSMaterial = CoreUtils.CreateEngineMaterial(blitPS);

                m_copyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingOpaques, samplingMaterial, blitPSMaterial);
            }
        }

        public override void Refresh()
        {
            for (int i = 1; i <= 8; i++)
            {
                if (_TileSize == 1 << i)
                    break;
                if (_TileSize>>i == 0)
                {
                    _TileSize = 1 << i;
                    break;
                }
            }
            _ForwardPlusPass?.Dispose();
            _ForwardPlusPass = null;

            _ForwardPlusGridPass = null;
        }

        public void Dispose()
        {
            base.Dispose();
        }
    }

}