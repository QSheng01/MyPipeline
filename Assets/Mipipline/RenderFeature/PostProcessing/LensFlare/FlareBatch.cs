using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
internal struct Vertexhelper
{
    public Vector3[] vertices; 
    public int[] triangles;
    public Vector2[] uv;
	public Color[] color;

    public Vertexhelper(int vertCount, int trisCount, int uvCount, int colorCount)
    {
        vertices = new Vector3[vertCount];
        triangles = new int[trisCount];
        uv = new Vector2[uvCount];
		color = new Color[colorCount];
    }
    public void FillMesh(Mesh outMesh)
    {
        outMesh.vertices = vertices;
        outMesh.triangles = triangles;
        outMesh.uv = uv;
		outMesh.colors = color;
    }
    public static void CombineAndFillMesh(IList<Vertexhelper> meshes, Mesh outMesh)
    {
        int vertexCount = 0, trisCount = 0, uvCount = 0, colorCount = 0;
        for(int i = 0; i < meshes.Count; ++i)
        {
            vertexCount += meshes[i].vertices.Length;
            trisCount += meshes[i].triangles.Length;
            uvCount += meshes[i].uv.Length;
			colorCount += meshes[i].color.Length;
        }
        Vertexhelper combinedMesh = new Vertexhelper(vertexCount, trisCount, uvCount, colorCount);
        int vi=0, ti = 0, uvi = 0, coli = 0;
        for (int i = 0; i < meshes.Count; ++i)
        {
            Vertexhelper vh = meshes[i];
            foreach (int triangles in vh.triangles)
                combinedMesh.triangles[ti++] = triangles + vi;
            foreach (Vector3 vertex in vh.vertices)
                combinedMesh.vertices[vi++] = vertex;
			foreach (Color col in vh.color)
				combinedMesh.color[coli++] = col;
			foreach (Vector2 uv in vh.uv)
                combinedMesh.uv[uvi++] = uv;
        }
        combinedMesh.FillMesh(outMesh);
    }
}

public class FlareBatch : MonoBehaviour
{
    //[SerializeField] private Camera m_FlareCamera;
    [SerializeField] private Camera m_GameCamera;
    [SerializeField] private List<FlareSource> m_SourceList;
    [SerializeField] private Material m_Materail;

    private MeshRenderer m_MeshRenderer;
    private MeshFilter m_MeshFilter;

    private Mesh m_Mesh;

    private List<Vertexhelper> m_TempMeshes = new List<Vertexhelper>();

    void Start()
    {
        m_MeshRenderer = GetComponentOrCreate<MeshRenderer>();
        m_MeshRenderer.sharedMaterial = m_Materail;
        m_MeshFilter = GetComponentOrCreate<MeshFilter>();
        
        // 自动配置 Camera
        if (m_GameCamera == null)
            m_GameCamera = Camera.main;
        
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        m_Mesh = new Mesh();
        m_Mesh.Clear();
        PopulateMesh();
    }

    void LateUpdate()
    {
        m_Mesh.Clear();
        m_TempMeshes.Clear();
        for (int i = 0; i < m_SourceList.Count; ++i)
        {
            if(m_SourceList[i].IsVisible)
                UpdateGeometry(m_SourceList[i], m_TempMeshes);
        }
        Vertexhelper.CombineAndFillMesh(m_TempMeshes, m_Mesh);
        m_MeshFilter.sharedMesh = m_Mesh;
    }

