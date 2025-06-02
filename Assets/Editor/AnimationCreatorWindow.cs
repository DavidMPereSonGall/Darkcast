using UnityEditor;
using UnityEngine;

public class AnimationCreatorWindow : EditorWindow
{
    private string animId = "NewAnimation";
    private string animValue = "";

    [MenuItem("Tools/Create Animation Data")]
    public static void ShowWindow()
    {
        GetWindow<AnimationCreatorWindow>("Create Animation Data");
    }

    private void OnGUI()
    {
        GUILayout.Label("Enter a name for the animation asset", EditorStyles.boldLabel);
        animId = EditorGUILayout.TextField("Animation Id", animId);
        animValue = EditorGUILayout.TextField("Value", animValue);

        if (GUILayout.Button("Create"))
        {
            CreateAsset(animId, animValue);
        }
    }

    private void CreateAsset(string animationId, string animationValue)
    {
        var animation = OperationUtils.StringToAnimation(animationValue);

        var asset = ScriptableObject.CreateInstance<AnimationObject>();
        asset.keyframes = animation.keyframes;
        Debug.Log(" ");
        foreach (var value in asset.keyframes[0].rotations)
        {
            Debug.Log(value);
        }
        asset.assignedKeyframes = animation.assignedKeyframes;
        asset.duration = animation.duration;

        string path = $"Assets/Resources/Animations/{animationId}.asset";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
    }
}
