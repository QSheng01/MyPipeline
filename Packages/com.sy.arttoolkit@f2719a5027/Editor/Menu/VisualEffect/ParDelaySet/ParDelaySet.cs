
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SYArtToolkit
{

    public class ParDelaySet : ToolInterface
    {
        public float delayoffset = 0;
        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            GUILayout.Label("将当前多选的粒子系统“StartDelay”在原基础上添加以下值\n（允许负值  但请注意粒子系统最低延迟为0）");
            delayoffset = EditorGUILayout.FloatField("StartDelay增量", delayoffset);

            if (GUILayout.Button("执行"))
            {
                GameObject[] go = Selection.gameObjects; //选了几个就记几个
                List<ParticleSystem> pars = new List<ParticleSystem>(); //选了几个就记几个
                for (int i = 0; i < go.Length; i++)
                {
                    if (go[i].GetComponent<ParticleSystem>())
                    {
                        pars.Add(go[i].GetComponent<ParticleSystem>());

                    }
                }

                Submit.ArtToolkitData.Instance.AddBatchModify(pars.Count);
                for (int j = 0; j < pars.Count; j++)
                {
                    ParticleSystem.MainModule main;
                    main = pars[j].main;
                    main.startDelay = new ParticleSystem.MinMaxCurve(main.startDelay.constant + delayoffset);
                }
            }

        }

    }
}