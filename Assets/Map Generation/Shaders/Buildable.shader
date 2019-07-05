Shader "Custom/Buildable"
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
			//o.Metallic = _Metallic;
			//o.Smoothness = _Glossiness;

			//float test = inverseLerp(0, 1, IN.height);
			//test = IN.height;

			//o.Albedo = float3(0, test, 0);
			//o.Alpha = 1;

			if (IN.height > percentComplete) {
				o.Alpha = 0.1;
			} else {
				o.Alpha = _Color.a;
			}
		}
		ENDCG
	
	}
		
	Fallback "Diffuse"
}




/*
 Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		float itemHeight;

		struct Input
		{
			// Pre-Defined
			float2 uv_MainTex;

			// Custom

			// Height clamped between 0 and 1
			float height;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

			float4 vertex = v.vertex;

			//if (hasSelection > 0.5) {
			   // o.isSelected = vertex.x > selectedXOffsetLow && vertex.x < selectedXOffsetHigh &&
			   // -vertex.z > selectedYOffsetLow && -vertex.z < selectedYOffsetHigh;
			//}
			//else {
			   // o.isSelected = false;
			//}
		}

		void surf(Input IN, inout SurfaceOutput o) {

			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
			o.Alpha = 0.5;

			//o.Albedo = (drawStrength * baseColor) + ((1 - drawStrength) * otherColor);
		}
		ENDCG
	}
	FallBack "Diffuse"
*/