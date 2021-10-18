using UnityEngine;

public class CameraGrabTextureRender : MonoBehaviour
{
    private static int CameraColorTextureId = Shader.PropertyToID("_CameraOpaqueTexture");
    private Camera mCamera = null;
    private RenderTexture mColorRT;
    
    private RenderTextureFormat mColorTextureFormat;    
    void Start()
    {
        if (!CheckRTFormartSupport())
        {
            return;
        }

        mCamera = GetComponent<Camera>();
        if (mCamera != null) 
        {
            RecreateRtTex();
        }
        else
        {
            enabled = false;
        }
    }

    bool CheckRTFormartSupport()
    {
        if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float))
        {
            mColorTextureFormat = RenderTextureFormat.RGB111110Float;
        }
        else if (SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB565))
        {
            mColorTextureFormat = RenderTextureFormat.RGB565;
        }
        else
        {
            Destroy(this);
            return false;
        }
        return true;
    }

    private void RecreateRtTex()
    {
        int Width = mCamera.pixelWidth;
        int Height = mCamera.pixelHeight;

        if (mColorRT != null)
        {
            Object.Destroy(mColorRT);
        }

        mColorRT = new RenderTexture(Width, Height, 16, mColorTextureFormat);
        mColorRT.name = "CustomOpaqueBuffer";
        mColorRT.enableRandomWrite = false;
        mColorRT.autoGenerateMips = false;
        mColorRT.useMipMap = false;
        mColorRT.antiAliasing = 1;
        
        mCamera.targetTexture = mColorRT;
    }

    private void LateUpdate()
    {
        Shader.SetGlobalTexture(CameraColorTextureId,mColorRT);
    }
}