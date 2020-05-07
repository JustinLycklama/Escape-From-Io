Shader "Custom/BuildableUniform"
{
	Properties {				
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
	}

	SubShader{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }		 
		Blend SrcAlpha OneMinusSrcAlpha
		Cull Back
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard vertex:vert fullforwardshadows alpha:fade

		float percentComplete;

		struct Input {
			float2 uv_MainTex;

			// Custom			
			float height; // Height relative to object
		};

		sampler2D _MainTex;		 

		fixed4 _Color;

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

			float4 vertex = v.vertex;
			o.height = vertex.y;
		}

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		void surf(Input IN, inout SurfaceOutputStandard o) {
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
					
			o.Alpha = clamp(percentComplete, 0.2, 1);			
		}
		ENDCG
	
	}
		
	Fallback "Diffuse"
}