using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    public class DrawSkyBoxRenderPass : BaseRenderPass
    {
        private DrawSkyboxPass _drawSkyBoxPass;
        
        public override void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!CheckValide(ref renderingData))
                return;

            var cameraData = renderingData.cameraData;
            
            bool isOverlayCamera = cameraData.renderType == CameraRenderType.Overlay;

            if (isOverlayCamera)
                return;
            
            if (cameraData.camera.TryGetComponent<Skybox>(out var cameraSkybox))
                if (cameraSkybox == null || cameraSkybox.material == null)
                    return;

            if (cameraData.camera.clearFlags != CameraClearFlags.Skybox)
                return;        
            
            if (_drawSkyBoxPass == null)
                _drawSkyBoxPass = new DrawSkyboxPass(_RenderPassEvent);
            
            renderer.EnqueuePass(_drawSkyBoxPass);
        }

        public override void Refresh()
        {
            _drawSkyBoxPass = null;
        }
    }
}

