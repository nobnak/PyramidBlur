using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Rendering;

namespace PyramidBlur {

    public class Blur : System.IDisposable {

        // [down,up]
        protected Level[] m_Pyramid;
        protected CommandBuffer cmd;

        protected PropertySheetFactory sheetFactory;
        protected PropertySheet sheet;

        public Blur(Shader blur) {
            cmd = new CommandBuffer();
            sheetFactory = new PropertySheetFactory();
            sheet = sheetFactory.Get(blur);

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++) {
                m_Pyramid[i] = new Level {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        #region IDisposable
        public void Dispose() {
            if (sheetFactory != null) {
                sheetFactory.Release();
                sheet = null;
                sheetFactory = null;
            }
            if (cmd != null) {
                cmd.Dispose();
                cmd = null;
            }
        }
        #endregion

        #region methods
        public void Render(Texture source, RenderTexture dest, Settings settings) {
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
            sheet.properties.SetFloat(ShaderIDs.SampleScale, sampleScale);

            int qualityOffset = settings.fastMode ? 1 : 0;

            // Downsample
            RenderTargetIdentifier lastDown = source;

            for (int i = 0; i < iterations; i++) {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                int pass = (int)Pass.Downsample13 + qualityOffset;

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, source.graphicsFormat);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, source.graphicsFormat);
                cmd.BlitFullscreenTriangle(lastDown, mipDown, sheet, pass);

                lastDown = mipDown;
                tw = tw / 2;
                tw = Mathf.Max(tw, 1);
                th = Mathf.Max(th / 2, 1);
            }

            //cmd.Blit(lastDown, dest);

            // Upsample
            int lastUp = m_Pyramid[iterations - 1].down;
            for (int i = iterations - 2; i >= 0; i--) {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                cmd.SetGlobalTexture(ShaderIDs.BloomTex, mipDown);
                cmd.BlitFullscreenTriangle(lastUp, mipUp, sheet, (int)Pass.UpsampleTent + qualityOffset);
                lastUp = mipUp;
            }

            cmd.Blit(lastUp, dest);

            // Cleanup
            for (int i = 0; i < iterations; i++) {
                cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            cmd.EndSample(Name);

            Graphics.ExecuteCommandBuffer(cmd);
        }
        #endregion

        #region declarations
        public const string Name = "BlurPyramid";
        public const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!
        public struct Level {
            internal int down;
            internal int up;
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