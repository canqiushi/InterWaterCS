Shader "Unlit/water"
{
	Properties
	{
		_Color("Color",color) = (1,1,1,1)
		_WaveTex ("Wave", 2D) = "white" {}
		_WaveMap("Wave Map",2D) = "bump"{}
		_WaveScale("Wave Scale",float) = 1

		_BottomColor("B Color",color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		GrabPass{"_GrabTex"}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "UnityStandardCoreForward.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;

			};

			struct v2f
			{								
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 screenPos : TEXCOORD1;
				float3 viewDir : TEXCOORD2;
				float3 normalWs : TEXCOORD3;
			};

			struct BRDFData
			{
				half3 diffuse;
				half3 specular;
				half perceptualRoughness;
				half roughness;
				half roughness2;
				half grazingTerm;

				// We save some light invariant BRDF terms so we don't have to recompute
				// them in the light loop. Take a look at DirectBRDF function for detailed explaination.
				half normalizationTerm;     // roughness * 4.0 + 2.0
				half roughness2MinusOne;    // roughness^2 - 1.0
			};

			sampler2D _WaveTex;
			sampler2D _CameraOpaqueTexture;			
			sampler2D _PlaneReflectTex;
			sampler2D _WaveMap;
			sampler2D _CameraDepthTexture;
			fixed _WaveScale;
			fixed4 _BottomColor;

			#define kDieletricSpec half4(0.04, 0.04, 0.04, 1.0 - 0.04) // standard dielectric reflectivity coef at incident angle (= 4%)
			#define HALF_MIN 6.103515625e-5

			half OneMinusReflectivityMetallic(half metallic)
			{
				half oneMinusDielectricSpec = kDieletricSpec.a;
				return oneMinusDielectricSpec - metallic * oneMinusDielectricSpec;
			}

			inline void InitializeBRDFData(half3 albedo, half metallic, half3 specular, half smoothness, half alpha, out BRDFData outBRDFData)
			{
				half oneMinusReflectivity = OneMinusReflectivityMetallic(metallic);
				half reflectivity = 1.0 - oneMinusReflectivity;

				outBRDFData.diffuse = albedo * oneMinusReflectivity;
				outBRDFData.specular = lerp(kDieletricSpec.rgb, albedo, metallic);

				outBRDFData.grazingTerm = saturate(smoothness + reflectivity);
				outBRDFData.perceptualRoughness = 1-smoothness;
				outBRDFData.roughness = max(outBRDFData.perceptualRoughness *outBRDFData.perceptualRoughness , HALF_MIN);
				outBRDFData.roughness2 = outBRDFData.roughness * outBRDFData.roughness;

				outBRDFData.normalizationTerm = outBRDFData.roughness * 4.0h + 2.0h;
				outBRDFData.roughness2MinusOne = outBRDFData.roughness2 - 1.0h;

			#ifdef _ALPHAPREMULTIPLY_ON
				outBRDFData.diffuse *= alpha;
				alpha = alpha * oneMinusReflectivity + reflectivity;
			#endif
			}

			half3 DirectBRDF(BRDFData brdfData, half3 normalWS, half3 lightDirectionWS, half3 viewDirectionWS)
			{
			//#ifndef _SPECULARHIGHLIGHTS_OFF
				float3 halfDir = normalize(float3(lightDirectionWS) + float3(viewDirectionWS));

				float NoH = saturate(dot(normalWS, halfDir));
				half LoH = saturate(dot(lightDirectionWS, halfDir));

				float d = NoH * NoH * brdfData.roughness2MinusOne + 1.00001f;

				half LoH2 = LoH * LoH;
				half specularTerm = brdfData.roughness2 / ((d * d) * max(0.1h, LoH2) * brdfData.normalizationTerm);

			#if defined (SHADER_API_MOBILE) || defined (SHADER_API_SWITCH)
				specularTerm = specularTerm - HALF_MIN;
				specularTerm = clamp(specularTerm, 0.0, 100.0); // Prevent FP16 overflow on mobiles
			#endif

				half3 color = specularTerm * brdfData.specular + brdfData.diffuse;
				return color;
			//#else
			//    return brdfData.diffuse;
			//#endif
			}

			float3 TransformObjectToWorld(float3 positionOS)
			{
				return mul(UNITY_MATRIX_M, float4(positionOS, 1.0)).xyz;
			}

			half CalculateFresnelTerm(half3 normalWS, half3 viewDirectionWS)
			{
				return pow(1.0 - saturate(dot(normalWS, viewDirectionWS)), 5);//fresnel TODO - find a better place
			}

			v2f vert (appdata v)
			{
				v2f o;				
				float h = tex2Dlod(_WaveTex,fixed4(v.uv,0,0)).r * 2 - 1;			
				float3 posWS = TransformObjectToWorld(v.vertex.xyz);
				posWS.y += h * 3;		
				o.pos = UnityWorldToClipPos(posWS);
				o.screenPos = ComputeScreenPos(o.pos);
				o.uv = v.uv;
				o.viewDir = normalize(_WorldSpaceCameraPos - posWS);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float time = _Time.y %1000;				
				
				float h = tex2D(_WaveTex,i.uv).r * 2 - 1;
				float sh = 0.35- h;
				float3 n = float3(exp(pow(sh - 0.75,2) * -20),
											exp(pow(sh - 0.50,2) * -40),
											exp(pow(sh - 0.25,2) * -20));
				
				half3 normal1 = UnpackScaleNormal(tex2D(_WaveMap,(i.uv + time * 0.05)), _WaveScale*1.2);
				half3 normal2 = UnpackScaleNormal(tex2D(_WaveMap,(i.uv + time * 0.025)),_WaveScale);
				half3 normal = BlendNormals(normal1,normal2);
				//normal = BlendNormals(n,normal);
				//float3 normalWs = i.normalWs;
				normal = BlendNormals(normal,n);
				//depth
				float rawD = UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture,UNITY_PROJ_COORD( i.screenPos)));
				float eyeDepth1 = LinearEyeDepth(rawD);
				half lookDepth = abs( ( eyeDepth1 - i.screenPos.w ));		

				//refraction
				fixed2 suv = i.screenPos.xy/i.screenPos.w;
				fixed3 refraction = tex2D(_CameraOpaqueTexture,suv + normal.xz * 0.01).rgb;
				fixed3 grab = refraction;
				//base 
				half waterDepth = lookDepth * 0.15;
				fixed3 waterCol = lerp(_Color,_BottomColor,waterDepth);				
				refraction *= waterCol;

				//fresnel 
				half fresnelTerm = CalculateFresnelTerm(normal,i.viewDir.xyz);
		
				//specular
				BRDFData brdfData;
				InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.95, 1, brdfData);
				half3 spec = DirectBRDF(brdfData, normal, _WorldSpaceLightPos0, i.viewDir) * _LightColor0;

				//reflection 
				half2 refectionUV = suv + normal.zx * half2(0.02,0.03);
				fixed3 reflection = tex2D(_PlaneReflectTex,fixed2(refectionUV.x,1-refectionUV.y));
				reflection *= fresnelTerm;
				reflection = clamp(reflection*0.5 + spec, 0, 1024);
								
				half3 comp = reflection + refraction;
				comp = lerp(grab,comp, saturate(lookDepth));

				return  float4(comp,1);
			}
			ENDCG
		}
	}
}
