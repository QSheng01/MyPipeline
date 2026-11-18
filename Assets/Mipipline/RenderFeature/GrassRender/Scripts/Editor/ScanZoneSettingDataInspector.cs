using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Environment;


namespace UnityEngine.Rendering.Environment
{
    public class LabelAttribute : PropertyAttribute
    {
        private string name = "";

        public string Name { get { return name; } }

        public LabelAttribute(string name)
        {
            this.name = name;
        }
    }
    [VolumeComponentEditor(typeof(ScanZoneSettingData))]
    public class ScanZoneSettingDataInspector : VolumeComponentEditor
    {
        [Label("色相")] protected SerializedDataParameter m_Hue;
        [Label("饱和度")] protected SerializedDataParameter m_Saturation;
        [Label("明度")] protected SerializedDataParameter m_Value;
        [Label("对比度")] protected SerializedDataParameter m_Contrast;
        protected SerializedDataParameter m_Brightness;

        //public override bool hasAdvancedMode => true;
        public override void OnEnable()
        {
            var o = new PropertyFetcher<ScanZoneSettingData>(serializedObject);
            m_Hue = Unpack(o.Find(x => x.Hue));
            m_Saturation = Unpack(o.Find(x => x.Saturation));
            m_Value = Unpack(o.Find(x => x.Value));
            m_Contrast = Unpack(o.Find(x => x.Contrast));
            m_Brightness = Unpack(o.Find(x => x.Brightness));
            base.OnEnable();
        }
        public override void OnDisable()
        {

        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Hue);
            PropertyField(m_Saturation);
            PropertyField(m_Value);
            PropertyField(m_Contrast);
            PropertyField(m_Brightness);
        }
    }
}
