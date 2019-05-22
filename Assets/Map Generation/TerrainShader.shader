Shader "Custom/TerrainShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
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

		float selectedXOffsetLow;
		float selectedXOffsetHigh;
		
		float selectedYOffsetLow;
		float selectedYOffsetHigh;

		struct Input
		{
			// Pre-Defined
			float2 uv_MainTex;

			// Custom
			bool isSelected;
		};
		
		void vert(inout appdata_full v, out Input o) {
		  UNITY_INITIALIZE_OUTPUT(Input,o);

		  float4 vertex = v.vertex;

		  o.isSelected = vertex.x > selectedXOffsetLow && vertex.x < selectedXOffsetHigh &&
			  -vertex.z > selectedYOffsetLow && -vertex.z < selectedYOffsetHigh;
		}

		sampler2D _MainTex;
        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;

			if (IN.isSelected) {
				o.Albedo *= 0.1 * float3(0, 1, 1);
			}
        }
        ENDCG
    }
    FallBack "Diffuse"
}
