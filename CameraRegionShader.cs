using System.Collections;
using UnityEngine;
using System.Threading.Tasks;

public class CameraRegionShader : MonoBehaviour
{
    public ComputeShader CameraRegionComputeShader;
    private RenderTexture DebugRenderTexture;
    public Texture2D DebugTexture;
    public float DispatchInterval = 0.2f;

    private Camera mainCamera;
    private ComputeBuffer outputBuffer;
    private int kernelHandle;
    private uint[] regionCount = new uint[2] { 6, 6 };
    private uint[] regionSize;
    private float[] averageColors;

    private RGBLEDController ledController;

    public bool debug = false;

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        kernelHandle = CameraRegionComputeShader.FindKernel("CSMain");

        ledController = new RGBLEDController("COM3");

        outputBuffer = new ComputeBuffer((int)(regionCount[0] * regionCount[1]), sizeof(float) * 3,
            ComputeBufferType.Default);
        averageColors = new float[regionCount[0] * regionCount[1] * 3];

        // Calculate region size
        regionSize = new uint[2]
        {
            (uint)Screen.width / regionCount[0],
            (uint)Screen.height / regionCount[1]
        };

        // Set texture size and output buffer
        CameraRegionComputeShader.SetInt("TextureWidth", Screen.width);
        CameraRegionComputeShader.SetInt("TextureHeight", Screen.height);
        CameraRegionComputeShader.SetBuffer(kernelHandle, "OutputBuffer", outputBuffer);

        DebugTexture = new Texture2D(Screen.width, Screen.height);
        DebugRenderTexture = new RenderTexture(Screen.width, Screen.height, 24);

        StartCoroutine(DispatchComputeShader());
    }

    IEnumerator DispatchComputeShader()
    {
        while (true)
        {
            mainCamera.targetTexture = DebugRenderTexture;
            CameraRegionComputeShader.SetTexture(kernelHandle, "InputTexture", DebugRenderTexture);

            mainCamera.Render();

            CameraRegionComputeShader.Dispatch(kernelHandle, (int)regionCount[0], (int)regionCount[1], 1);

            outputBuffer.GetData(averageColors);
            
            Debug.Log("Sending RGB data to Arduino...");
            Task.Run(() => ledController.SendRGBData(averageColors, DispatchInterval, (int)regionCount[0], (int)regionCount[1]));

            if (debug)
                UpdateDebugTexture(averageColors);

            mainCamera.targetTexture = null;

            yield return new WaitForSeconds(DispatchInterval);
        }
    }

    void UpdateDebugTexture(float[] averageColors)
    {
        for (int y = 0; y < regionCount[1]; y++)
        {
            for (int x = 0; x < regionCount[0]; x++)
            {
                int index = (y * (int)regionCount[0] + x) * 3;
                Color color = new Color(averageColors[index], averageColors[index + 1], averageColors[index + 2]);
                Debug.Log($"Region ({x}, {y}) - Color: {color}");
                for (int i = 0; i < regionSize[1]; i++)
                {
                    for (int j = 0; j < regionSize[0]; j++)
                    {
                        DebugTexture.SetPixel(x * (int)regionSize[0] + j, y * (int)regionSize[1] + i, color);
                    }
                }
            }
        }

        DebugTexture.Apply();
    }

    void OnDestroy()
    {
        outputBuffer.Release();
        ledController.Close();
    }
}
