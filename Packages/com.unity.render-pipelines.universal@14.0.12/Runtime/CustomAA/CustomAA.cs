using System;

namespace UnityEngine.Rendering.Universal
{
    public class CustomAA
    {
        public enum CustomAAQuality
        {
            MaximumPerformance,
            Balanced,
            MaximumQuality,
            UltraPerformance,
            Auto,
        }

        public static readonly float[] CustomAAQualityScale = new float[] {0.6f, 0.8f, 1.0f, 0.5f, 1};

        public enum ExecuteEvent
        {
            BeforePost,
            AfterPost,
        }

        [Serializable]
        public struct Settings
        {
            public bool EnableCustomScale;
            public float CustomScale;
            public CustomAAQuality Quality;
            public ExecuteEvent Event;
            public bool UseOptimalSetting;
            public bool EnableSharpness;
            public float Sharpness;
            //public float AntiGhost;
            public static Settings Create()
            {
                Settings s;

                s.Quality           = CustomAAQuality.MaximumQuality;
                s.Event             = ExecuteEvent.AfterPost;
                s.EnableSharpness   = false;
                s.UseOptimalSetting = false;
                s.Sharpness         = 1.0f;
                s.CustomScale       = 1.0f;
                s.EnableCustomScale = false;
                // s.AntiGhost         = 1.0f;
                return s;
            }
        } 
        
        //将抗锯齿的输出结果Blit到target上
        //@param inverse 是否需要图片反转，默认为true
        public static void BlitToTarget(CommandBuffer cmd, RTHandle target, CameraData cameraData, bool inverse = true)
        {
            if (cameraData.IsDLAAEnabled())
            {
                DLAAPass.BlitToTarget(cmd, target, inverse);
            }
            else if (cameraData.IsFSREnabled())
            {
                FSRPass.BlitToTarget(cmd, target, inverse);
            }
        }

        public static RTHandle GetAASource(bool useDLAA, bool useFsr)
        {
            if (useDLAA)
            {
                return DLAAPass.GetSource();
            }
            else if (useFsr)
            {
                return FSRPass.GetSource();
            }
            return null;
        }

        /*//绑定RT为抗锯齿的输出结果
        public static void BindTarget(RTHandle target)
        {
            
        }*/
    }
}