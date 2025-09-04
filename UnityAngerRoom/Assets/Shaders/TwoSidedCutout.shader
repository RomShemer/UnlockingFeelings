Shader "UF/TwoSidedCutout"
{
    Properties {
        _MainTex ("Albedo (RGB) Alpha (A)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.4
    }
    SubShader {
        Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Cull Off            // ❗ מצייר גם קדימה וגם אחורה
        ZWrite On

        Pass {
            AlphaToMask On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cutoff;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv);
                clip(c.a - _Cutoff);   // חותך לפי האלפא
                return c;
            }
            ENDCG
        }
    }
}
