Shader "Unlit/CircleIcon"
{
    Properties
    {
        [HideInInspector]_MainTex ("Texture", 2D) = "white" {}
        _Radios ("半径", float) = 1
        _Transition ("过渡", float) = 4
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
                float2 uv : TEXCOORD0;
                float2 center : TEXCOORD1;
                float2 ratio : TEXCOORD2;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 center : TEXCOORD1;
                float2 ratio : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Radios;
            float _Transition;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.center = v.center;
                o.ratio = v.ratio;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                fixed gray = col.r * .3 + col.g * .59 + col.b * .11;
                col.rgb = lerp(col.rgb, fixed3(gray, gray, gray), i.ratio.y);
                float l = length(i.uv - i.center) * i.ratio.x;
                col.a *= 1 - sin(clamp((l - _Radios) * _Transition, 0, UNITY_HALF_PI));
                return col;
            }
            ENDCG
        }
    }
}
