using System;
using UnityEditor;
using UnityEngine;

public class BumpedSpecularEditor : ShaderGUI
{
    enum BaseType
    {
        Colorization = 0,
        HUEShift = 1,
    }
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

    static readonly int s_BaseType = Shader.PropertyToID("_BaseType");
    static readonly int s_ShaderType = Shader.PropertyToID("_ShaderType");
    static readonly int s_SkinType = Shader.PropertyToID("_SkinType");
    static readonly int s_LightType = Shader.PropertyToID("_LightType");

    const string k_SkinGroupKeyword = "_Skin_";
    const string k_NormalGroupKeyword = "_Normal_";
    const string k_BaseHSVGroupKeyword = "_Base_HSV_";
    const string k_SkinHSVGroupKeyword = "_Skin_HSV_";
    const string k_SpecularGroupKeyword = "_Specular_";
    const string k_EmissionGroupKeyword = "_Emission_";
    const string k_AmbientGroupKeyword = "_Ambient_";
    
    const string k_HideForCharacter = "_HideForCharacter";
    //const string k_HideForSkin = "_SkinOnly";

    const string k_BaseColorOnKeyword = "_BASE_COLOR_ON";
    const string k_BaseHsvOnKeyword = "_HSV_ON";
    const string k_BaseColorKeyword = "_Color";
    
    const string k_SkinTogglePropertyName = "_ToggleSkin";
    const string k_SkinOnKeyword = "_SKIN_ON";
    const string k_SkinColorOnKeyword = "_SKIN_COLOR_ON";
    const string k_SkinHsvKeyword = "_SKIN_HSV_ON";
    const string k_SkinColorPropertyName = "_Skin_Color";
    //const string k_SkinBlend = "_SKIN_BLEND_ON";
    //const string k_BlendValuePropertyName = "_BlendValue";
    
    const string k_AmbientOnKeyword = "_AMBIENT_ON";
    const string k_AmbientFactorPropertyName = "_Ambient_Factor";
    
    const string k_NormalTogglePropertyName = "_HideForCharacter_ToggleBumpMap";
    const string k_NormalOnKeyword = "_NORMAL_MAP_ON";
    
    const string k_SpecularOnKeyword = "_SPECULAR_ON";
    const string k_EmissionOnKeyword = "_EMISSION_ON";
    
    int m_SelectedBaseType;
    int m_SelectedShaderType;
    int m_SelectedSkinType;
    int m_SelectedLightType;
    
    bool cashedSpecularToggle = false;
    bool cashedNormalToggle = false;
    bool cashedEmissionToggle = false;

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

        var baseColorOn = material.IsKeywordEnabled(k_BaseColorOnKeyword);
        var skinOn = material.IsKeywordEnabled(k_SkinOnKeyword);
        var skinColorOn = material.IsKeywordEnabled(k_SkinColorOnKeyword);
        var normalOn = material.IsKeywordEnabled(k_NormalOnKeyword);
        var specularOn = material.IsKeywordEnabled(k_SpecularOnKeyword);
        var emissionOn = material.IsKeywordEnabled(k_EmissionOnKeyword);
        var ambientOn = material.IsKeywordEnabled(k_AmbientOnKeyword);
        var isGeneral = (ShaderType)m_SelectedShaderType == ShaderType.General;
        
        if (!isGeneral)
        {
            if (!cashedNormalToggle && material.IsKeywordEnabled(k_NormalOnKeyword))
                cashedNormalToggle = true;
            
            if (!cashedSpecularToggle && material.IsKeywordEnabled(k_SpecularOnKeyword))
                cashedSpecularToggle = true;
            
            if (!cashedEmissionToggle && material.IsKeywordEnabled(k_EmissionOnKeyword))
                cashedEmissionToggle = true;
            //material.DisableKeyword(k_SkinBlend);
            material.DisableKeyword(k_NormalOnKeyword);
            material.DisableKeyword(k_SpecularOnKeyword);
            material.DisableKeyword(k_EmissionOnKeyword);
        }
        else
        {
            if (cashedNormalToggle)
            {
                material.EnableKeyword(k_NormalOnKeyword);
                cashedNormalToggle = false;
            }
            
            if (cashedSpecularToggle)
            {
                material.EnableKeyword(k_SpecularOnKeyword);
                cashedSpecularToggle = false;
            }

            if (cashedEmissionToggle)
            {
                material.EnableKeyword(k_EmissionOnKeyword);
                cashedEmissionToggle = false;
            }
        }
        
        if (!skinOn)
        {
            material.DisableKeyword(k_SkinHsvKeyword);
            material.DisableKeyword(k_SkinColorOnKeyword);
        }
        
