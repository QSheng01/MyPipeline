using UnityEngine;
using UnityEngine.Rendering.Universal;


/// <summary>
/// 镜子管理脚本 —— 挂在新建的Camera上
/// </summary>
[ExecuteInEditMode]
public class ChinarMirror : MonoBehaviour
{
    public  GameObject mirrorPlane;  //镜子
    public  Camera     mainCamera;   //主摄像机
    private Camera     mirrorCamera; //镜像摄像机
    public RenderTexture outputRT;


    private void Start()
    {
        mirrorCamera = GetComponent<Camera>();
        mirrorPlane = GameObject.Find("40002MirriorPlane");
        mainCamera = Camera.main;
        // UniversalAdditionalCameraData cameraData = mirrorCamera.GetComponent<UniversalAdditionalCameraData>();
        mirrorCamera.targetTexture = outputRT;
        MeshRenderer meshRenderer = mirrorPlane.GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetTexture("_ReflectRT", outputRT);
    }


    private void Update()
    {
        // if (null == mirrorPlane || null == mirrorCamera || null == mainCamera) return;
        // Vector3 postionInMirrorSpace    = mirrorPlane.transform.InverseTransformPoint(mainCamera.transform.position); //将主摄像机的世界坐标位置转换为镜子的局部坐标位置
        // postionInMirrorSpace.y          = -postionInMirrorSpace.y;  //一般y为镜面的法线方向
        // mirrorCamera.transform.position = mirrorPlane.transform.TransformPoint(postionInMirrorSpace);                 //转回到世界坐标系的位置

        if (mainCamera && mirrorPlane)
        {
            Vector3 mainCamPos = mainCamera.transform.position;
            Vector3 planePos = mirrorPlane.transform.position;
            float mainCamHeight = mainCamPos.y - planePos.y;
            float yPos = planePos.y - mainCamHeight;
            mirrorCamera.transform.position = new Vector3(mainCamPos.x, yPos, mainCamPos.z);
        }

    }
}