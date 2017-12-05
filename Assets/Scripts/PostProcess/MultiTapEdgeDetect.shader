Shader "Hidden/MultiTapEdgeDetect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_angleThreshold("Edge Threshold Angle", Float) = 80
		_depthWeight("Weight of depth difference", Float) = 300
		_EdgeColor ("EdgeHighlightColor", Color)  = (1,1,1,1)
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define PI 3.14159265358979323846264338327
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 pos : SV_POSITION;
				half2 uv : TEXCOORD0;
				half2 taps[4] : TEXCOORD1;
			};

			uniform float _angleThreshold, _depthWeight;
			uniform float4 _EdgeColor;

			sampler2D _MainTex;
			half4 _MainTex_TexelSize;
			half4 _BlurOffsets;
			sampler2D _CameraDepthNormalsTexture;
			float4 _CameraDepthNormalsTexture_TexelSize;

			void GetMaxDeltas(float2 center, half2 taps[4], out float normalDelta, out float depthDelta)
			{
				float4 px_center = tex2D(_CameraDepthNormalsTexture, center);
				float3 normalValue;
				float depthValue;

				DecodeDepthNormal(px_center, depthValue, normalValue);

				normalDelta = depthDelta = 0;				
				for(int i = 0; i < 4; i++)
				{
					float4 px_current = tex2D(_CameraDepthNormalsTexture, taps[i]);
					float3 normal_current;
					float depth_current;

					DecodeDepthNormal(px_current, depth_current, normal_current);

					float angle = abs(acos(dot(normalValue, normal_current) / (length(normalValue) * length(normal_current)))) * 180 / PI;
					normalDelta = max(normalDelta, angle);
					depthDelta = max(depthDelta, abs(depth_current - depthValue));						
				}

			}

			v2f vert (appdata_img v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord - _BlurOffsets.xy * _MainTex_TexelSize.xy;

				o.taps[0] = o.uv + _MainTex_TexelSize * _BlurOffsets.xy;
				o.taps[1] = o.uv - _MainTex_TexelSize * _BlurOffsets.xy;
				o.taps[2] = o.uv + _MainTex_TexelSize * _BlurOffsets.xy * half2(1,-1);
				o.taps[3] = o.uv - _MainTex_TexelSize * _BlurOffsets.xy * half2(1,-1);
				return o;
			}
			

			//fixed4 frag (v2f i) : SV_Target
			//{
			//	fixed4 col = tex2D(_MainTex, i.uv);
			//	// just invert the colors
			//	col = 1 - col;
			//	return col;
			//}

			fixed4 frag (v2f i) : SV_Target
			{
				float depthDelta, normalDelta;
				GetMaxDeltas(i.uv, i.taps, normalDelta, depthDelta);
				float delta = step(_angleThreshold, normalDelta) + depthDelta * _depthWeight;

				float4 edgeColor = float4(_EdgeColor.rgb * clamp(delta, 0, 1) , 1);
				return edgeColor;

				//fixed4 col = tex2D(_MainTex, i.uv);
				//// just invert the colors
				//col = 1 - col;
				//return col;
			}
			ENDCG
		}
	}
}
