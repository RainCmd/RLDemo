Shader "Unlit/StateBar"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Scale ("刻度", 2D) = "white" {}
        _Step ("刻度值", Float) = 100
        [HideInInspector]_StencilComp ("Stencil Comparison", Float) = 8
	    [HideInInspector]_Stencil ("Stencil ID", Float) = 0
	    [HideInInspector]_StencilOp ("Stencil Operation", Float) = 0
	    [HideInInspector]_StencilWriteMask ("Stencil Write Mask", Float) = 255
	    [HideInInspector]_StencilReadMask ("Stencil Read Mask", Float) = 255
	    [HideInInspector]_ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
	    Stencil
	    {
		    Ref [_Stencil]
		    Comp [_StencilComp]
		    Pass [_StencilOp] 
		    ReadMask [_StencilReadMask]
		    WriteMask [_StencilWriteMask]
	    }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 pos : TEXCOORD1;
                float2 rng : TEXCOORD2;
                float2 state : TEXCOORD3;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 pos : TEXCOORD1;
                float2 rng : TEXCOORD2;
                float2 state : TEXCOORD3;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _Scale;
            float _Step;
            v2f vert (appdata v)
            {
                v2f o;
                o.color = v.color;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.pos = v.pos;
                o.rng = v.rng;
                o.state = v.state;
                return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float x = i.pos.x / i.rng.x * i.state.y;
                clip(i.state.x - x);
                col.a *= 1 - tex2D(_Scale, float2(1 - x / _Step, i.pos.y / i.rng.y)).a;
                return col;
            }
            ENDCG
        }
    }
}
