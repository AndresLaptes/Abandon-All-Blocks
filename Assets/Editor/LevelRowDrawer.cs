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

            // Estilo visual: Si es 1, el fondo será verde o normal; si es 0, más oscuro
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = elemento.intValue == 1 ? Color.green : Color.grey;

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