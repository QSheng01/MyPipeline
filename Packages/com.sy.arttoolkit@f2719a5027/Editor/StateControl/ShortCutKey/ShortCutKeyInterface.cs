
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;


namespace SYArtToolkit
{

    public class ShortCutKeyInterface : ToolInterface
    {
        private ReorderableList reorderableList;
        private List<SingleShortCutKeySet> shortCutKeySets;
        private HashSet<String> availableShortcutIds;

        public ShortCutKeyInterface()
        {
            availableShortcutIds = new HashSet<string>();
            var availableProfileIds = ShortcutManager.instance.GetAvailableShortcutIds();
            foreach (var availableProfileId in availableProfileIds)
            {
                var s = ShortcutManager.instance.GetShortcutBinding(availableProfileId).ToString();
                if (s != "" && !availableShortcutIds.Contains(s))
                    availableShortcutIds.Add(s);
            }
            shortCutKeySets = new List<SingleShortCutKeySet>();
            if (shortCutKeyIds.Length != names.Length) Debug.LogError("长度不一样");
            for (int i = 0; i < shortCutKeyIds.Length; ++i)
            {
                shortCutKeySets.Add(new SingleShortCutKeySet(names[i],
                    ShortcutManager.instance.GetShortcutBinding(shortCutKeyIds[i]).ToString(), shortCutKeyIds[i]));
            }

            reorderableList = new ReorderableList(shortCutKeySets, typeof(SingleShortCutKeySet),
                    true, true, false, false);

            reorderableList.drawHeaderCallback = (Rect rect) => { GUI.Label(rect, "快捷键设置---点击快捷键，按BackSpace取消快捷键"); };
            reorderableList.drawElementCallback = DrawElementCallBack;
            reorderableList.elementHeightCallback = ElementHeightCallBack;
            /////////
            invokes = new List<(string id, ShortcutBinding binding)>();
        }

        private List<(string id, ShortcutBinding binding)> invokes;
        void DrawElementCallBack(Rect rect, int index, bool isActive, bool isFocused)
        {
            GUI.Label(rect, shortCutKeySets[index].name);
            GUI.Label(new Rect(rect.x + rect.width - 100, rect.y, 100, rect.height), shortCutKeySets[index].shortKeyCurrent);
            if (isActive)
            {
                var tempRes = GetCode();
                if (tempRes.succeed)
                {
                    if (tempRes.binding.ToString() == "")
                    {
                        ShortcutManager.instance.RebindShortcut(shortCutKeySets[index].shortCutKeyId,
                            tempRes.binding);
                        availableShortcutIds.Remove(shortCutKeySets[index].shortKeyCurrent);
                        shortCutKeySets[index].shortKeyCurrent = tempRes.binding.ToString();
                    }
                    else if (!availableShortcutIds.Contains(tempRes.binding.ToString()))
                    {
                        ShortcutManager.instance.RebindShortcut(shortCutKeySets[index].shortCutKeyId,
                            tempRes.binding);
                        availableShortcutIds.Add(tempRes.binding.ToString());
                        shortCutKeySets[index].shortKeyCurrent = tempRes.binding.ToString();
                    }
                    else
                    {
                        Debug.LogWarning("快捷键不可用");
                    }
                }
            }
        }

