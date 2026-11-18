
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using Vector2 = UnityEngine.Vector2;


namespace SYArtToolkit
{
    public class AssetsAudit : ToolInterface
    {
        public static bool AssetCheckPath(string[] paths)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (string path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;
                if (File.Exists(path) && path.EndsWith(".prefab"))
                {
                    var resLog = AssetCheckLog(AssetDatabase.LoadAssetAtPath<GameObject>(path), new StringBuilder());
                    if (string.IsNullOrEmpty(resLog)) continue;
                    else
                    {
                        stringBuilder.Append(string.Format($"{path} 检测失败原因:{resLog}\n"));
                    }
                }

            }
            bool res = string.IsNullOrEmpty(stringBuilder.ToString());
            if (!res)
            {
                using (FileStream fileStream = new FileStream("Assets/SYPackages/ArtToolBox/Editor/CheckDataLog.txt", FileMode.Create, FileAccess.Write))
                {
                    // 将内容转换为字节数组
                    byte[] buffer = System.Text.Encoding.Default.GetBytes(stringBuilder.ToString());
                    // 写入文件
                    fileStream.Write(buffer, 0, buffer.Length);
                }


                //if(!File.Exists(savePathTxt)) { File.Create(savePathTxt); }
                //System.IO.File.WriteAllText(savePathTxt, stringBuilder.ToString());

                //File.WriteAllBytes(savePathTxt, By)
            }

            return res;
        }


        public static string AssetCheckLog(GameObject gameObject, StringBuilder log)
        {

            //

            //AllClear();
            //foreach (var obj in allGameObject)
            //{
            if (gameObject == null) return log.ToString();
            var ps = gameObject.GetComponentsInChildren<ParticleSystem>();
            Submit.ArtToolkitData.Instance.AddAssetCheck(1);
            foreach (var p in ps)
            {
                if (p == null) continue;
                var em = p.emission;
                var render = p.GetComponent<ParticleSystemRenderer>(); //一定可以找到

                if (p.main.prewarm)
                {
                    log.Append($"{p.gameObject.name}开启了预热;");
                }

                if (em.enabled == false || render.enabled == false)
                {
                    log.Append($"{p.gameObject.name}是空粒子;");
                }

                if (em.rateOverTime.constant > 50)
                {
                    log.Append($"{p.gameObject.name}粒子数量过于多;");
                }

                if (render.renderMode != ParticleSystemRenderMode.Mesh)
                {
                    if (render.mesh) log.Append($"{p.gameObject.name}含有空Mesh;");
                }
                else
                {
                    if (render.mesh)
                    {
                        foreach (var c in render.mesh.name)
                        {
                            if (c < 40 || c > 122)
                            {
                                log.Append($"{render.gameObject.name}/{render.mesh.name}网格名称不规范;");
                            }
                        }
                        string matpath = AssetDatabase.GetAssetPath(render.mesh);
                        if (matpath != "Library/unity default resources")
                        {
                            log.Append($"{render.gameObject.name}/{render.mesh.name}网格使用了默认网格; ");
                        }

                    }
                }




            }
            var mfs = gameObject.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null)
                {
                    log.Append($"{mf.gameObject.name}/{mf.name}网格为空;");
                    continue;
                }

                var so = new SerializedObject(mf);
                var mesh = so.FindProperty("m_Mesh");
                if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                {
                    log.Append($"{mf.gameObject.name}/{mf.name}网格丢失引用;");
                }

