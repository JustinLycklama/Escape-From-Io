Shader "Custom/TerrainSelection"
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

		float layoutTextures[20*20];	
		float indexPriority[10];

		float mapLayoutWidth;
		float mapLayoutHeight;

		float tileSize;

		float mapXOffsetLow;
		float mapXOffsetHigh;

		float mapYOffsetLow;
		float mapYOffsetHigh;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);


		// Used as a bool. 0 is skip selection calculations
		//float hasSelection;

		//float selectedXOffsetLow;
		//float selectedXOffsetHigh;
		//
		//float selectedYOffsetLow;
		//float selectedYOffsetHigh;


		struct Input
		{
			// Pre-Defined
			float3 worldPos;
			float3 worldNormal;

			//float2 uv_MainTex;

			// Custom
			float2 layoutCoordinate;
			//bool isSelected;

			//bool isLand;
			//float index;

			//float coordX;
			//float coordY;
		};
		
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			
			float4 vertex = v.vertex;

			float layoutCoordinateX = ((vertex.x - mapXOffsetLow) / (tileSize));
			float layoutCoordinateY = ((-vertex.z - mapYOffsetLow) / (tileSize));

			o.layoutCoordinate = float2(layoutCoordinateX, layoutCoordinateY);



				//o.coordX = layoutCoordinateX;
				//o.coordY = layoutCoordinateY;

				//int texturesIndex = layoutCoordinateY * mapLayoutWidth + layoutCoordinateX;

				////o.index = texturesIndex;
				//o.isLand = layoutTextures[texturesIndex];

				//if ((vertex.x - mapXOffsetLow) % tileSize == 0 || (-vertex.z - mapYOffsetLow) % tileSize == 0) {
				   // o.isLand = 0;
				//}

				//if (floor(((vertex.x - mapXOffsetLow) + 1) / tileSize) != layoutCoordinateX || floor(((-vertex.z - mapYOffsetLow) + 1) / tileSize) != layoutCoordinateY) {
				   // o.isLand = 0;
				//}

				//if (floor(((vertex.x - mapXOffsetLow) - 1) / tileSize) != layoutCoordinateX || floor(((-vertex.z - mapYOffsetLow) - 1) / tileSize) != layoutCoordinateY) {
				   // o.isLand = 0;
				//}


				//if (hasSelection > 0.5) {
				   // o.isSelected = vertex.x > selectedXOffsetLow && vertex.x < selectedXOffsetHigh &&
				   // -vertex.z > selectedYOffsetLow && -vertex.z < selectedYOffsetHigh;
				//}
				//else {
				   // o.isSelected = false;
				//}
		}

		sampler2D _MainTex;
		fixed4 _Color;

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}






		void surf(Input IN, inout SurfaceOutput o) {
			const float overrideLine = 1 / tileSize; // Point where higher terrain takes complete precedence
		
			float x = IN.layoutCoordinate.x;
			float y = IN.layoutCoordinate.y;

			int floorX = floor(x);
			int floorY = floor(y);

			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			// Check for override
			//bool override = false;

			int sampleX = floorX;
			int sampleY = floorY;

			float drawStrengthX = 1;
			float drawStrengthY = 1;

			if (x - floorX < overrideLine && floorX > 0) {
				sampleX = floorX - 1;
				drawStrengthX = (x - floorX) / overrideLine;
			}

			if ((floorX + 1) - x < overrideLine && floorX < mapLayoutWidth - 1) {
				sampleX = floorX + 1;
				drawStrengthX = ((floorX + 1) - x) / overrideLine;
			}

			// Set drawstrength to a value between 0.5 and 1
			drawStrengthX = (drawStrengthX + 1) / 2;

		
			// We are overridden by adjacent
			if (indexPriority[layoutTextures[sampleIndex]] > indexPriority[layoutTextures[baseIndex]]) {
				// We are overridden by adjacent. HEAVILY favour the adjacent color
				drawStrengthX = drawStrengthX * drawStrengthX * drawStrengthX;
			}
			// We Override Adjacent
			else if (indexPriority[layoutTextures[sampleIndex]] < indexPriority[layoutTextures[baseIndex]]) {
				// We override the adjacent, do not take any samples from them
				drawStrengthX = 1;				
			}

			// If we neither override or are overriden by adjacent, drawstrength will blend the two evenly in the center

			// The color from our terrain at this index
			int baseIndex = floorY * mapLayoutWidth + floorX;
			float3 baseColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[baseIndex]);
			
			// The color to blend from adjacent index
			int sampleIndex = sampleY * mapLayoutWidth + sampleX;
			float3 otherXColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[sampleIndex]);

			o.Albedo = (drawStrengthX * baseColor) + ((1 - drawStrengthX) * otherXColor); 

		
			//if (IN.isSelected) {
			//	o.Albedo *= float3(0, 1, 1);
			//}

		}
        ENDCG
    }
    FallBack "Diffuse"
}
