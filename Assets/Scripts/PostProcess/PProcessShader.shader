// Upgrade NOTE: replaced 'defined DIAGONAL_KERNEL' with 'defined (DIAGONAL_KERNEL)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/BWDiffuse" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_DepthNormalTex ("Base (RGB)", 2D) = "white" {}
		_DownSample("DownSample", Int) = 3
		_EdgeColor ("EdgeHighlightColor", Color)  = (1,1,1,1)
		_angleThreshold("Edge Threshold Angle", Float) = 80
		_depthWeight("Weight of depth difference", Float) = 300
		_texelSizeDivider("Factor to divide the depthNormals texture texel size (Affects line thickness)", Range(0.5, 2)) = 2
		_kernelRadius("Radius for pixel lookup", Int) = 1
	}
	SubShader {
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#define PI 3.14159265358979323846264338327
			#include "UnityCG.cginc"
 
			uniform sampler2D _MainTex;
			uniform sampler2D _DepthNormalTex;
			uniform int _DownSample;
			uniform float _angleThreshold, _depthWeight, _texelSizeDivider;
			uniform int _kernelRadius;
			uniform float4 _EdgeColor;


			sampler2D _CameraDepthTexture;
			sampler2D _CameraDepthNormalsTexture;

			float4 _CameraDepthNormalsTexture_TexelSize;

			void GetMaxDeltas(int kernelRadius, float2 center, out float normalDelta, out float depthDelta)
			{
				float4 px_center = tex2D(_CameraDepthNormalsTexture, center);
				float3 normalValue;
				float depthValue;

				float2 stepSize = _CameraDepthNormalsTexture_TexelSize / _texelSizeDivider;
				DecodeDepthNormal(px_center, depthValue, normalValue);

				normalDelta = depthDelta = 0;				
				for(int i = -kernelRadius; i <= kernelRadius; i++)
					for(int j = -(kernelRadius-abs(i)); j <= kernelRadius-abs(i); j++) // USES DIAGONAL KERNEL
					//for(int j = -kernelRadius; j <= kernelRadius; j++)
					{
						float4 px_current = tex2D(_CameraDepthNormalsTexture, center + float2(i * stepSize.x, j * stepSize.y));
						float3 normal_current;
						float depth_current;

						DecodeDepthNormal(px_current, depth_current, normal_current);

						float angle = abs(acos(dot(normalValue, normal_current) / (length(normalValue) * length(normal_current)))) * 180 / PI;
						normalDelta = max(normalDelta, angle);
						depthDelta = max(depthDelta, abs(depth_current - depthValue));						
					}

			}

			void GetDelta(float2 center, out float normalDelta, out float depthDelta) {
				float4 px_center = tex2D(_CameraDepthNormalsTexture, center);
				float3 normalValue;
				float depthValue;
				DecodeDepthNormal(px_center, depthValue, normalValue);

				float4 px_ds = tex2D(_DepthNormalTex, center);
				float3 normalValue_ds;
				float depthValue_ds;
				DecodeDepthNormal(px_ds, depthValue_ds, normalValue_ds);

				float angle = abs(acos(dot(normalValue, normalValue_ds) / (length(normalValue) * length(normalValue_ds)))) * 180 / PI;
				normalDelta = angle;
				depthDelta = abs(depthValue_ds - depthValue);
			}

			struct v2f {
				float4 pos: SV_POSITION;
				float4 scrPos : TEXCOORD1;
			};

			v2f vert(appdata_base v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.scrPos = ComputeScreenPos(o.pos);

				return o;
			}

			float4 frag(v2f i) : COLOR 
			{
				float3 normalValues;
				float depthValue;

				float depthDelta, normalDelta;
				GetMaxDeltas(_kernelRadius, i.scrPos.xy, normalDelta, depthDelta);
				//GetDelta(i.scrPos.xy, normalDelta, depthDelta);
				float delta = step(_angleThreshold, normalDelta) + depthDelta * _depthWeight;

				//clip(2 - delta);

				float4 edgeColor = float4(_EdgeColor.rgb * clamp(delta, 0, 1) , 1);
				return edgeColor;
			}

			ENDCG
		}
	}
}