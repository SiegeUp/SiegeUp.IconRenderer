Shader "IconRenderer/TransparencyPostprocess"
{
    Properties
    {
        _MainTex ("Main", 2D) = "black" {}
        _WhiteBgTex ("White Background", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        ZTest Always Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 ScaleOffset;
            sampler2D _MainTex;
            sampler2D _WhiteBgTex;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv.xy * ScaleOffset.xy + ScaleOffset.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 whiteBgCol = tex2D(_WhiteBgTex, i.uv);
                float colIntencity = (col.r + col.g + col.b) / 3;
                float whiteBgColIntencity = (whiteBgCol.r + whiteBgCol.g + whiteBgCol.b) / 3;
                return float4(col.rgb, 1 - (whiteBgColIntencity - colIntencity));
            }
            ENDCG
        }
    }
}