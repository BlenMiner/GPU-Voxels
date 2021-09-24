Shader "Hidden/SEGIVoxelizeScene_CC" {
	Properties
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		_Cutoff ("Alpha Cutoff", Range(0,1)) = 0.333
		_BlockerValue ("Blocker Value", Range(0, 10)) = 0
	}
	SubShader 
	{
		Cull Off
		ZTest Always
		
		Pass
		{
			CGPROGRAM
			
				#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom
				#include "UnityCG.cginc"
				
				RWTexture3D<uint> RG0;
				
				int LayerToVisualize;
				
				float4x4 SEGIVoxelViewFront;
				float4x4 SEGIVoxelViewLeft;
				float4x4 SEGIVoxelViewTop;
				
				sampler2D _MainTex;
				sampler2D _EmissionMap;
				float _Cutoff;
				float4 _MainTex_ST;
				half4 _EmissionColor;

				float SEGISecondaryBounceGain;
				
				float _BlockerValue;
				
				
				struct v2g
				{
					float4 pos : SV_POSITION;
					half4 uv : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float angle : TEXCOORD2;
				};
				
				struct g2f
				{
					float4 pos : SV_POSITION;
					half4 uv : TEXCOORD0;
					float3 normal : TEXCOORD1;
					float angle : TEXCOORD2;
				};

				struct FaceData
                {
                    int blockFaceMask;
                    int blockUVs;
                    float3 position;
                };

				#ifdef SHADER_API_D3D11

                StructuredBuffer<FaceData> _Faces;

                UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(int, m_size_x)
                UNITY_DEFINE_INSTANCED_PROP(int, m_size_y)
                UNITY_DEFINE_INSTANCED_PROP(int, m_size_z)
                UNITY_DEFINE_INSTANCED_PROP(float3, m_worldPos)
                UNITY_INSTANCING_BUFFER_END(Props)

                #endif

                #define BLOCK_UV_SIZE 0.0625

                static const uint M_Top = 1u;
                static const uint M_Bottom = 2u;
                static const uint M_Left = 4u;
                static const uint M_Right = 8u;
                static const uint M_Front = 16u;
                static const uint M_Back = 32u;

                float4 GetOffset(uint vertexIndex)
                {
                    switch(vertexIndex)
                    {
                        case 1:
                            return float4(0, 1, 0, 0);
                        case 4:
                        case 2:
                            return float4(1, 1, 0, 0);
                        case 5:
                            return float4(1, 0, 0, 0);
                        default:
                            return float4(0, 0, 0, 0);
                    }
                }

                float4 GetOffsetInverted(uint vertexIndex)
                {
                    switch(vertexIndex)
                    { 
                        case 2:
                            return float4(0, 1, 0, 0);
                        case 5:
                        case 1:
                            return float4(1, 1, 0, 0);
                        case 4:
                            return float4(1, 0, 0, 0);
                        default:
                            return float4(0, 0, 0, 0);
                    }
                }

                int GetUV(int value, int pos)
                {
                    value >>= (pos * 4);
                    return value & 0xF;
                }

                float4 TransformUV(float4 uv, int blockUVs, int3 normal)
                {
                    float4 offset = float4(0, 0, 0, 0);
                    
                    if (normal.y > 0)
                    {
                        offset.x = GetUV(blockUVs, 0);
                        offset.y = GetUV(blockUVs, 1);
                    }
                    else if (normal.y < 0)
                    {
                        offset.x = GetUV(blockUVs, 2);
                        offset.y = GetUV(blockUVs, 3);
                    }
                    else if (normal.z > 0)
                    {
                        offset.x = GetUV(blockUVs, 6);
                        offset.y = GetUV(blockUVs, 7);
                    }
                    else
                    {
                        offset.x = GetUV(blockUVs, 4);
                        offset.y = GetUV(blockUVs, 5);
                    }



                    uv *= BLOCK_UV_SIZE;
                    uv.y = 1 - uv.y;

                    return (offset / 16.0) + uv;
                }

                float4 GetVertex(FaceData face, uint vertexIndex, out float4 uv, out float3 normal)
                {
                    float4 pos = float4(face.position.xyz, 1);

                    switch(face.blockFaceMask)
                    {
                        case M_Top:
                            ++pos.y;
                            uv = GetOffset(vertexIndex);
                            pos.xz += uv.xy;
                            normal = float3(0, 1, 0);
                            break;
                        case M_Bottom:
                            uv = GetOffsetInverted(vertexIndex);
                            pos.xz += uv.xy;
                            normal = float3(0, -1, 0);
                            break;

                        case M_Left:
                            uv = GetOffset(vertexIndex);
                            pos.yz += uv.xy;
                            normal = float3(-1, 0, 0);

                            uv = float4(uv.y, 1 - uv.x, 0, 0);
                            break;

                        case M_Right:
                            ++pos.x;
                            uv = GetOffsetInverted(vertexIndex);
                            pos.yz += uv.xy;
                            normal = float3(1, 0, 0);
                            uv = float4(uv.y, 1 - uv.x, 0, 0);
                            break;

                        case M_Front:
                            ++pos.z;
                            uv = GetOffsetInverted(vertexIndex);
                            pos.xy += uv.xy;
                            normal = float3(0, 0, 1);
                            uv.y = 1 - uv.y;
                            break;

                        case M_Back:
                            uv = GetOffset(vertexIndex);
                            pos.xy += uv.xy;
                            normal = float3(0, 0, -1);
                            uv.y = 1 - uv.y;
                            break;

                        default: break;
                    }

                    uv = TransformUV(uv, face.blockUVs, normal);
                    return pos;
                }

                struct c_appdata_full {
                    float4 vertex : POSITION;
                    float4 tangent : TANGENT;
                    float3 normal : NORMAL;
                    float4 texcoord : TEXCOORD0;
                    float4 texcoord1 : TEXCOORD1;
                    float4 texcoord2 : TEXCOORD2;
                    float4 texcoord3 : TEXCOORD3;
                    fixed4 color : COLOR;
                    
                    uint id : SV_VertexID;
                    uint instanceID : SV_InstanceID;
                };
				
				half4 _Color;
				
				v2g vert(c_appdata_full v)
				{
					v2g o;
					
					#ifdef SHADER_API_D3D11
                    FaceData face = _Faces[v.id / 6];

					float4 vertex = GetVertex(face, v.id % 6, v.texcoord, v.normal);

                    #endif
					
					o.normal = UnityObjectToWorldNormal(v.normal);
					float3 absNormal = abs(o.normal);
					
					o.pos = vertex;
					
					o.uv = float4(TRANSFORM_TEX(v.texcoord.xy, _MainTex), 1.0, 1.0);
					
					
					return o;
				}
				
				int SEGIVoxelResolution;
				int SEGIVoxelAA;
				#define VoxelResolution (SEGIVoxelResolution * (1 + SEGIVoxelAA))

				float4x4 SEGIVoxelVPFront;
				float4x4 SEGIVoxelVPLeft;
				float4x4 SEGIVoxelVPTop;
				
				[maxvertexcount(3)]
				void geom(triangle v2g input[3], inout TriangleStream<g2f> triStream)
				{
					v2g p[3];
					for (int i = 0; i < 3; i++)
					{
						p[i] = input[i];
						p[i].pos = UnityObjectToClipPos(p[i].pos);	
					}
					
					

					float3 realNormal = float3(0.0, 0.0, 0.0);
					
					float3 V = p[1].pos.xyz - p[0].pos.xyz;
					float3 W = p[2].pos.xyz - p[0].pos.xyz;
					
					realNormal.x = (V.y * W.z) - (V.z * W.y);
					realNormal.y = (V.z * W.x) - (V.x * W.z);
					realNormal.z = (V.x * W.y) - (V.y * W.x);
					
					float3 absNormal = abs(realNormal);
					

					
					int angle = 0;
					if (absNormal.z > absNormal.y && absNormal.z > absNormal.x)
					{
						angle = 0;
					}
					else if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
					{
						angle = 1;
					}
					else if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
					{
						angle = 2;
					}
					else
					{
						angle = 0;
					}
					
					for (int i = 0; i < 3; i ++)
					{
						float3 op = p[i].pos.xyz * float3(1.0, 1.0, 1.0);
						op.z = op.z * 2.0 - 1.0;

						if (angle == 0)
						{
							p[i].pos.xyz = op.xyz;	
						}
						else if (angle == 1)
						{
							p[i].pos.xyz = op.zyx * float3(1.0, 1.0, -1.0);
						}
						else
						{
							p[i].pos.xyz = op.xzy * float3(1.0, 1.0, -1.0);
						}

						p[i].pos.z = p[i].pos.z * 0.5 + 0.5;
						
						#if defined(UNITY_REVERSED_Z)
						p[i].pos.z = 1.0 - p[i].pos.z;
						#else
						p[i].pos.z *= -1.0;
						#endif
						
						p[i].angle = (float)angle;
					}
					
					triStream.Append(p[0]);
					triStream.Append(p[1]);
					triStream.Append(p[2]);
				}

				float3 rgb2hsv(float3 c)
				{
					float4 k = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
					float4 p = lerp(float4(c.bg, k.wz), float4(c.gb, k.xy), step(c.b, c.g));
					float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

					float d = q.x - min(q.w, q.y);
					float e = 1.0e-10;

					return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
				}

				float3 hsv2rgb(float3 c)
				{
					float4 k = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
					float3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
					return c.z * lerp(k.xxx, saturate(p - k.xxx), c.y);
				}

				float4 DecodeRGBAuint(uint value)
				{
					uint ai = value & 0x0000007F;
					uint vi = (value / 0x00000080) & 0x000007FF;
					uint si = (value / 0x00040000) & 0x0000007F;
					uint hi = value / 0x02000000;

					float h = float(hi) / 127.0;
					float s = float(si) / 127.0;
					float v = (float(vi) / 2047.0) * 10.0;
					float a = ai * 2.0;

					v = pow(v, 3.0);

					float3 color = hsv2rgb(float3(h, s, v));

					return float4(color.rgb, a);
				}

				uint EncodeRGBAuint(float4 color)
				{
					//7[HHHHHHH] 7[SSSSSSS] 11[VVVVVVVVVVV] 7[AAAAAAAA]
					float3 hsv = rgb2hsv(color.rgb);
					hsv.z = pow(hsv.z, 1.0 / 3.0);

					uint result = 0;

					uint a = min(127, uint(color.a / 2.0));
					uint v = min(2047, uint((hsv.z / 10.0) * 2047));
					uint s = uint(hsv.y * 127);
					uint h = uint(hsv.x * 127);

					result += a;
					result += v * 0x00000080; // << 7
					result += s * 0x00040000; // << 18
					result += h * 0x02000000; // << 25

					return result;
				}

				void interlockedAddFloat4(RWTexture3D<uint> destination, int3 coord, float4 value)
				{
					uint comp;
					uint orig = destination[coord];

					[allow_uav_condition]
					do
					{
						comp = orig;
						InterlockedCompareExchange(destination[coord], comp, EncodeRGBAuint(max(DecodeRGBAuint(orig), value)), orig);
					} 
					while (orig != comp);
				}

				void interlockedAddFloat4b(RWTexture3D<uint> destination, int3 coord, float4 value)
				{
					uint comp;
					uint orig = destination[coord];

					[allow_uav_condition]
					do
					{
						comp = orig;
						InterlockedCompareExchange(destination[coord], comp, EncodeRGBAuint(max(DecodeRGBAuint(orig), value)), orig);
					} 
					while (orig != comp);
				}

				float4x4 SEGIVoxelToGIProjection;
				float4x4 SEGIVoxelProjectionInverse;
				sampler2D SEGISunDepth;
				float4 SEGISunlightVector;
				float4 GISunColor;
				float4 SEGIVoxelSpaceOriginDelta;
				
				sampler3D SEGICurrentIrradianceVolume;
				int SEGIInnerOcclusionLayers;

				


				float4 SEGIClipmapOverlap;
				
				float4 frag (g2f input) : SV_TARGET
				{
					int3 coord = int3((int)(input.pos.x), (int)(input.pos.y), (int)(input.pos.z * VoxelResolution));
					
					float3 absNormal = abs(input.normal);
					
					int angle = 0;
					
					angle = (int)round(input.angle);
					
					if (angle == 1)
					{
						coord.xyz = coord.zyx;
						coord.z = VoxelResolution - coord.z - 1;
					}
					else if (angle == 2)
					{
						coord.xyz = coord.xzy;
						coord.y = VoxelResolution - coord.y - 1;
					}
					
					float3 fcoord = (float3)(coord.xyz) / VoxelResolution;

					float3 minCoord = (SEGIClipmapOverlap.xyz * 1.0 + 0.5) - SEGIClipmapOverlap.w * 0.5;
					minCoord += 16.0 / VoxelResolution;
					float3 maxCoord = (SEGIClipmapOverlap.xyz * 1.0 + 0.5) + SEGIClipmapOverlap.w * 0.5;
					maxCoord -= 16.0 / VoxelResolution;


					if (fcoord.x > minCoord.x && fcoord.x < maxCoord.x &&
						fcoord.y > minCoord.y && fcoord.y < maxCoord.y &&
						fcoord.z > minCoord.z && fcoord.z < maxCoord.z)
					{
						discard;
					}


					float4 shadowPos = mul(SEGIVoxelProjectionInverse, float4(fcoord * 2.0 - 1.0, 0.0));
					shadowPos = mul(SEGIVoxelToGIProjection, shadowPos);
					shadowPos.xyz = shadowPos.xyz * 0.5 + 0.5;
					
					float sunDepth = tex2Dlod(SEGISunDepth, float4(shadowPos.xy, 0, 0)).x;
					#if defined(UNITY_REVERSED_Z)
					sunDepth = 1.0 - sunDepth;
					#endif

					float sunVisibility = saturate((sunDepth - shadowPos.z + 0.2525) * 1000.0);


					float sunNdotL = saturate(dot(input.normal, -SEGISunlightVector.xyz));
					
					float4 tex = tex2D(_MainTex, input.uv.xy);
					float4 emissionTex = tex2D(_EmissionMap, input.uv.xy);
					
					float4 color = _Color;

					if (length(_Color.rgb) < 0.0001)
					{
						color.rgb = float3(1, 1, 1);
					}
					else
					{
						color.rgb *= color.a;
					}

					
					float3 col = sunVisibility.xxx * sunNdotL * color.rgb * tex.rgb * GISunColor.rgb * GISunColor.a + _EmissionColor.rgb * 0.9 * emissionTex.rgb;

					float4 prevBounce = tex3D(SEGICurrentIrradianceVolume, fcoord + SEGIVoxelSpaceOriginDelta.xyz);
					col.rgb += prevBounce.rgb * 0.2 * SEGISecondaryBounceGain * tex.rgb * color.rgb;

					 
					float4 result = float4(col.rgb, 2.0);

					
					const float sqrt2 = sqrt(2.0) * 1.2;

					coord /= SEGIVoxelAA + 1;


					if (_BlockerValue > 0.01)
					{
						result.a += 20.0;
						result.a += _BlockerValue;
						result.rgb = float3(0.0, 0.0, 0.0);
					}

					interlockedAddFloat4(RG0, coord, result);

					if (SEGIInnerOcclusionLayers > 0)
					{
						interlockedAddFloat4b(RG0, coord - int3((int)(input.normal.x * sqrt2 * 1.0), (int)(input.normal.y * sqrt2 * 1.0), (int)(input.normal.z * sqrt2 * 1.0)), float4(0.0, 0.0, 0.0, 14.0));
					}

					if (SEGIInnerOcclusionLayers > 1)
					{
						interlockedAddFloat4b(RG0, coord - int3((int)(input.normal.x * sqrt2 * 2.0), (int)(input.normal.y * sqrt2 * 2.0), (int)(input.normal.z * sqrt2 * 2.0)), float4(0.0, 0.0, 0.0, 22.0));
					}
					
					return float4(0.0, 0.0, 0.0, 0.0);
				}
			
			ENDCG
		}
	} 
	FallBack Off
}
