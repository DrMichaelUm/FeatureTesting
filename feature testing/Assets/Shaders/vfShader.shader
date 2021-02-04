Shader "Blackwood/vfShader" 
{
    Properties
    {
        _Color ("Base Color (RGB)", color) = (0,0,0,0)
        _MainTex ("Texture", 2D) = "white" {}
        _Wawyness ("Wawe Amount", Range(0,4)) = 0.2 
        //_Albedo ("Albedo", Range(0,1)) = 1
        _LightningDirection ("LightDir", Vector) = (1, 1, 1, 0)
        _AmbientLight ("AmbientLight (RGB)", Color) = (.1, .3, .1, 1)
        _DiffuseLight ("DiffuseLight (RGB)", Color) = (.3, .6, .6, 1)
      
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

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
                float3 worldNormal : TEXCOORD1;
            };


            sampler2D _MainTex;
            fixed4 _Color;
            //float _Albedo;
            float3 _LightningDirection;
            fixed3 _AmbientLight;
            fixed3 _DiffuseLight;
            float _Wawyness;
            
            // vertex shader
            VertexOutput vert (VertexInput input)
            {
                VertexOutput output;
                
                //input.vertex.y = sin(input.vertex.x + _Time.y)*_Wawyness;
                //input.vertex.xyz += (input.normal*(sin(_Time.y)*0.5 + 0.5)) * _Wawyness;
                
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.worldNormal = UnityObjectToWorldNormal(input.normal);
                output.worldNormal = input.normal;
                return output;
            }
            
            fixed4 frag (VertexOutput input) : SV_Target
            {
                float2 uv = input.uv;
                fixed4 col = tex2D(_MainTex, uv);
                
                fixed3 ambientLight = _AmbientLight;
                fixed3 diffuseColor = _DiffuseLight;
                
                float3 lightDir = normalize(_LightningDirection);
                float diffuseMap = saturate(dot(lightDir, input.worldNormal));
                float3 diffuseLight =  diffuseColor * diffuseMap;
                
                col *= float4(ambientLight + diffuseLight, 0);
                return col;
            }
            
            ENDCG
        }
    }
    
    FallBack "Universl Render Pipeline/Autodesk Interactive/Autodesk Interactive"
}