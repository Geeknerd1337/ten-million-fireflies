Shader "Joshomaton/FireFlyModel"
{
    Properties
    {
		[HDR]
        _FarColor("Far color", Color) = (.2, .2, .2, 1)
        _MainTex("Main Texture", 2D) = "white" {} // Define the texture property
    _Emission("Emission Texture", 2D) = "black" {} // Define the texture property
    [HDR]
    _NearColor("Near color", Color) = (.2, .2, .2, 1)

    }
        SubShader
    {
        Pass
        {
            Tags
            {
                "RenderType" = "Opaque"
                "Queue" = "Opaque" // Set render queue to Transparent
                "RenderPipeline" = "UniversalRenderPipeline"
            }


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
            float _SyncedTime;

            StructuredBuffer<float4> nearest_firefly_buffer;
            StructuredBuffer<float4> position_buffer_2;
            float4 color_buffer[8];

            // Declare texture and sampler
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
			TEXTURE2D(_Emission);
			SAMPLER(sampler_Emission);

  

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
                // Forward vector (from object to target), constrained to XZ plane
                float3 forward = normalize(float3(targetPos.x - objectPos.x, 0, targetPos.z - objectPos.z));

                // Right vector (cross product of up and forward)
                float3 up = float3(0, 1, 0);  // Y is up
                float3 right = normalize(cross(up, forward));

                // Apply a 90-degree rotation around the Y-axis to the forward vector
                float angle = radians(-90.0);
                float3 rotatedForward = float3(
                    forward.x * cos(angle) - forward.z * sin(angle),
                    0,
                    forward.x * sin(angle) + forward.z * cos(angle)
                    );

                // Recalculate right vector after the rotation
                right = normalize(cross(up, rotatedForward));

                // Construct the rotation matrix from the rotated forward, right, and up vectors
                return float3x3(right, up, rotatedForward);
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
  
				
                float4 start = nearest_firefly_buffer[instance_id];
                float3 color = lerp(_NearColor, _FarColor, noise(instance_id % 1));
          
				
			
                
                float time = sin((_SyncedTime + ((start.x + start.y + start.z) % 100)) * 0.1);
      
                float noiseScalar = 10;
				//Different noise value on each axis
				float noiseValueX = noise(time + start.x % 1);
				float noiseValueY = noise(time + start.y % 1);
				float noiseValueZ = noise(time + start.z % 1);
                float3 noise_now = float3(noiseValueX, noiseValueY, noiseValueZ) * noiseScalar;

                float time_future = sin(((_Time.y + 0.01) + ((start.x + start.y + start.z) % 100)) * 0.1);
				//Noise future
				float noiseValueX_future = noise(time_future + start.x % 1);
				float noiseValueY_future = noise(time_future + start.y % 1);
                float noiseValueZ_future = noise(time_future + start.z % 1);
				float3 noise_future = float3(noiseValueX_future, noiseValueY_future, noiseValueZ_future) * noiseScalar;
			    
                float3x3 lookAtMatrix = CreateLookAtMatrix(start.xyz + noise_now.xyz, start.xyz + noise_future.xyz);
                
				//Add noise to the position
				start.xyz += noise_now.xyz;
                
                
                

                

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
				half4 emission = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, i.uv);

                // Mix texture color with lighting
                return half4(lerp(texColor.rgb, i.color.rgb, emission.r),texColor.r);
            }
            ENDHLSL
        }
    }
}
