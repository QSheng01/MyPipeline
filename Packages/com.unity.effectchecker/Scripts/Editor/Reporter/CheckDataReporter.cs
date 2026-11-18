#if UNITY_EDITOR
using OfficeOpenXml;
using OfficeOpenXml.Style;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SY.EffectChecker
{
    public class CheckDataReporter
    {
        public static void ExportReporter(string csv_path, string m_app_id, string m_python_path, string m_remark, bool m_debug, string scene = "")
        {
            AnalyzePrintToExcel(csv_path);
            ExportDataToJson(csv_path);
            //ExportDataMgr.ReportWeb(m_app_id, m_python_path, m_remark, m_debug);
            EditorPrefs.SetString(ExportDataConfig.sceneKey, "");
            EditorApplication.isPlaying = false;
        }

        public static void ExportDataToJson(string workDir)
        {
            ExportDataMgr exportDataMgr = new ExportDataMgr();
            exportDataMgr.effectInfo = new EffectInfo();
            exportDataMgr.effectInfo.effectList = new List<EffectData>();
            var effectCheckInfos = EffectPreChecker.Instance.GetAllParticleEffectInfo();
            foreach (var info in effectCheckInfos)
            {
                EffectData effectData = new EffectData();
                effectData.effects_preform = info.name;
                effectData.chartlet_memory = CorrectNaNFloat(Math.Round(info.memorySize, 2));
                effectData.no_repeat_chartlet = info.textureCount;
                effectData.particle_component = info.particleSystemCount;
                effectData.most_drawcall = info.particleEffecDrawCall;
                effectData.most_particle = info.maxParticleCount;
                effectData.average_overdraw_ratio = CorrectNaNFloat(Math.Round(info.overDrawRate, 2));
                effectData.most_overdraw_ratio = CorrectNaNFloat(Math.Round(info.maxOverDrawRate, 2));
                effectData.open_preman_preform = info.openPrewarnCount;
                effectData.close_preman_preform = info.closePrewarnCount;
                effectData.effects_preform_path = info.effectPrePath;
                effectData.more_10sec_effects = info.isExceedPlayTimeLimit;
                effectData.color_grid = info.colorMeshs.Replace("\n", "");
                effectData.uv2_grid = info.uv2Meshs.Replace("\n", "");
                effectData.uv3_or_uv4_grid = info.uv3OrUv4Meshs.Replace("\n", "");
                effectData.most_play = info.maxTrisCount;
                effectData.avg_effect_pixel_orgin_fill = CorrectNaNFloat(Math.Round(info.globalOverdraw, 2));
                effectData.avg_effect_pixel_reality_fill = CorrectNaNFloat(Math.Round(info.maxGlobalOverDraw, 2));
                exportDataMgr.effectInfo.effectList.Add(effectData);
            }

            exportDataMgr.SaveDataToJson(workDir);
        }

        private static double CorrectNaNFloat(double x)
        {
            return (double.IsNaN(x) || double.IsInfinity(x)) ? 0 : x;
        }

        public static string[] AnalysisCheckConfigFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string[] effectIDs = File.ReadAllLines(filePath);
                return effectIDs;
            }
            else
            {
                Debug.Log("filePath:" + filePath + "不存在");
            }
            return null;
        }

        public static void AnalyzePrintToExcel(string searchPath)
        {
            string outputPath = Path.Combine(searchPath, "特效检测报告" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx");

            var newFile = new FileInfo(outputPath);
            if (newFile.Exists)
            {
                newFile.Delete();
            }

            using (var package = new ExcelPackage(newFile))
            {
                ParticleEffectReporter.CreateAndFillWorksheet(package.Workbook.Worksheets);
                if (package.Workbook.Worksheets.Count != 0)
                {
                    package.Save();
                    Debug.Log("检测文档导出路径:" + outputPath);
                }
                else
                {
                    Debug.Log("未检测到特效资源");
                }
            }
            AssetDatabase.Refresh();
            //缓存本次报告导出地址，可以用于查看本地报告
            EditorPrefs.SetString(EffectCheckerConfig.localReportDirKey, outputPath);
        }

        /// <summary>
        /// 创建Excel表分页
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="title"></param>
        /// <param name="colCount"></param>
        public static void CreateWorkSheetBase(ExcelWorksheet ws, string title, int colCount)
        {
            //全体颜色
            // ws.Cells.Style.Font.Color.SetColor(ColorTranslator.FromHtml("#3d4d65"));
            {
                //边框样式
                var border = ws.Cells.Style.Border;
                border.Bottom.Style = border.Top.Style = border.Left.Style = border.Right.Style
                    = ExcelBorderStyle.Thin;

                //边框颜色
                //var clr = ColorTranslator.FromHtml("#B2C6C9");
                //border.Bottom.Color.SetColor(clr);
                //border.Top.Color.SetColor(clr);
                //border.Left.Color.SetColor(clr);
                //border.Right.Color.SetColor(clr);
            }

            //标题
            ws.Cells[1, 1].Value = title;
            using (var range = ws.Cells[1, 1, 1, colCount])
            {
                range.Merge = true;
                range.Style.Font.Bold = true;
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }
            ws.Row(1).Height = 30;
        }


    }
}
#endif