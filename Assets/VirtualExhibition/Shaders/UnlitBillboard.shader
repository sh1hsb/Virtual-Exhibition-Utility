Shader "Unlit/Billboard"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Color("Color", Color) = (1,1,1,1)
        [KeywordEnum(OFF, ALL_AXIS, Y_AXIS)] _BILLBOARD("Billboard Mode", Float) = 2
    }

    SubShader
    {
        Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" "DisableBatching" = "True" }

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _BILLBOARD_OFF _BILLBOARD_ALL_AXIS _BILLBOARD_Y_AXIS

            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            v2f vert(appdata v)
            {
                v2f o;

                #if _BILLBOARD_OFF
                {
                    o.vertex = UnityObjectToClipPos(v.vertex);
                }
                #elif _BILLBOARD_ALL_AXIS
                {
                    float3 viewPos = UnityObjectToViewPos(float3(0, 0, 0));

                    float3 scaleRotatePos = mul((float3x3)unity_ObjectToWorld, v.vertex);

                    viewPos += float3(scaleRotatePos.xy, -scaleRotatePos.z);

                    o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1));
                }
                #elif _BILLBOARD_Y_AXIS
                {
                    float3 viewPos = UnityObjectToViewPos(float3(0, 0, 0));

                    float3 scaleRotatePos = mul((float3x3)unity_ObjectToWorld, v.vertex);

                    float3x3 ViewRotateY = float3x3(
                        1, UNITY_MATRIX_V._m01, 0,
                        0, UNITY_MATRIX_V._m11, 0,
                        0, UNITY_MATRIX_V._m21, -1
                    );
                    viewPos += mul(ViewRotateY, scaleRotatePos);

                    o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1));
                }
                #endif

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
