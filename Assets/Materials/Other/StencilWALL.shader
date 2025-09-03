Shader "Unlit/StencilShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _HoleFeather("Hole Feather", Range(0,0.1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Stencil
            {
                Ref 1
                Comp NotEqual // stencil == 1 不畫
                Pass Keep
            }
            Stencil
            {
                Ref 2
                Comp NotEqual // stencil == 2 不畫
                Pass Keep
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _HoleFeather;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 簡單柔邊：判斷 stencil 邊緣的片段可選用 Alpha Clip
                // URP/HDRP 的標準做法需要 Shader Graph 可更精準
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
