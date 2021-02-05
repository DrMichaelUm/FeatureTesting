Shader "Blackwood/vfShader" 
{
    Properties
    {
        [Toggle(_BASE_COLOR_ON)] _ToggleBaseColor("Use Base Color", int) = 0
        [ShowIf(_BASE_COLOR_ON)] _Color ("Base Color (RGB)", Color) = (0,0,0,0)
        
        [Toggle(_BASE_MAP_ON)] _ToggleBaseMap("Use Base Map", int) = 0
        [ShowIf(_BASE_MAP_ON)] _MainTex ("Texture", 2D) = "white" {}
        
        //_Wawyness ("Wawe Amount", Range(0,4)) = 0.2
        _LightningDirection ("LightDir", Vector) = (1, 1, 1, 0)
        _LightColor ("Light Color (RGB)", Color) = (0.3, 0.6, 0.6, 1)
        
        [Toggle(_AMBIENT_ON)] _ToggleAmbient("Use Ambient Light", int) = 0
        _AmbientColor ("Ambient Color (RGB)", Color) = (.1, .3, .1, 1)
        _AmbientFactor ("Ambient Factor", Range(0,1)) = 0.6
    
        [Toggle(_SPECULAR_ON)] _ToggleSpecular("Use Specular", int) = 0
        _Gloss ("Gloss", Range(0.01, 1)) = 1
    }
    CGINCLUDE
    
    
    
    half remap(
            half inMin, half inMax,
            half outMin, half outMax,
            half value )
    {
        return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
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
            #pragma shader_feature_local _ _SPECULAR_ON
            
            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"

            struct  VertexInput
            {
                float4 vertex : POSITION; // vertex position
                float2 uv : TEXCOORD0; // texture coordinate
                float3 normal : NORMAL;
            };

            struct VertexOutput
            {
                float2 uv : TEXCOORD0; // texture coordinate
                float4 vertex : SV_POSITION; // clip space position
                float3 normal : NORMAL;
                float3 worldNormal : TEXCOORD1;
                float3 worldVertex : TEXCOORD2;
            };

            //CBUFFER_START(UnityPerMaterial)
            sampler2D _MainTex;
            fixed4 _Color;
            float3 _LightningDirection;
            fixed3 _LightColor;
            fixed3 _AmbientColor;
            fixed _AmbientFactor;
            
            #if _SPECULAR_ON
            fixed _Gloss;
            #endif
            //float _Wawyness;
            //CBUFFER_END
            
            // vertex shader
            VertexOutput vert (VertexInput input)
            {
                VertexOutput output;
                
                //input.vertex.y = sin(input.vertex.x + _Time.y)*_Wawyness;
                //input.vertex.xyz += (input.normal*(sin(_Time.y)*0.5 + 0.5)) * _Wawyness;
                
                output.uv = input.uv;
                output.worldVertex = mul (unity_ObjectToWorld, input.vertex);
                output.vertex = UnityObjectToClipPos(input.vertex);
                //output.normal = input.normal;
                output.normal = input.normal;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                return output;
            }
            
            fixed4 frag (VertexOutput input) : SV_Target
            {
                
                //Params
                float2 uv = input.uv;
                float3 baseColor = _Color;
                float3 worldNormal = normalize(input.worldNormal);
                float3 lightDir = _WorldSpaceLightPos0.xyz;//normalize(_LightningDirection);
                fixed3 lightColor = _LightColor0.rgb;//_LightColor;
                fixed3 ambientColor = _AmbientColor;
                fixed ambientFactor = _AmbientFactor;
                float3 camPos = _WorldSpaceCameraPos.xyz;
                float3 worldPos = input.worldVertex;
                fixed4 col = tex2D(_MainTex, uv);
                float3 finalLight = (0,0,0);
                
                //Diffuse light
                fixed3 ambientLight = ambientColor;
                fixed3 diffuseColor = lightColor;
                float diffuseMap = saturate( dot(lightDir, worldNormal) );
                fixed lambertian = remap(0.0f, 1.0f, ambientFactor, 1.0f, diffuseMap);
                finalLight += lambertian;
                
                #if _BASE_COLOR_ON
                finalLight *= _Color.rgb * lightColor;
                #endif
                
                //Specular Light
                #if _SPECULAR_ON
                fixed3 specularColor = lightColor;
                float3 view2fragDir = normalize(worldPos - camPos);
                float3 viewReflection = reflect(view2fragDir, worldNormal);
                float specularDirectLightMap = pow(saturate( dot(viewReflection, lightDir) ), _Gloss * 128);
                float specularDirectLight = specularDirectLightMap * specularColor;
                
                finalLight += specularDirectLight;
                #endif
                //return float4(finalLight, 0);
                //return float4(col.rgb, 0);
                col *= float4(finalLight, 0);
                return col;
            }
            
            
            
            ENDCG
        }
    }
    
    FallBack "Universl Render Pipeline/Autodesk Interactive/Autodesk Interactive"
}