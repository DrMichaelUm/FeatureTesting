// _SkinOnly to hide without skin
// _HideForCharacter to hide in character mode

Shader "Blackwood/Final Bumped Specular" {
    Properties {
        [HideInInspector]
        _ShaderType("ShaderType", Int) = 0
        
        [HideInInspector]
        _SkinType("SkinType", Int) = 0
        
        [HideInInspector]
        _LightType("LightType", Int) = 0
        
        _MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1)
        
        [Toggle(_SKIN_ON)] _ToggleSkin("Skin", Int) = 0
        [ShowIf(_SKIN_ON)] _SkinOnly_SkinTex ("Skin (RBG) Mask (A)", 2D) = "white" {}
        
        [ShowIf(_SKIN_COLOR)] _ColorizationColor("Skin Color", Color) = (1,1,1)
        
        [ShowIf(_SKIN_HUE)] _HueShift("HUE Shift", Range(0, 1)) = 0
        [ShowIf(_SKIN_HUE)] _SaturationShift("Saturation", Range(0, 10)) = 1
        [ShowIf(_SKIN_HUE)] _BrightnessShift("Brightness", Range(0, 10)) = 1
        
        [Toggle(_SKIN_BLEND_ON)] _HideForCharacter_SkinOnly_ToggleBlend("Blend Texture On Skin", Int) = 0
        [ShowIf(_SKIN_BLEND_ON)] _BlendTex ("Blend (RBG)", 2D) = "white" {}
        [ShowIf(_SKIN_BLEND_ON)] _BlendValue("Blend Value", Range(0, 1)) = 0

        [Toggle(_NORMAL_ON)] _HideForCharacter_ToggleNormal("Normal Map", Int) = 0
        [ShowIf(_NORMAL_ON)][NoScaleOffset] _BumpMap ("Normal Map (RGB)", 2D) = "bump" {}
        
        [Toggle(_SPECULAR_ON)] _HideForCharacter_ToggleSpecular("Specular Map", Int) = 0
        [ShowIf(_SPECULAR_ON)] _SpecularTex ("Specular Map", 2D) = "white" {}
        [ShowIf(_SPECULAR_ON)] _Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        
        [Toggle(_EMISSION_ON)] _HideForCharacter_ToggleEmission("Emission", Int) = 0
        [ShowIf(_EMISSION_ON)] _EmissionMap("", 2D) = "white" {}
        [ShowIf(_EMISSION_ON)][HDR] _EmissionColor("", Color) = (1,1,1)
        
        [ShowIf(_AMBIENT_ON)] _AmbientFactor("Ambient Light", Range( 0.0, 1.0 ) ) = 0.6
        _Saturation ("Saturation", Range(0.0,10)) = 1.0
		_Brightness ("Brightness", Range(0.0,10)) = 1.0
    }
    
    SubShader {
        Tags { "RenderType"="Base" "RenderPipeline" = "UniversalPipeline" }
        LOD 250
    
        CGPROGRAM

        #pragma shader_feature_local _ _NORMAL_ON
        #pragma shader_feature_local _ _SKIN_ON
        #pragma shader_feature_local _ _SKIN_BLEND_ON
        #pragma shader_feature_local _ _SKIN_HUE
        #pragma shader_feature_local _ _SKIN_COLOR
        #pragma shader_feature_local _ _SPECULAR_ON
		#pragma shader_feature_local _ _EMISSION_ON
		#pragma shader_feature_local _ _AMBIENT_ON
        
        #pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview novertexlights
        
        sampler2D _MainTex;
        float4 _Tint;

        int _LightType;

        #if _SKIN_ON
            sampler2D _SkinOnly_SkinTex;
        #endif

        #if _SKIN_BLEND_ON
            sampler2D _BlendTex;
            float _BlendValue;
        #endif

        #if _SKIN_COLOR
            float4 _ColorizationColor;
        #endif

        #if _SKIN_HUE
            float _HueShift;
            float _SaturationShift;
            float _BrightnessShift;
        #endif

        #if _NORMAL_ON
            sampler2D _BumpMap;
        #endif
        
        #if _SPECULAR_ON
            sampler2D _SpecularTex;
            half _Shininess;
        #endif
        
        #if _EMISSION_ON
            float4 _EmissionColor;
            sampler2D _EmissionMap;
        #endif
        
        float _AmbientFactor;
        float _Saturation;
	    float _Brightness;
        
        half remap(
            half inMin, half inMax,
            half outMin, half outMax,
            half value )
        {
            return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
        }
        
    #if _SKIN_HUE
        float3 shift_col(float3 RGB, float3 shift)
        {
            float3 RESULT = float3(RGB);
            float VSU = shift.z * shift.y * cos(shift.x * 3.14159265 / 180);
            float VSW = shift.z * shift.y * sin(shift.x * 3.14159265 / 180);

            RESULT.x = (.299 * shift.z + .701 * VSU + .168 * VSW) * RGB.x
                + (.587 * shift.z - .587 * VSU + .330 * VSW) * RGB.y
                + (.114 * shift.z - .114 * VSU - .497 * VSW) * RGB.z;

            RESULT.y = (.299 * shift.z - .299 * VSU - .328 * VSW) * RGB.x
                + (.587 * shift.z + .413 * VSU + .035 * VSW) * RGB.y
                + (.114 * shift.z - .114 * VSU + .292 * VSW) * RGB.z;

            RESULT.z = (.299 * shift.z - .3 * VSU + 1.25 * VSW) * RGB.x
                + (.587 * shift.z - .588 * VSU - 1.05 * VSW) * RGB.y
                + (.114 * shift.z + .886 * VSU - .203 * VSW) * RGB.z;

            return (RESULT);
        }
    #endif

        inline fixed4 LightingMobileBlinnPhong (SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
        {
            fixed4 c;
            
            if (_LightType == 0)
            {
                c.rgb = s.Albedo;
                return c;
            }
            
            fixed diff = max (0, dot (s.Normal, lightDir));
            fixed nh = max (0, dot (s.Normal, halfDir));

            #if _AMBIENT_ON 
                fixed lambertian = remap(0.0f, 1.0f, _AmbientFactor, 1.0f, diff);
            #endif
       
        
            #if _SPECULAR_ON
                fixed spec = pow (nh, s.Specular*128) * s.Gloss;

            #if _AMBIENT_ON 
                c.rgb = (s.Albedo * _LightColor0.rgb * lambertian + _LightColor0.rgb * spec) * atten;
            #else
                c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
            #endif

            #else

            #if _AMBIENT_ON 
                c.rgb = (s.Albedo * _LightColor0.rgb * lambertian) * atten;
            #else
                c.rgb = (s.Albedo * _LightColor0.rgb * diff) * atten;
            #endif

            #endif
            
            UNITY_OPAQUE_ALPHA(c.a);
            return c;
        }
        
        struct Input {
            float2 uv_MainTex;
        };
        
        void surf (Input IN, inout SurfaceOutput o) {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);

            #if _SKIN_ON
                fixed4 mask = tex2D(_SkinOnly_SkinTex, IN.uv_MainTex);
                
                #if _SKIN_BLEND_ON
                    fixed4 blend = tex2D(_BlendTex, IN.uv_MainTex);
                    mask = lerp(mask, blend, _BlendValue);
                #endif
                
                #if _SKIN_HUE
                    mask.rgb = shift_col(mask.rgb,
                        float3(_HueShift * 360, _SaturationShift, _BrightnessShift));
                #endif
                
                #if _SKIN_COLOR
                    mask.rgb = tex.rgb * _ColorizationColor;
                #endif
                
                tex.rgb = tex.rgb * (1 - mask.a) + mask.rgb * mask.a;
            #endif
            
            const float3 kLumCoeff = float3( 0.2125, 0.7154, 0.0721 );
            float l = dot( tex.rgb, kLumCoeff );
            float3 intensity = l;
            float3 intensityRGB =  float3( lerp( intensity, tex.rgb, _Saturation ) );
            tex = half4( intensityRGB * _Brightness, tex.a );
            
            o.Albedo = tex.rgb * _Tint;
            o.Alpha = tex.a;
            
            #if _EMISSION_ON
                o.Emission = tex2D(_EmissionMap, IN.uv_MainTex) * _EmissionColor;
            #endif
            
            #if _SPECULAR_ON
                fixed4 specTex = tex2D(_SpecularTex, IN.uv_MainTex) * _Shininess;
                o.Gloss = specTex.r;
                o.Specular = specTex.g;
            #endif
            
            #if _NORMAL_ON
                o.Normal = UnpackNormal (tex2D(_BumpMap, IN.uv_MainTex));
            #endif
        }

        ENDCG
    }
    CustomEditor "BumpedSpecularEditor"
    FallBack "Universal Render Pipeline/Lit"
}