        float ElementHeightCallBack(int index)
        {
            return 20;
        }
        (bool succeed, ShortcutBinding binding) GetCode()
        {
            (bool succeed, ShortcutBinding binding) res;
            res.succeed = false;
            res.binding = new ShortcutBinding();
            if (Event.current.type == EventType.KeyUp)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.Backspace:
                        res.succeed = true;
                        break;
                    case KeyCode.Q:
                    case KeyCode.W:
                    case KeyCode.E:
                    case KeyCode.R:
                    case KeyCode.T:
                    case KeyCode.Y:
                    case KeyCode.U:
                    case KeyCode.I:
                    case KeyCode.O:
                    case KeyCode.P:
                    case KeyCode.A:
                    case KeyCode.S:
                    case KeyCode.D:
                    case KeyCode.F:
                    case KeyCode.G:
                    case KeyCode.H:
                    case KeyCode.J:
                    case KeyCode.K:
                    case KeyCode.L:
                    case KeyCode.Z:
                    case KeyCode.X:
                    case KeyCode.C:
                    case KeyCode.V:
                    case KeyCode.B:
                    case KeyCode.N:
                    case KeyCode.M:
                    case KeyCode.Alpha0:
                    case KeyCode.Alpha1:
                    case KeyCode.Alpha2:
                    case KeyCode.Alpha3:
                    case KeyCode.Alpha4:
                    case KeyCode.Alpha5:
                    case KeyCode.Alpha6:
                    case KeyCode.Alpha7:
                    case KeyCode.Alpha8:
                    case KeyCode.Alpha9:
                    case KeyCode.UpArrow:
                    case KeyCode.DownArrow:
                    case KeyCode.LeftArrow:
                    case KeyCode.RightArrow:
                    case KeyCode.F1:
                    case KeyCode.F2:
                    case KeyCode.F3:
                    case KeyCode.F4:
                    case KeyCode.F5:
                    case KeyCode.F6:
                    case KeyCode.F7:
                    case KeyCode.F8:
                    case KeyCode.F9:
                        ShortcutModifiers tempShortcutModifiers = 0;
                        if (Event.current.alt)
                            tempShortcutModifiers |= ShortcutModifiers.Alt;
                        if (Event.current.shift)
                            tempShortcutModifiers |= ShortcutModifiers.Shift;
                        if (Event.current.control)
                            tempShortcutModifiers |= ShortcutModifiers.Action;
                        res.succeed = true;
                        res.binding = new ShortcutBinding(new KeyCombination(Event.current.keyCode, tempShortcutModifiers));
                        break;
                }
            }
            return res;
        }

        private Vector2 scroll;
        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            reorderableList.DoLayoutList();
            EditorGUILayout.Space(200);
            //egg.InputPassword();
            GUILayout.EndScrollView();
        }

        private class SingleShortCutKeySet
        {
            public string name;
            public string shortKeyCurrent;
            public string shortCutKeyId;

            public SingleShortCutKeySet(string na, string current, string id)
            {
                name = na;
                shortKeyCurrent = current;
                shortCutKeyId = id;
            }
        }

        private string[] names = new string[]
        {
            "打开(关闭)美术工具箱",
            "快速打开文件夹快捷键",
            "快速打开场景快捷键",
            "资产引用查找快捷键",
            "烘焙CubeMap快捷键",
            "贴图合并快捷键",
            "渐变生成快捷键",
            "序列拆分快捷键",
            "贴图缩放快捷键",
            "通道一键设置快捷键",
            "贴图编辑快捷键",
            "格式转变快捷键",
            "噪音生成快捷键",
            "通道合并快捷键",
            "贴图裁剪快捷键",
            "贴图模糊快捷键",
            "批量修改粒子快捷键",
            "批量设置delay快捷键",
            "清理无用材质依赖快捷键",
            "性能检测快捷键",
            "资产审核快捷键",
            "渲染检测快捷键",
            "新建材质快捷键",
            "特效优化快捷键",
            "Shader替换快捷键",
            "查找预制体快捷键",
            "更新节点快捷键"
        };

        private string[] shortCutKeyIds = new string[]
        {
            "Main Menu/"+ShortCutKey.OpenWindowName,
            "Main Menu/"+ShortCutKey.RapidFileName,
            "Main Menu/"+ShortCutKey.RapidSceneName,
            "Main Menu/"+ShortCutKey.AssetRefSearchName,
            "Main Menu/"+ShortCutKey.RenderCubemap,
            "Main Menu/"+ShortCutKey.MergeTextureName ,
            "Main Menu/"+ShortCutKey.RampGenerateName ,
            "Main Menu/"+ShortCutKey.SplitFrameName ,
            "Main Menu/"+ShortCutKey.ModifySizeName ,
            "Main Menu/"+ShortCutKey.ChannelSetName ,
            "Main Menu/"+ShortCutKey.TextureEditorName ,
            "Main Menu/"+ShortCutKey.FormatChangeName,
            "Main Menu/"+ShortCutKey.NoiseGenerateName ,
            "Main Menu/"+ShortCutKey.ChannelCombineName ,
            "Main Menu/"+ShortCutKey.TextureCropName ,
            "Main Menu/"+ShortCutKey.TextureBlurName,
            "Main Menu/"+ShortCutKey.SetParticleName,
            "Main Menu/"+ShortCutKey.SetParticleDelayName,
            "Main Menu/"+ShortCutKey.CleanMaterialName ,
            "Main Menu/"+ShortCutKey.PerformanceName,
            "Main Menu/"+ShortCutKey.AssetsAuditName ,
            "Main Menu/"+ShortCutKey.DrawCallTestName,
            "Main Menu/"+ShortCutKey.NewEffectUniversalMaterial,
            "Main Menu/"+ShortCutKey.EffectsBatchOptimization,
            "Main Menu/"+ShortCutKey.MaterialReplaceShader,
            "Main Menu/"+ShortCutKey.PropertyFindPrefab,
            "Main Menu/"+ShortCutKey.PrefabUpdateNode
        };

    }



}