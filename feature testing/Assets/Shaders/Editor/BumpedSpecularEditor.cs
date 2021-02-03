using System;
using UnityEditor;
using UnityEngine;

public class BumpedSpecularEditor : ShaderGUI
{
    enum LightType
    {
        Unlit = 0,
        BlinnPhong = 1,
        Ambient = 2
    }
    
    enum ShaderType
    {
        General = 0,
        Character = 1,
    }
    
    enum SkinType
    {
        Colorization = 0,
        HUEShift = 1,
    }

    static readonly int s_ShaderType = Shader.PropertyToID("_ShaderType");
    static readonly int s_SkinType = Shader.PropertyToID("_SkinType");
    static readonly int s_LightType = Shader.PropertyToID("_LightType");
    
    const string k_HideForCharacter = "_HideForCharacter";
    const string k_HideForSkin = "_SkinOnly";
    
    const string k_SkinOnKeyword = "_SKIN_ON";
    const string k_SkinColorizationKeyword = "_SKIN_COLOR";
    const string k_SkinHueKeyword = "_SKIN_HUE";
    const string k_AmbientKeyword = "_AMBIENT_ON";
    
    const string k_SkinBlend = "_SKIN_BLEND_ON";
    const string k_SkinNormal = "_NORMAL_ON";
    const string k_SkinSpecular = "_SPECULAR_ON";
    const string k_SkinEmission = "_EMISSION_ON";
    
    const string k_SkinTexPropertyName = "_SkinOnly_SkinTex";
    const string k_SkinTogglePropertyName = "_ToggleSkin";
    const string k_BlendValuePropertyName = "_BlendValue";
    const string k_NormalTogglePropertyName = "_HideForCharacter_ToggleNormal";
    const string k_EmissionColorPropertyName = "_EmissionColor";
    const string k_AmbientFactorPropertyName = "_AmbientFactor";

    int m_SelectedShaderType;
    int m_SelectedSkinType;
    int m_SelectedLightType;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        var material = materialEditor.target as Material;
        if (material == null)
            return;

        m_SelectedShaderType = GUILayout.Toolbar(material.GetInt(s_ShaderType), 
            Enum.GetNames(typeof(ShaderType)));
        material.SetInt(s_ShaderType, m_SelectedShaderType);
        
        EditorGUILayout.Space();

        materialEditor.SetDefaultGUIWidths();
        
        var skinOn = material.IsKeywordEnabled(k_SkinOnKeyword);
        var isGeneral = (ShaderType)m_SelectedShaderType == ShaderType.General;
        
        if (!isGeneral)
        {
            material.DisableKeyword(k_SkinBlend);
            material.DisableKeyword(k_SkinNormal);
            material.DisableKeyword(k_SkinSpecular);
            material.DisableKeyword(k_SkinEmission);
        }
        
        if (!skinOn)
        {
            material.DisableKeyword(k_SkinHueKeyword);
            material.DisableKeyword(k_SkinColorizationKeyword);
        }
        
        for (var i = 0; i != properties.Length; ++i)
        {
            if (properties[i].name.Contains(k_HideForSkin) && !skinOn)
                continue;

            if (properties[i].name == k_SkinTogglePropertyName)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                
                if (skinOn)
                    GUILayout.BeginVertical("HelpBox");
            }
            
            if (isGeneral && properties[i].name == k_NormalTogglePropertyName)
                GUILayout.BeginVertical("HelpBox");

            if (properties[i].name == k_AmbientFactorPropertyName)
            {
                GUILayout.BeginVertical("HelpBox");
                
                m_SelectedLightType = GUILayout.Toolbar(material.GetInt(s_LightType), 
                    Enum.GetNames(typeof(LightType)));
                material.SetInt(s_LightType, m_SelectedLightType);
                
                if (m_SelectedLightType == (int)LightType.Ambient)
                    material.EnableKeyword(k_AmbientKeyword);
                else
                    material.DisableKeyword(k_AmbientKeyword);
            }

            if (!properties[i].name.Contains(k_HideForCharacter) || isGeneral)
            {
                properties[i].DisplayProperty(materialEditor);

                if (properties[i].name == k_SkinTexPropertyName)
                {
                    m_SelectedSkinType = GUILayout.Toolbar(material.GetInt(s_SkinType), 
                        Enum.GetNames(typeof(SkinType)));
                    material.SetInt(s_SkinType, m_SelectedSkinType);

                    if (skinOn)
                    {
                        if (m_SelectedSkinType == (int)SkinType.Colorization)
                        {
                            material.EnableKeyword(k_SkinColorizationKeyword);
                            material.DisableKeyword(k_SkinHueKeyword);
                        }
                        else
                        {
                            material.EnableKeyword(k_SkinHueKeyword);
                            material.DisableKeyword(k_SkinColorizationKeyword);
                        }
                    }
                }
            }
            else
                material.DisableKeyword(properties[i].name);

            if (skinOn && properties[i].name == k_BlendValuePropertyName)
            {
                GUILayout.EndVertical();
            }
            
            if (isGeneral && properties[i].name == k_EmissionColorPropertyName)
            {
                GUILayout.EndVertical();
            }
        }
        
        GUILayout.EndVertical();
        //materialEditor.PropertiesDefaultGUI(properties);
    }
}
