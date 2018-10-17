using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    public partial class RTHandleSystem
    {
        public class RTHandle
        {
            internal RTHandleSystem             m_Owner;
            internal RenderTexture              m_RT;
            internal RenderTargetIdentifier     m_NameID;
            internal bool                       m_EnableMSAA = false;
            internal bool                       m_EnableRandomWrite = false;
            internal string                     m_Name;

            internal Vector2 scaleFactor        = Vector2.one;
            internal ScaleFunc scaleFunc;

            public bool                         useScaling { get; internal set; }
            public Vector2Int                   referenceSize {get; internal set; }

            public RenderTexture rt
            {
                get
                {
                    return m_RT;
                }
            }

            public RenderTargetIdentifier nameID
            {
                get
                {
                    return m_NameID;
                }
            }

            // Keep constructor private
            internal RTHandle(RTHandleSystem owner)
            {
                m_Owner = owner;
            }

            public static implicit operator RenderTexture(RTHandle handle)
            {
                return handle.rt;
            }

            public static implicit operator RenderTargetIdentifier(RTHandle handle)
            {
                return handle.nameID;
            }

            internal void SetRenderTexture(RenderTexture rt, RTCategory category)
            {
                m_RT=  rt;
                m_NameID = new RenderTargetIdentifier(rt);
            }

            public void Release()
            {
                m_Owner.m_AutoSizedRTs.Remove(this);
                CoreUtils.Destroy(m_RT);
                m_NameID = BuiltinRenderTextureType.None;
                m_RT = null;
            }

            public Vector2Int GetScaledSize(Vector2Int refSize)
            {
                if (scaleFunc != null)
                {
                    return scaleFunc(refSize);
                }
                else
                {
                    return new Vector2Int(
                        x: Mathf.RoundToInt(scaleFactor.x * refSize.x),
                        y: Mathf.RoundToInt(scaleFactor.y * refSize.y)
                        );
                }
            }
        }
    }
}
