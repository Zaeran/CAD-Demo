﻿Shader "Stencils/Material/BloomGain2" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
    _Gain ("Gain", Range(1.0, 10.0)) = 1.0
    _MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
    Stencil {
      Ref 2
      Comp equal
      Pass keep
      Fail keep
    }
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		  #pragma surface surf Lambert

		  sampler2D _MainTex;
      fixed4 _Color;
      float _Gain;

		  struct Input {
			  float2 uv_MainTex;
		  };

		  void surf (Input IN, inout SurfaceOutput o) {
			  half4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			  o.Albedo = c.rgb * _Gain;
			  o.Alpha = c.a * _Gain;
		  }
		ENDCG
	} 
	FallBack "VertexLit"
}
