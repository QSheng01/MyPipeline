using CustomShaderEditor;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// https://zhuanlan.zhihu.com/p/97256929
/// </summary>
public class MaterialCustomLightDirDrawer : MaterialPropertyDrawer
{
    private readonly string groupName;
    private readonly List<string> enableKeyword;
    float height = 16;
    bool isEditor = false;

    bool starEditor = true;
    GameObject selectGameObj;
    public Quaternion rot = Quaternion.identity;

    MaterialProperty m_prop;

    public MaterialCustomLightDirDrawer(string groupName)
    {
        //this.groupName = groupName;
        CustomShaderUtils.TrytoGetEnableKeyword(groupName, out this.groupName, ref this.enableKeyword);
    }

    public MaterialCustomLightDirDrawer()
    {
    }

    //判断是否为Vector类型
    private bool IsPropertyTypeSuitable(MaterialProperty prop)
    {
        return prop.type == MaterialProperty.PropType.Vector;
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        //如果不是Vector类型，则把unity的默认警告框的高度40
        if (!IsPropertyTypeSuitable(prop))
        {
            return 40f;
        }

        height = EditorGUI.GetPropertyHeight(SerializedPropertyType.Vector3, new GUIContent(label));
        return height + 4;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        bool show = editor.IsHeaderShowInInspector(groupName);
        show &= editor.IsShowInInspectorByKeyword(enableKeyword);
        if (show)
        {
            //如果不是Vector类型，则显示一个警告框
            if (!IsPropertyTypeSuitable(prop))
            {
                GUIContent c = EditorGUIUtility.IconContent("console.erroricon",
                    "LightDir used on a non-Vector property: " + prop.name);
                EditorGUI.LabelField(position, c, EditorStyles.helpBox);
                return;
            }

            EditorGUI.BeginChangeCheck();

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 0f;

            Color oldColor = GUI.color;
            if (isEditor) GUI.color = Color.green;

            //绘制属性
            Rect VectorRect = new Rect(position)
            {
                width = position.width - 10
            };
            
            Vector3 value = new Vector3(prop.vectorValue.x, prop.vectorValue.y, prop.vectorValue.z);
            value = EditorGUI.Vector3Field(VectorRect, label, value);

            //绘制开关
            Rect TogglegRect = new Rect(position)
            {
                x = position.xMax - 85f,
                //y = (height > 16) ? position.y + 16 : position.y,
                y = position.y + 25,
                width = 85f,
                height = 16
            };
            isEditor = GUI.Toggle(TogglegRect, isEditor, "修改光照方向", "Button");

            if (isEditor)
            {
                if (starEditor)
                {
                    m_prop = prop;
                    InitSenceGUI(value);
                }
            }
            else
            {
                if (!starEditor)
                {
                    ClearSenceGUI();
                }
            }
            
            Rect ApplyLightDirRect = new Rect(position) 
            {
                x = position.xMax - 180f,
                //y = (height > 16) ? position.y + 16 : position.y,
                y = position.y + 25,
                width = 85f,
                height = 16
            };

            bool isApplyMainLightDir = GUI.Toggle(ApplyLightDirRect, isEditor, "应用主灯方向", "Button"); // 应用当前主灯光方向
            if (isApplyMainLightDir)
            {
                if (RenderSettings.sun != null)
                {
                    value = -RenderSettings.sun.transform.forward;
                }
            }

            isApplyMainLightDir = false;
            
            GUI.color = oldColor;
            EditorGUIUtility.labelWidth = oldLabelWidth;
            
            GUILayout.Space(-this.GetPropertyHeight(prop, label, editor) + 45);
            
            if (EditorGUI.EndChangeCheck())
            {
                prop.vectorValue = new Vector4(value.x, value.y, value.z);
            }
        }
        else
        {
            GUILayout.Space(-this.GetPropertyHeight(prop, label, editor) - 2);
        }
    }

    void InitSenceGUI(Vector3 value)
    {
        Tools.current = Tool.None;
        selectGameObj = Selection.activeGameObject;
        //Vector3 worldDir = selectGameObj.transform.rotation * value;
        Vector3 worldDir = value;
        rot = Quaternion.FromToRotation(Vector3.forward, worldDir);
        SceneView.duringSceneGui += OnSceneGUI;
        starEditor = false;
    }

    void ClearSenceGUI()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        m_prop = null;
        selectGameObj = null;
        starEditor = true;
    }

    void OnSceneGUI(SceneView senceView)
    {
        if (Selection.activeGameObject != selectGameObj)
        {
            ClearSenceGUI();
            isEditor = false;
            return;
        }

        Vector3 pos = selectGameObj.transform.position;

        rot = Handles.RotationHandle(rot, pos);
        //Vector3 newlocalDir = Quaternion.Inverse(selectGameObj.transform.rotation) *  rot * Vector3.forward;
        Vector3 newDir = rot * Vector3.forward;

        m_prop.vectorValue = new Vector4(newDir.x, newDir.y, newDir.z);
        
        Handles.color = Color.green;
        //Handles.ConeHandleCap(0, pos, rot, HandleUtility.GetHandleSize(pos), EventType.Repaint);
        var rrr = rot;
        rrr.SetLookRotation(rot * -Vector3.forward);
        Handles.ArrowHandleCap(0, pos, rrr, HandleUtility.GetHandleSize(pos), EventType.Repaint);
    }
}