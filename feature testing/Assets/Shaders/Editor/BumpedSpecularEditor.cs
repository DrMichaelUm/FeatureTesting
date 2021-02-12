using System;
using UnityEditor;
using UnityEngine;

public class BumpedSpecularEditor : ShaderGUI
{
    private class ShaderGroupStates
    {
        public bool baseColorOn;
        public bool skinOn;
        public bool skinColorOn;
        public bool skinBlendOn;
        public bool normalOn;
        public bool specularOn;
        public bool emissionOn;
        public bool ambientOn;
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
    const string k_SkinHsvKeyword = "_SKIN_HSV_ON";
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

    bool m_cashedSkinBlendToggle = false;
    bool m_cashedSpecularToggle = false;
    bool m_cashedNormalToggle = false;
    bool m_cashedEmissionToggle = false;
    
    private readonly ShaderGroupStates _shaderGroupStates = new ShaderGroupStates();
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

        _shaderGroupStates.baseColorOn = material.IsKeywordEnabled(k_BaseColorOnKeyword);
        _shaderGroupStates.skinOn = material.IsKeywordEnabled(k_SkinOnKeyword);
        _shaderGroupStates.skinColorOn = material.IsKeywordEnabled(k_SkinColorOnKeyword);
        _shaderGroupStates.normalOn = material.IsKeywordEnabled(k_NormalOnKeyword);
        _shaderGroupStates.specularOn = material.IsKeywordEnabled(k_SpecularOnKeyword);
        _shaderGroupStates.emissionOn = material.IsKeywordEnabled(k_EmissionOnKeyword);
        _shaderGroupStates.ambientOn = material.IsKeywordEnabled(k_AmbientOnKeyword);
        _shaderGroupStates.skinBlendOn = material.IsKeywordEnabled(k_SkinBlendOnKeyword);
        
        var isGeneral = (ShaderType) m_SelectedShaderType == ShaderType.General;
        if (!isGeneral)
        {
            if (!m_cashedSkinBlendToggle && material.IsKeywordEnabled(k_SkinBlendOnKeyword))
                m_cashedSkinBlendToggle = true;
            
            if (!m_cashedNormalToggle && material.IsKeywordEnabled(k_NormalOnKeyword)) 
                m_cashedNormalToggle = true;
            
            if (!m_cashedSpecularToggle && material.IsKeywordEnabled(k_SpecularOnKeyword))
                m_cashedSpecularToggle = true;
            
            if (!m_cashedEmissionToggle && material.IsKeywordEnabled(k_EmissionOnKeyword))
                m_cashedEmissionToggle = true;
            
            material.DisableKeyword(k_SkinBlendOnKeyword);
            material.DisableKeyword(k_NormalOnKeyword);
            material.DisableKeyword(k_SpecularOnKeyword);
            material.DisableKeyword(k_EmissionOnKeyword);
        }
        else
        {
            if (m_cashedSkinBlendToggle)
            {
                material.EnableKeyword(k_SkinBlendOnKeyword);
                m_cashedSkinBlendToggle = false;
            }
            
            if (m_cashedNormalToggle)
            {
                material.EnableKeyword(k_NormalOnKeyword);
                m_cashedNormalToggle = false;
            }
            
            if (m_cashedSpecularToggle)
            {
                material.EnableKeyword(k_SpecularOnKeyword);
                m_cashedSpecularToggle = false;
            }
        
            if (m_cashedEmissionToggle)
            {
                material.EnableKeyword(k_EmissionOnKeyword);
                m_cashedEmissionToggle = false;
            }
        }
        
        if (!_shaderGroupStates.skinOn)
        {
            material.DisableKeyword(k_SkinHsvKeyword);
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
                
                if (_shaderGroupStates.skinOn)
                    GUILayout.BeginVertical("HelpBox");
            }

            //Skin Color|HSV toolbar and switch handle
            if (_shaderGroupStates.skinOn && properties[i].name == k_SkinColorPropertyName)
            {
                m_SelectedSkinType = GUILayout.Toolbar(material.GetInt(s_SkinType), 
                                                       Enum.GetNames(typeof(SkinType)));
                material.SetInt(s_SkinType, m_SelectedSkinType);
            
                if (_shaderGroupStates.skinOn)
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
            
            if (_shaderGroupStates.skinOn && properties[i].name == k_NormalTogglePropertyName)
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
                    _shaderGroupStates.ambientOn = material.IsKeywordEnabled(k_AmbientOnKeyword);  //Bug fix for Editor event calls
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
    bool CanDraw (MaterialProperty property, ShaderGroupStates shaderGroupStates)
    {
        // ReSharper disable once ReplaceWithSingleAssignment.True
        bool canDraw = true;

        if (shaderGroupStates.baseColorOn && property.name.Contains(k_BaseHSVGroupKeyword))
            canDraw = false;

        if (!shaderGroupStates.baseColorOn && property.name == k_BaseColorKeyword)
            canDraw = false;

        if (!shaderGroupStates.skinOn && property.name.Contains(k_SkinGroupKeyword))
            canDraw = false;

        if (shaderGroupStates.skinColorOn && property.name.Contains(k_SkinHSVGroupKeyword))
            canDraw = false;

        if (!shaderGroupStates.skinColorOn && property.name == k_SkinColorPropertyName)
            canDraw = false;

        if (!this._shaderGroupStates.skinBlendOn && property.name.Contains(k_SkinBlendGroupKeyword))
            canDraw = false;
        
        if (!shaderGroupStates.normalOn && property.name.Contains(k_NormalGroupKeyword))
            canDraw = false;

        if (!shaderGroupStates.specularOn && property.name.Contains(k_SpecularGroupKeyword))
            canDraw = false;

        if (!shaderGroupStates.emissionOn && property.name.Contains(k_EmissionGroupKeyword))
            canDraw = false;

        if (!shaderGroupStates.ambientOn && property.name == k_AmbientFactorPropertyName)
            canDraw = false;

        return canDraw;
    }
}
