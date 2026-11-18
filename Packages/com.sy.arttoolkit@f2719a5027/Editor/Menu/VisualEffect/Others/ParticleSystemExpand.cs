
using UnityEditor;
using UnityEngine;



namespace SYArtToolkit
{
    [CustomEditor(typeof(ParticleSystem))]
    public class ParticleSystemExpand : DecoratorEditor
    {
        private ParticleSystem thisParticleSystem; //自身的粒子系统

        public ParticleSystemExpand() : base("ParticleSystemInspector")
        {

        }

        private void OnEnable()
        {
            thisParticleSystem = target as ParticleSystem;
        }

        public override void OnInspectorGUI()
        {
            GUIUpdate_ParticleTransform();
            base.OnInspectorGUI();
            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
        }

        private void GUIUpdate_ParticleTransform()
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("重置:", GUILayout.Width(34), GUILayout.Height(20));
            if (GUILayout.Button("位移", GUILayout.Width(40), GUILayout.Height(20)))
            {
                thisParticleSystem.transform.localPosition = Vector3.zero;

                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            if (GUILayout.Button("旋转", GUILayout.Width(40), GUILayout.Height(20)))
            {
                thisParticleSystem.transform.localEulerAngles = new Vector3(0, 0, 0);

                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            if (GUILayout.Button("缩放", GUILayout.Width(40), GUILayout.Height(20)))
            {
                thisParticleSystem.transform.localScale = Vector3.one;
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            GUILayout.Space(20);
            GUILayout.Label("清空:", GUILayout.Width(32), GUILayout.Height(20));
            if (GUILayout.Button("粒子材质", GUILayout.Width(60), GUILayout.Height(20)))
            {
                ParticleSystemRenderer parrender = thisParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>();
                parrender.material = null;
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            if (GUILayout.Button("拖尾材质", GUILayout.Width(60), GUILayout.Height(20)))
            {
                ParticleSystemRenderer parrender = thisParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>();
                parrender.trailMaterial = null;
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            if (GUILayout.Button("Mesh", GUILayout.Width(44), GUILayout.Height(20)))
            {
                ParticleSystemRenderer parrender = thisParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>();
                parrender.mesh = null;
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("开启:", GUILayout.Width(34), GUILayout.Height(20));
            if (GUILayout.Button("拖尾", GUILayout.Width(40), GUILayout.Height(20)))
            {
                KaiTrail();
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            if (GUILayout.Button("星链", GUILayout.Width(40), GUILayout.Height(20)))
            {
                KaiRibbnon();
                Submit.ArtToolkitData.Instance.AddConvenient(1);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(-24);
            GUILayout.EndVertical();
        }

        void KaiTrail()
        {
            ParticleSystem particle = thisParticleSystem.gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            ParticleSystem.TrailModule trail = particle.trails;
            trail.enabled = true;
            trail.mode = ParticleSystemTrailMode.PerParticle;
            trail.worldSpace = true;
            trail.dieWithParticles = false;
            trail.sizeAffectsWidth = false;
            trail.inheritParticleColor = false;
            ParticleSystem.MinMaxGradient minMaxGradient = new ParticleSystem.MinMaxGradient();
            minMaxGradient.mode = ParticleSystemGradientMode.Gradient;
            trail.colorOverLifetime = minMaxGradient;
            trail.colorOverTrail = minMaxGradient;
            trail.ratio = 1;
            trail.lifetime = (1 / main.startLifetime.constantMax) * 0.25f;
            ParticleSystemRenderer render = thisParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>();
            render.trailMaterial = render.sharedMaterial;
        }

        void KaiRibbnon()
        {
            ParticleSystem particle = thisParticleSystem.gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particle.main;
            ParticleSystem.TrailModule trail = particle.trails;
            trail.enabled = true;
            trail.mode = ParticleSystemTrailMode.Ribbon;
            trail.worldSpace = true;
            trail.dieWithParticles = false;
            trail.sizeAffectsWidth = false;
            trail.inheritParticleColor = false;
            ParticleSystem.MinMaxGradient minMaxGradient = new ParticleSystem.MinMaxGradient();
            minMaxGradient.mode = ParticleSystemGradientMode.Gradient;
            trail.colorOverLifetime = minMaxGradient;
            trail.colorOverTrail = minMaxGradient;
            trail.ratio = 1;
            trail.lifetime = (1 / main.startLifetime.constantMax) * 0.25f;
            trail.widthOverTrail = 0.1f;
            ParticleSystemRenderer render = thisParticleSystem.gameObject.GetComponent<ParticleSystemRenderer>();
            render.trailMaterial = render.sharedMaterial;
        }
    }

}