  //   void UpdateGeometry(FlareSource source, List<Vertexhelper> meshes)
  //   {
  //       Vector3 viewportPos = source.ViewportPosition;
  //       if (viewportPos.z < 0) // 光在背后
  //           return;
  //       Vector2 center = source.Center; // 光晕“中心”，后续这个值可以变
  //       Vector2 flareSpacePos = ViewportToFlareSpace(viewportPos); // 光源在flare space的坐标
		// Vector2 flareVec = flareSpacePos - center;
  //       float angle = Mathf.Atan2(flareSpacePos.y, flareSpacePos.x) * Mathf.Rad2Deg;
  //       float fac = 1;
  //       if (source.SpreadMaximum != 0)
  //           fac = Mathf.Clamp(flareVec.magnitude / source.SpreadMaximum, 0, 1); // 扩散比例
  //       //List<Vertexhelper> meshes = new List<Vertexhelper>();
  //       for (int i = 0; i < source.Flares.Count; ++i)
  //       {
  //           Flare flare = source.Flares[i];
		// 	if (!flare.IsActive)
		// 		continue;
  //           Vector2 size = source.GetFlareSize(flare);
  //           float sclae = Mathf.Lerp(flare.ScaleRange.x, flare.ScaleRange.y, source.GetScaleCurveValue(fac));
  //           float alpha = Mathf.Lerp(1, 0, source.GetAlphaCurveValue(fac));
  //           Vector2 halfSize = size / 2;
  //           Vertexhelper vh = new Vertexhelper();
  //           vh.vertices = new Vector3[]
  //           {
  //               Vec3(-halfSize.x, -halfSize.y, -i*0.01f),
  //               Vec3(-halfSize.x, halfSize.y, -i*0.01f),
  //               Vec3(halfSize.x, halfSize.y, -i*0.01f),
  //               Vec3(halfSize.x, -halfSize.y, -i*0.01f),
  //           };
		// 	Color col = flare.Color;
  //           col.a *= source.AlphaBase;
  //           col.a *= alpha;
  //           vh.color = new Color[] { col, col, col, col };
  //
		// 	Vector2 pos = flareSpacePos - flare.DistanceAspect * flareVec * source.SpreadAmount;
		// 	Vector3 _pos = new Vector3(pos.x, pos.y, 0);
  //
  //           for (int j = 0; j < vh.vertices.Length; ++j)
  //           {
  //               vh.vertices[j] *= sclae;
  //               vh.vertices[j] = Quaternion.Euler(0, 0, flare.Rotation) * vh.vertices[j];
  //               if(flare.RotateWith)
  //                   vh.vertices[j] = Quaternion.Euler(0, 0, angle) * vh.vertices[j];
  //               vh.vertices[j] += _pos;
  //           }
  //           vh.uv = new Vector2[]
  //           {
  //               Vec2(0,0),
  //               Vec2(0,1),
  //               Vec2(1,1),
  //               Vec2(1,0),
  //           };
  //           for(int j = 0; j < vh.uv.Length; ++j)
  //           {
  //               Vector4 scaleOffset = flare.Atlas.GetScaleOffset(flare.Index);
  //               vh.uv[j] *= new Vector2(scaleOffset.x, scaleOffset.y);
  //               vh.uv[j] += new Vector2(scaleOffset.z, scaleOffset.w);
  //           }
  //           vh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
  //           meshes.Add(vh);
  //       }
  //   }
  
