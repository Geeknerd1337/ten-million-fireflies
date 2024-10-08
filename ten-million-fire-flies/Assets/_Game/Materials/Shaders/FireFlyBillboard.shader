Shader "Joshomaton/FireflyBillboard"
{
    Properties
    {
        _FarColor("Far color", Color) = (.2, .2, .2, 1)
        _MainTex("Main Texture", 2D) = "white" {} // Define the texture property
        _NearColor("Near color", Color) = (.2, .2, .2, 1)

    }
        SubShader
    {
        Pass
        {
            Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent" // Set render queue to Transparent
                "RenderPipeline" = "UniversalRenderPipeline"
            }

            // Enable blending for transparency
            Blend OneMinusDstColor One
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "noiseSimplex.cginc"
            float4 _FarColor;
		    float4 _NearColor;
            float4 _CameraPosition;  // Adding the camera position
            float4x4 _Rotation;

            StructuredBuffer<float4> position_buffer_1;
            StructuredBuffer<float4> position_buffer_2;
            float4 color_buffer[8];

            // Declare texture and sampler
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

  

            struct attributes
            {
                float3 normal : NORMAL;
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0; // Add UV for texture sampling
                float4 color : COLOR;
            };

            struct varyings
            {
                float4 vertex : SV_POSITION;
                float3 diffuse : TEXCOORD2;
                float3 color : TEXCOORD3;
                float2 uv : TEXCOORD0; // Pass UV to fragment shader
            };

            float3x3 CreateLookAtMatrix(float3 objectPos, float3 targetPos)
            {
                // Forward vector (from object to camera)
                float3 forward = normalize(targetPos - objectPos);

                // Right vector (cross product of up and forward)
                float3 up = float3(0, 1, 0);  // Assuming Y is up
                float3 right = normalize(cross(up, forward));

                // Recalculate the up vector using the right and forward vectors
                up = cross(forward, right);

                // Construct a rotation matrix from right, up, and forward
                return float3x3(right, up, forward);
            }

            float hash(float n) {
                // Improved hash function using bit manipulation
                uint x = asuint(n);  // Convert float to uint for bit manipulation
                x = (x ^ 61u) ^ (x >> 16u);
                x = x + (x << 3u);
                x = x ^ (x >> 4u);
                x = x * 0x27d4eb2du;
                x = x ^ (x >> 15u);
                return frac(asfloat(x));
            }

            float noise(float3 p) {
                float3 i = floor(p);
                float3 f = frac(p);

                f = f * f * (3.0 - 2.0 * f); // Fade function

                float n = i.x + i.y * 57.0 + 113.0 * i.z;

                float res = lerp(lerp(lerp(hash(n + 0.0), hash(n + 1.0), f.x),
                    lerp(hash(n + 57.0), hash(n + 58.0), f.x), f.y),
                    lerp(lerp(hash(n + 113.0), hash(n + 114.0), f.x),
                        lerp(hash(n + 170.0), hash(n + 171.0), f.x), f.y), f.z);
                return res;
            }

            varyings vert(attributes v, const uint instance_id : SV_InstanceID)
            {
  
				
                float4 start = position_buffer_1[instance_id];
                const float3 color = lerp(_NearColor, _FarColor, noise((start.x + start.y + start.z) % 1));
			
                
                float time = sin((_Time.y + (instance_id % 100)) * 0.1);
      
                
				//Different noise value on each axis
				float noiseValueX = noise(time + start.x % 1);
				float noiseValueY = noise(time + start.y % 1);
				float noiseValueZ = noise(time + start.z % 1);

				//Add noise to the position
				start.xyz += float3(noiseValueX, noiseValueY, noiseValueZ) * 10;
                
                
                

                float3x3 lookAtMatrix = CreateLookAtMatrix(start, _CameraPosition.xyz);

                float3 worldPosition = mul(v.vertex, lookAtMatrix) + start;

                varyings o;
                o.vertex = mul(UNITY_MATRIX_VP,float4(worldPosition, 1.0f));
                o.diffuse = saturate(dot(v.normal, _MainLightPosition.xyz));
                o.color = color;
                o.uv = v.uv; // Pass UV coordinates to fragment shader

                return o;
            }

            half4 frag(const varyings i) : SV_Target
            {
                // Sample texture using UV coordinates
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                const float3 lighting = i.diffuse * 1.7;

                // Mix texture color with lighting
                return half4(texColor.rgb * i.color ,texColor.r);
            }
            ENDHLSL
        }
    }
}
