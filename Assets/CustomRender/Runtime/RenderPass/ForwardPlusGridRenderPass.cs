using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    public class ForwardPlusGridRenderPass : BaseRenderPass
    {

        public ForwardPlusGridPass _ForwardPlusGridPass;
        [SerializeField]public Material _ShowGridMaterial;
        
        
        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;

            if (_ShowGridMaterial == null)
                return;

            if (_ForwardPlusGridPass == null)
                _ForwardPlusGridPass = new ForwardPlusGridPass(_RenderPassEvent, _ShowGridMaterial);

        }

        public override void Refresh()
        {
            
        }
    }
}