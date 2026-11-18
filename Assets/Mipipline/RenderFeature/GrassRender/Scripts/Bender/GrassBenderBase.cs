using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Grass
{
	public class GrassBenderBase
    {
        public enum BenderType
        {
            Mesh,
            Trail,
            ParticleSystem
        }
		

		public static void ValidateParticleSystem(GrassBender b)
        {
            if (!b.particleSystem) return;

            if (!b.particleRenderer) b.particleRenderer = b.particleSystem.GetComponent<ParticleSystemRenderer>();

            b.psGrad = b.particleSystem.colorOverLifetime;
            b.hasParticleTrails = b.particleSystem.trails.enabled;
        }

        public static Gradient GetGradient(AnimationCurve curve)
        {
            int accuracy = 8;

            Gradient g = new Gradient();

            if (curve == null) curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

            GradientColorKey[] colorKeys = new GradientColorKey[accuracy];
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

            for (int i = 0; i < accuracy; i++)
            {
                float s = (float)i / (float)accuracy;
                colorKeys[i].time = i == 1 ? 0.05f : s;

                //Nullify the start of the gradient
                var value = i == 0 ? 0 : curve.Evaluate(s);

                colorKeys[i].color = new Color(value, 0f, 0f);
            }

            alphaKeys[0].time = 0;
            alphaKeys[0].alpha = 1f;
            alphaKeys[1].time = 1f;
            alphaKeys[1].alpha = 1f;
            g.SetKeys(colorKeys, alphaKeys);

            return g;
        }
    }
}