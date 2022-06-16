
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Kimi
{
    public class DepthNormalOnlyRenderPass : BaseRenderPass
    {

        public DepthNormalOnlyPass _DepthNormalOnlyPass;
        
        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;
            
            
        }

        public override void Refresh()
        {
            _DepthNormalOnlyPass = null;
        }
    }

}
