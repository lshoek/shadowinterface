Shader "Custom/DepthObjectDebug" 
{
	Properties 
	{
	    _Color ("Main Color", Color) = (1,1,1,1)
	    _MainTex ("Base (RGB)", 2D) = "white" {}
	    _Threshold ("Threshold", Float) = -1.0
	}

	SubShader 
	{
	    Tags { "RenderType"="Opaque" }
	    LOD 200

	   	Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
		#pragma surface surf Lambert vertex:vert

		sampler2D _MainTex;
		fixed4 _Color;
		float _Threshold;

		struct Input 
		{
		    float2 uv_MainTex;
		   	float3 worldPos;	
		};

		void vert (inout appdata_base IN, out Input OUT) 
		{
        	UNITY_INITIALIZE_OUTPUT(Input, OUT);
        	OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
        }

		void surf (Input IN, inout SurfaceOutput OUT) 
		{
		    fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
		    //OUT.Emission = _Color.rgb;
		    OUT.Albedo = c.rgb;
		    OUT.Alpha = 1.0;

		    if (IN.worldPos.y < _Threshold)
		    	discard;
		}

		fixed4 LightingNoLighting(SurfaceOutput s, fixed3 lightDir, fixed atten) 
		{
			return fixed4(0.0, 0.0, 0.0, 0.0); //half4(s.Albedo, s.Alpha);
     	}

		ENDCG
	}

	Fallback "Legacy Shaders/VertexLit"
}
