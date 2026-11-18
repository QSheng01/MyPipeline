
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{
    public class TextureBlur : TextureBase
    {
        private Texture2D windowBackground; //黑口
        private Texture2D TextureScrouce; //灰
        private Texture2D TexturePreview; //灰
        private Texture2D TextureEmmsion; //黑;
        public Vector2Int resolution = new Vector2Int(128, 128);
        public string nameText = "";
        private string fileText;
        private Texture2D preview;
        private BlurMode blurMode = BlurMode.Gaussian;
        float time;
        private Vector2 myMousePosition = new Vector2(0, 0); //鼠标在窗口的位置
        GUIStyle style1 = new GUIStyle();
        GUIStyle style2 = new GUIStyle();
        GUIStyle shurukuangstyle = new GUIStyle();
        GUIStyle shurukuangstyle1 = new GUIStyle();

        private Texture2D sourceTex0;
        private Texture2D inputTexture; //灰色
        public Shader ShaderPreview;
        public Material MaterialPreview;
        public Material EmmsionPreview;

        public Shader gsblur;
        public Shader fsblur;
        public Shader rsblur;
        public Material addmat; //保存时绘制到RT用ADD以保证RGB不和alpha相乘
        public Material alphamat; //预览时 绘制到RT用Alpha以保证RGB被alpha相乘 显示正常TexturePreview
        private Material addmatGUIPoint;
        Vector4 upAndDown = new Vector4(1, 0, -1, 0); //赋此值给shader内的偏移量

        Vector4 leftAndRight = new Vector4(0, 1, 0, -1);

        //public float blurwh = 0.5f;
        Color[] colorall; //颜色数组


        public float blurScale = 0.15f;
        public float directionPlate = 0.5f;
        public float xPoint = 0.5f;
        public float yPoint = 0.5f;
        private float maskRange = 0f; //模糊遮罩的大小
        private float maskPower = 0.2f; //模糊遮罩的power
        public float blurw = 0.5f; //模糊轴心x
        public float blurh = 0.5f; //模糊轴心y
        private bool ispoint = false; //鼠标是在调整中心点
        private float pointXrange = 0; //调整放射中心的X 框右侧减左侧的值的结果 存储在此
        private float pointYrange = 0;
        private Vector2 myPointpos;
        private Texture2D texture2D;
        private Vector2 ww;
        private string OutPath;
        private Vector2 sroll = new Vector2(0, 0);

        //新建一个窗口先，图像有一个特点就是要区分颜色空间

        //....................
        public TextureBlur()
        {
            texture2D = new Texture2D(128, 128);
            texture2D.name = "TexBlur.texture2D";
            SetColorSpaceValue();
            if (gsblur == null) //获取shader
            {
                gsblur = Shader.Find("Hidden/ArtToolkit/GaussianBlur");
                fsblur = Shader.Find("Hidden/ArtToolkit/TexRadialBlur");
                rsblur = Shader.Find("Hidden/ArtToolkit/TexRotateBlur");
                Shader addhuizhi = Shader.Find("Hidden/ArtToolkit/Add");
                Shader alphahuizhi = Shader.Find("Hidden/ArtToolkit/Alpha");
                addmat = new Material(addhuizhi);
                alphamat = new Material(alphahuizhi);
                addmatGUIPoint = new Material(Shader.Find("Hidden/ArtToolkit/TexRadialBlur_Point"));
                addmatGUIPoint.SetColor("_PointColor", new Color(1, 0, 0, 0.5f));

                //如何初始化材质呢如下，并且也给Shader赋值了
                ShaderPreview = Shader.Find("Hidden/ArtToolkit/YuLangShader");
                MaterialPreview = new Material(ShaderPreview);
                EmmsionPreview = new Material(ShaderPreview);
            }

            preview = new Texture2D(350, 350);
            preview.name = "Texblur.preview";
            windowBackground = Resources.Load<Texture2D>("背景图");
            TextureScrouce = Resources.Load<Texture2D>("底图1");
            TexturePreview = Resources.Load<Texture2D>("底图1");
            TextureEmmsion = Resources.Load<Texture2D>("底图2");
            inputTexture = Resources.Load<Texture2D>("底图2");
            pointXrange = 393 - 23;
            pointYrange = 552 - 182;
            myPointpos = new Vector2(211, 378);

            style1.fontStyle = FontStyle.Bold;
            style1.fontSize = 35;
            style1.normal.textColor = Color.white;

            style2.fontStyle = FontStyle.Bold;
            style2.fontSize = 15;
            style2.normal.textColor = Color.white;

            shurukuangstyle.normal.background = inputTexture;
            shurukuangstyle.fixedWidth = 144f;
            shurukuangstyle.fixedHeight = 25f;
            shurukuangstyle.normal.textColor = new Color(1, 1, 1, 1);
            shurukuangstyle.alignment = TextAnchor.MiddleCenter; //文本锚点
            shurukuangstyle.stretchWidth = true;
            shurukuangstyle.stretchHeight = true;
            shurukuangstyle.fontSize = 18;

            shurukuangstyle1.normal.background = inputTexture;
            shurukuangstyle1.fixedWidth = 280f;
            shurukuangstyle1.fixedHeight = 25f;
            shurukuangstyle1.normal.textColor = new Color(1, 1, 1, 1);
            shurukuangstyle1.stretchWidth = true;
            shurukuangstyle1.stretchHeight = true;
            shurukuangstyle1.fontSize = 18;
        }

        //....................................函数........................................
        private Vector2 scroll = Vector2.zero;
        private Texture2D backgound = new Texture2D(680, 620);

        //.............................OnGUI......................................
        public override void InterfaceShow(bool atThisWindow, Rect noRect)
        {
            bool update = false;
            scroll = GUILayout.BeginScrollView(scroll, false, false);
            GUILayout.Label(backgound);
            EditorGUI.DrawPreviewTexture(new Rect(0, 0, 1000, 650), windowBackground);

            if (GUI.Button(new Rect(330, 150, 115, 20), "高斯模糊_GS", BaseShow.GetButtonStyle((int)blurMode, (int)BlurMode.Gaussian))) //设置原文件路径为路径
            {
                if (blurMode != BlurMode.Gaussian) update = true;
                blurMode = BlurMode.Gaussian;
            }

            if (GUI.Button(new Rect(448, 150, 115, 20), "旋转模糊_RB", BaseShow.GetButtonStyle((int)blurMode, (int)BlurMode.Rotation))) //设置原文件路径为路径
            {
                if (blurMode != BlurMode.Rotation) update = true;
                blurMode = BlurMode.Rotation;
            }

            if (GUI.Button(new Rect(566, 150, 115, 20), "放射模糊_FS", BaseShow.GetButtonStyle((int)blurMode, (int)BlurMode.Radial))) //设置原文件路径为路径
            {
                if (blurMode != BlurMode.Radial) update = true;
                blurMode = BlurMode.Radial;
            }
            EditorGUI.BeginChangeCheck();
            sourceTex0 = TextureScrouce =
                EditorGUI.ObjectField(new Rect(0, 30, 180, 150), sourceTex0, typeof(Texture), false) as Texture2D;
            if (EditorGUI.EndChangeCheck()) update = true;
            //拉条

            myMousePosition = Event.current.mousePosition; //获取鼠标在窗口中的位置

            switch (blurMode)
            {
                case BlurMode.Gaussian:

                    EditorGUI.BeginChangeCheck();
                    blurScale = EditorGUI.Slider(new Rect(400f, 550f, 280, 20), blurScale, 0, 1f);
                    EditorGUI.Slider(new Rect(100f, 550f, 200, 20), maskRange, -1.5f, 5);
                    EditorGUI.Slider(new Rect(100f, 590f, 200, 20), maskPower, 0, 20);
                    directionPlate = EditorGUI.Slider(new Rect(400f, 590f, 280, 20), directionPlate, 0, 1f);
                    if (EditorGUI.EndChangeCheck())
                        update = true;
                    if (update) GSBlur();
                    break;
                case BlurMode.Rotation:
                    if (myMousePosition.x > 330 && myMousePosition.x < 680 && myMousePosition.y > 170 &&
                        myMousePosition.y < 520)
                    {
                        //判断鼠标是否在调整中心点的区域内
                        if (Event.current.type == EventType.MouseDrag)
                        {
                            xPoint = Mathf.Lerp(0, 1, (myMousePosition.x - 330) / (680 - 330));
                            yPoint = Mathf.Lerp(1, 0, (myMousePosition.y - 170) / (520 - 170));
                            update = true;
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    blurScale = EditorGUI.Slider(new Rect(400f, 550f, 280, 20), blurScale, 0, 1f);
                    EditorGUI.Slider(new Rect(100f, 550f, 200, 20), maskRange, -1.5f, 5);
                    EditorGUI.Slider(new Rect(100f, 590f, 200, 20), maskPower, 0, 20);
                    EditorGUI.Slider(new Rect(400f, 590f, 280, 20), directionPlate, 0, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        update = true;
                    }
                    if (update) RSBlur();
                    break;
                case BlurMode.Radial:
                    if (myMousePosition.x > 10 && myMousePosition.x < 310 && myMousePosition.y > 220 &&
                        myMousePosition.y < 520)
                    {
                        //判断鼠标是否在调整中心点的区域内
                        //Debug.Log("鼠标悬停在调整中心的框内："+myMousePosition);
                        ispoint = true;
                    }
                    else
                    {
                        ispoint = false;
                    }

                    EditorGUI.BeginChangeCheck();
                    blurScale = EditorGUI.Slider(new Rect(400f, 550f, 280, 20), blurScale, 0, 1f);
                    maskRange = EditorGUI.Slider(new Rect(100f, 550f, 200, 20), maskRange, -1.5f, 5);
                    maskPower = EditorGUI.Slider(new Rect(100f, 590f, 200, 20), maskPower, 0, 20);
                    EditorGUI.Slider(new Rect(400f, 590f, 280, 20), directionPlate, 0, 1f);
                    if (EditorGUI.EndChangeCheck())
                    {
                        update = true;
                    }

                    if (Event.current.type == EventType.MouseDrag && ispoint)
                    {
                        //Debug.Log("鼠标悬停在调整中心的框内：" + "正在拖动");
                        blurw = (myMousePosition.x + 20) / pointXrange;
                        blurh = (myMousePosition.y - 180) / pointYrange;
                        myPointpos = myMousePosition;
                        update = true;
                    }
                    if (update) FSBlur();
                    break;
            }

            EditorGUI.DrawPreviewTexture(new Rect(10, 220, 300, 300), preview, addmatGUIPoint,
                ScaleMode.StretchToFill);


            //预览框
            RenderTexture rt = RenderTexture.GetTemporary(350, 350);
            RenderTexture prous = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(TexturePreview, MaterialPreview);
            preview.ReadPixels(new Rect(0, 0, 350, 350), 0, 0);
            preview.Apply();
            RenderTexture.active = prous;
            RenderTexture.ReleaseTemporary(rt);
            GUI.Label(new Rect(330, 170, 350, 350), preview);

            //EditorGUI.DrawPreviewTexture(new Rect(330, 170f, 350, 350), TexturePreview, MaterialPreview);

            //设置保存按钮
            if (GUI.Button(new Rect(380, 110, 300, 30), "保存"))
            {
                if (TextureScrouce != null)
                {
                    if (BlurMode.Gaussian == blurMode)
                    {
                        GSBlurSave();
                    }

                    if (BlurMode.Rotation == blurMode)
                    {
                        RSBlurSave();
                    }

                    if (BlurMode.Radial == blurMode)
                    {
                        FSBlurSave();
                    }
                }
            }
            //新建一二维的文本框，用于起名字和设路径以及分辨率

            //写字
            //Handles.Label(new Vector3(380, 40, 0), "贴图模糊三合一工具(高斯/旋转/放射)", style1);
            GUI.Label(new Rect(380, 40, 70, 30), "设置分别率：");
            resolution = EditorGUI.Vector2IntField(new Rect(450, 45, 200, 30), "", resolution);
            GUI.Label(new Rect(380, 70, 70, 30), "命名：");
            nameText = GUI.TextField(new Rect(420, 70, 200, 30), nameText);
            if (nameText == "")
                if (TextureScrouce != null)
                    nameText = TextureScrouce.name;
            Handles.Label(new Vector3(320, 550, 0), "模糊强度", style2);
            Handles.Label(new Vector3(215, 130, 0), "原图", style1);
            Handles.Label(new Vector3(12, 200, 0), "在本区域内拖动以调整放射中心(限放射模糊)", style2);
            Handles.Label(new Vector3(320, 590, 0), "方向(高斯)", style2);
            Handles.Label(new Vector3(12, 550, 0), "范围(放射)", style2);
            Handles.Label(new Vector3(12, 590, 0), "硬度(放射)", style2);

            GUILayout.EndScrollView();

        }

        //......................................优化...............................................

        //.......................................模糊计算...............................................




        void GSBlur()
        {
            if (TextureScrouce != null)
            {
                RenderTexture rt = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt1 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt2 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture.active = rt; //RT运行
                GL.PushMatrix(); //GL.获取矩阵
                GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
                Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
                Graphics.DrawTexture(rect, TextureScrouce, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Material mat1 = new Material(gsblur);
                mat1.SetFloat("_BlurSpread", blurScale * 0.01f);
                mat1.SetVector("_offset1", new Vector4(1, 0, 0, 0));
                mat1.SetVector("_offset2", new Vector4(0, 1, 0, 0));
                Graphics.Blit(rt, rt1);
                float cishu = Mathf.Lerp(30, 150, blurScale); //拉条
                for (int i = 0; i < cishu; i++) //高斯模糊循环
                {
                    //Graphics.Blit(rt1, rt2, mat1);
                    //Graphics.Blit(rt2, rt1, mat2);
                    float x1 = 0;
                    float x2 = 0;
                    float y1 = 0;
                    float y2 = 0;
                    x1 = Mathf.Lerp(upAndDown.x, 0, directionPlate) * 2;
                    x2 = Mathf.Lerp(upAndDown.z, 0, directionPlate) * 2;
                    y1 = Mathf.Lerp(0, leftAndRight.y, directionPlate) * 2;
                    y2 = Mathf.Lerp(0, leftAndRight.w, directionPlate) * 2;
                    mat1.SetVector("_offset1", new Vector2(x1, upAndDown.y)); //设置shader中的左右偏移方向
                    mat1.SetVector("_offset2", new Vector2(x2, upAndDown.w));
                    Graphics.Blit(rt1, rt2, mat1);
                    mat1.SetVector("_offset1", new Vector2(leftAndRight.x, y1)); //设置shader中的上下偏移方向
                    mat1.SetVector("_offset2", new Vector2(leftAndRight.z, y2));
                    Graphics.Blit(rt2, rt1, mat1);
                }

                Graphics.Blit(rt1, rt);
                GL.PopMatrix(); //GL.保存矩阵
                texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture2D.Apply();
                TexturePreview = texture2D; //预览
                RenderTexture.active = null; //RT置空  

                rt.Release();
                rt1.Release();
                rt2.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(rt1);
                Object.DestroyImmediate(rt2);

            }
        }

        void GSBlurSave()
        {
            if (TextureScrouce != null)
            {
                Texture2D YYY = DeCompress(TextureScrouce);
                RenderTexture rt = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt1 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt2 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture.active = rt; //RT运行
                GL.PushMatrix(); //GL.获取矩阵
                GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
                Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
                Graphics.DrawTexture(rect, YYY, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Material mat1;
                mat1 = new Material(gsblur);
                mat1.SetFloat("_BlurSpread", blurScale * 0.01f);
                mat1.SetVector("_offset1", new Vector4(1, 0, 0, 0));
                mat1.SetVector("_offset2", new Vector4(0, 1, 0, 0));
                Graphics.Blit(rt, rt1);
                float cishu = Mathf.Lerp(30, 150, blurScale);
                for (int i = 0; i < cishu; i++) //高斯模糊循环
                {
                    float x1 = Mathf.Lerp(upAndDown.x, 0, directionPlate) * 2;
                    float x2 = Mathf.Lerp(upAndDown.z, 0, directionPlate) * 2;
                    float y1 = Mathf.Lerp(0, leftAndRight.y, directionPlate) * 2;
                    float y2 = Mathf.Lerp(0, leftAndRight.w, directionPlate) * 2;
                    mat1.SetVector("_offset1", new Vector2(x1, upAndDown.y)); //设置shader中的左右偏移方向
                    mat1.SetVector("_offset2", new Vector2(x2, upAndDown.w));
                    Graphics.Blit(rt1, rt2, mat1);
                    mat1.SetVector("_offset1", new Vector2(leftAndRight.x, y1)); //设置shader中的上下偏移方向
                    mat1.SetVector("_offset2", new Vector2(leftAndRight.z, y2));
                    Graphics.Blit(rt2, rt1, mat1);
                }

                Graphics.Blit(rt1, rt);
                GL.PopMatrix(); //GL.保存矩阵
                Texture2D texture2Dsave =
                    new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat,
                        false); //, TextureFormat.RGBAFloat, false
                //RGBA32造成存图严重色阶
                texture2Dsave.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture2Dsave.Apply();
                colorall = new Color[(int)(texture2Dsave.width * texture2Dsave.height)]; //初始化颜色数组长度为出图宽高
                colorall = texture2Dsave.GetPixels();
                if (PlayerSettings.colorSpace == ColorSpace.Linear) //判断色彩空间，如果是线性空间则校正色彩
                {
                    for (int j = 0; j < colorall.Length; j++)
                    {
                        colorall[j].r = Mathf.Pow(colorall[j].r, 1 / 2.2f);
                        colorall[j].g = Mathf.Pow(colorall[j].g, 1 / 2.2f);
                        colorall[j].b = Mathf.Pow(colorall[j].b, 1 / 2.2f);
                        //colorall[j].a = Mathf.Pow(colorall[j].a, 1 / 2.2f);
                    }
                }

                texture2Dsave.SetPixels(colorall); //写入color[]到 像素数据 
                texture2Dsave.Apply();
                RenderTexture.active = null; //RT置空
                Save(GetTextureFolder(TextureScrouce), nameText, texture2Dsave, AssetDatabase.GetAssetPath(TextureScrouce));
                rt.Release();
                rt1.Release();
                rt2.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(rt1);
                Object.DestroyImmediate(rt2);
                Object.DestroyImmediate(texture2Dsave);
            }
        }

        void RSBlur()
        {
            if (TextureScrouce != null)
            {
                //if(rt) RenderTexture.ReleaseTemporary(rt);
                //if(rt1) RenderTexture.ReleaseTemporary(rt1);
                //if(rt2) RenderTexture.ReleaseTemporary(rt2);
                RenderTexture rt = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt1 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt2 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture.active = rt; //RT运行
                GL.PushMatrix(); //GL.获取矩阵
                GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
                Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
                //Graphics.DrawTexture(rect, sourceTex, alphamat);//渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Graphics.DrawTexture(rect, TextureScrouce, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Material mat1;
                mat1 = new Material(rsblur);
                mat1.SetFloat("_XPoint", xPoint);
                mat1.SetFloat("_YPoint", yPoint);
                mat1.SetFloat("_Count", blurScale * 628);
                Graphics.Blit(rt, rt1, mat1);
                //Graphics.Blit(rt1, rt2, mat1);
                //Graphics.Blit(rt2, rt);
                GL.PopMatrix(); //GL.保存矩阵
                texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture2D.Apply();
                TexturePreview = texture2D; //预览
                RenderTexture.active = null; //RT置空        

                rt.Release();
                rt1.Release();
                rt2.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(rt1);
                Object.DestroyImmediate(rt2);


            }
        }

        void RSBlurSave()
        {
            if (TextureScrouce != null)
            {
                Texture2D YYY = DeCompress(TextureScrouce);
                //if(rt) RenderTexture.ReleaseTemporary(rt);
                //if(rt1) RenderTexture.ReleaseTemporary(rt1);
                //if(rt2) RenderTexture.ReleaseTemporary(rt2);
                RenderTexture rt = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt1 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt2 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture.active = rt; //RT运行
                GL.PushMatrix(); //GL.获取矩阵
                GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
                Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
                Graphics.DrawTexture(rect, YYY, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Material mat1;
                mat1 = new Material(rsblur);
                mat1.SetFloat("_XPoint", xPoint);
                mat1.SetFloat("_YPoint", yPoint);
                mat1.SetFloat("_Count", blurScale * 628);
                Graphics.Blit(rt, rt1, mat1);
                Graphics.Blit(rt1, rt);
                GL.PopMatrix(); //GL.保存矩阵
                Texture2D texture2Dsave =
                    new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat,
                        false); //, TextureFormat.RGBAFloat, false
                texture2Dsave.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture2Dsave.Apply();
                colorall = new Color[(int)(texture2Dsave.width * texture2Dsave.height)]; //初始化颜色数组长度为出图宽高
                colorall = texture2Dsave.GetPixels();
                if (PlayerSettings.colorSpace == ColorSpace.Linear) //判断色彩空间，如果是线性空间则校正色彩
                {
                    for (int j = 0; j < colorall.Length; j++)
                    {
                        colorall[j].r = Mathf.Pow(colorall[j].r, 1 / 2.2f);
                        colorall[j].g = Mathf.Pow(colorall[j].g, 1 / 2.2f);
                        colorall[j].b = Mathf.Pow(colorall[j].b, 1 / 2.2f);
                        //colorall[j].a = Mathf.Pow(colorall[j].a, 1 / 2.2f);
                    }
                }

                texture2Dsave.SetPixels(colorall); //写入color[]到 像素数据 
                texture2Dsave.Apply();
                RenderTexture.active = null; //RT置空
                string path = AssetDatabase.GetAssetPath(TextureScrouce);
                int i = path.Length - 1;
                for (; i >= 0; i--)
                {
                    if (path[i] == '/') break;
                }

                Save(GetTextureFolder(TextureScrouce), nameText, texture2Dsave, AssetDatabase.GetAssetPath(TextureScrouce));
                // RenderTexture.ReleaseTemporary(rt);
                // RenderTexture.ReleaseTemporary(rt1);
                // RenderTexture.ReleaseTemporary(rt2);
                rt.Release();
                rt1.Release();
                rt2.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(rt1);
                Object.DestroyImmediate(rt2);
                Object.DestroyImmediate(texture2Dsave);
            }
        }

        void FSBlur()
        {
            if (TextureScrouce != null) //TextureScrouce
            {
                //if(rt) RenderTexture.ReleaseTemporary(rt);
                //if(rt1) RenderTexture.ReleaseTemporary(rt1);
                //if(rt2) RenderTexture.ReleaseTemporary(rt2);
                RenderTexture rt = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt1 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture rt2 = new RenderTexture(128, 128, 0, RenderTextureFormat.ARGBFloat);
                RenderTexture.active = rt; //RT运行
                GL.PushMatrix(); //GL.获取矩阵
                GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
                Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
                Graphics.DrawTexture(rect, TextureScrouce, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
                Material mat1;
                mat1 = new Material(fsblur);
                mat1.SetFloat("_BlurScale", 1);
                mat1.SetFloat("_XPoint", blurw);
                mat1.SetFloat("_YPoint", 1 - blurh);
                mat1.SetFloat("_maskRange", maskRange);
                mat1.SetFloat("_maskPower", maskPower);
                addmatGUIPoint.SetFloat("_maskRange", maskRange);
                addmatGUIPoint.SetFloat("_maskPower", maskPower);
                addmatGUIPoint.SetFloat("_XPoint", blurw);
                addmatGUIPoint.SetFloat("_YPoint", 1 - blurh);
                mat1.SetFloat("_Count", Mathf.Clamp(blurScale * 1000, 1, 1000));
                Graphics.Blit(rt, rt1, mat1);
                Graphics.Blit(rt1, rt);
                GL.PopMatrix(); //GL.保存矩阵
                texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                texture2D.Apply();
                TexturePreview = texture2D; //预览
                RenderTexture.active = null; //RT置空            
                // RenderTexture.ReleaseTemporary(rt);
                // RenderTexture.ReleaseTemporary(rt1);
                // RenderTexture.ReleaseTemporary(rt2);
                rt.Release();
                rt1.Release();
                rt2.Release();
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(rt1);
                Object.DestroyImmediate(rt2);

            }
        }

        void FSBlurSave()
        {
            // if (TextureScrouce != null)
            // {
            //     string mypath;
            //     if (AssetDatabase.GetAssetPath(TextureScrouce) == "")
            //     {
            //         mypath = OutPath;
            //         int i = mypath.Length - 1;
            //         for (; i >= 0; i--)
            //         {
            //             if (mypath[i] == '/') break;
            //         }
            //
            //         mypath = mypath.Substring(0, i);
            //     }
            //     else
            //     {
            //         mypath = GetFullPath(TextureScrouce);//获取源文件在系统中的完整路径
            //     }
            Texture2D YYY = DeCompress(TextureScrouce);
            RenderTexture rt = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
            RenderTexture rt1 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
            RenderTexture rt2 = new RenderTexture((int)resolution.x, (int)resolution.y, 0, RenderTextureFormat.ARGBFloat);
            RenderTexture.active = rt; //RT运行
            GL.PushMatrix(); //GL.获取矩阵
            GL.LoadPixelMatrix(0, rt.width, rt.height, 0); //GL.设置像素参数
            Rect rect = new Rect(0, 0, rt.width, rt.height); //定义一个矩形  位置 宽高
            Graphics.DrawTexture(rect, YYY, addmat); //渲染.绘制tex，参数传入矩形和要绘制进去的贴图
            Material mat1;
            mat1 = new Material(fsblur);
            mat1.SetFloat("_BlurScale", 1);
            mat1.SetFloat("_XPoint", blurw);
            mat1.SetFloat("_YPoint", 1 - blurh);
            mat1.SetFloat("_maskRange", maskRange);
            mat1.SetFloat("_maskPower", maskPower);
            mat1.SetFloat("_Count", Mathf.Clamp(blurScale * 1000, 1, 1000));
            Graphics.Blit(rt, rt1, mat1);
            Graphics.Blit(rt1, rt);
            GL.PopMatrix(); //GL.保存矩阵
            Texture2D texture2Dsave =
                new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false); //, TextureFormat.RGBAFloat, false
            texture2Dsave.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture2Dsave.Apply();

            colorall = new Color[(int)(texture2Dsave.width * texture2Dsave.height)]; //初始化颜色数组长度为出图宽高
            colorall = texture2Dsave.GetPixels();
            if (PlayerSettings.colorSpace == ColorSpace.Linear) //判断色彩空间，如果是线性空间则校正色彩
            {
                for (int j = 0; j < colorall.Length; j++)
                {
                    colorall[j].r = Mathf.Pow(colorall[j].r, 1 / 2.2f);
                    colorall[j].g = Mathf.Pow(colorall[j].g, 1 / 2.2f);
                    colorall[j].b = Mathf.Pow(colorall[j].b, 1 / 2.2f);
                    //colorall[j].a = Mathf.Pow(colorall[j].a, 1 / 2.2f);
                }
            }

            texture2Dsave.SetPixels(colorall); //写入color[]到 像素数据 
            texture2Dsave.Apply();

            RenderTexture.active = null; //RT置空

            string path = AssetDatabase.GetAssetPath(TextureScrouce);
            int i = path.Length - 1;
            for (; i >= 0; i--)
            {
                if (path[i] == '/') break;
            }

            Save(GetTextureFolder(TextureScrouce), nameText, texture2Dsave, AssetDatabase.GetAssetPath(TextureScrouce));

            // RenderTexture.ReleaseTemporary(rt);
            // RenderTexture.ReleaseTemporary(rt1);
            // RenderTexture.ReleaseTemporary(rt2);
            rt.Release();
            rt1.Release();
            rt2.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(rt1);
            Object.DestroyImmediate(rt2);

        }

        private enum BlurMode
        {
            Gaussian = 0,
            Rotation = 1,
            Radial = 2
        }
    }
}