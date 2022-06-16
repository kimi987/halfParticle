

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    
    public class KForwardRenderRender : ScriptableRenderer
    {
    
        private static class Profiling
        {
            private const string k_Name = nameof(UniversalRenderer);
        }
    
        public static StencilState m_DefaultStencilState;
        public static StencilStateData stencilData;

        /// <summary>
        /// 编辑器数据
        /// </summary>
        private KForwardRenderRenderData _rendererData;
        

        public KForwardRenderRender(ScriptableRendererData data) : base(data)
        {
            _rendererData = data as KForwardRenderRenderData;
        
            stencilData = _rendererData.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
            
            
            foreach (var pass in _rendererData.rendererPasses)
            {
                if (pass)
                    pass.AddRenderPass(context, this, ref renderingData);
            
            }
        }
    }

}
