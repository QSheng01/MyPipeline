
using UnityEditor;
using UnityEngine;


namespace SYArtToolkit
{
    public class DrawCallCalculate : ToolInterface
    {
        private TestInterface[] testInterfaces = new TestInterface[3];
        private string[] toolBar = {
            "运行场景下的DrawCall测试",
            "OverDraw测试",
            "新建场景下的DrawCall测试"
        };
        private int theSelect = 0;
        private int lastSelect = 0;



        public override void Exit()
        {
            //batchesRecorder.Dispose();
            for (int i = 0; i < 3; ++i)
            {
                if (testInterfaces[i] != null) testInterfaces[i].Destroy();
            }
        }


        public DrawCallCalculate()//构造函数
        {
        }

        ~DrawCallCalculate()
        {
            for (int i = 0; i < 3; ++i)
            {
                if (testInterfaces[i] != null) testInterfaces[i].Destroy();
            }
        }
        //
        public override void InterfaceShow(bool noBool, Rect noRect)
        {
            EditorGUI.BeginChangeCheck();
            theSelect = GUILayout.Toolbar(theSelect, toolBar);
            if (theSelect != lastSelect)
            {
                testInterfaces[lastSelect].Destroy();
            }
            lastSelect = theSelect;
            if (testInterfaces[theSelect] == null)
            {
                switch (theSelect)
                {
                    case 0:
                        testInterfaces[0] = new DrawCallCalculateDynamic();
                        break;
                    case 1:
                        testInterfaces[1] = new OverDrawCalculate();
                        break;
                    case 2:
                        testInterfaces[2] = new DrawCallCalculateStatic();
                        break;
                }
            }
            testInterfaces[theSelect].Test();

        }
        public interface TestInterface
        {
            void Test();
            void Destroy();
        }


    }
}