                foreach (var c in mf.sharedMesh.name)
                {
                    if (c < 40 || c > 122)
                    {
                        log.Append($"{mf.gameObject.name}/{mf.sharedMesh.name}网格名称不规范;");
                    }
                }
                string matpath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (matpath != "Library/unity default resources")
                {
                    log.Append($"{matpath}使用了默认材质;");
                }
            }
            var colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (c)
                    log.Append($"{c.gameObject.name}含有碰撞体;");
            }
            var collider2s = gameObject.GetComponentsInChildren<Collider2D>();
            foreach (var c in collider2s)
            {
                if (c)
                    log.Append($"{c.gameObject.name}含有碰撞体;");
            }

            HashSet<Material> mats1 = new HashSet<Material>();
            HashSet<Material> mats2 = new HashSet<Material>();
            var rs = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var r in rs)
            {
                if (r == null) continue;
                var so = new SerializedObject(r);
                var mesh = so.FindProperty("m_Materials");
                //if(mesh.)   95
                if (mesh != null && mesh.isArray)
                {
                    int length = mesh.arraySize;
                    for (int i = 0; i < length; ++i)
                    {
                        var m = mesh.GetArrayElementAtIndex(i);
                        if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                        {
                            log.Append($"{r.gameObject.name}材质球引用丢失;");
                        }
                    }
                }
                if (r.shadowCastingMode != ShadowCastingMode.Off || r.receiveShadows ||
                    r.lightProbeUsage != LightProbeUsage.Off ||
                    r.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    log.Append($"{r.gameObject.name}粒子开启了阴影;");
                }

                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    if (mats2.Contains(mat)) continue;
                    else mats2.Add(mat);
                    switch (mat.name)
                    {
                        case "Default-Material":
                        case "Default-ParticleSystem":
                        case "Lit":
                        case "ParticlesUnlit":
                        case "SimpleLit":
                        case "Sprite-Lit-Default":
                        case "Sprite-Unlit-Default":
                        case "TerrainLit":
                            {
                                log.Append($"{r.gameObject.name}/{mat.name}使用了默认的材质球;");
                                break;
                            }
                        default:
                            foreach (var c in mat.name)
                            {
                                if (c < 40 || c > 122)
                                {
                                    log.Append($"{r.gameObject.name}/{mat.name}材质球名称不规范; ");
                                }
                            }
                            if (!mats1.Contains(mat))
                            {
                                mats1.Add(mat);
                            }
                            break;
                    }


                    SerializedObject psSource = new SerializedObject(mat);
                    SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
                    SerializedProperty property = emissionProperty.FindPropertyRelative("m_TexEnvs");
                    for (int j = property.arraySize - 1; j >= 0; j--)
                    {
                        string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                        if (!mat.HasProperty(propertyName))
                        {
                            log.Append($"{r.gameObject.name}/{mat.name}材质球多余的引用;");
                            break;
                        }
                    }








                }

                if (mats1.Count > 1)
                {
                    log.Append($"{r.gameObject}多材质;");


                }
                mats1.Clear();

            }
            var ans = gameObject.GetComponentsInChildren<Animator>();
            foreach (var animator1 in ans)
            {
                if (animator1 == null) continue;
                foreach (var c in animator1.name)
                {
                    if (c < 40 || c > 122)
                    {
                        log.Append($"{animator1.gameObject.name}/{c}动画名称不规范;");
                    }
                }
                var rac = animator1.runtimeAnimatorController;
                if (rac)
                {
                    var acs = animator1.runtimeAnimatorController.animationClips;
                    foreach (var c in rac.name)
                    {
                        if (c < 40 || c > 122)
                        {
                            log.Append($"{animator1.gameObject.name}/{c}动画名称不规范;");
                        }
                    }


                    if (acs != null && acs.Length > 0)
                    {
                        foreach (var ac in acs)
                        {
                            // if (!anims.ContainsKey(ac))
                            // {
                            //     anims.Add(ac, false);
                            //     string matpath = AssetDatabase.GetAssetPath(ac);
                            //     if (matpath.Replace(animPathAsset, "") == matpath)
                            //     {
                            //         anims[ac] = true;
                            //         withAnimPath = true;
                            //     }
                            // }
                            foreach (var c in ac.name)
                            {
                                if (c < 40 || c > 122)
                                {
                                    log.Append($"{animator1.gameObject.name}/{c}动画名称不规范;");
                                }
                            }
                        }
                    }
                }
            }

            //  var mf = obj.obj.GetComponent<MeshFilter>();
            // if (mf != null)
            // {
            //
            // }
            var prs = gameObject.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (var pr in prs)
            {

                if (pr != null)
                {


                    var so = new SerializedObject(pr);
                    var mesh = so.FindProperty("m_Mesh");
                    if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                    {
                        log.Append($"{pr.gameObject.name}网格引用丢失;");
                    }
                    mesh = so.FindProperty("m_Materials");
                    //if(mesh.)   95
                    if (mesh != null && mesh.isArray)
                    {
                        int length = mesh.arraySize;
                        for (int i = 0; i < length; ++i)
                        {
                            var m = mesh.GetArrayElementAtIndex(i);
                            if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                            {
                                log.Append($"{pr.gameObject.name}材质球引用丢失;");
                            }
                        }
                    }
                }

            }


            return log.ToString();
        }


        public static bool AssetCheck(GameObject gameObject)
        {

            //

            //AllClear();
            //foreach (var obj in allGameObject)
            //{
            if (gameObject == null) return true;
            var ps = gameObject.GetComponentsInChildren<ParticleSystem>();
            Submit.ArtToolkitData.Instance.AddAssetCheck(1);
            foreach (var p in ps)
            {
                if (p == null) continue;
                var em = p.emission;
                var render = p.GetComponent<ParticleSystemRenderer>(); //一定可以找到

                if (p.main.prewarm)
                {
                    return false;
                }

                if (em.enabled == false || render.enabled == false)
                {
                    return false;
                }

                if (em.rateOverTime.constant > 50)
                {
                    return false;
                }

                if (render.renderMode != ParticleSystemRenderMode.Mesh)
                {
                    if (render.mesh) return false;
                }
                else
                {
                    if (render.mesh)
                    {
                        foreach (var c in render.mesh.name)
                        {
                            if (c < 40 || c > 122)
                            {
                                return false;
                            }
                        }
                        string matpath = AssetDatabase.GetAssetPath(render.mesh);
                        if (matpath != "Library/unity default resources")
                        {
                            return false;
                        }

                    }
                }




            }
            var mfs = gameObject.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) return false;

                var so = new SerializedObject(mf);
                var mesh = so.FindProperty("m_Mesh");
                if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                {
                    return false;
                }

                foreach (var c in mf.sharedMesh.name)
                {
                    if (c < 40 || c > 122)
                    {
                        return false;
                    }
                }
                string matpath = AssetDatabase.GetAssetPath(mf.sharedMesh);
                if (matpath != "Library/unity default resources")
                {
                    return false;
                }
            }
            var colliders = gameObject.GetComponentsInChildren<Collider>();
            foreach (var c in colliders)
            {
                if (c) return false;
            }
            var collider2s = gameObject.GetComponentsInChildren<Collider2D>();
            foreach (var c in collider2s)
            {
                if (c) return false;
            }

            var rs = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var r in rs)
            {
                if (r == null) continue;
                var so = new SerializedObject(r);
                var mesh = so.FindProperty("m_Materials");
                //if(mesh.)   95
                if (mesh != null && mesh.isArray)
                {
                    int length = mesh.arraySize;
                    for (int i = 0; i < length; ++i)
                    {
                        var m = mesh.GetArrayElementAtIndex(i);
                        if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                        {
                            return false;
                        }
                    }
                }
                if (r.shadowCastingMode != ShadowCastingMode.Off || r.receiveShadows ||
                    r.lightProbeUsage != LightProbeUsage.Off ||
                    r.reflectionProbeUsage != ReflectionProbeUsage.Off)
                {
                    return false;
                }

                HashSet<Material> mats1 = new HashSet<Material>();
                foreach (var mat in r.sharedMaterials)
                {
                    if (mat == null) continue;
                    switch (mat.name)
                    {
                        case "Default-Material":
                        case "Default-ParticleSystem":
                        case "Lit":
                        case "ParticlesUnlit":
                        case "SimpleLit":
                        case "Sprite-Lit-Default":
                        case "Sprite-Unlit-Default":
                        case "TerrainLit":
                            {
                                return false;
                            }
                        default:
                            foreach (var c in mat.name)
                            {
                                if (c < 40 || c > 122)
                                {
                                    return false;
                                }
                            }
                            if (!mats1.Contains(mat))
                            {
                                mats1.Add(mat);
                            }
                            break;
                    }


                    SerializedObject psSource = new SerializedObject(mat);
                    SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
                    SerializedProperty property = emissionProperty.FindPropertyRelative("m_TexEnvs");
                    for (int j = property.arraySize - 1; j >= 0; j--)
                    {
                        string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                        if (!mat.HasProperty(propertyName))
                        {
                            return false;
                        }
                    }









                }

                if (mats1.Count > 1) return false;


            }

            var ans = gameObject.GetComponentsInChildren<Animator>();
            foreach (var animator1 in ans)
            {
                foreach (var c in animator1.name)
                {
                    if (c < 40 || c > 122)
                    {
                        return false;
                    }
                }
                var rac = animator1.runtimeAnimatorController;
                if (rac)
                {
                    var acs = animator1.runtimeAnimatorController.animationClips;

                    foreach (var c in rac.name)
                    {
                        if (c < 40 || c > 122)
                        {
                            return false;
                        }
                    }

                    if (acs != null && acs.Length > 0)
                    {
                        foreach (var ac in acs)
                        {
                            // if (!anims.ContainsKey(ac))
                            // {
                            //     anims.Add(ac, false);
                            //     string matpath = AssetDatabase.GetAssetPath(ac);
                            //     if (matpath.Replace(animPathAsset, "") == matpath)
                            //     {
                            //         anims[ac] = true;
                            //         withAnimPath = true;
                            //     }
                            // }
                            foreach (var c in ac.name)
                            {
                                if (c < 40 || c > 122)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            //  var mf = obj.obj.GetComponent<MeshFilter>();
            // if (mf != null)
            // {
            //
            // }
            var prs = gameObject.GetComponentsInChildren<ParticleSystemRenderer>();
            foreach (var pr in prs)
            {

                if (pr != null)
                {


                    var so = new SerializedObject(pr);
                    var mesh = so.FindProperty("m_Mesh");
                    if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                    {
                        return false;
                    }
                    mesh = so.FindProperty("m_Materials");
                    //if(mesh.)   95
                    if (mesh != null && mesh.isArray)
                    {
                        int length = mesh.arraySize;
                        for (int i = 0; i < length; ++i)
                        {
                            var m = mesh.GetArrayElementAtIndex(i);
                            if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                            {
                                return false;
                            }
                        }
                    }
                }

            }


            return true;
        }





        private ArtToolBoxGlobalData assets;
        /// <summary>
        ///public Transform obj;  指向场景物体
        ///public string name;    所有的父集路径+物体的名字
        ///public bool set;       是否应用修改
        /// </summary>
        private class NameAndObj
        {
            public Transform obj;
            public string name;
            public bool set;
            public NameAndObj(Transform obj, string name, bool set = true)
            {
                this.obj = obj;
                this.name = name;
                this.set = set;
            }
        }
        /// <summary>
        /// 无效的材质引用
        /// </summary>
        private HashSet<Material> cleanMaterial = new HashSet<Material>();//材质清理
        private bool cleanMaterialFold = true;//是否折叠
        private bool cleanMaterialButton = false;//一键清除按钮
        public class ShaderArray : ScriptableObject
        {
            public List<Shader> shaders = new List<Shader>();
        }
        private ShaderArray shaderPools;//shader池
        private SerializedObject serObject;
        private SerializedProperty serPropery;
        private bool withoutMaterialFold = true;//折叠

        /// <summary>
        /// shader池之外的物体
        /// </summary>
        private HashSet<Material> withoutMaterial = new HashSet<Material>();//池子之外的物体

        private bool isChecking = false;//是否检测

        private GameObject inputGameObject = null;//输入物体
        private List<NameAndObj> allGameObject = new List<NameAndObj>();//所有物体
        /// <summary>
        /// 空粒子
        /// </summary>
        private List<NameAndObj> emptyParticle = new List<NameAndObj>();//
        /// <summary>
        /// 有碰撞体的物体
        /// </summary>
        private List<NameAndObj> collider = new List<NameAndObj>();//
        /// <summary>
        /// mesh设置不正确
        /// </summary>
        private List<NameAndObj> meshSet = new List<NameAndObj>();//
        /// <summary>
        /// 多材质
        /// </summary>
        private List<NameAndObj> multiMaterial = new List<NameAndObj>();//
        /// <summary>
        /// 开启预热的物体
        /// </summary>
        private List<NameAndObj> prewarm = new List<NameAndObj>();//
        /// <summary>
        /// 拥有内置材质
        /// </summary>
        private List<NameAndObj> builtInMaterial = new List<NameAndObj>();//
        /// <summary>
        /// 粒子速度过快
        /// </summary>
        private List<NameAndObj> tooRapid = new List<NameAndObj>();//

        /// <summary>
        /// 无效的mesh引用
        /// </summary>
        private List<NameAndObj> invalidMesh = new List<NameAndObj>();//
                                                                      //思路：分为多个物体。

        //这个是miss的网格和材质球
        private List<NameAndObj> missMeshAndMaterial = new List<NameAndObj>();


        private bool emptyParticleFold = true;
        private bool colliderFold = true;
        private bool meshSetFold = true;
        private bool multiMaterialFold = true;
        private bool prewarmFold = true;
        private bool builtInMaterialFold = true;
        private bool tooRapidFold = true;
        private bool invalidMeshFold = true;
        private bool missMeshAndMaterialFold = true;

        private bool buttonInvalidMesh = false;
        private bool buttonCollider = false;
        private bool buttonMeshSet = false;

        private bool buttonEmpty = false;
        private bool buttonMissMeshAndMaterial = false;

        //材质
        private string materialPath = "";
        private string materialPathAsset = "";
        private bool materialPathFold = true;
        private bool withMaterialPath = false;
        /// <summary>
        /// 材质路径检测
        /// </summary>
        private Dictionary<Material, bool> materials = new Dictionary<Material, bool>();

        //贴图  虽然更加擅长a但是要强行改变。
        private string texturePath = "";
        private string texturePathAsset = "";
        private bool texturePathFold = true;
        private bool withTexturePath = false;

        /// <summary>
        /// 图片路径检测
        /// </summary>
        private Dictionary<Texture, bool> textures = new Dictionary<Texture, bool>();

        //mesh
        private string meshPath = "";
        private string meshPathAsset = "";
        private bool meshPathFold = true;
        private bool withMeshPath = false;

        /// <summary>
        /// 模型路径检测
        /// </summary>
        private Dictionary<Mesh, bool> meshes = new Dictionary<Mesh, bool>();

        //
        private string animPath = "";
        private string animPathAsset = "";
        private bool animPathFold = true;
        private bool withAnimPath = false;
        /// <summary>
        /// 动画路径检测
        /// </summary>
        private Dictionary<Object, bool> anims = new Dictionary<Object, bool>();

        private HashSet<Object> incorrect = new HashSet<Object>();
        private bool incorrectFold = true;

        GUIStyle style = new GUIStyle();
        private Vector2 srcoll;

        public AssetsAudit()
        {
            assets = GetScriptableObjectDataArtToolBox.UpdateData(assets);
            shaderPools = ScriptableObject.CreateInstance<ShaderArray>();
            shaderPools.shaders = assets.shaderPools;
            if (shaderPools == null) shaderPools.shaders = new List<Shader>();
            serObject = new SerializedObject(shaderPools);
            serPropery = serObject.FindProperty(" shaders");

            materialPath = assets.standardPath.materialPath;
            texturePath = assets.standardPath.texturePath;
            meshPath = assets.standardPath.meshPath;
            animPath = assets.standardPath.animatorPath;

        }

        public override void InterfaceShow(bool fdf, Rect positionas)
        {
            assets = GetScriptableObjectDataArtToolBox.UpdateData(assets);
            if (shaderPools == null)
            {
                shaderPools = ScriptableObject.CreateInstance<ShaderArray>();
                shaderPools.shaders = assets.shaderPools;
                if (shaderPools == null) shaderPools.shaders = new List<Shader>();
                serObject = new SerializedObject(shaderPools);
                serPropery = serObject.FindProperty(" shaders");
            }
            assets.standardPath.materialPath = materialPath;
            assets.standardPath.texturePath = texturePath;
            assets.standardPath.meshPath = meshPath;
            assets.standardPath.animatorPath = animPath;
            assets.shaderPools = shaderPools.shaders;
            EditorUtility.SetDirty(assets);
            style.fontStyle = FontStyle.Italic;
            GUILayout.Space(10);
            UIObjField();
            if (inputGameObject == null) return;


            srcoll = GUILayout.BeginScrollView(srcoll);
            {
                PathField();
                Show();
                LabelCorrect();
            }
            GUILayout.EndScrollView();

            DealALl();

            if (buttonCollider || buttonEmpty || buttonMeshSet || buttonInvalidMesh || cleanMaterialButton || buttonMissMeshAndMaterial)
            {

                isChecking = true;
                buttonCollider = false;
                buttonEmpty = false;
                buttonMeshSet = false;
                buttonInvalidMesh = false;
                cleanMaterialButton = false;
                buttonMissMeshAndMaterial = false;

                Submit.ArtToolkitData.Instance.AddEffectOptimization(1);

            }


            if (isChecking)
            {
                allGameObject.Clear();
                GetAllChild(inputGameObject.transform);
                AllCheck();
                isChecking = false;
            }
        }

        private void AllCheck()
        {
            Submit.ArtToolkitData.Instance.AddAssetCheck(1);
            AllClear();
            foreach (var obj in allGameObject)
            {
                ParticleSystem p = GetParticle(obj.obj.gameObject); //粒子系统
                CheckAtParticle(p, obj);//粒子系统中检查

                Mesh mesh = GetMeshFilter(obj.obj.gameObject);
                CheckAtMesh(mesh, obj);//Mesh中检查

                Component colliderTemp = GetCollider(obj.obj.gameObject);
                CheckAtCollider(colliderTemp, obj);

                Renderer temp = GetRenderer(obj.obj.gameObject);
                CheckAtRenderer(temp, obj);

                Material[] mats = GetMaterials(temp);
                CheckAtMaterial(mats, obj);


                Texture[] texs = GetTextures(mats);
                CheckAtTexture(texs);

                Animator animator = GetAnimator(obj.obj.gameObject);
                CheckAtAnim(animator);
                CheckMiss(obj);


            }

        }
        private void AllClear()
        {

            collider.Clear();
            prewarm.Clear();
            emptyParticle.Clear();
            meshSet.Clear();
            multiMaterial.Clear();
            builtInMaterial.Clear();
            tooRapid.Clear();
            invalidMesh.Clear();
            cleanMaterial.Clear();
            missMeshAndMaterial.Clear();
            materials.Clear();
            withMaterialPath = false;

            textures.Clear();
            withTexturePath = false;

            meshes.Clear();
            withMeshPath = false;

            anims.Clear();
            withAnimPath = false;
            withoutMaterial.Clear();
        }
        private void DealALl()
        {
            DealEmpty();
            DealCollder();
            DealMeshSet();
            DealInvalidMesh();
            DealMissReference();
        }

        #region others
        private void LabelCorrect()
        {

            if (prewarm.Count == 0) GUILayout.Label("<color=green>预热检测：符合规范</color>", style);

            if (tooRapid.Count == 0) GUILayout.Label("<color=green>发射速度检测：符合规范</color>", style);

            if (emptyParticle.Count == 0) GUILayout.Label("<color=green>空粒子检测：符合规范</color>", style);

            if (collider.Count == 0) GUILayout.Label("<color=green>碰撞体检测：符合规范</color>", style);

            if (meshSet.Count == 0) GUILayout.Label("<color=green>MeshRender设置检测：符合规范</color>", style);

            if (multiMaterial.Count == 0) GUILayout.Label("<color=green>多材质引用检测：符合规范</color>", style);

            if (builtInMaterial.Count == 0) GUILayout.Label("<color=green>内置材质检测：符合规范</color>", style);

            if (invalidMesh.Count == 0) GUILayout.Label("<color=green>无效的Mesh检测：符合规范</color>", style);

            if (incorrect.Count == 0) GUILayout.Label("<color=green>命名检测：符合规范</color>", style);

            if (!withMaterialPath) GUILayout.Label("<color=green>材质路径检测：符合规范</color>", style);

            if (!withTexturePath) GUILayout.Label("<color=green>贴图路径检测：符合规范</color>", style);

            if (!withMeshPath) GUILayout.Label("<color=green>模型路径检测：符合规范</color>", style);

            if (!withAnimPath) GUILayout.Label("<color=green>动画路径检测：符合规范</color>", style);

            if (withoutMaterial.Count == 0) GUILayout.Label("<color=green>Shader池检测：符合规范</color>", style);

            if (cleanMaterial.Count == 0) GUILayout.Label("<color=green>无效的贴图引用检测：符合规范</color>", style);

            if (missMeshAndMaterial.Count == 0) GUILayout.Label("<color=green>丢失的材质球和网格引用检测：符合规范</color>", style);

        }

        private void PathField()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (materialPath == "") materialPath = Application.dataPath;
            materialPathAsset =
                materialPath.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "");
            GUILayout.Label("<color=grey>" + materialPathAsset + "</color>", style);
            if (GUILayout.Button("<color=white>" + "选择材质检查路径" + "</color>", style))
            {
                materialPath = EditorUtility.OpenFolderPanel("xxx", materialPath, "");
                if (!materialPath.Contains(Application.dataPath)) materialPath = "";
                if (materialPath != "") materialPath = materialPath.Substring(Application.dataPath.Length - 6);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (texturePath == "") texturePath = Application.dataPath;
            texturePathAsset =
                texturePath.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "");
            GUILayout.Label("<color=grey>" + texturePathAsset + "</color>", style);
            if (GUILayout.Button("<color=white>" + "选择贴图检查路径" + "</color>", style))
            {
                texturePath = EditorUtility.OpenFolderPanel("xxx", texturePath, "");
                if (!texturePath.Contains(Application.dataPath)) texturePath = "";
                if (texturePath != "") texturePath = texturePath.Substring(Application.dataPath.Length - 6);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (meshPath == "") meshPath = Application.dataPath;
            meshPathAsset = meshPath.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "");
            GUILayout.Label("<color=grey>" + meshPathAsset + "</color>", style);
            if (GUILayout.Button("<color=white>" + "选择模型检查路径" + "</color>", style))
            {
                meshPath = EditorUtility.OpenFolderPanel("xxx", meshPath, "");
                if (!meshPath.Contains(Application.dataPath)) meshPath = "";
                if (meshPath != "") meshPath = meshPath.Substring(Application.dataPath.Length - 6);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            if (animPath == "") animPath = Application.dataPath;
            animPathAsset = animPath.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6), "");
            GUILayout.Label("<color=grey>" + animPathAsset + "</color>", style);
            if (GUILayout.Button("<color=white>" + "选择动画检查路径" + "</color>", style))
            {
                animPath = EditorUtility.OpenFolderPanel("xxx", animPath, "");

                if (!animPath.Contains(Application.dataPath)) animPath = "";
                if (animPath != "") animPath = animPath.Substring(Application.dataPath.Length - 6);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

        }

        private void UIObjField()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            inputGameObject = (GameObject)EditorGUILayout.ObjectField(inputGameObject, typeof(GameObject), true);
            isChecking = GUILayout.Button("开始检查") || isChecking;
            if (EditorGUI.EndChangeCheck())
            {
                isChecking = true;
            }
            GUILayout.EndHorizontal();
        }

        private void GetAllChild(Transform obj, string name = "")
        {
            allGameObject.Add(new NameAndObj(obj, name + "/" + obj.name));

            for (int i = 0; i < obj.transform.childCount; ++i)
            {
                GetAllChild(obj.transform.GetChild(i), name + "/" + obj.name);
            }
        }
        private void Show()
        {
            UIPrewarm();
            UITooRapid();
            UIEmpty();
            UICollderCheck();
            UIMeshSet();
            UIMultiMaterial();
            UIBuildInMaterial();
            UIInvalidMesh();
            UICheckName();
            UICheckMaterialPath();
            UICheckTexturePath();
            UICheckMeshPath();
            UICheckAnimPath();
            UICheckShaderAtPools();
            UICleanMaterial();
            UIMissMeshAndMaterial();
        }


        #endregion

        #region get

        private Mesh GetMeshFilter(GameObject obj)
        {
            var mf = obj.GetComponent<MeshFilter>();
            if (mf) return mf.sharedMesh;
            return null;
        }

        private ParticleSystem GetParticle(GameObject obj)
        {
            return obj.GetComponent<ParticleSystem>();
        }

        private Renderer GetRenderer(GameObject obj)
        {
            return obj.GetComponent<Renderer>();
        }

        private Component GetCollider(GameObject obj)
        {
            Component res = obj.GetComponent<Collider>();
            if (res) return res;
            return obj.GetComponent<Collider2D>();
        }

        private Material[] GetMaterials(Renderer r)
        {
            List<Material> res = new List<Material>();
            if (r == null) return res.ToArray();
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;
                switch (mat.name)
                {
                    case "Default-Material":
                    case "Default-ParticleSystem":
                    case "Lit":
                    case "ParticlesUnlit":
                    case "SimpleLit":
                    case "Sprite-Lit-Default":
                    case "Sprite-Unlit-Default":
                    case "TerrainLit":
                        {
                            break;
                        }
                    default:
                        {
                            res.Add(mat);
                            break;
                        }
                }
            }
            return res.ToArray();
        }

        private Animator GetAnimator(GameObject obj)
        {
            return obj.GetComponent<Animator>();
        }

        private Texture[] GetTextures(Material[] mats)
        {
            List<Texture> res = new List<Texture>();
            if (mats == null) return res.ToArray();
            foreach (var mat in mats)
            {
                Shader shader = mat.shader;
                for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); ++i)
                {
                    if (ShaderUtil.GetPropertyType(shader, i) ==
                        ShaderUtil.ShaderPropertyType.TexEnv)
                    {
                        string propertyName = ShaderUtil.GetPropertyName(shader, i);
                        Texture tex = mat.GetTexture(propertyName);
                        if (tex) res.Add(tex);
                    }
                }
            }
            return res.ToArray();
        }

        #endregion

        #region Check

        private void CheckMiss(NameAndObj obj)
        {
            var mf = obj.obj.GetComponent<MeshFilter>();
            if (mf != null)
            {
                var so = new SerializedObject(mf);
                var mesh = so.FindProperty("m_Mesh");
                if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                {
                    if (!missMeshAndMaterial.Contains(obj))
                        missMeshAndMaterial.Add(obj);
                }
            }
            var mr = obj.obj.GetComponent<MeshRenderer>();
            if (mr != null)
            {

                var so = new SerializedObject(mr);
                var mesh = so.FindProperty("m_Materials");
                //if(mesh.)   95
                if (mesh != null && mesh.isArray)
                {
                    int length = mesh.arraySize;
                    for (int i = 0; i < length; ++i)
                    {
                        var m = mesh.GetArrayElementAtIndex(i);
                        if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                        {
                            if (!missMeshAndMaterial.Contains(obj))
                                missMeshAndMaterial.Add(obj);
                        }
                    }
                }
            }

            var pr = obj.obj.GetComponent<ParticleSystemRenderer>();
            if (pr != null)
            {


                var so = new SerializedObject(pr);
                var mesh = so.FindProperty("m_Mesh");
                if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                {
                    if (!missMeshAndMaterial.Contains(obj))
                        missMeshAndMaterial.Add(obj);
                }
                mesh = so.FindProperty("m_Materials");
                //if(mesh.)   95
                if (mesh != null && mesh.isArray)
                {
                    int length = mesh.arraySize;
                    for (int i = 0; i < length; ++i)
                    {
                        var m = mesh.GetArrayElementAtIndex(i);
                        if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                        {
                            if (!missMeshAndMaterial.Contains(obj))
                                missMeshAndMaterial.Add(obj);
                        }
                    }
                }
            }
        }

        private void CheckAtRenderer(Renderer temp, NameAndObj obj)
        {
            if (temp == null) return;
            if (temp.shadowCastingMode != ShadowCastingMode.Off || temp.receiveShadows ||
                temp.lightProbeUsage != LightProbeUsage.Off ||
                temp.reflectionProbeUsage != ReflectionProbeUsage.Off)
            {
                meshSet.Add(obj);
            }

            HashSet<Material> mats = new HashSet<Material>();
            foreach (var mat in temp.sharedMaterials)
            {
                if (mat == null) continue;
                switch (mat.name)
                {
                    case "Default-Material":
                    case "Default-ParticleSystem":
                    case "Lit":
                    case "ParticlesUnlit":
                    case "SimpleLit":
                    case "Sprite-Lit-Default":
                    case "Sprite-Unlit-Default":
                    case "TerrainLit":
                        {
                            builtInMaterial.Add(obj);
                            break;
                        }
                    default:
                        if (!mats.Contains(mat))
                        {
                            mats.Add(mat);
                        }
                        break;
                }
            }
            if (mats.Count > 1 && (!multiMaterial.Contains(obj))) multiMaterial.Add(obj);

        }
        private void CheckAtTexture(Texture[] l)
        {
            if (l == null) return;
            foreach (var tex in l)
            {
                if (!textures.ContainsKey(tex))
                {
                    textures.Add(tex, false);
                    string texpath = AssetDatabase.GetAssetPath(tex);
                    if (texpath.Replace(texturePathAsset, "") == texpath)
                    {
                        textures[tex] = true;
                        withTexturePath = true;
                    }
                }

                CheckName(tex);
            }
        }

        private void CheckAtCollider(Component isCollider, NameAndObj obj = null)
        {
            if (isCollider) collider.Add(obj);
        }

        private void CheckAtMaterial(Material[] mats, NameAndObj obj = null)
        {

            if (mats == null) return;
            foreach (var mat in mats)
            {
                if (mat == null) continue;
                CheckName(mat);
                ClearMaterial(mat);

                if (!materials.ContainsKey(mat))
                {
                    materials.Add(mat, false);
                    string matpath = AssetDatabase.GetAssetPath(mat);
                    if (matpath.Replace(materialPathAsset, "") == matpath)
                    {
                        materials[mat] = true;
                        withMaterialPath = true;
                    }
                }

                if (!shaderPools.shaders.Contains(mat.shader))
                {
                    withoutMaterial.Add(mat);
                }


            }
        }

        private void CheckAtParticle(ParticleSystem p, NameAndObj obj)
        {
            if (p == null) return;
            var em = p.emission;
            var render = p.GetComponent<ParticleSystemRenderer>(); //一定可以找到

            if (p.main.prewarm)
            {
                prewarm.Add(obj); //粒子系统的预热
            }

            if (em.enabled == false || render.enabled == false)
            {
                //一定可以找到emission和renderer
                emptyParticle.Add(obj);
            }

            if (em.rateOverTime.constant > 50)
            {
                tooRapid.Add(obj);
            }

            if (render.renderMode != ParticleSystemRenderMode.Mesh)
            {
                if (render.mesh) invalidMesh.Add(obj);
            }

            CheckAtMesh(render.mesh, obj);
        }

        private void CheckAtMesh(Mesh m, NameAndObj obj)
        {
            if (m == null) return;
            CheckName(m);
            if (!meshes.ContainsKey(m))
            {
                meshes.Add(m, false);
                string matpath = AssetDatabase.GetAssetPath(m);
                if (matpath != "Library/unity default resources" && matpath.Replace(meshPathAsset, "") == matpath)
                {
                    meshes[m] = true;
                    withMeshPath = true;
                }
            }
        }

        private void CheckAtAnim(Animator animator)
        {
            if (animator == null) return;

            CheckName(animator);
            var rac = animator.runtimeAnimatorController;
            if (rac)
            {
                var acs = animator.runtimeAnimatorController.animationClips;

                if (!anims.ContainsKey(rac))
                {
                    anims.Add(rac, false);
                    string matpath = AssetDatabase.GetAssetPath(rac);
                    if (matpath.Replace(animPathAsset, "") == matpath)
                    {
                        anims[rac] = true;
                        withAnimPath = true;
                    }
                }
                CheckName(rac);

                if (acs != null && acs.Length > 0)
                {
                    foreach (var ac in acs)
                    {
                        if (!anims.ContainsKey(ac))
                        {
                            anims.Add(ac, false);
                            string matpath = AssetDatabase.GetAssetPath(ac);
                            if (matpath.Replace(animPathAsset, "") == matpath)
                            {
                                anims[ac] = true;
                                withAnimPath = true;
                            }
                        }

                        CheckName(ac);
                    }
                }
            }
        }

        private void CheckName(Object Obj)
        {
            if (Obj == null) return;
            if (!incorrect.Contains(Obj)) return;
            foreach (var c in Obj.name)
            {
                if (c < 40 || c > 122)
                {
                    incorrect.Add(Obj);
                    return;
                }
            }

        }

        #endregion

        #region show
        private void UIPrewarm()
        {
            if (prewarm.Count > 0)
            {
                GUILayout.BeginHorizontal();
                prewarmFold = EditorGUILayout.Foldout(prewarmFold, "预热");
                GUILayout.Label(" <color=red>" + prewarm.Count + "个</color>", style);
                // buttonPrewarm=GUILayout.Button("一键修改");//未接受
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (prewarmFold)
                {
                    foreach (var p in prewarm)
                    {
                        if (!p.obj) continue;
                        GUILayout.BeginHorizontal();
                        //p.set = GUILayout.Toggle(p.set, "");
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + p.name + "</color>", style))
                        {
                            Selection.activeObject = p.obj;
                            EditorGUIUtility.PingObject(p.obj);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.Space(10);
            }
        }

        private void UIEmpty()
        {
            if (emptyParticle.Count > 0)
            {
                GUILayout.BeginHorizontal();
                emptyParticleFold = EditorGUILayout.Foldout(emptyParticleFold, "空粒子");
                GUILayout.Label(" <color=red>" + emptyParticle.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (emptyParticleFold)
                {
                    foreach (var p in emptyParticle)
                    {
                        if (!p.obj) continue;
                        GUILayout.BeginHorizontal();
                        p.set = GUILayout.Toggle(p.set, "");
                        if (GUILayout.Button("<color=white>" + p.name + "</color>", style))
                        {
                            Selection.activeObject = p.obj;
                            EditorGUIUtility.PingObject(p.obj);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                    buttonEmpty = GUILayout.Button("一键清理"); //未接受
                }
                GUILayout.Space(10);
            }
        }

        private void UICollderCheck()
        {
            if (collider.Count > 0)
            {
                GUILayout.BeginHorizontal();
                colliderFold = EditorGUILayout.Foldout(colliderFold, "碰撞体");
                GUILayout.Label(" <color=red>" + collider.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (colliderFold)
                {
                    foreach (var e in collider)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        e.set = GUILayout.Toggle(e.set, "");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                    buttonCollider = GUILayout.Button("点我一键清理"); //未接受
                }
                GUILayout.Space(10);
            }
        }

        private void UIMeshSet()
        {
            if (meshSet.Count > 0)
            {
                GUILayout.BeginHorizontal();
                {
                    meshSetFold = EditorGUILayout.Foldout(meshSetFold, "MeshRender设置");
                    GUILayout.Label(" <color=red>" + meshSet.Count + "个</color>", style);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (meshSetFold)
                {
                    foreach (var e in meshSet)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        e.set = GUILayout.Toggle(e.set, "");
                        //GUILayout.Label(e.name);
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                    buttonMeshSet = GUILayout.Button("一键修改"); //未接受
                }
                GUILayout.Space(10);
            }
        }

        private void UIMultiMaterial()
        {
            if (multiMaterial.Count > 0)
            {
                GUILayout.BeginHorizontal();
                multiMaterialFold = EditorGUILayout.Foldout(multiMaterialFold, "多材质");
                GUILayout.Label(" <color=red>" + multiMaterial.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (multiMaterialFold)
                {
                    foreach (var e in multiMaterial)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.Space(10);
            }
        }

        private void UIMissMeshAndMaterial()
        {
            if (missMeshAndMaterial.Count > 0)
            {
                GUILayout.BeginHorizontal();
                missMeshAndMaterialFold = EditorGUILayout.Foldout(missMeshAndMaterialFold, "丢失的材质球和网格");
                GUILayout.Label(" <color=red>" + missMeshAndMaterial.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (missMeshAndMaterialFold)
                {
                    foreach (var e in missMeshAndMaterial)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        e.set = GUILayout.Toggle(e.set, "");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }


                    if (GUILayout.Button("一键清空所有的丢失引用"))
                    {
                        buttonMissMeshAndMaterial = true;
                    }
                }
                GUILayout.Space(10);
            }
        }

        private void UIBuildInMaterial()
        {
            if (builtInMaterial.Count > 0)
            {
                GUILayout.BeginHorizontal();
                builtInMaterialFold = EditorGUILayout.Foldout(builtInMaterialFold, "内置材质");
                GUILayout.Label(" <color=red>" + builtInMaterial.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (builtInMaterialFold)
                {
                    foreach (var e in builtInMaterial)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.Space(10);
            }
        }

        private void UITooRapid()
        {
            if (tooRapid.Count > 0)
            {
                GUILayout.BeginHorizontal();
                tooRapidFold = EditorGUILayout.Foldout(tooRapidFold, "发射速度过快");
                GUILayout.Label(" <color=red>" + tooRapid.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (tooRapidFold)
                {
                    foreach (var e in tooRapid)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(10);
            }
        }

        private void UIInvalidMesh()
        {
            if (invalidMesh.Count > 0)
            {
                GUILayout.BeginHorizontal();
                invalidMeshFold = EditorGUILayout.Foldout(invalidMeshFold, "无效的mesh引用");
                GUILayout.Label(" <color=red>" + invalidMesh.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (invalidMeshFold)
                {
                    foreach (var e in invalidMesh)
                    {
                        if (!e.obj) continue;
                        GUILayout.BeginHorizontal();
                        e.set = GUILayout.Toggle(e.set, "");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            //Selection.activeObject = e.obj;
                            Selection.activeTransform = e.obj;
                            EditorGUIUtility.PingObject(e.obj);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                    buttonInvalidMesh = GUILayout.Button("点我一键清空");
                }

                GUILayout.Space(10);
            }
        }

        private void UICleanMaterial()
        {
            if (cleanMaterial.Count > 0)
            {
                GUILayout.BeginHorizontal();
                cleanMaterialFold = EditorGUILayout.Foldout(cleanMaterialFold, "无效的贴图引用");
                GUILayout.Label(" <color=red>" + cleanMaterial.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (cleanMaterialFold)
                {
                    foreach (var e in cleanMaterial)
                    {
                        if (!e) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + e.name + "</color>", style))
                        {
                            EditorGUIUtility.PingObject(e);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                    cleanMaterialButton = GUILayout.Button("点我一键清空(只在编辑模式下生效)");
                    if (cleanMaterialButton)
                    {
                        isChecking = true;
                        Material[] mats = new Material[cleanMaterial.Count];
                        int i = 0;
                        foreach (var e in cleanMaterial)
                        {
                            mats[i] = e;
                            i++;
                        }

                        if (EditorApplication.isPlaying == false) ClearMaterialProperties(mats);
                        else Debug.Log("运行模式下修改material的引用会出错，已经阻止");
                        AssetDatabase.Refresh();
                    }
                }

                GUILayout.Space(10);
            }
        }

        private void UICheckName()
        {
            if (incorrect.Count > 0)
            {
                GUILayout.BeginHorizontal();
                incorrectFold = EditorGUILayout.Foldout(incorrectFold, "名称不规范");
                GUILayout.Label(" <color=red>" + incorrect.Count + "个</color>", style);
                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                if (incorrectFold)
                {
                    foreach (var e in incorrect)
                    {
                        if (!e) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + e.ToString() + "</color>", style))
                        {
                            //Selection.activeObject = e.obj;
                            EditorGUIUtility.PingObject((UnityEngine.Object)e);
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.Space(5);
                }

                GUILayout.Space(10);
            }
        }

        private void UICheckMaterialPath()
        {
            if (withMaterialPath)
            {
                EditorGUILayout.BeginHorizontal();
                materialPathFold = EditorGUILayout.Foldout(materialPathFold, "材质路径检查");
                GUILayout.Label("<color=red>" + materialPathAsset + "</color>", style);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                //EditorGUILayout.BeginHorizontal();
                if (materialPathFold)
                {
                    foreach (var mat in materials)
                    {
                        if (mat.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(" ");
                            if (GUILayout.Button("<color=white>" + AssetDatabase.GetAssetPath(mat.Key) + "</color>",
                                    style))
                            {
                                //EditorUtility.
                                EditorGUIUtility.PingObject(mat.Key);
                                //AssetDatabase.OpenAsset( mat.Key);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.Space(10);
            }

            //EditorGUILayout.EndHorizontal();
        }

        private void UICheckTexturePath()
        {
            if (withTexturePath)
            {
                EditorGUILayout.BeginHorizontal();
                texturePathFold = EditorGUILayout.Foldout(texturePathFold, "贴图路径检查");
                GUILayout.Label("<color=red>" + texturePathAsset + "</color>", style);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                //EditorGUILayout.BeginHorizontal();

                if (texturePathFold)
                {
                    foreach (var mat in textures)
                    {
                        if (mat.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(" ");
                            if (GUILayout.Button("<color=white>" + AssetDatabase.GetAssetPath(mat.Key) + "</color>",
                                    style))
                            {
                                //EditorUtility.
                                EditorGUIUtility.PingObject(mat.Key);
                                //.OpenAsset( mat.Key);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.Space(10);

                //EditorGUILayout.EndHorizontal();
            }
        }

        private void UICheckMeshPath()
        {
            if (withMeshPath)
            {
                EditorGUILayout.BeginHorizontal();
                meshPathFold = EditorGUILayout.Foldout(meshPathFold, "模型路径检查");
                GUILayout.Label("<color=red>" + meshPathAsset + "</color>", style);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (meshPathFold)
                {
                    foreach (var mat in meshes)
                    {
                        if (mat.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(" ");
                            if (GUILayout.Button("<color=white>" + AssetDatabase.GetAssetPath(mat.Key) + "</color>",
                                    style))
                            {
                                //EditorUtility.
                                EditorGUIUtility.PingObject(mat.Key);
                                //.OpenAsset( mat.Key);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                //EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);
            }
        }

        private void UICheckAnimPath()
        {
            if (withAnimPath)
            {
                EditorGUILayout.BeginHorizontal();
                animPathFold = EditorGUILayout.Foldout(animPathFold, "动画路径检查");
                // if ( animPath == "")  animPath = Application.dataPath;
                //  animPathAsset =  animPath.Replace(Application.dataPath.Substring(0, Application.dataPath.Length - 6),"");
                // if ( withAnimPath)
                // {
                //     GUILayout.Label("<color=red>"+ animPathAsset+"</color>", style);
                // }
                // else
                // {
                //     GUILayout.Label("<color=green>"+ animPathAsset+"</color>", style);
                // }
                // if (GUILayout.Button("<color=white>"+"选择路径"+"</color>",style))
                // {
                //      animPath = EditorUtility.OpenFolderPanel("xxx",  animPath, "");
                // }
                GUILayout.Label("<color=red>" + animPathAsset + "</color>", style);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);

                if (animPathFold)
                {
                    //EditorGUILayout.BeginHorizontal();



                    foreach (var mat in anims)
                    {
                        if (mat.Value)
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(" ");
                            if (GUILayout.Button(
                                    "<color=white>" + AssetDatabase.GetAssetPath((UnityEngine.Object)mat.Key) +
                                    "</color>",
                                    style))
                            {
                                //EditorUtility.
                                EditorGUIUtility.PingObject((UnityEngine.Object)mat.Key);
                                //.OpenAsset( mat.Key);
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
                GUILayout.Space(10);

                //EditorGUILayout.EndHorizontal();
            }
        }

        private void UICheckShaderAtPools()
        {
            if (serPropery != null) EditorGUILayout.PropertyField(serPropery, new GUIContent("请输入shader池"), true);
            serObject.ApplyModifiedProperties();

            if (withoutMaterial.Count > 0)
            {
                withoutMaterialFold = EditorGUILayout.Foldout(withoutMaterialFold, "不在ShaderPools的Material");
                if (withoutMaterialFold)
                {
                    foreach (var m in withoutMaterial)
                    {
                        if (!m) continue;
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("");
                        if (GUILayout.Button("<color=white>" + m.name + "</color>", style))
                        {
                            EditorGUIUtility.PingObject(m);
                        }

                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.Space(10);
            }
        }

        #endregion

        #region deal

        private void DealEmpty()
        {
            if (buttonEmpty)
            {
                foreach (var e in emptyParticle)
                {
                    if (e.obj && e.set)
                    {
                        Object.DestroyImmediate(e.obj.GetComponent<ParticleSystem>(), true);
                        Object.DestroyImmediate(e.obj.GetComponent<ParticleSystemRenderer>(), true);
                    }
                }

                //buttonEmpty = false;
            }
        }

        private void DealCollder()
        {
            if (buttonCollider)
            {
                foreach (var e in collider)
                {
                    if (e.obj && e.set)
                    {
                        var c = e.obj.GetComponent<Collider>();
                        var c2 = e.obj.GetComponent<Collider2D>();
                        Object.DestroyImmediate(c, true);
                        Object.DestroyImmediate(c2, true);
                    }
                }

                //buttonCollider = false;
            }
        }

        private void DealMeshSet()
        {
            if (buttonMeshSet)
            {
                foreach (var e in meshSet)
                {
                    if (e.obj && e.set)
                    {
                        var m = e.obj.GetComponent<Renderer>();
                        m.shadowCastingMode = ShadowCastingMode.Off;
                        m.receiveShadows = false;
                        m.lightProbeUsage = LightProbeUsage.Off;
                        m.reflectionProbeUsage = ReflectionProbeUsage.Off;
                    }
                }
                //buttonMeshSet = false;
            }
        }

        private void DealInvalidMesh()
        {
            if (buttonInvalidMesh)
            {
                foreach (var e in invalidMesh)
                {
                    if (e.obj)
                        e.obj.GetComponent<ParticleSystemRenderer>().mesh = null;
                }

                //buttonInvalidMesh = false;
            }
        }


        private void DealMissReference()
        {
            if (buttonMissMeshAndMaterial)
            {

                foreach (var e in missMeshAndMaterial)
                {
                    if (e.obj)
                    {
                        var mf = e.obj.GetComponent<MeshFilter>();
                        if (mf != null)
                        {
                            var so = new SerializedObject(mf);
                            var mesh = so.FindProperty("m_Mesh");
                            if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                            {
                                mesh.objectReferenceInstanceIDValue = 0;
                            }
                            so.ApplyModifiedProperties();
                        }

                        var mr = e.obj.GetComponent<MeshRenderer>();
                        if (mr != null)
                        {

                            var so = new SerializedObject(mr);
                            var mesh = so.FindProperty("m_Materials");
                            //if(mesh.)   95
                            if (mesh != null && mesh.isArray)
                            {
                                int length = mesh.arraySize;
                                for (int i = 0; i < length; ++i)
                                {
                                    var m = mesh.GetArrayElementAtIndex(i);
                                    if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                                    {
                                        m.objectReferenceInstanceIDValue = 0;
                                    }
                                }
                            }
                            so.ApplyModifiedProperties();
                        }

                        var pr = e.obj.GetComponent<ParticleSystemRenderer>();
                        if (pr != null)
                        {


                            var so = new SerializedObject(pr);
                            var mesh = so.FindProperty("m_Mesh");
                            if (mesh.objectReferenceValue == null && mesh.objectReferenceInstanceIDValue != 0)
                            {

                                mesh.objectReferenceInstanceIDValue = 0;
                            }

                            mesh = so.FindProperty("m_Materials");
                            //if(mesh.)   95
                            if (mesh != null && mesh.isArray)
                            {
                                int length = mesh.arraySize;
                                for (int i = 0; i < length; ++i)
                                {
                                    var m = mesh.GetArrayElementAtIndex(i);
                                    if (m.objectReferenceValue == null && m.objectReferenceInstanceIDValue != 0)
                                    {

                                        m.objectReferenceInstanceIDValue = 0;
                                    }
                                }
                            }
                            so.ApplyModifiedProperties();
                        }

                        EditorUtility.SetDirty(e.obj.gameObject);

                    }
                }

                //buttonMissMeshAndMaterial = false;
            }
        }


        private void ClearMaterial(Material mat)
        {
            if (mat == null) return;
            if (cleanMaterial.Contains(mat)) return;

            SerializedObject psSource = new SerializedObject(mat);
            SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
            SerializedProperty property = emissionProperty.FindPropertyRelative("m_TexEnvs");
            for (int j = property.arraySize - 1; j >= 0; j--)
            {
                string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                if (!mat.HasProperty(propertyName))
                {
                    cleanMaterial.Add(mat);
                    break;
                }
            }
        }

        private static void ClearMaterialProperties(Material[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
            {
                EditorUtility.DisplayProgressBar("清理中...", objs[i].name, i / objs.Length);
                Material mat = objs[i];
                if (mat)
                {
                    SerializedObject psSource = new SerializedObject(mat);
                    SerializedProperty emissionProperty = psSource.FindProperty("m_SavedProperties");
                    SerializedProperty texEnvs = emissionProperty.FindPropertyRelative("m_TexEnvs");
                    if (CleanMaterialSerializedProperty(texEnvs, mat))
                    {
                        //Debug.Log("Find and clean useless texture properties" + mat.name);
                    }

                    psSource.ApplyModifiedProperties();
                    EditorUtility.SetDirty(mat);
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        private static bool CleanMaterialSerializedProperty(SerializedProperty property, Material mat)
        {
            bool res = false;
            for (int j = property.arraySize - 1; j >= 0; j--)
            {
                string propertyName = property.GetArrayElementAtIndex(j).FindPropertyRelative("first").stringValue;
                if (!mat.HasProperty(propertyName))
                {
                    if (propertyName == "_MainTex")
                    {
                        if (property.GetArrayElementAtIndex(j).FindPropertyRelative("second")
                                .FindPropertyRelative("m_Texture").objectReferenceValue != null)
                        {
                            property.GetArrayElementAtIndex(j).FindPropertyRelative("second")
                                .FindPropertyRelative("m_Texture").objectReferenceValue = null;
                            res = true;
                        }
                    }
                    else
                    {
                        property.DeleteArrayElementAtIndex(j);
                        //Debug.Log("Delete legacy property in serialized object:" + propertyName);
                        res = true;
                    }
                }
            }
            return res;
        }
        #endregion

        private class CheckWindow : EditorWindow
        {
            private static CheckWindow instance;
            private GameObject[] gameObjects;
            private bool[] checkAccess;
            private ReorderableList reorderableList;
            public void OnEnable()
            {
                reorderableList = new ReorderableList(gameObjects, typeof(GameObject), false, false, false, false);
                reorderableList.drawElementCallback = (Rect rect, int index, bool a, bool b) =>
                {
                    EditorGUI.ObjectField(new Rect(rect.x, rect.y, rect.width * 0.7f, rect.height), gameObjects[index], typeof(GameObject), false);
                    if (checkAccess[index])
                    {
                        GUI.Label(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height), "检测成功");
                    }
                    else
                    {
                        if (GUI.Button(new Rect(rect.x + rect.width * 0.7f, rect.y, rect.width * 0.3f, rect.height),
                                "检测不通过"))
                        {
                            ShortCutKey.OpenTool17();
                            WindowControl.Instance.baseShow.ToolsInterfaceShow(null, new Rect(0, 0, 0, 0));

                            var assetsAudit =
                                WindowControl.Instance.baseShow.selectTools[(int)WindowControl.Instance.baseShow.tool] as AssetsAudit
                            ;
                            assetsAudit.inputGameObject = gameObjects[index];
                            assetsAudit.isChecking = true;
                            //inputGameObject=
                        }
                    }
                };
            }

            private Vector2 v2;

            private void OnGUI()
            {
                v2 = EditorGUILayout.BeginScrollView(v2);
                reorderableList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }


            //[MenuItem("Assets/SVN/Commit")]
            //private static void Commit()
            //{

            //    Object[] objs = Selection.objects;

            //    List<GameObject> gameObjects = new List<GameObject>();
            //    StringBuilder sb0 = new StringBuilder();
            //    foreach (var obj in objs)
            //    {
            //        Debug.Log(obj);
            //        if (obj == null) continue;
            //        if (obj is DefaultAsset &&
            //            Directory.Exists(AssetDatabase.GetAssetPath(obj)))
            //        {
            //            var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { AssetDatabase.GetAssetPath(obj) });

            //            foreach (var guid in guids)
            //            {
            //                GameObject g =
            //                    AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            //                if (g != null && !gameObjects.Contains(g)) gameObjects.Add(g);
            //            }

            //        }
            //        else if (obj is GameObject)
            //        {
            //            gameObjects.Add(obj as GameObject);
            //        }

            //        if (sb0.Length == 0)
            //        {
            //            sb0.Append(AssetDatabase.GetAssetPath(obj));
            //        }
            //        else
            //        {

            //            sb0.Append("*");
            //            sb0.Append(AssetDatabase.GetAssetPath(obj));
            //        }

            //    }

            //    if (gameObjects.Count > 0)
            //    {

            //        if (instance == null) instance = GetWindow<CheckWindow>();
            //        instance.gameObjects = gameObjects.ToArray();
            //        instance.checkAccess = new bool[gameObjects.Count];
            //        instance.OnEnable();
            //        instance.Show();
            //    }

            //    StringBuilder sb = new StringBuilder();
            //    for (int i = 0; i < instance.checkAccess.Length; ++i)
            //    {
            //        instance.checkAccess[i] = AssetCheck(instance.gameObjects[i]);
            //        if (i != 0)
            //        {
            //            sb.Append(AssetDatabase.GetAssetPath(instance.gameObjects[i]));
            //        }
            //        else
            //        {
            //            sb.Append("*");
            //            sb.Append(AssetDatabase.GetAssetPath(instance.gameObjects[i]));

            //        }

            //    }
            //    if (sb0.Length > 0)
            //    {
            //        Process.Start("TortoiseProc.exe", "/command:commit /path:" + sb0.ToString() + " /closeonend:0");
            //    }

            //    if (sb.Length > 0)
            //    {

            //        //Process.Start("TortoiseProc.exe", "/command:commit /path:" + sb.ToString() + " /closeonend:0");
            //    }






            //}


            //[MenuItem("Assets/SVN/Add")]
            //private static void Add()
            //{

            //    Object[] objs = Selection.objects;

            //    StringBuilder sb0 = new StringBuilder();
            //    foreach (var obj in objs)
            //    {
            //        if (obj == null) continue;

            //        if (sb0.Length == 0)
            //        {
            //            sb0.Append(AssetDatabase.GetAssetPath(obj));

            //        }
            //        else
            //        {

            //            sb0.Append("*");
            //            sb0.Append(AssetDatabase.GetAssetPath(obj));
            //        }

            //    }
            //    if (sb0.Length > 0)
            //    {
            //        Process.Start("TortoiseProc.exe", "/command:add /path:" + sb0.ToString() + " /closeonend:0");
            //    }






            //}

            [MenuItem("Assets/美术工具箱/资产审核")]
            private static void Check()
            {
                Object[] objs = Selection.objects;

                List<GameObject> gameObjects = new List<GameObject>();
                foreach (var obj in objs)
                {
                    //Debug.Log(obj);
                    if (obj == null) continue;
                    if (obj is DefaultAsset &&
                        Directory.Exists(AssetDatabase.GetAssetPath(obj)))
                    {
                        var guids = AssetDatabase.FindAssets("t:Prefab", new string[] { AssetDatabase.GetAssetPath(obj) });

                        foreach (var guid in guids)
                        {
                            GameObject g =
                                AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                            if (g != null && !gameObjects.Contains(g)) gameObjects.Add(g);
                        }

                    }
                    else if (obj is GameObject)
                    {
                        gameObjects.Add(obj as GameObject);
                    }

                }

                if (gameObjects.Count > 0)
                {

                    if (instance == null) instance = GetWindow<CheckWindow>();
                    instance.gameObjects = gameObjects.ToArray();
                    instance.checkAccess = new bool[gameObjects.Count];
                    instance.OnEnable();
                    instance.Show();
                    for (int i = 0; i < instance.checkAccess.Length; ++i)
                    {
                        instance.checkAccess[i] = AssetCheck(instance.gameObjects[i]);
                    }
                }


            }
        }


    }
}
