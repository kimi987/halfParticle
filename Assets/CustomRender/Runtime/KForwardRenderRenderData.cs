using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    [CreateAssetMenu(menuName = "Kimi/KForwardRenderRender")]
    public class KForwardRenderRenderData : ScriptableRendererData
    {
        
        [SerializeField] StencilStateData m_DefaultStencilState = new StencilStateData() { passOperation = StencilOp.Replace }; 
        
        [SerializeField] internal List<BaseRenderPass> m_BaseRenderPasses = new List<BaseRenderPass>();
    
        [SerializeField] internal List<long> m_BaseRenderPassMap = new List<long>();
    
        /// <summary>
        /// List of additional render pass features for this renderer.
        /// </summary>
        public List<BaseRenderPass> rendererPasses
        {
            get => m_BaseRenderPasses;
        }

        public List<long> rendererPassMap
        {
            get => m_BaseRenderPassMap;
        }
    
        protected override ScriptableRenderer Create()
        {
            return new KForwardRenderRender(this);
        }
        
        
        public StencilStateData defaultStencilState
        {
            get => m_DefaultStencilState;
            set
            {
                SetDirty();
                m_DefaultStencilState = value;
            }
        }

        public void RefreshRenderPasses()
        {
            if (m_BaseRenderPasses != null)
                foreach (var pass in m_BaseRenderPasses)
                    pass.Refresh();
        }
    
        
        #if UNITY_EDITOR
        public bool ValidateRendererPasses()
        {

            return false;
        }

        public void RemoveRendererPass(int index)
        {
            m_BaseRenderPasses.RemoveAt(index);
            m_BaseRenderPassMap.RemoveAt(index);
        }
        #endif
    }
}

