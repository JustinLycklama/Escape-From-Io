// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/VertexShader"
{
    Properties
    {
 


	}
		SubShader
	{
		Pass {
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			struct VertInput {
				float4 pos : POSITION;
			};

			struct VertOutput {
				float4 pos : SV_POSITION;
				half3 color : COLOR;
			};

			VertOutput vert(VertInput i) {
				VertOutput o;

				o.pos = UnityObjectToClipPos(i.pos);
				o.color = i.pos.xyz;

				return o;
			}

			half4 frag(VertOutput i) : COLOR {
				return half4(i.color, 1.0f);
			}

			ENDCG
		}
    }
    FallBack "Diffuse"
}
