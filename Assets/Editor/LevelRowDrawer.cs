using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(LevelRow))]
public class LevelRowDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Obtenemos el array de columnas
        SerializedProperty columnas = property.FindPropertyRelative("columnas");

        // Ajustamos el diseño para que sea horizontal
        float width = position.width / 5; // 5 columnas
        float padding = 2f;

        EditorGUI.BeginProperty(position, label, property);

        // Etiqueta de la fila (opcional, p.ej: "Fila 0")
        Rect labelRect = new Rect(position.x, position.y, 50, position.height);
        // EditorGUI.LabelField(labelRect, label); // Descomenta si quieres ver el nombre de fila

        for (int i = 0; i < 5; i++)
        {
            if (i >= columnas.arraySize) break;

            SerializedProperty elemento = columnas.GetArrayElementAtIndex(i);
            
            // Calculamos la posición de cada cuadro
            Rect rect = new Rect(position.x + (i * width), position.y, width - padding, position.height);

            // 0 = vacío (gris), 1 = suelo (verde), 2+ = suelo + decoración (color cíclico por valor)
            Color originalColor = GUI.backgroundColor;
            int v = elemento.intValue;
            if (v <= 0) GUI.backgroundColor = Color.grey;
            else if (v == 1) GUI.backgroundColor = Color.green;
            else GUI.backgroundColor = Color.HSVToRGB(((v - 2) * 0.17f + 0.5f) % 1f, 0.65f, 1f);

            // Dibujamos el campo como un IntField muy pequeño
            elemento.intValue = EditorGUI.IntField(rect, elemento.intValue);

            GUI.backgroundColor = originalColor;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight; // Solo ocupa una línea
    }
}