Shader "Custom/vfShader" 
{
    Properties
    {
        [HideInInspector]
        _BaseType("BaseType", Int) = 0
        
        [HideInInspector]
        _ShaderType("ShaderType", Int) = 0
        
        [HideInInspector]
        _SkinType("SkinType", Int) = 0
        
        [HideInInspector]
        _LightType("LightType", Int) = 0
        
        _MainTex ("Texture", 2D) = "white" {}
        [ShowIf(_BASE_COLOR_ON)] _Color ("Base Color (RGB)", Color) = (0, 0, 0, 0)
        
        [ShowIf(_HSV_ON)] _Base_HSV_Hue ("Hue", Range(0, 1)) = 0
        [ShowIf(_HSV_ON)] _Base_HSV_Saturation ("Saturation", Range(0,1)) = 0
        [ShowIf(_HSV_ON)] _Base_HSV_Value ("Value", Range(0, 1)) = 0
        
        [Toggle(_SKIN_ON)] _ToggleSkin ("Skin", int) = 0
        [ShowIf(_SKIN_ON)] _Skin_Tex ("Skin (RGB) Mask (A)", 2D) = "white" {}
        [ShowIf(_SKIN_COLOR_ON)] _Skin_Color ("Skin Color", Color) = (0,0,0,0)
        
        [ShowIf(_SKIN_HSV_ON)] _Skin_HSV_Hue ("Hue", Range(0, 1)) = 0
        [ShowIf(_SKIN_HSV_ON)] _Skin_HSV_Saturation ("Saturation", Range(0,1)) = 0
        [ShowIf(_SKIN_HSV_ON)] _Skin_HSV_Value ("Value", Range(0, 1)) = 0
        
        [Toggle(_SKIN_BLEND_ON)] _HideForCharacter_Skin_ToggleBlend("Blend Texture On Skin", Int) = 0
        [ShowIf(_SKIN_BLEND_ON)] _Skin_Blend_Tex ("Blend (RBG)", 2D) = "white" {}
        [ShowIf(_SKIN_BLEND_ON)] _Skin_Blend_Value("Blend Value", Range(0, 1)) = 0
        
        [Toggle(_NORMAL_MAP_ON)] _HideForCharacter_ToggleBumpMap ("Use Normal Map", int) = 0
        [ShowIf(_NORMAL_MAP_ON)] [Normal] [NoScaleOffset] _Normal_BumpMap ("Bump Map", 2D) = "bump" {}
        
        [Toggle(_SPECULAR_ON)] _HideForCharacter_ToggleSpecular("Use Specular", int) = 0
        [ShowIf(_SPECULAR_ON)] _Specular_Tex ("Specular Map", 2D) = "white" {}
        [ShowIf(_SPECULAR_ON)] _Specular_Shininess ("Shininess", Range(0.01, 1)) = 1
        
        [Toggle(_ATTENUATION_ON)] _ToggleAttenuation("Use Attenuation", int) = 0
        
        [Toggle(_EMISSION_ON)] _HideForCharacter_ToggleEmission("Use Emission", int) = 0
        [ShowIf(_EMISSION_ON)] [NoScaleOffset] _Emission_Map ("Emission Map", 2D) = "white" {}
        [ShowIf(_EMISSION_ON)] [HDR] _Emission_Color ("Emission Color (HDR)" , Color) = (0, 0, 0, 0)
        
        [ShowIf(_AMBIENT_ON)] _Ambient_Factor ("Ambient Factor", Range(0,1)) = 0.6
         _Saturation ("Saturation", Range(0, 10)) = 1
         _Brightness ("Brightness", Range(0, 10)) = 1
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
            #pragma shader_feature_local _ _HSV_ON
            #pragma shader_feature_local _ _SKIN_ON
            #pragma shader_feature_local _ _SKIN_COLOR_ON
            #pragma shader_feature_local _ _SKIN_HSV_ON
            #pragma shader_feature_local _ _SKIN_BLEND_ON
            #pragma shader_feature_local _ _NORMAL_MAP_ON
            #pragma shader_feature_local _ _SPECULAR_ON
            #pragma shader_feature_local _ _EMISSION_ON
            #pragma shader_feature_local _ _ATTENUATION_ON
            #pragma shader_feature_local _ _AMBIENT_ON
            
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
            
            int _LightType;
            
            sampler2D _MainTex;
                
            #if _HSV_ON
                float _Base_HSV_Hue;
                float _Base_HSV_Saturation;
                float _Base_HSV_Value;
            #elif _BASE_COLOR_ON
                fixed4 _Color;
            #endif
            
            #if _SKIN_ON
                sampler2D _Skin_Tex;
                
                #if _SKIN_HSV_ON
                    float _Skin_HSV_Hue;
                    float _Skin_HSV_Saturation;
                    float _Skin_HSV_Value;
                #elif _SKIN_COLOR_ON 
                    fixed4 _Skin_Color;
                #endif
                
                #if _SKIN_BLEND_ON
                    sampler2D _Skin_Blend_Tex;
                    float _Skin_Blend_Value;
                #endif
            #endif
            
            #if _NORMAL_MAP_ON
                sampler2D _Normal_BumpMap;
            #endif
            
            #if _SPECULAR_ON
                sampler2D _Specular_Tex;
                fixed _Specular_Shininess;
            #endif
            
            #if _EMISSION_ON
                sampler2D _Emission_Map;
                float3 _Emission_Color;
            #endif
            
            #if _AMBIENT_ON
                float _Ambient_Factor;
            #endif
            
            float _Saturation;
            float _Brightness;
            
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
                float4 resultCol = float4(0, 0, 0, 0);
                float2 uv = input.uv;
                fixed4 baseMap = tex2D(_MainTex, uv);
                fixed3 baseColor = fixed3(0, 0, 0);
                
                #if _SKIN_ON
                    fixed4 mask = tex2D(_Skin_Tex, uv);
                    
                    #if _SKIN_BLEND_ON
                        fixed4 blend = tex2D(_Skin_Blend_Tex, uv);
                        mask = lerp(mask, blend, _Skin_Blend_Value);
                    #endif
                    
                    #if _SKIN_HSV_ON
                        //mask.rgb = shift_col(mask.rgb,
                        //    float3(_HueShift * 360, _SaturationShift, _BrightnessShift));
                        mask.rgb = HSV_To_RGB(_Skin_HSV_Hue, _Skin_HSV_Saturation, _Skin_HSV_Value);
                    #elif _SKIN_COLOR_ON
                        mask.rgb = baseMap.rgb * _Skin_Color;
                    #endif
                    baseMap.rgb = baseMap.rgb * (1 - mask.a) + mask.rgb * mask.a;
                #endif
                
                //Saturation and Brightness
                const float3 kLumCoeff = float3( 0.2125, 0.7154, 0.0721 );
                float3 intensity = dot( baseMap.rgb, kLumCoeff );
                float3 intensityRGB =  float3( lerp( intensity, baseMap.rgb, _Saturation ) );
                baseMap = half4( intensityRGB * _Brightness, baseMap.a );
                
                #if _HSV_ON
                    baseColor = HSV_To_RGB(_Base_HSV_Hue, _Base_HSV_Saturation, _Base_HSV_Value);
                #elif _BASE_COLOR_ON
                    baseColor = _Color.rgb;
                #endif
                
                baseMap *= float4(baseColor, baseMap.a);
                
                if (_LightType == 0)
                    return baseMap;
                
                float3 camPos = _WorldSpaceCameraPos.xyz;
                float3 lightPos = normalize(_WorldSpaceLightPos0.xyz);
                float3 worldPos = input.worldVertex;
                float3 view2fragDir = normalize(worldPos - camPos);
                
                #if _NORMAL_MAP_ON
                    half3 tnormal = UnpackNormal(tex2D(_Normal_BumpMap, input.uv));
                    // transform normal from tangent to world space
                    float3 worldNormal;
                    worldNormal.x = dot(input.tspace0, tnormal);
                    worldNormal.y = dot(input.tspace1, tnormal);
                    worldNormal.z = dot(input.tspace2, tnormal);
                    worldNormal = normalize(worldNormal);
                #else
                    float3 worldNormal = normalize(input.worldNormal);
                #endif
                
                //Attenuation
                #if _ATTENUATION_ON
                    float3 light2fragDir = normalize(worldPos - lightPos);
                    float oneOverDistance = 1.0 / length(light2fragDir);
                    float attenuation = lerp(1.0, oneOverDistance, lightPos);
                #endif
                
                //Diffuse light
                #if _ATTENUATION_ON
                    float diffuseMap = attenuation * saturate( dot(lightPos, worldNormal) );
                #else
                    float diffuseMap = saturate( dot(lightPos, worldNormal) );
                #endif

                //lambertian
                #if _AMBIENT_ON
                    fixed ambientFactor = _Ambient_Factor;
                    fixed lambertian = remap(0.0f, 1.0f, ambientFactor, 1.0f, diffuseMap);
                #else
                    fixed lambertian = diffuseMap;
                #endif
                
                //Blinn Phong Specular
                #if _SPECULAR_ON
                    fixed4 specTex = tex2D(_Specular_Tex, input.uv) * _Specular_Shininess;
                    float gloss = specTex.r;
                    float specular = specTex.g;
                    
                    float3 halfwayNormal = normalize(lightPos - view2fragDir);
                    float specularDirectLightMap = pow( saturate( dot(worldNormal, halfwayNormal )), specular * 128) * gloss * 2;
                    
                    #if _ATTENUATION_ON
                        float specularDirectLight = attenuation * specularDirectLightMap;
                    #else
                        float specularDirectLight = specularDirectLightMap;
                    #endif
                    
                #else
                    float specularDirectLight = 0;
                #endif
                
                //Emission
                #if _EMISSION_ON
                    float4 emissionColor = float4(_Emission_Color * tex2D(_Emission_Map, input.uv).rgb, 0);
                #else
                    float4 emissionColor = float4(0, 0, 0, 0);
                #endif
                
                resultCol.rgb = baseMap * _LightColor0.rgb * lambertian + _LightColor0.rgb * specularDirectLight + emissionColor;
                return resultCol;
            }
            
            ENDCG
        }
    }
    
    FallBack "Universl Render Pipeline/Lit"
    CustomEditor "BumpedSpecularEditor" 
}