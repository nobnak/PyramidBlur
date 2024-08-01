Shader "Hidden/Blur" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        #include "Sampling.cginc"

        struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vert (appdata v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        sampler2D _MainTex;
        float4 _MainTex_TexelSize;

        float  _SampleScale;

        fixed4 frag (v2f i) : SV_Target {
            fixed4 col = tex2D(_MainTex, i.uv);
            return col;
        }
        
        float4 FragDownsample13(v2f i) : SV_Target {
            return DownsampleBox13Tap(_MainTex, i.uv, _MainTex_TexelSize.xy);
        }
        float4 FragDownsample4(v2f i) : SV_Target {
            return DownsampleBox4Tap(_MainTex, i.uv, _MainTex_TexelSize.xy);
        }
        float4 FragUpsampleTent(v2f i) : SV_Target {
            return UpsampleTent(_MainTex, i.uv, _MainTex_TexelSize.xy, _SampleScale);
        }
        float4 FragUpsampleBox(v2f i) : SV_Target {
            return UpsampleBox(_MainTex, i.uv, _MainTex_TexelSize.xy, _SampleScale);
        }
        ENDCG

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragDownsample13
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragDownsample4
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragUpsampleTent
            ENDCG
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment FragUpsampleBox
            ENDCG
        }
    }
}
