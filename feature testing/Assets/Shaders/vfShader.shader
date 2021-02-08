Shader "Blackwood/vfShader" 
{
    Properties
    {
        [Toggle(_BASE_COLOR_ON)] _ToggleBaseColor("Use Base Color", int) = 0
        [ShowIf(_BASE_COLOR_ON)] _Color ("Base Color (RGB)", Color) = (0,0,0,0)
        [Toggle(_HSV_ON)] _ToggleHSV("Use HSV", int) = 0
        _Hue ("Hue", Range(0, 1)) = 0
        _Saturation ("Saturation", Range(0,1)) = 0
        _Value ("Value", Range(0, 1)) = 0
        
        [Toggle(_BASE_MAP_ON)] _ToggleBaseMap("Use Base Map", int) = 0
        [ShowIf(_BASE_MAP_ON)] _MainTex ("Texture", 2D) = "white" {}
        
        //_LightningDirection ("LightDir", Vector) = (1, 1, 1, 0)
        //_LightColor ("Light Color (RGB)", Color) = (0.3, 0.6, 0.6, 1)
        
        
        [Toggle(_NORMAL_MAP_ON)] _ToggleBumpMap ("Use Normal Map", int) = 0
        [Normal][NoScaleOffset] _BumpMap ("Bump Map", 2D) = "bump" {}
        
        [Toggle(_AMBIENT_ON)] _ToggleAmbient("Use Ambient Light", int) = 0
        _AmbientFactor ("Ambient Factor", Range(0,1)) = 0.6
    
        [Toggle(_SPECULAR_ON)] _ToggleSpecular("Use Specular", int) = 0
        _Gloss ("Gloss", Range(0.01, 1)) = 1
        
        [Toggle(_ATTENUATION_ON)] _ToggleAttenuation("Use Attenuation", int) = 0
        
        [Toggle(_EMISSION_ON)] _ToggleEmission("Use Emission", int) = 0
        [NoScaleOffset] _EmissionMap ("Emission Map", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color (HDR)" , Color) = (0, 0, 0, 0)
    }
    CGINCLUDE

    half remap(
            half inMin, half inMax,
            half outMin, half outMax,
            half value )
    {
        return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
    }
    
    float3 HSV_To_RGB(float hue, float saturation, float value)
        {
                float3 HSV = float3(hue, saturation, value);
                float3 RGB = HSV.z;
                   
                   float var_h = HSV.x * 6;
                   float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
                   float var_1 = HSV.z * (1.0 - HSV.y);
                   float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                   float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                   if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                   else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                   else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                   else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                   else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                   else                 { RGB = float3(HSV.z, var_1, var_2); }
       
           return (RGB);
        }
    
    
    
    ENDCG
    
    SubShader
    {
        Pass
        {
            Tags
			{
			    "RenderType" = "Opaque"
			    "RenderPipeline" = "UniversalPipeline"
				"LightMode" = "UniversalForward"
			}
			
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _ _BASE_COLOR_ON
            #pragma shader_feature_local _ _BASE_MAP_ON
            #pragma shader_feature_local _ _NORMAL_MAP_ON
            #pragma shader_feature_local _ _HSV_ON
            #pragma shader_feature_local _ _SPECULAR_ON
            #pragma shader_feature_local _ _ATTENUATION_ON
            #pragma shader_feature_local _ _EMISSION_ON
            
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct  VertexInput
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
            };

            struct VertexOutput
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 vertex : SV_POSITION; // clip space position
                float3 normal : NORMAL;
                float3 worldNormal : TEXCOORD1;
                float3 worldVertex : TEXCOORD2;
                
                half3 tspace0 : TEXCOORD3; // tangent.x, bitangent.x, normal.x
                half3 tspace1 : TEXCOORD4; // tangent.y, bitangent.y, normal.y
                half3 tspace2 : TEXCOORD5; // tangent.z, bitangent.z, normal.z
            };

            CBUFFER_START(UnityPerMaterial)
            
            sampler2D _MainTex;
            sampler2D _BumpMap;
            fixed4 _Color;
            #if _HSV_ON
                float _Hue;
                float _Saturation;
                float _Value;
            #endif
            float3 _LightningDirection;
            fixed3 _LightColor;
            fixed3 _AmbientColor;
            fixed _AmbientFactor;
            float3 _EmissionColor;
            
            #if _SPECULAR_ON
                fixed _Gloss;
            #endif
            
            CBUFFER_END
            
            // vertex shader
            VertexOutput vert (VertexInput input)
            {
                VertexOutput output;
                
                half3 worldNormal = UnityObjectToWorldNormal(input.normal);
                half3 wTangent = UnityObjectToWorldDir(input.tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                half tangentSign = input.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(worldNormal, wTangent) * tangentSign;
                
                // output the tangent space matrix
                output.tspace0 = half3(wTangent.x, wBitangent.x, worldNormal.x);
                output.tspace1 = half3(wTangent.y, wBitangent.y, worldNormal.y);
                output.tspace2 = half3(wTangent.z, wBitangent.z, worldNormal.z);
                
                output.uv = input.uv;
                output.worldVertex = mul (unity_ObjectToWorld, input.vertex);
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.normal = input.normal;
                output.worldNormal = worldNormal;
                return output;
            }
            
            fixed4 frag (VertexOutput input) : SV_Target
            {
                
                //Params
                float2 uv = input.uv;
                float3 baseColor = _Color;
                
                #if _NORMAL_MAP_ON
                    // sample the normal map, and decode from the Unity encoding
                    half3 tnormal = UnpackNormal(tex2D(_BumpMap, input.uv));
                    // transform normal from tangent to world space
                    float3 worldNormal;
                    worldNormal.x = dot(input.tspace0, tnormal);
                    worldNormal.y = dot(input.tspace1, tnormal);
                    worldNormal.z = dot(input.tspace2, tnormal);
                #else
                    float3 worldNormal = normalize(input.worldNormal);
                #endif
                
                float3 camPos = _WorldSpaceCameraPos.xyz;
                float3 worldPos = input.worldVertex;
                float3 lightPos = _WorldSpaceLightPos0.xyz;//normalize(_LightningDirection);
                float3 view2fragDir = normalize(worldPos - camPos);
                fixed3 lightColor = _LightColor0.rgb;//_LightColor;
                fixed ambientFactor = _AmbientFactor;
                
                #if _BASE_COLOR_ON
                    #if _HSV_ON
                        baseColor = HSV_To_RGB(_Hue, _Saturation, _Value);
                    #else
                        baseColor = _Color.rgb;
                    #endif
                #endif
                
                #if _BASE_MAP_ON
                    fixed4 baseMap = tex2D(_MainTex, uv) * float4(baseColor, 0);
                #else
                    fixed4 baseMap = fixed4(baseColor, 0);
                #endif
                
                //Attenuation
                #if _ATTENUATION_ON
                    float3 light2fragDir = worldPos - lightPos;
                    float oneOverDistance = 1.0 / length(light2fragDir);
                    float attenuation = lerp(1.0, oneOverDistance, lightPos);
                #endif

                //Diffuse light
                #if _ATTENUATION_ON
                    float diffuseMap = saturate( dot(lightPos, worldNormal) ) * lightColor;
                #else
                    float diffuseMap = saturate( dot(lightPos, worldNormal) ) * lightColor;
                #endif
                fixed lambertian = remap(0.0f, 1.0f, ambientFactor, 1.0f, diffuseMap);
                
                //Blinn Phong Specular
                #if _SPECULAR_ON
                    fixed3 specularColor = lightColor;
                    float3 viewReflection = reflect(view2fragDir, worldNormal);
                    float3 halfwayNormal = normalize(lightPos - view2fragDir);
                    float specularDirectLightMap = pow( dot(worldNormal, halfwayNormal ), _Gloss * 128);
                    
                    #if _ATTENUATION_ON
                        float specularDirectLight = attenuation * lightColor * specularColor * specularDirectLightMap;
                    #else
                        float specularDirectLight = lightColor * specularColor * specularDirectLightMap;
                    #endif
                    
                #else
                    float specularDirectLight = 0;
                #endif
                
                //Emission
                #if _EMISSION_ON
                    float4 emissionColor = float4(_EmissionColor /** SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.baseMapUV).rgb*/, 0);
                #else
                    float4 emissionColor = float4(0,0,0,0);
                #endif
                
                float4 col = lambertian * baseMap + specularDirectLight + emissionColor;
                return col;
            }
            
            ENDCG
        }
    }
    
    FallBack "Universl Render Pipeline/Autodesk Interactive/Autodesk Interactive"
}