      void UpdateGeometry(FlareSource source, List<Vertexhelper> meshes)
    {
        // Vector3 viewportPos = source.ViewportPosition;
        if (!source.IsVisible) //不显示，将不再更新
            return;

        Vector2 center = source.Center; // 光晕“中心”，后续这个值可以变
        Vector2 flareSpacePos = ViewportToPerspFlareSpace(source.ViewportPosition); // 光源在flare space的坐标
        Vector2 flareVec = flareSpacePos - center;
        float angle = Mathf.Atan2(flareSpacePos.y, flareSpacePos.x) * Mathf.Rad2Deg;
        float fac = 1;
        if (source.SpreadMaximum != 0)
            fac = Mathf.Clamp(flareVec.magnitude / source.SpreadMaximum, 0, 1); // 扩散比例，离中心越远，显得越大

        //List<Vertexhelper> meshes = new List<Vertexhelper>();
        //遍历创建的每一个资源，根据配置生成顶点信息
        for (int i = 0; i < source.Flares.Count; ++i)
        {
            Flare flare = source.Flares[i];
            if (!flare.IsActive)
                continue;

            Vector2 size = source.GetFlareSize(flare);
            float scale = Mathf.Lerp(flare.ScaleRange.x, flare.ScaleRange.y, source.GetScaleCurveValue(fac));
            float alpha = Mathf.Lerp(1, 0, source.GetAlphaCurveValue(fac));

            //实例化一个顶点数据类
            Vertexhelper vh = new Vertexhelper();

            //生成颜色
            Color col = flare.Color;
            col.a *= source.AlphaBase;
            col.a *= alpha;
            vh.color = new Color[] { col, col, col, col };

            //计算出位置
            Vector2 pos = flareSpacePos - flare.DistanceAspect * flareVec * source.SpreadAmount;
            Vector3 _pos = new Vector3(pos.x, pos.y, 0);

            Vector2 halfSize = size / 2;
            
            vh.vertices = new Vector3[]
            {
                Vec3(-halfSize.x, -halfSize.y, -i*0.01f),
                Vec3(-halfSize.x, halfSize.y, -i*0.01f),
                Vec3(halfSize.x, halfSize.y, -i*0.01f),
                Vec3(halfSize.x, -halfSize.y, -i*0.01f),
            };
            //对每个点的位置进行缩放旋转和偏移
            for (int j = 0; j < vh.vertices.Length; ++j)
            {
                vh.vertices[j] *= scale;
                vh.vertices[j] = Quaternion.Euler(0, 0, flare.Rotation) * vh.vertices[j];
                if (flare.RotateWith)
                    vh.vertices[j] = Quaternion.Euler(0, 0, angle) * vh.vertices[j];
                vh.vertices[j] += _pos;

                vh.vertices[j].z += 3;

                vh.vertices[j] = m_GameCamera.ViewportToWorldPoint(PerspFlareSpaceToViewport(vh.vertices[j]));
                // vh.vertices[j] -= m_GameCamera.transform.position;
            }

            //uv
            vh.uv = new Vector2[]
            {
                Vec2(0,0),
                Vec2(0,1),
                Vec2(1,1),
                Vec2(1,0),
            };
            for (int j = 0; j < vh.uv.Length; ++j)
            {
                Vector4 scaleOffset = flare.Atlas.GetScaleOffset(flare.Index);
                vh.uv[j] *= new Vector2(scaleOffset.x, scaleOffset.y);
                vh.uv[j] += new Vector2(scaleOffset.z, scaleOffset.w);
            }

            //面的索引
            vh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

            meshes.Add(vh);
        }
    }
    /// <summary>
    /// 将viewport坐标转换到“透视flare空间”
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector3 ViewportToPerspFlareSpace(Vector3 pos)
    {
        //将视线空间的位置，转换到了以中心为0 0， -1到1的空间
        pos += new Vector3(-0.5f, -0.5f, 0);
        pos.x *= m_GameCamera.aspect; //横轴乘以宽高比
        pos.y *= 1; //纵轴
        pos.z = 0;
        pos *= 2;
        return pos;
    }

    /// <summary>
    /// 将“透视flare空间”坐标转换到viewport空间
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    Vector3 PerspFlareSpaceToViewport(Vector3 pos)
    {
        pos /= 2;
        pos.x /= m_GameCamera.aspect;
        pos += new Vector3(0.5f, 0.5f, 0);
        return pos;
    }


    void PopulateMesh()
    {
        m_MeshFilter.mesh = m_Mesh;
    }

    // 将viewport坐标转换到“flare空间”
    // Vector3 ViewportToFlareSpace(Vector3 pos)
    // {
    //     pos += new Vector3(-0.5f, -0.5f, 0);
    //     pos.x *= m_FlareCamera.aspect * m_FlareCamera.orthographicSize * 2;
    //     pos.y *= m_FlareCamera.orthographicSize * 2;
    //     pos.z = 0;
    //     return pos;
    // }

    T GetComponentOrCreate<T>() where T: Component
    {
        T component = GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent(typeof(T)) as T;
        return component;
    }

    Vector2 Vec2(float x, float y)
    {
        Vector2 v = new Vector2(x, y);
        return v;
    }

    Vector3 Vec3(float x, float y, float z)
    {
        Vector3 v = new Vector3(x, y, z);
        return v;
    }

}
