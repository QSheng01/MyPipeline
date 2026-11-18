using EffectsPostProcess.Scripts.Controllers;
using UnityEditor;

namespace EffectsPostProcess.Scripts.ControllerEditors
{
    public partial class EffectsPostProcessControllerEditor
    {
        private bool _ColorAdjustmentGroup   = false;

        private SerializedProperty colorFilter = null;
        private SerializedProperty contrast = null;
        private SerializedProperty saturation = null;
        private SerializedProperty brightness = null;

        private void InitLightnessProperties()
        {
            colorFilter = serializedObject.FindProperty("colorFilter");
            saturation= serializedObject.FindProperty("saturation");
            contrast = serializedObject.FindProperty("contrast");
            brightness = serializedObject.FindProperty("brightness");
        }

        private void ColorAdjustmentComp()
        {
            if ( ! ( (EffectsPostProcessController)target ).selectedRenderersList.Contains(Controllers.EffectsPostProcess.ColorAdjustment) )
            {//目标对象是EffectsPostProcessController类型且其中的selectedRenderersList包含ColorAdjustment才能执行下一步
                return;
            }

            //EditorGUILayout.BeginFoldoutHeaderGroup方法开启一个可折叠组标题。_ColorAdjustmentGroup参数 tracking是否展开该组,作为下次调用的参考状态
            this._ColorAdjustmentGroup = EditorGUILayout.BeginFoldoutHeaderGroup(_ColorAdjustmentGroup, "颜色调整（ColorAdjustment）");
            //可以控制绘制GUI元素时的缩进量，让后续在该组下绘制的GUI元素相对缩进一级，这行代码把缩进级别增加1
            EditorGUI.indentLevel++;

            if (_ColorAdjustmentGroup)
            {
                EditorGUILayout.BeginHorizontal();

                OptionButton(Controllers.EffectsPostProcess.ColorAdjustment);

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(colorFilter);
                EditorGUILayout.PropertyField(contrast);
                EditorGUILayout.PropertyField(saturation);
                EditorGUILayout.PropertyField(brightness);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space(20);

        }
    }
}