        GUILayout.BeginVertical("HelpBox");
        
        for (var i = 0; i != properties.Length; ++i)
        {

            //if (properties[i].name.Contains(k_HideForSkin) && !skinOn)
            //    continue;
            
            //Base Color|HSV switch
            if (properties[i].name == k_BaseColorKeyword)
            {
                m_SelectedBaseType = GUILayout.Toolbar(material.GetInt(s_BaseType), 
                                                       Enum.GetNames(typeof(BaseType)));
                material.SetInt(s_BaseType, m_SelectedBaseType);
                
                if (m_SelectedBaseType == (int)BaseType.Colorization)
                {
                    material.EnableKeyword(k_BaseColorOnKeyword);
                    material.DisableKeyword(k_BaseHsvOnKeyword);
                }
                else
                {
                    material.EnableKeyword(k_BaseHsvOnKeyword);
                    material.DisableKeyword(k_BaseColorOnKeyword);
                }
            }
            
            if (properties[i].name == k_SkinTogglePropertyName)
                GUILayout.EndVertical();

            if (properties[i].name == k_SkinTogglePropertyName)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                
                if (skinOn)
                    GUILayout.BeginVertical("HelpBox");
            }

            //Skin Color|HSV switch
            if (skinOn && properties[i].name == k_SkinColorPropertyName)
            {
                m_SelectedSkinType = GUILayout.Toolbar(material.GetInt(s_SkinType), 
                                                       Enum.GetNames(typeof(SkinType)));
                material.SetInt(s_SkinType, m_SelectedSkinType);
            
                if (skinOn)
                {
                    if (m_SelectedSkinType == (int)SkinType.Colorization)
                    {
                        material.EnableKeyword(k_SkinColorOnKeyword);
                        material.DisableKeyword(k_SkinHsvKeyword);
                    }
                    else
                    {
                        material.EnableKeyword(k_SkinHsvKeyword);
                        material.DisableKeyword(k_SkinColorOnKeyword);
                    }
                }
            }
            
            if (skinOn && properties[i].name == k_NormalTogglePropertyName)
            {
                GUILayout.EndVertical();
            }
            if (isGeneral && properties[i].name == k_NormalTogglePropertyName)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                
                GUILayout.BeginVertical("HelpBox");
            }

            if (isGeneral && properties[i].name == k_AmbientFactorPropertyName)
            {
                GUILayout.EndVertical();
            }
            
            if (properties[i].name == k_AmbientFactorPropertyName)
            {
                EditorGUILayout.Space();
                EditorGUILayout.Space();
                
                GUILayout.BeginVertical("HelpBox");
            
                m_SelectedLightType = GUILayout.Toolbar(material.GetInt(s_LightType), 
                                                        Enum.GetNames(typeof(LightType)));
                material.SetInt(s_LightType, m_SelectedLightType);
            
                if (m_SelectedLightType == (int)LightType.Ambient)
                    material.EnableKeyword(k_AmbientOnKeyword);
                else
                    material.DisableKeyword(k_AmbientOnKeyword);
            }
                

            if (!properties[i].name.Contains(k_HideForCharacter) || isGeneral)
            {
                
                if (CanDraw(properties[i]))
                    properties[i].DisplayProperty(materialEditor);
            }
            else
                material.DisableKeyword(properties[i].name);
        }
        
        GUILayout.EndVertical();
        //materialEditor.PropertiesDefaultGUI(properties);

        bool CanDraw (MaterialProperty property)
        {
            // ReSharper disable once ReplaceWithSingleAssignment.True
            bool canDraw = true;

            if (baseColorOn && property.name.Contains(k_BaseHSVGroupKeyword))
                canDraw = false;
                    
            if (!baseColorOn && property.name == k_BaseColorKeyword)
                canDraw = false;
                    
            if (!skinOn && property.name.Contains(k_SkinGroupKeyword))
                canDraw = false;

            if (skinColorOn && property.name.Contains(k_SkinHSVGroupKeyword))
                canDraw = false;

            if (!skinColorOn && property.name == k_SkinColorPropertyName)
                canDraw = false;
                    
            if (!normalOn && property.name.Contains(k_NormalGroupKeyword))
                canDraw = false;

            if (!specularOn && property.name.Contains(k_SpecularGroupKeyword))
                canDraw = false;

            if (!emissionOn && property.name.Contains(k_EmissionGroupKeyword))
                canDraw = false;

            if (!ambientOn && property.name.Contains(k_AmbientGroupKeyword))
                canDraw = false;

            return canDraw;
        }
    }
}
