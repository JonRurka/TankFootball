Shader "Custom/VolumetricClouds" {
	Properties {
        _Iterations("Iterations", Range(0, 200)) = 100
        _ViewDistance("View Distance", Range(0, 5)) = 2
        _SkyColor("Sky Color", Color) = (0.176, 0.478, 0.871, 1)
        _CloudColor("Cloud Color", Color) = (1, 1, 1, 1)
        _CloudDensity("Cloud Density", Range(0, 1)) = 0.5
	}
	SubShader {
        Pass{ 
		    Blend SrcAlpha Zero

		    CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _NoiseOffsets;
            float3 _CamPos;
            float3 _CamRight;
            float3 _CamUp;
            float3 _CamForward;
            float _AspectRatio;
            float _FieldOfView;

            int _Iterations;
            float _ViewDistance;
            float3 _SkyColor;
            float4 _CloudColor;
            float _CloudDensity;

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_base v) {
    			v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.texcoord;
				return o;
            }

            float noise(float3 x) { x *= 4.0; float3 p = floor(x); float3 f = frac(x); f = f*f*(3.0 - 2.0*f); float2 uv = (p.xy + float2(37.0, 17.0)*p.z) + f.xy; float2 rg = tex2D(_NoiseOffsets, (uv + 0.5) / 256.0).yx; return lerp(rg.x, rg.y, f.z); }

            float fbm(float3 pos, int octaves) { float f = 0.; for (int i = 0; i < octaves; i++) { f += noise(pos) / pow(2, i + 1); pos *= 2.01; } f /= 1 - 1 / pow(2, octaves + 1); return f; }

            fixed4 frag(v2f i) : SV_Target{
                float2 uv = (i.uv - 0.5) * _FieldOfView;
                uv.x *= _AspectRatio;

                float3 ray = _CamUp * uv.y + _CamRight * uv.x + _CamForward;
                float3 pos = _CamPos * 0.4;


                float3 p = pos;

                float density = 0;

                for (float i = 0; i < _Iterations; i++) {
                    float f = i / _Iterations;

                    float alpha = smoothstep(0, _Iterations * 0.2, i) * (1 - f) * (1 - f);

                    float denseClouds = smoothstep(_CloudDensity, 0.75, fbm(p, 5));
                    float lightClouds = (smoothstep(-0.2, 1.2, fbm(p * 2, 2)) - 0.5) * 0.5;

                    density += (lightClouds + denseClouds) * alpha;

                    p = pos + ray * f * _ViewDistance;
                }
                float3 color = _SkyColor + (_CloudColor.rgb - 0.5) * (density / _Iterations) * 20 * _CloudColor.a;

                return fixed4(color, 1);
            }
		    ENDCG
        }
	}
}
