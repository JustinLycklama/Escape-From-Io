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

		float leftTextures[22];
		float rightTextures[22];
		float topTextures[22];
		float bottomTextures[22];

		float layoutTextures[22*22];	
		float indexPriority[10];

		float mapLayoutWidth;
		float mapLayoutHeight;

		float tileSize;

		float mapXOffsetLow;
		float mapXOffsetHigh;

		float mapYOffsetLow;
		float mapYOffsetHigh;

		UNITY_DECLARE_TEX2DARRAY(baseTextures);

		float selectionX;
		float selectionY;

		// Used as a bool. 0 is skip selection calculations
		float hasSelection;

		//float selectedXOffsetLow;
		//float selectedXOffsetHigh;
		//
		//float selectedYOffsetLow;
		//float selectedYOffsetHigh;

		//sampler2D _MainTex;
		//fixed4 _Color;

		struct Input
		{
			// Pre-Defined
			float3 worldPos;
			float3 worldNormal;

			//float2 uv_MainTex;

			// Custom
			float2 layoutCoordinate;
		};
		
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			
			float4 vertex = v.vertex;

			float layoutCoordinateX = ((vertex.x - mapXOffsetLow) / (tileSize));
			float layoutCoordinateY = ((-vertex.z - mapYOffsetLow) / (tileSize));

			o.layoutCoordinate = float2(layoutCoordinateX + 1, layoutCoordinateY + 1);


				//if (hasSelection > 0.5) {
				   // o.isSelected = vertex.x > selectedXOffsetLow && vertex.x < selectedXOffsetHigh &&
				   // -vertex.z > selectedYOffsetLow && -vertex.z < selectedYOffsetHigh;
				//}
				//else {
				   // o.isSelected = false;
				//}
		}

		float inverseLerp(float a, float b, float value) {
			return saturate((value - a) / (b - a));
		}

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {

			if (textureIndex == -1) {
				textureIndex = 0; // Default to water
			}

			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}

		void sampleAndStrength(float origCoord, int sizeInDimension, out int sampleCoord, out float drawStrength) {
			const float overrideLine = 1 / tileSize; // Point where higher terrain takes complete precedence

			int floorCoord = floor(origCoord);

			sampleCoord = floorCoord;
			drawStrength = 1;

			if (origCoord - floorCoord < overrideLine && floorCoord > 0) {
				sampleCoord = floorCoord - 1;
				drawStrength = (origCoord - floorCoord) / overrideLine;
			}

			if ((floorCoord + 1) - origCoord < overrideLine && floorCoord < sizeInDimension - 1) {
				sampleCoord = floorCoord + 1;
				drawStrength = ((floorCoord + 1) - origCoord) / overrideLine;
			}
		}

		/*float3 axisBlendColor(float origCoord, float otherCoord, int sizeInDimension, bool isX, float3 worldPos, float3 blendAxes) {
			const float overrideLine = 1 / tileSize; // Point where higher terrain takes complete precedence

			int floorCoord = floor(origCoord);
			int floorOther = floor(otherCoord);

			int sampleCoord;
			float drawStrength;

			sampleAndStrength(origCoord, sizeInDimension, sampleCoord, drawStrength);

			// The color from our terrain at this index
			int baseIndex = (isX) ? floorOther * sizeInDimension + floorCoord : floorCoord * sizeInDimension + floorOther;
			float3 baseColor = triplanar(worldPos, 100, blendAxes, layoutTextures[baseIndex]);

			// The color to blend from adjacent index
			int sampleIndex = (isX) ? floorOther * sizeInDimension + sampleCoord : sampleCoord * sizeInDimension + floorOther;								
			float3 otherColor = triplanar(worldPos, 100, blendAxes, layoutTextures[sampleIndex]);

			// Set drawstrength to a value between 0.5 and 1
			drawStrength = (drawStrength + 1) / 2;

			// We are overridden by adjacent
			if (indexPriority[layoutTextures[sampleIndex]] > indexPriority[layoutTextures[baseIndex]]) {
				// We are overridden by adjacent. HEAVILY favour the adjacent color
				drawStrength = drawStrength * drawStrength * drawStrength;
			}
			// We Override Adjacent
			else if (indexPriority[layoutTextures[sampleIndex]] < indexPriority[layoutTextures[baseIndex]]) {
				// We override the adjacent, do not take any samples from them
				drawStrength = 1;
			}

			// If we neither override or are overriden by adjacent, drawstrength will blend the two evenly in the center
			return (drawStrength * baseColor) + ((1 - drawStrength) * otherColor);
		}*/


		void surf(Input IN, inout SurfaceOutput o) {
	
			float x = IN.layoutCoordinate.x;
			float y = IN.layoutCoordinate.y;

			int floorX = floor(x);
			int floorY = floor(y);

			float3 blendAxes = abs(IN.worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			// Sample
			int sampleCoordX = floorX;
			int sampleCoordY = floorY;
			float drawStrengthX = 1;
			float drawStrengthY = 1;

			sampleAndStrength(x, mapLayoutWidth, sampleCoordX, drawStrengthX);
			sampleAndStrength(y, mapLayoutHeight, sampleCoordY, drawStrengthY);

			// The color to blend from adjacent index
			int baseIndex = floorY * mapLayoutWidth + floorX;
			float3 baseColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[baseIndex]);

			// Is this tile selected
			if (hasSelection && floorX - 1 == selectionX && floorY - 1 == selectionY) {
				baseColor *= float3(0, 1, 1);
			}

			float3 otherColor = float3(0, 0, 0);
			int highestSamplePriority = -1;

			// When sampling a corner, merge the two options 
			if (sampleCoordX != floorX && sampleCoordY != floorY) {

				//int sampleIndex = sampleCoordY * mapLayoutWidth + sampleCoordX;
				//int priority = indexPriority[layoutTextures[sampleIndex]];

				int firstSampleIndex = floorY * mapLayoutWidth + sampleCoordX;
				int secondSampleIndex = sampleCoordY * mapLayoutWidth + floorX;

				int firstPriority = indexPriority[layoutTextures[firstSampleIndex]];
				int secondPriority = indexPriority[layoutTextures[secondSampleIndex]];

				if (firstPriority == secondPriority) {
					int finalSampleIndex = sampleCoordY * mapLayoutWidth + sampleCoordX;

					if (indexPriority[layoutTextures[finalSampleIndex]] > firstPriority) {
						highestSamplePriority = indexPriority[layoutTextures[finalSampleIndex]];

						otherColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[finalSampleIndex]);

						float combinedStrength = (drawStrengthX + drawStrengthY) / 1.25;

						if (combinedStrength > 1) {
							combinedStrength = 1;
						}

						drawStrengthX = combinedStrength;
						drawStrengthY = combinedStrength;
					} else {
						highestSamplePriority = firstPriority;

						float3 firstColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[firstSampleIndex]);
						float3 secondColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[secondSampleIndex]);

						otherColor = firstColor / 2 + secondColor / 2;
					}
				}

				else if (firstPriority > secondPriority) {
					sampleCoordY = floorY;
					highestSamplePriority = firstPriority;
					otherColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[firstSampleIndex]);
				} 

				else {
					sampleCoordX = floorX;
					highestSamplePriority = secondPriority;
					otherColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[secondSampleIndex]);


				}								

				//float savedSampleY = sampleCoordY;
				//if (priority < indexPriority[layoutTextures[firstSampleIndex]]) {
				//	priority = indexPriority[layoutTextures[sampleIndex]];
				//	sampleCoordY = floorY;
				//}

				//if (priority < indexPriority[layoutTextures[secondSampleIndex]]) {
				//	sampleCoordY = savedSampleY;
				//	sampleCoordX = floorX;
				//	priority = indexPriority[layoutTextures[secondSampleIndex]];
				//}

				//// If we are still using the corner
				//if (priority == indexPriority[layoutTextures[sampleIndex]]) {
				//	float combinedStrength = (drawStrengthX + drawStrengthY) / 2;
				//	drawStrengthX = combinedStrength;
				//	drawStrengthY = combinedStrength;
				//}
			}
			else {
				int sampleIndex = sampleCoordY * mapLayoutWidth + sampleCoordX;

				// Don't bother the blurr if there is nothing to blurr
				if (drawStrengthX != 1 || drawStrengthY != 1) {
					int textureIndex = layoutTextures[sampleIndex];
					if (textureIndex != -1) {
						highestSamplePriority = indexPriority[layoutTextures[sampleIndex]];
						otherColor = triplanar(IN.worldPos, 100, blendAxes, layoutTextures[sampleIndex]);
					}
				}
			}			

			// Is our other sample selected
			if (hasSelection && sampleCoordX - 1 == selectionX && sampleCoordY - 1 == selectionY) {
				otherColor *= float3(0, 1, 1);
			}

			// Set drawstrength to a value between 0.5 and 1
			drawStrengthX = (drawStrengthX + 1) / 2;
			drawStrengthY = (drawStrengthY + 1) / 2;

			// We are overridden by adjacent
			if (highestSamplePriority > indexPriority[layoutTextures[baseIndex]]) {
				// We are overridden by adjacent. HEAVILY favour the adjacent color
				drawStrengthX = drawStrengthX * drawStrengthX * drawStrengthX;
				drawStrengthY = drawStrengthY * drawStrengthY * drawStrengthY;
			}
			// We Override Adjacent
			else if (highestSamplePriority < indexPriority[layoutTextures[baseIndex]]) {
				// We override the adjacent, do not take any samples from them
				drawStrengthX = 1;
				drawStrengthY = 1;
			}

			float drawStrength = drawStrengthX;
			if (drawStrengthY < drawStrength) {
				drawStrength = drawStrengthY;
			}

			// If we neither override or are overriden by adjacent, drawstrength will blend the two evenly in the center
			o.Albedo = (drawStrength * baseColor) + ((1 - drawStrength) * otherColor);
			//o.Albedo = float3(1, 1, 1);
		}
        ENDCG
    }
    FallBack "Diffuse"
}
