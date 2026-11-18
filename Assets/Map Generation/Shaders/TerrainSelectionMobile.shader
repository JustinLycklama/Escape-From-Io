// Terrain shader for every MapContainer (created in MapsManager). Blends each layout tile's texture into its neighbours.
//
// #26: every pixel takes a weighted blend of the 3x3 layout tiles around it. A tile's weight falls off exponentially with the
// distance (in tile units) from the pixel to that tile's square, so the blend is continuous across edges and rounds off at
// corners, with no special cases. Higher-priority regions (mountain > land > water) get a weight boost, which pushes their
// texture a little past the tile edge and over the cliff quad between the two. Albedo, selection tint and bump all blend
// with the same weights.

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
		// 3.5 for texture arrays. Note UnpackScaleNormal only applies its scale at target >= 3.0; the old shader passed 1000
		// at the default target, where it was ignored, so the bump strength here is 1 to keep the same look.
		#pragma target 3.5

		float2 mapLayout;
		float tileSize;

		float4 mapOffset;

		// Given a layout width and height of 5, plus 2 for overhang in each direction
		float layoutTextures[7*7];
		UNITY_DECLARE_TEX2DARRAY(baseTextures);
		UNITY_DECLARE_TEX2DARRAY(bumpMapTextures);

		float indexPriority[20];
		float indexScale[20];

		// Height band per texture index, in mesh-local height (normalized 0..1), set by MapContainer.SetupMaterialShader (#31).
		// A texture only shows between its low and high edge: water below the water line, ground on its plateau, rock above it.
		float heightLow[20];
		float heightHigh[20];

		// Max width / height per map is 10x10
		float actionAtIndex[10*10];

		float2 selection;

		// Used as a bool. 0 is skip selection calculations
		float hasSelection;

		// Blend tuning, in layout-tile units (one tile is tileSize height samples, so 0.1 = one sample)
		static const float blendFalloff = 0.045;    // exponential falloff distance of a neighbour's influence
		static const float priorityReach = 0.07;     // how far each priority step pushes a texture past its tile edge
		static const float minimumWeight = 0.004;    // skip sampling tiles below this (normalized) weight
		static const float heightSoftness = 0.006;   // half-width of the fade at a height band edge (0.006 = 0.6 world units)

		struct Input
		{
			// Pre-Defined
			float3 worldPos;
			float3 worldNormal; INTERNAL_DATA

			// Custom
			float2 layoutCoordinate;
			float localHeight;   // mesh-local y = normalized terrain height

			// World-space tangent frame, built the same way Unity's surface shader does, so surf can convert
			// its world-space bump normal into the tangent space o.Normal expects (#27)
			float3 frameTangent;
			float3 frameBinormal;
			float3 frameNormal;
		};

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.frameNormal = UnityObjectToWorldNormal(v.normal);
			o.frameTangent = UnityObjectToWorldDir(v.tangent.xyz);
			o.frameBinormal = cross(o.frameNormal, o.frameTangent) * v.tangent.w * unity_WorldTransformParams.w;

			float4 vertex = v.vertex;

			float layoutCoordinateX = ((vertex.x - mapOffset.r) / (tileSize));
			float layoutCoordinateY = ((-vertex.z - mapOffset.b) / (tileSize));

			o.layoutCoordinate = float2(layoutCoordinateX + 1, layoutCoordinateY + 1);
			o.localHeight = vertex.y;
		}

		/* Surface Helpers */

		float3 triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;
			float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
			float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
			float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
			return xProjection + yProjection + zProjection;
		}

		// Triplanar normal map with UDN blend, returned in world space
		half3 triplanarBump(float3 worldPos, float3 worldNormal, float scale, float3 blendAxes, int textureIndex) {
			float3 scaledWorldPos = worldPos / scale;

			half3 tnormalX = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.z, scaledWorldPos.y, textureIndex)), 1);
			half3 tnormalY = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)), 1);
			half3 tnormalZ = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(bumpMapTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)), 1);

			// Swizzle world normals into tangent space and apply UDN blend
			tnormalX = half3(tnormalX.xy + worldNormal.zy, worldNormal.x);
			tnormalY = half3(tnormalY.xy + worldNormal.xz, worldNormal.y);
			tnormalZ = half3(tnormalZ.xy + worldNormal.xy, worldNormal.z);

			// Swizzle tangent normals to match world orientation and triblend
			return normalize(
				tnormalX.zyx * blendAxes.x +
				tnormalY.xzy * blendAxes.y +
				tnormalZ.xyz * blendAxes.z
			);
		}

		// Distance from point p to the unit square [tile, tile + 1], in tile units (0 inside)
		float distanceToTile(float2 p, float2 tile) {
			float2 outside = max(max(tile - p, p - (tile + 1)), 0);
			return length(outside);
		}

		/* Surf */

		void surf(Input IN, inout SurfaceOutput o) {
			float3 worldNormal = WorldNormalVector(IN, o.Normal);

			// Triplanar projection weights come from the triangle's real facing (#28), not the interpolated vertex normal:
			// terrain normals are creased flat at cliff feet (MeshGenerator.CalculateNormals), which picked the top-down
			// projection for cliff faces and stretched the texture down them. Lighting still uses the smooth normals.
			float3 faceNormal = normalize(cross(ddy(IN.worldPos), ddx(IN.worldPos)));
			float3 blendAxes = pow(abs(faceNormal), 3);
			blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

			float2 p = IN.layoutCoordinate;
			int2 centre = int2(floor(p));

			// Pass 1: weights for the 3x3 neighbourhood
			float weights[9];
			int indices[9];
			float totalWeight = 0;

			[unroll]
			for (int i = 0; i < 9; i++) {
				int2 tile = centre + int2(i % 3 - 1, i / 3 - 1);

				bool inMap = tile.x >= 0 && tile.y >= 0 && tile.x < mapLayout.x && tile.y < mapLayout.y;
				int layoutIndex = tile.y * mapLayout.x + tile.x;
				int textureIndex = inMap ? (int)layoutTextures[layoutIndex] : -1;

				float weight = 0;
				if (textureIndex >= 0) {
					float reach = indexPriority[textureIndex] * priorityReach;
					weight = exp(-(distanceToTile(p, tile) - reach) / blendFalloff);

					// Height band (#31): the distance blend is purely horizontal, so near a bank or cliff foot it put water up the
					// bank and rock out onto flat ground. Fade each texture outside its region's heights. The small floor keeps a
					// valid blend if every candidate is out of band (e.g. a tile mid-terraform).
					float h = IN.localHeight;
					float inBand = smoothstep(heightLow[textureIndex] - heightSoftness, heightLow[textureIndex] + heightSoftness, h)
						* (1 - smoothstep(heightHigh[textureIndex] - heightSoftness, heightHigh[textureIndex] + heightSoftness, h));
					weight *= inBand + 0.001;
				}

				weights[i] = weight;
				indices[i] = textureIndex;
				totalWeight += weight;
			}

			// Pass 2: blend albedo and bump of every tile with meaningful weight
			float3 albedo = 0;
			float3 normal = 0;
			float usedWeight = 0;

			[unroll]
			for (int j = 0; j < 9; j++) {
				float weight = weights[j] / max(totalWeight, 1e-5);

				if (weight > minimumWeight || j == 4) {
					int textureIndex = indices[j];
					if (textureIndex < 0) {
						continue;
					}

					int2 tile = centre + int2(j % 3 - 1, j / 3 - 1);
					int layoutIndex = tile.y * mapLayout.x + tile.x;
					float scale = indexScale[textureIndex];

					float3 colour = triplanar(IN.worldPos, scale, blendAxes, textureIndex);

					// Selection and queued-action tints belong to their tile, so they blend at the edges too
					if (hasSelection > 0.5 && tile.x - 1 == selection.x && tile.y - 1 == selection.y) {
						colour *= float3(0, 1, 1);
					}

					if (actionAtIndex[layoutIndex] > 0) {
						colour *= float3(0.75, 0.75, 1);
					}

					albedo += colour * weight;
					normal += triplanarBump(IN.worldPos, worldNormal, scale, blendAxes, textureIndex) * weight;
					usedWeight += weight;
				}
			}

			usedWeight = max(usedWeight, 1e-5);
			o.Albedo = albedo / usedWeight;
			// The bump blend is a world-space normal, but surface shaders expect o.Normal in tangent space (#27).
			// Writing the world normal directly scrambled the lighting (flat ground lit like a wall, slopes lit from
			// the wrong side). The terrain mesh has tangents for this (MeshData.FinalizeMesh).
			float3 bumpedWorldNormal = normalize(normal / usedWeight);
			o.Normal = normalize(float3(
				dot(bumpedWorldNormal, IN.frameTangent),
				dot(bumpedWorldNormal, IN.frameBinormal),
				dot(bumpedWorldNormal, IN.frameNormal)));
		}
		ENDCG
	}

	FallBack "Mobile/Diffuse"
}
