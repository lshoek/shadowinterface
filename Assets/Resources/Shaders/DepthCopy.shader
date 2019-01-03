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
		_DepthNormalsTex("Base (RGB)", 2D) = "white" {}
		_SurfaceCutoff("Surface Cutoff", Float) = 0.05
	}

	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			CGPROGRAM
			uniform sampler2D _DepthNormalsTex;
			uniform float _SurfaceCutoff;

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
				float d;
				float3 n;

				DecodeDepthNormal(tex2D(_DepthNormalsTex, i.uv), d, n);

    			float3 forward = float3(0, 0, -1);

    			d = (dot(forward, n) < _SurfaceCutoff) ? d : 0;
    			d = (d <= 1.0) ? d : 0;

				return inv(d * 1.5);
			}
			ENDCG
		}
	}
		
	Fallback "Diffuse" 
}
