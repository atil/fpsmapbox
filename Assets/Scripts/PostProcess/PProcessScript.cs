using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[ExecuteInEditMode]
public class PProcessScript : MonoBehaviour
{
    private Camera _camera;
    private Material _edgeDetectMaterial;
    private Material _gaussianBlurMaterial;
    private Material _blendMaterial;
    private RenderTexture _sourceTexture;

    public bool UseRenderImage;
    public float off;

    #region EdgeDetection
    public float angleThreshold = 80, depthWeight = 300;
    [SerializeField] private Shader _edgeDetectShader;
    [SerializeField] private Shader _blendShader;
    [SerializeField] private Color _edgeColor;
    #endregion

    #region GaussianBlur
    [SerializeField] private Shader _blurShader;
    [SerializeField, Range(0, 8)] private int _iteration = 4;
    [SerializeField, Range(0.0f,1.0f)] private float blurSpread = 0.6f;
    #endregion

    // Creates a private material used to the effect
    void Awake()
    {
        _camera = GetComponent<Camera>();
        _edgeDetectMaterial = new Material(_edgeDetectShader);
        _gaussianBlurMaterial = new Material(_blurShader);
        _blendMaterial = new Material(_blendShader);
        _camera.depthTextureMode = DepthTextureMode.DepthNormals;
       
    }

    // Postprocess the image
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (UseRenderImage)
        {
            RenderTexture rt1 = RenderTexture.GetTemporary(source.width, source.height);

            EdgeDetectionPass(source, destination);
            //BlurPass(rt1, destination);

            Graphics.Blit(source, destination, _blendMaterial);
            Graphics.Blit(rt1, destination, _blendMaterial);

            RenderTexture.ReleaseTemporary(rt1);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    void EdgeDetectionPass(RenderTexture source, RenderTexture destination)
    {
        _edgeDetectMaterial.SetColor("_EdgeColor", _edgeColor);
        _edgeDetectMaterial.SetFloat("_angleThreshold", angleThreshold);
        _edgeDetectMaterial.SetFloat("_depthWeight", depthWeight);
        Graphics.BlitMultiTap(source, destination, _edgeDetectMaterial,
                                                    new Vector2(-off, -off),
                                                    new Vector2(-off, off),
                                                    new Vector2(off, off),
                                                    new Vector2(off, -off));
    }

    // Performs one blur iteration.
    public void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
    {
        float off = 0.5f + iteration * blurSpread;
        Graphics.BlitMultiTap(source, dest, _gaussianBlurMaterial,
                               new Vector2(-off, -off),
                               new Vector2(-off, off),
                               new Vector2(off, off),
                               new Vector2(off, -off)
            );
    }

    // Downsamples the texture to a quarter resolution.
    private void DownSample4x(RenderTexture source, RenderTexture dest)
    {
        float off = 1.0f;
        Graphics.BlitMultiTap(source, dest, _gaussianBlurMaterial,
                               new Vector2(-off, -off),
                               new Vector2(-off, off),
                               new Vector2(off, off),
                               new Vector2(off, -off)
            );
    }
    void BlurPass(RenderTexture source, RenderTexture destination)
    {
            int rtW = source.width/4;
            int rtH = source.height/4;
            RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);

            // Copy source to the 4x4 smaller texture.
            DownSample4x (source, buffer);

            // Blur the small texture
            for(int i = 0; i < _iteration; i++)
            {
                RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);
                FourTapCone (buffer, buffer2, i);
                RenderTexture.ReleaseTemporary(buffer);
                buffer = buffer2;
            }
            Graphics.Blit(buffer, destination);

            RenderTexture.ReleaseTemporary(buffer);
    }
}