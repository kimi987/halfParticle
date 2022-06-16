using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Kimi
{
    public class DrawObjectsRenderPass : BaseRenderPass
    {
        public DrawObjectsPass _DrawObjectsPass;
        [SerializeField] public LayerMask _DrawLayerMask;
        [SerializeField] public List<string> _ShaderTagIds = new List<string>();


        public DrawObjectsRenderPass()
        {
            _DrawLayerMask.value = Int32.MaxValue;
        }

        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;

            if (_DrawObjectsPass == null)
            {
                ShaderTagId[] ids = new ShaderTagId[_ShaderTagIds.Count];
                for (int i = 0; i < _ShaderTagIds.Count; i++)
                {
                    ids[i] = new ShaderTagId(_ShaderTagIds[i]);
                }
                _DrawObjectsPass = new DrawObjectsPass(name, ids, true, _RenderPassEvent, GetRenderQueueRange(),
                    _DrawLayerMask, KForwardRenderRender.m_DefaultStencilState, KForwardRenderRender.stencilData.stencilReference);
            }
            
            renderer.EnqueuePass(_DrawObjectsPass);
        }

        public override void Refresh()
        {
            _DrawObjectsPass = null;
        }
    }

}