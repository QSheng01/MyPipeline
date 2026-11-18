using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class ParticleExpand
    {
        [MenuItem("CONTEXT/ParticleSystem/一键设置自定义顶点数据相关选项（CustomData1、2+UV2）")] //第一个CONTEXT 是固定的 第二个是组件 第三个是方法命名
        static public void CustomData002(MenuCommand cmd) //在其他组件上扩展，MenuCommand是获取其他组件，这个值是unity传的 根据组件类型获取对应组件
        {
            //Material mat;


            ParticleSystem par = (ParticleSystem)cmd.context; //强制转型从MenuCommand.context获得组件
            //从cmd.context中获取ParticleSystem组件。cmd.context包含与菜单关联的对象,这里是ParticleSystem
            ParticleSystemRenderer particleSystemRenderer = par.gameObject.GetComponent<ParticleSystemRenderer>();
            //通过ParticleSystem组件获取其GameObject,然后获取ParticleSystemRenderer组件
            ParticleSystem.CustomDataModule customData = par.customData; //自定义顶点数据流模块
            //通过ParticleSystem组件直接获取其CustomData模块
            customData.enabled = true;
            // 启用CustomData模块
            customData.SetMode(ParticleSystemCustomData.Custom1, ParticleSystemCustomDataMode.Vector);
            customData.SetMode(ParticleSystemCustomData.Custom2, ParticleSystemCustomDataMode.Vector);
            // 设置CustomData通道为Vector模式
            List<ParticleSystemVertexStream> vertexStream = new List<ParticleSystemVertexStream>();
            // 创建一个带有顶点流列表的List
            vertexStream.Add(ParticleSystemVertexStream.Position);
            vertexStream.Add(ParticleSystemVertexStream.Normal);
            vertexStream.Add(ParticleSystemVertexStream.Color);
            vertexStream.Add(ParticleSystemVertexStream.UV);
            vertexStream.Add(ParticleSystemVertexStream.UV2);
            vertexStream.Add(ParticleSystemVertexStream.Custom1XYZW);
            vertexStream.Add(ParticleSystemVertexStream.Custom2XYZW);
            //vertexStream.Add(ParticleSystemVertexStream.UV3);
            // 添加Position、Normal、Color、UV、UV2、UV3、Custorm1XYZW、Custom2XYZW顶点流
            particleSystemRenderer.SetActiveVertexStreams(vertexStream);
            // 启用渲染器的vertexStream列表中的所有顶点流
            Submit.ArtToolkitData.Instance.AddConvenient(1);
        }
    }
}