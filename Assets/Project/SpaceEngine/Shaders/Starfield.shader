﻿// Procedural planet generator.
// 
// Copyright (C) 2015-2017 Denis Ovchinnikov [zameran] 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. Neither the name of the copyright holders nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION)HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
// THE POSSIBILITY OF SUCH DAMAGE.
// 
// Creation Date: Undefined
// Creation Time: Undefined
// Creator: zameran

Shader "SpaceEngine/Stars/Starfield" 
{
	SubShader 
	{
		Tags { "Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Background" }
		//Blend OneMinusDstColor OneMinusSrcAlpha
		//Blend OneMinusDstAlpha SrcAlpha
		Blend OneMinusDstAlpha OneMinusSrcAlpha
		ZWrite Off 
		Fog { Mode Off }

		Pass
		{	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			uniform float _StarIntensity;
			uniform float4x4 _RotationMatrix;
			uniform float4 _Tab[8];
		
			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float2 texcoord : TEXCOORD;
			};
		
			struct v2f 
			{
				float4 pos : SV_POSITION;
				half4 color : COLOR;
				half2 uv : TEXCOORD0;
			};	
			
			float GetFlickerAmount(in float2 pos)
			{	
				float2 hash = frac(pos.xy * 256);
				float index = frac(hash.x + (hash.y + 1) * (_Time.x * 2 + unity_DeltaTime.z));

				index *= 8;

				float f = frac(index) * 2.5;
				int i = (int)index;

				return _Tab[i].x + f * _Tab[i].y;
			}	
		
			v2f vert(appdata v)
			{
				v2f OUT;

				float3 worldPosition = mul((float3x3)_RotationMatrix, v.vertex.xyz) + _WorldSpaceCameraPos.xyz; 

				float magnitude = 6.5 + v.color.w * (-1.44 - 1.5);
				float brightness = GetFlickerAmount(v.vertex.xy) * pow(5.0, (-magnitude - 1.44) / 2.5);
						
				half4 color = _StarIntensity * half4(brightness * v.color.xyz * 3, brightness);
			
				OUT.pos = mul(UNITY_MATRIX_MVP, float4(worldPosition, 1));
				OUT.color = color;
				OUT.uv = 6.5 * v.texcoord.xy - 6.5 * float2(0.5, 0.5);
			
				return OUT;
			}

			half4 frag(v2f IN) : SV_Target
			{
				half scale = exp(-dot(IN.uv.xy, IN.uv.xy));

				half3 color = IN.color.xyz * scale + 5 * IN.color.w * pow(scale, 10);

				return half4(color, 0);
			}
			ENDCG
		}
	}
}