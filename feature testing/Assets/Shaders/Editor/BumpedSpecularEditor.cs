using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BumpedSpecularEditor : ShaderGUI
{

    private class BumpSpecularGroupStates
    {
        public Dictionary<string, ShaderGroupState> states;

        public BumpSpecularGroupStates(List<(string toggleKeyword, string groupKeyword)> keywords)
        {
            states = new Dictionary<string, ShaderGroupState>();

            foreach (var keyword in keywords)
            {
                states.Add(keyword.toggleKeyword, new ShaderGroupState(keyword.toggleKeyword, keyword.groupKeyword));
            }
        }
    }

    class ShaderGroupState
    {
        public string toggleKeyword { get; private set; }
        public string groupKeyword { get; private set; }
        public bool isActive;
        public bool cashedIsActive;
        public ShaderGroupState(string toggleKeyword, string groupKeyword)
        {
            this.toggleKeyword = toggleKeyword;
            this.groupKeyword = groupKeyword;
            this.isActive = false;
            this.cashedIsActive = false;
        }

        public void SetParamsFromMaterial(Material material, bool cashedIsActive) 
        {
            this.isActive = material.IsKeywordEnabled(this.toggleKeyword);
            this.cashedIsActive = cashedIsActive;
        }

    }
    
    enum BaseType
    {
        Colorization = 0,
        HSVShift = 1,
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
        HSVShift = 1,
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
    private const string k_SkinBlendGroupKeyword = "_Blend_";
    
    const string k_HideForCharacter = "_HideForCharacter";

    const string k_BaseColorOnKeyword = "_BASE_COLOR_ON";
    const string k_BaseHsvOnKeyword = "_HSV_ON";
    const string k_BaseColorKeyword = "_Color";
    
    const string k_SkinTogglePropertyName = "_ToggleSkin";
    const string k_SkinOnKeyword = "_SKIN_ON";
    const string k_SkinColorOnKeyword = "_SKIN_COLOR_ON";
    const string k_SkinHsvOnKeyword = "_SKIN_HSV_ON";
    const string k_SkinColorPropertyName = "_Skin_Color";
    const string k_SkinBlendOnKeyword = "_SKIN_BLEND_ON";
    
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
    
    private static readonly List<(string toggleKeyword, string groupKeyword)> _shaderGroupKeywords = 
        new List<(string toggleKeyword, string groupKeyword)>() 
        {   
            (k_BaseColorOnKeyword, k_BaseHSVGroupKeyword), 
            (k_SkinOnKeyword, k_SkinGroupKeyword), 
            (k_SkinColorOnKeyword, k_SkinHSVGroupKeyword), 
            (k_SkinBlendOnKeyword, k_SkinBlendGroupKeyword), 
            (k_NormalOnKeyword, k_NormalGroupKeyword),
            (k_SpecularOnKeyword, k_SpecularGroupKeyword),
            (k_EmissionOnKeyword, k_EmissionGroupKeyword),
            (k_AmbientOnKeyword, k_AmbientGroupKeyword)
        };
    
    private readonly BumpSpecularGroupStates _shaderGroupStates = new BumpSpecularGroupStates(_shaderGroupKeywords);

    
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

        //Fill states of shader property groups
        foreach (var shaderGroupState in _shaderGroupStates.states)
        {
            shaderGroupState.Value.SetParamsFromMaterial(material, shaderGroupState.Value.cashedIsActive);
        }

        var skinOn = _shaderGroupStates.states[k_SkinOnKeyword].isActive;
        var isGeneral = (ShaderType) m_SelectedShaderType == ShaderType.General;
        
        if (!isGeneral) //If Character
        {
            foreach (var shaderGroupState in _shaderGroupStates.states)
            {
                if (!shaderGroupState.Value.cashedIsActive && shaderGroupState.Value.isActive)
                    shaderGroupState.Value.cashedIsActive = true;
            }
            
            material.DisableKeyword(k_SkinBlendOnKeyword);
            material.DisableKeyword(k_NormalOnKeyword);
            material.DisableKeyword(k_SpecularOnKeyword);
            material.DisableKeyword(k_EmissionOnKeyword);
        }
        else 
        {
            foreach (var shaderGroupState in _shaderGroupStates.states)
            {
                if (shaderGroupState.Value.cashedIsActive)
                {
                    material.EnableKeyword(shaderGroupState.Key);
                    shaderGroupState.Value.cashedIsActive = false;
                }
            }
        }
        
        if (!skinOn)
        {
            material.DisableKeyword(k_SkinHsvOnKeyword);
            material.DisableKeyword(k_SkinColorOnKeyword);
            material.DisableKeyword(k_SkinBlendOnKeyword);
        }
        
        //Begin help box for Base Color|HSV
        GUILayout.BeginVertical("HelpBox");
        
        for (var i = 0; i != properties.Length; ++i)
        {
            //Base Color|HSV toolbar and switch handle
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

            //Skin Color|HSV toolbar and switch handle
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
                        material.DisableKeyword(k_SkinHsvOnKeyword);
                    }
                    else
                    {
                        material.EnableKeyword(k_SkinHsvOnKeyword);
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

                if (m_SelectedLightType == (int) LightType.Ambient)
                {
                    material.EnableKeyword(k_AmbientOnKeyword);
                    _shaderGroupStates.states[k_AmbientOnKeyword].isActive = material.IsKeywordEnabled(k_AmbientOnKeyword);  //Bug fix for Editor event calls
                }

                if (m_SelectedLightType != (int) LightType.Ambient)
                    material.DisableKeyword(k_AmbientOnKeyword);
            }
        

            if (!properties[i].name.Contains(k_HideForCharacter) || isGeneral)
            {
                if (CanDraw(properties[i], _shaderGroupStates))
                    properties[i].DisplayProperty(materialEditor);
            }
        }
        
        GUILayout.EndVertical();
    }
    
    //TODO Implement some kind of ShowIf attribute and remove this method
    bool CanDraw (MaterialProperty property, BumpSpecularGroupStates bumpSpecularGroupStates)
    {
       
        if (bumpSpecularGroupStates.states[k_BaseColorOnKeyword].isActive && property.name.Contains(k_BaseHSVGroupKeyword))
            return false;

        if (bumpSpecularGroupStates.states[k_SkinColorOnKeyword].isActive &&
            property.name.Contains(k_SkinHSVGroupKeyword))
            return false;
        
        foreach (var shaderGroupState in _shaderGroupStates.states)
        {
            if (!shaderGroupState.Value.isActive && property.name.Contains(shaderGroupState.Value.groupKeyword))
                return false;
        }

        return true;
    }
}
