using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PyramidBlur {

    //based on
    // https://github.com/Unity-Technologies/FPSSample/blob/6b8b27aca3690de9e46ca3fe5780af4f0eff5faa/Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/Bloom.shader
    public class Blur : System.IDisposable {

        public const string SHADER_Blur = "Blur";

        // [down,up]
        protected Level[] m_Pyramid;
        protected Material mat;
        protected CommandBuffer cmd;

        public Blur(Shader blur) {
            cmd = new CommandBuffer() { name =  SHADER_Blur };
            mat = new Material(blur);

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++) {
                m_Pyramid[i] = new Level {
                    down = Shader.PropertyToID($"_Blur_Down_{i}"),
                    up = Shader.PropertyToID($"_Blur_Up_{i}"),
                };
            }
        }
        public Blur() : this(Resources.Load<Shader>(SHADER_Blur)) { }

        #region IDisposable
        public void Dispose() {
            if (cmd != null) {
                cmd.Dispose();
                cmd = null;
            }
            if (mat != null) {
                (Application.isPlaying ? (System.Action<Object>)Object.Destroy : Object.DestroyImmediate)(mat);
                mat = null;
            }
        }
        #endregion

        #region methods
        public void Render(Texture source, RenderTexture dest, Settings settings) {
            var cmd = Write(source, dest, settings);
            Graphics.ExecuteCommandBuffer(cmd);
        }
        public CommandBuffer Write(Texture source, RenderTexture dest, Settings settings) {
            cmd.Clear();
            cmd.BeginSample(Name);

            // Do bloom on a half-res buffer, full-res doesn't bring much and kills performances on
            // fillrate limited platforms
            int tw = Mathf.FloorToInt(source.width / 2f);
            int th = Mathf.FloorToInt(source.height / 2f);

            // Determine the iteration count
            int s = Mathf.Max(tw, th);
            float logs = Mathf.Log(s, 2f) + Mathf.Min(settings.diffusion, 10f) - 10f;
            int logs_i = Mathf.FloorToInt(logs);
            int iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logs - logs_i;
            mat.SetFloat(ShaderIDs.SampleScale, sampleScale);

            int qualityOffset = settings.fastMode ? 1 : 0;

            // Downsample
            RenderTargetIdentifier lastDown = source;
            var downPass = (int)Pass.Downsample13 + qualityOffset;
            for (int i = 0; i < iterations; i++) {
                var mipDown = m_Pyramid[i].down;
                var mipUp = m_Pyramid[i].up;

                var format = source.graphicsFormat;

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, format);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, format);

                cmd.Blit(lastDown, mipDown, mat, downPass);

                lastDown = mipDown;
                tw = tw / 2;
                tw = Mathf.Max(tw, 1);
                th = Mathf.Max(th / 2, 1);
            }

            // Upsample
            RenderTargetIdentifier lastUp = m_Pyramid[iterations - 1].down;
            var upPass = (int)Pass.UpsampleTent + qualityOffset;
            for (int i = iterations - 2; i >= 0; i--) {
                var mipUp = m_Pyramid[i].up;

                cmd.Blit(lastUp, mipUp, mat, upPass);

                lastUp = mipUp;
            }

            cmd.Blit(lastUp, dest);

            // Cleanup
            for (int i = 0; i < iterations; i++) {
                cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample(Name);
            return cmd;
        }
        #endregion

            #region declarations
        public const string Name = "BlurPyramid";
        public const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!
        public struct Level {
            public int down;
            public int up;
        }
        public enum Pass {
            Downsample13,
            Downsample4,
            UpsampleTent,
            UpsampleBox,
        }
        [System.Serializable]
        public class Settings {
            [Range(0f, 10f)]
            public float diffusion;
            public bool fastMode;
        }
        #endregion
    }
}