Shader "Custom/DepthCopy"
{
	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Extensions.cginc"
	#pragma vertex VERT
	#pragma fragment FRAG
	ENDCG

	Properties
	{
		_DepthTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			CGPROGRAM
			uniform sampler2D _DepthTex;

			struct appdata
			{
				float4 vertex : POSITION;
				half4 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				half4 uv : TEXCOORD0;
			};
			
			v2f VERT (appdata IN)
			{
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(IN.vertex);
				OUT.uv = IN.uv;
				return OUT;
			}

			fixed4 FRAG (v2f i) : SV_Target
			{
				return Linear01Depth(SAMPLE_DEPTH_TEXTURE_PROJ(_DepthTex , i.uv));
			}
			ENDCG
		}
	}
		
	Fallback "Diffuse" 
}
