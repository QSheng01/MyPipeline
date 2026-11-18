#if UNITY_EDITOR
using OfficeOpenXml;

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace SY.EffectChecker
{
    public class RulePreset
    {
        public string kind;
        public string regex;
        public float memory;
        public float texnum;
        public int drawcall;
        public int parsystem;
        public float od;
        public float mod;
        public float god;
        public float gmod;
        public int warm;



    }


    public class ReadExcel
    {


        public bool IsOpen()
        {

            var defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString("EffectCheckerInput")); ;
            if (defaultAsset == null) return true;
            FileInfo fileInfo = new FileInfo(AssetDatabase.GetAssetPath(defaultAsset));
            return true;
        }

        public HashSet<RulePreset> GetHash()
        {
            var defaultAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(EditorPrefs.GetString("EffectCheckerInput")); ;
            if (defaultAsset == null) return null;
            var result = new HashSet<RulePreset>();
            FileInfo fileInfo = new FileInfo(AssetDatabase.GetAssetPath(defaultAsset));
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[0];
                int row = worksheet.Dimension.Rows;
                for (int i = 2; i <= row; ++i)
                {
                    var p = new RulePreset();

                    p.kind = (string)worksheet.Cells[i, 1].Value;
                    if (p.kind == "") continue;
                    p.regex = (string)worksheet.Cells[i, 2].Value;
                    try
                    {
                        p.memory = Convert.ToSingle(worksheet.Cells[i, 3].Value);
                        p.texnum = Convert.ToInt32(worksheet.Cells[i, 4].Value);
                        p.drawcall = Convert.ToInt32(worksheet.Cells[i, 5].Value);
                        p.parsystem = Convert.ToInt32(worksheet.Cells[i, 6].Value);
                        p.od = Convert.ToSingle(worksheet.Cells[i, 7].Value);
                        p.mod = Convert.ToSingle(worksheet.Cells[i, 8].Value);
                        p.god = Convert.ToSingle(worksheet.Cells[i, 9].Value);
                        p.gmod = Convert.ToSingle(worksheet.Cells[i, 10].Value);
                        p.warm = Convert.ToInt32(worksheet.Cells[i, 11].Value);
                    }
                    catch
                    {
                        p.memory = 0;
                        p.texnum = 0;
                        p.drawcall = 0;
                        p.parsystem = 0;
                        p.od = 0;
                        p.mod = 0;
                        p.god = 0;
                        p.gmod = 0;
                        p.warm = 0;
                    }
                    result.Add(p);
                }

            }

            return result;
        }






    }
}
#endif

