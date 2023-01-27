using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PyramidBlur.Effects {

    [ExecuteAlways]
    public class BlurEffect : MonoBehaviour {

        public Tuner tuner = new Tuner();
        public Links links = new Links();

        protected Blur blur;

        #region unity
        private void OnEnable() {
            blur = new Blur(links.blur);
        }
        private void OnDisable() {
            if (blur != null) {
                blur.Dispose();
                blur = null;
            }
        }
        private void OnRenderImage(RenderTexture source, RenderTexture destination) {
            if (blur != null) {
                blur.Render(source, destination, tuner.blur);
            } else {
                Graphics.Blit(source, destination);
            }
        }
        #endregion

        #region declarations
        [System.Serializable]
        public class Links {
            public Shader blur;
        }
        [System.Serializable]
        public class Tuner {
            public Blur.Settings blur = new Blur.Settings();
        }
        #endregion
    }
}
