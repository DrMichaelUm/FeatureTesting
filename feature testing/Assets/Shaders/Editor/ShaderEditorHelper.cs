using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ShaderEditorHelper
{
	public static void DisplayProperty(this MaterialProperty property, MaterialEditor materialEditor)
	{
		if ((uint) (property.flags & MaterialProperty.PropFlags.HideInInspector) <= 0U)
			materialEditor.ShaderProperty(EditorGUILayout.GetControlRect(
			                                                             true, materialEditor.GetPropertyHeight(property, property.displayName),
			                                                             EditorStyles.layerMaskField), property, property.displayName);
	}
}