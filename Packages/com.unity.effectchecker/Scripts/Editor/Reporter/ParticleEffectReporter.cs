#if UNITY_EDITOR
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Collections.Generic;
using System.Drawing;
using Color = System.Drawing.Color;

namespace SY.EffectChecker
{
    public static class ParticleEffectReporter
    {
        public static void CreateAndFillWorksheet(ExcelWorksheets wss)
        {
            Dictionary<string, (ExcelWorksheet ws, int index)> dictionary = new Dictionary<string, (ExcelWorksheet, int)>();

            var infoList = EffectPreChecker.Instance.GetAllParticleEffectInfo();

            if (infoList == null || infoList.Count == 0)
            {
                return;
            }
            ReadExcel readExcel = new ReadExcel();
            var hash = readExcel.GetHash();
            foreach (var info in infoList)
            {
                RulePreset rulePreset = null;
                if (hash == null) info.category = "特效检测";
                else
                {
                    foreach (var p in hash)
                    {
                        if (GlobMatcher.IsMatch(p.regex, info.name))
                        {
                            rulePreset = p;
                            info.category = p.kind; break;
                        }

                    }
                    if (string.IsNullOrEmpty(info.category)) info.category = "特效检测";
                }
                string titleName = info.category;
                if (!dictionary.ContainsKey(titleName))
                {
                    ExcelWorksheet ews = wss.Add(titleName);

                    int columnCount = infoList.Count;

                    //标签颜色
                    //ews.TabColor = ColorTranslator.FromHtml("#b490f5");
                    CheckDataReporter.CreateWorkSheetBase(ews, titleName, 18);

                    //建议值和平均值
                    ews.Cells[2, 2].Value = string.Format("贴图内存建议:{0}", EffectCheckerConfig.maxMemorySize);
                    ews.Cells[2, 3].Value = string.Format("贴图数量建议:{0}", EffectCheckerConfig.maxTextureCount);
                    ews.Cells[2, 4].Value = string.Format("组件数量建议:{0}", EffectCheckerConfig.maxParticleSystemCount);
                    ews.Cells[2, 5].Value = string.Format("最高DrawCall数建议:<{0}", EffectCheckerConfig.maxDrawCall);
                    ews.Cells[2, 6].Value = string.Format("最高粒子数建议:<{0}", EffectCheckerConfig.maxParticleCount);
                    ews.Cells[2, 7].Value =
                        string.Format("单个平均每像素overDraw率建议:<{0}", EffectCheckerConfig.maxAveragePixelRate);

                    //列头
                    ews.Cells[3, 1].Value = "特效预制体";
                    ews.Cells[3, 2].Value = "贴图内存(MB)";
                    ews.Cells[3, 3].Value = "不重复贴图数量";
                    ews.Cells[3, 4].Value = "特效中所有粒子系统组件数量";
                    ews.Cells[3, 5].Value = "最高DrawCall数";
                    ews.Cells[3, 6].Value = "最高粒子数";
                    ews.Cells[3, 7].Value = "平均每像素overDraw率";
                    ews.Cells[3, 8].Value = "最高每像素overDraw率";
                    ews.Cells[3, 9].Value = "平均全局像素overDraw率";
                    ews.Cells[3, 10].Value = "最高全局像素overDraw率";
                    ews.Cells[3, 11].Value = "开启Prewarn的粒子预制数";
                    ews.Cells[3, 12].Value = "未开启Prewarn的粒子预制数";
                    ews.Cells[3, 13].Value = "特效预制体路径";
                    ews.Cells[3, 14].Value = "超出10秒特效播放时间";
                    ews.Cells[3, 15].Value = "包含Color属性的网格";
                    ews.Cells[3, 16].Value = "包含uv2属性的网格";
                    ews.Cells[3, 17].Value = "包含uv3或uv4属性的网格";
                    ews.Cells[3, 18].Value = "最高播放面数";

                    using (var range = ews.Cells[3, 1, 3, 18])
                    {
                        //字体样式
                        range.Style.Font.Bold = true;

                        //背景颜色
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        //range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#DDEBF7"));

                        //开启自动筛选
                        range.AutoFilter = true;
                    }

                    //列宽
                    ews.Column(1).Width = 135;
                    for (int i = 1; i <= columnCount; i++)
                    {
                        ews.Column(i).Width = 15;
                        ews.Column(i).Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    }

                    ews.View.FreezePanes(4, 1);

                    //EffectInfo effectInfo = new EffectInfo();

                    int start = 4;

                    dictionary.Add(titleName, (ews, start));

                }

                var ws = dictionary[titleName].ws;
                int startRow = dictionary[titleName].index;

                ws.Row(startRow).Collapsed = true;
                ws.Cells[startRow, 1].Value = info.name;
                ws.Cells[startRow, 2].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[startRow, 2].Value = info.memorySize;
                if (rulePreset != null && rulePreset != null && info.memorySize > rulePreset.memory) ws.Cells[startRow, 2].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 3].Value = info.textureCount;
                if (rulePreset != null && rulePreset != null && info.textureCount > rulePreset.texnum) ws.Cells[startRow, 3].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 4].Value = info.particleSystemCount;
                if (rulePreset != null && rulePreset != null && info.particleSystemCount > rulePreset.parsystem) ws.Cells[startRow, 4].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 5].Value = info.particleEffecDrawCall;
                if (rulePreset != null && rulePreset != null && info.particleEffecDrawCall > rulePreset.drawcall) ws.Cells[startRow, 5].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 6].Value = info.maxParticleCount;
                if (rulePreset != null && rulePreset != null && info.textureCount > rulePreset.texnum) ws.Cells[startRow, 6].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 7].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[startRow, 7].Value = info.overDrawRate;
                if (rulePreset != null && rulePreset != null && info.overDrawRate > rulePreset.od) ws.Cells[startRow, 7].Style.Font.Color.SetColor(Color.Red);

                ws.Cells[startRow, 8].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[startRow, 8].Value = info.maxOverDrawRate;
                if (rulePreset != null && rulePreset != null && info.maxOverDrawRate > rulePreset.mod) ws.Cells[startRow, 8].Style.Font.Color.SetColor(Color.Red);

                ws.Cells[startRow, 9].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[startRow, 9].Value = info.globalOverdraw;
                if (rulePreset != null && rulePreset != null && info.globalOverdraw > rulePreset.god) ws.Cells[startRow, 9].Style.Font.Color.SetColor(Color.Red);

                ws.Cells[startRow, 10].Style.Numberformat.Format = "#,##0.00";
                ws.Cells[startRow, 10].Value = info.maxGlobalOverDraw;
                if (rulePreset != null && rulePreset != null && info.maxGlobalOverDraw > rulePreset.gmod) ws.Cells[startRow, 10].Style.Font.Color.SetColor(Color.Red);


                ws.Cells[startRow, 11].Value = info.openPrewarnCount;
                if (rulePreset != null && rulePreset != null && info.openPrewarnCount > rulePreset.warm) ws.Cells[startRow, 11].Style.Font.Color.SetColor(Color.Red);
                ws.Cells[startRow, 12].Value = info.closePrewarnCount;
                ws.Cells[startRow, 13].Value = info.effectPrePath;
                ws.Cells[startRow, 14].Value = info.isExceedPlayTimeLimit ? "True" : "False";
                ws.Cells[startRow, 15].Value = info.colorMeshs;
                ws.Cells[startRow, 16].Value = info.uv2Meshs;
                ws.Cells[startRow, 17].Value = info.uv3OrUv4Meshs;
                ws.Cells[startRow, 18].Value = info.maxTrisCount;
                for (int i = 1; i <= 18; i++)
                {
                    ws.Cells[startRow, i].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    ws.Cells[startRow, i].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                }


                using (var range = ws.Cells[4, 16, 4, 16])
                {
                    range.Merge = true;
                }

                ws.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[1, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                ws.Cells[1, 1].Value = ws.Cells[1, 1].Value;
                dictionary[titleName] = (ws, startRow + 1);

            }







            infoList.Clear();
        }

    }


}
#endif