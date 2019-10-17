// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified Bumped shader. Differences from regular Bumped one:
// - no Main Color
// - Normalmap uses Tiling/Offset of the Base texture
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "Custom/TerrainSelectionMobile" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
		[NoScaleOffset] _BumpMap("Normalmap", 2D) = "bump" {}
	}

		SubShader{
			Tags { "RenderType" = "Opaque" }
			LOD 250

		CGPROGRAM
		#pragma surface surf Lambert noforwardadd vertex:vert

		float2 mapLayout;
		float tileSize;

		float4 mapOffset;

		// Given a layout width and height of 5, plus 2 for overhang in each direction
		float layoutTextures[7*7];
		UNITY_DECLARE_TEX2DARRAY(baseTextures);
		UNITY_DECLARE_TEX2DARRAY(bumpMapTextures);

		float indexPriority[20];
		float indexScale[20];

		float2 selection;

		// Used as a bool. 0 is skip selection calculations
		float hasSelection;

		struct Input
		{
			// Pre-Defined
			float3 worldPos;
			float3 worldNormal; INTERNAL_DATA

			//float2 uv_MainTex;

			// Custom
			float2 layoutCoordinate;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

			float4 vertex = v.vertex;

			float layoutCoordinateX = ((vertex.x - mapOffset.r) / (tileSize));
			float layoutCoordinateY = ((-vertex.z - mapOffset.b) / (tileSize));

			o.layoutCoordinate = float2(layoutCoordinateX + 1, layoutCoordinateY + 1);


			//if (hasSelection > 0.5) {
			   // o.isSelected = vertex.x > selectedXOffsetLow && vertex.x < selectedXOffsetHigh &&
			   // -vertex.z > selectedYOffsetLow && -vertex.z < selectedYOffsetHigh;
			//}
			//else {
			   // o.isSelected = false;
			//}
		}

		/* Surface Helpers */

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

		half3 triplanarBump(float3 worldPos, float3 worldNormal, float scale, float3 blendAxes, int textureIndex) {

			if (textureIndex == -1) {
				textureIndex = 0; // Default to water
			}

			// UDN blend
			
			// Triplanar uvs
			//float2 uvX = worldPos.zy; // x facing plane
			//float2 uvY = worldPos.xz; // y facing plane
			//float2 uvZ = worldPos.xy; // z facing plane
			
										
			// Tangent space normal maps
			
			// Original
			//half3 tnormalX = UnpackNormal(tex2D(_BumpMap, uvX));
			//half3 tnormalY = UnpackNormal(tex2D(_BumpMap, uvY));
			//half3 tnormalZ = UnpackNormal(tex2D(_BumpMap, uvZ));

			float3 scaledWorldPos = worldPos / scale;

			half3 tnormalX = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.z, scaledWorldPos.y, textureIndex)), 1000);
			half3 tnormalY = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)), 1000);
			half3 tnormalZ = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)), 1000);
		

			// Swizzle world normals into tangent space and apply UDN blend.
			// These should get normalized, but it's very a minor visual
			// difference to skip it until after the blend.
			tnormalX = half3(tnormalX.xy + worldNormal.zy, worldNormal.x);
			tnormalY = half3(tnormalY.xy + worldNormal.xz, worldNormal.y);
			tnormalZ = half3(tnormalZ.xy + worldNormal.xy, worldNormal.z);


			// Swizzle tangent normals to match world orientation and triblend
			half3 newNormal = normalize(
				tnormalX.zyx * blendAxes.x +
				tnormalY.xzy * blendAxes.y +
				tnormalZ.xyz * blendAxes.z
			);

			return newNormal;
		}

		void sampleAndStrength(float origCoord, int sizeInDimension, out int sampleCoord, out float drawStrength) {
			const float overrideLine = (1 / tileSize) / 2; // Point where higher terrain takes complete precedence

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

		/* Surf */

		void surf(Input IN, inout SurfaceOutput o) {
			//fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			/*o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex));*/

			float3 worldNormal = WorldNormalVector(IN, o.Normal);

			float x = IN.layoutCoordinate.x;
			float y = IN.layoutCoordinate.y;

			int floorX = floor(x);
			int floorY = floor(y);

			float3 blendAxes = abs(worldNormal);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			// Sample
			int sampleCoordX = floorX;
			int sampleCoordY = floorY;
			float drawStrengthX = 1;
			float drawStrengthY = 1;

			sampleAndStrength(x, mapLayout.x, sampleCoordX, drawStrengthX);
			sampleAndStrength(y, mapLayout.y, sampleCoordY, drawStrengthY);

			// The color to blend from adjacent index
			int baseIndex = floorY * mapLayout.x + floorX;
			float3 baseColor = triplanar(IN.worldPos, indexScale[layoutTextures[baseIndex]], blendAxes, layoutTextures[baseIndex]);


			// Is this tile selected
			if (hasSelection && floorX - 1 == selection.x && floorY - 1 == selection.y) {
				baseColor *= float3(0, 1, 1);
			}

			float3 otherColor = float3(0, 0, 0);
			int highestSamplePriority = -1;

			// When sampling a corner, merge the two options 
			if (sampleCoordX != floorX && sampleCoordY != floorY) {

				//int sampleIndex = sampleCoordY * mapLayoutWidth + sampleCoordX;
				//int priority = indexPriority[layoutTextures[sampleIndex]];

				int firstSampleIndex = floorY * mapLayout.x + sampleCoordX;
				int secondSampleIndex = sampleCoordY * mapLayout.x + floorX;

				int firstPriority = indexPriority[layoutTextures[firstSampleIndex]];
				int secondPriority = indexPriority[layoutTextures[secondSampleIndex]];

				if (firstPriority == secondPriority) {
					int finalSampleIndex = sampleCoordY * mapLayout.x + sampleCoordX;

					if (indexPriority[layoutTextures[finalSampleIndex]] > firstPriority) {
						highestSamplePriority = indexPriority[layoutTextures[finalSampleIndex]];

						otherColor = triplanar(IN.worldPos, indexScale[layoutTextures[finalSampleIndex]], blendAxes, layoutTextures[finalSampleIndex]);

						float combinedStrength = (drawStrengthX + drawStrengthY) / 1.25;

						if (combinedStrength > 1) {
							combinedStrength = 1;
						}

						drawStrengthX = combinedStrength;
						drawStrengthY = combinedStrength;
					}
					else {
						highestSamplePriority = firstPriority;

						float3 firstColor = triplanar(IN.worldPos, indexScale[layoutTextures[firstSampleIndex]], blendAxes, layoutTextures[firstSampleIndex]);
						float3 secondColor = triplanar(IN.worldPos, indexScale[layoutTextures[secondSampleIndex]], blendAxes, layoutTextures[secondSampleIndex]);

						otherColor = firstColor / 2 + secondColor / 2;
					}
				}

				else if (firstPriority > secondPriority) {
					sampleCoordY = floorY;
					highestSamplePriority = firstPriority;
					otherColor = triplanar(IN.worldPos, indexScale[layoutTextures[firstSampleIndex]], blendAxes, layoutTextures[firstSampleIndex]);
				}

				else {
					sampleCoordX = floorX;
					highestSamplePriority = secondPriority;
					otherColor = triplanar(IN.worldPos, indexScale[layoutTextures[secondSampleIndex]], blendAxes, layoutTextures[secondSampleIndex]);


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
				int sampleIndex = sampleCoordY * mapLayout.x + sampleCoordX;

				// Don't bother the blurr if there is nothing to blurr
				if (drawStrengthX != 1 || drawStrengthY != 1) {
					int textureIndex = layoutTextures[sampleIndex];
					if (textureIndex != -1) {
						highestSamplePriority = indexPriority[layoutTextures[sampleIndex]];
						otherColor = triplanar(IN.worldPos, indexScale[layoutTextures[sampleIndex]], blendAxes, layoutTextures[sampleIndex]);
					}
				}
			}

			// Is our other sample selected
			if (hasSelection && sampleCoordX - 1 == selection.x && sampleCoordY - 1 == selection.y) {
				otherColor *= float3(0, 1, 1);
			}

			// Set drawstrength to a value between 0.5 and 1
			drawStrengthX = (drawStrengthX + 1) / 2;
			drawStrengthY = (drawStrengthY + 1) / 2;

			// We are overridden by adjacent
			if (highestSamplePriority > indexPriority[layoutTextures[baseIndex]]) {
				// We are overridden by adjacent. HEAVILY favour the adjacent color
				drawStrengthX = pow(drawStrengthX, 5);
				drawStrengthY = pow(drawStrengthY, 5);
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
			//o.Albedo = float3(1, 0, 1);

			half3 newWorldNormal = triplanarBump(IN.worldPos, worldNormal, indexScale[layoutTextures[baseIndex]], blendAxes, layoutTextures[baseIndex]);
			//o.Albedo = newWorldNormal;
			o.Normal = newWorldNormal;
		}
		ENDCG
	}

		FallBack "Mobile/Diffuse"
}
