using UnityEngine;

public class Seed : MonoBehaviour
{
    [Header("Seed Settings")]
    public string gameSeed = "";
    [ReadOnly] public int currentSeed;

    public int GetSeed()
    {
        if (string.IsNullOrEmpty(gameSeed))
            gameSeed = GenerateRandomString(8);

        currentSeed = gameSeed.GetHashCode();
        Random.InitState(currentSeed);
        return currentSeed;
    }

    string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] result = new char[length];
        for (int i = 0; i < length; i++)
            result[i] = chars[Random.Range(0, chars.Length)];
        return new string(result);
    }
}

public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif
