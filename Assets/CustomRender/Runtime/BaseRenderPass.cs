
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Kimi
{
    public enum CRenderType
    {
        Base,
        Overlay,
        All,
    }

    public enum CRenderQueue
    {
        All, 
        Opaque,
        Transparent,
    }
    public abstract class BaseRenderPass : ScriptableObject, IDisposable
    {
        [SerializeField, HideInInspector] private bool m_Active = true;
        /// <summary>
        /// Returns the state of the ScriptableRenderFeature (true: the feature is active, false: the feature is inactive). Use the method ScriptableRenderFeature.SetActive to change the value of this variable.
        /// </summary>
        public bool isActive => m_Active;
    
        [SerializeField] public RenderPassEvent _RenderPassEvent;
        [SerializeField] public CRenderQueue _RenderQueueRange;
        [SerializeField] public List<string> _CameraTags = new List<string>(){"MainCamera"};
        [SerializeField] public CRenderType _CameraRenderType;
        [SerializeField] public bool _EnableSceneCamera = true;
        [SerializeField] public bool _EnablePerviewCamera = true;
        
        
        
        /// <summary>
        /// 当前渲染的Pass
        /// </summary>
        public ScriptableRenderPass RenderPass { get; }

        public bool CheckValide(ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isPreviewCamera && _EnablePerviewCamera)
                return isActive;
            if (renderingData.cameraData.isSceneViewCamera && _EnableSceneCamera)
                return isActive;
            
            var camera = renderingData.cameraData.camera;
            var cameraRenderType = (CRenderType)renderingData.cameraData.renderType;
            if (_CameraRenderType != CRenderType.All && cameraRenderType != _CameraRenderType)
                return false;
 
            foreach (var cameraTag in _CameraTags)
            {
                if (camera.CompareTag(cameraTag))
                {
                    return isActive;
                }
            }
            // if ((camera.gameObject.layer & _CameraLayer) == 0)
            //     return false;

            return false;
        }

        public RenderQueueRange GetRenderQueueRange()
        {
            switch (_RenderQueueRange)
            {
                case CRenderQueue.All:
                    return RenderQueueRange.all;
                case CRenderQueue.Opaque:
                    return RenderQueueRange.opaque;
                case CRenderQueue.Transparent:
                    return RenderQueueRange.transparent;
            }
            
            return RenderQueueRange.all;
        }
        public abstract void AddRenderPass(ScriptableRenderContext context, ScriptableRenderer renderer, ref RenderingData renderingData);

        public abstract void Refresh();

        public void Dispose()
        {
            // Dispose(true);
            GC.SuppressFinalize(this);
        }
        
    }

}

