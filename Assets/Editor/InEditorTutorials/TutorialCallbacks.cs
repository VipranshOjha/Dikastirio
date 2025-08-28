using UnityEngine;
using XRMultiplayer;

#if UNITY_EDITOR
using UnityEditor;
using Unity.Tutorials.Core.Editor;
using UnityEditor.SceneManagement;

/// <summary>
/// Implement your Tutorial callbacks here.
/// </summary>
[CreateAssetMenu(fileName = DefaultFileName, menuName = "Tutorials/" + DefaultFileName + " Instance")]
public class TutorialCallbacks : ScriptableObject
{
    /// <summary>
    /// The default file name used to create asset of this class type.
    /// </summary>
    public const string DefaultFileName = "TutorialCallbacks";


    [SerializeField] string m_GUID;
    [SerializeField] Tutorial m_SetupTutorial;

    /// <summary>
    /// Creates a TutorialCallbacks asset and shows it in the Project window.
    /// </summary>
    /// <param name="assetPath">
    /// A relative path to the project's root. If not provided, the Project window's currently active folder path is used.
    /// </param>
    /// <returns>The created asset</returns>
    public static ScriptableObject CreateAndShowAsset(string assetPath = null)
    {
        assetPath = assetPath ?? $"{TutorialEditorUtils.GetActiveFolderPath()}/{DefaultFileName}.asset";
        var asset = CreateInstance<TutorialCallbacks>();
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
        EditorUtility.FocusProjectWindow(); // needed in order to make the selection of newly created asset to really work
        Selection.activeObject = asset;
        return asset;
    }

    [ContextMenu("Select Object By GUID")]
    public void SelectSceneObjectByGUID()
    {
        var path = AssetDatabase.GUIDToAssetPath(m_GUID);
        var obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(obj != null)
            Selection.activeObject = obj;
        else
            Debug.LogError($"Object with GUID {m_GUID} not found in scene.");
    }

    public void ShowUGSTutorial()
    {
        TutorialWindowUtils.StartTutorial(m_SetupTutorial);
    }

    public bool IsConnected()
    {
        return XRINetworkGameManager.Connected.Value;
    }

    public void ToggleEditorPause(bool toggle)
    {
        EditorApplication.isPaused = toggle;
    }

    public void SelectNetworkManager()
    {
        Selection.activeObject = FindFirstObjectByType<NetworkManagerVRMultiplayer>();
    }

    public void SelectOfflineMenuAppearancePanel()
    {
        var appearanceMenus = FindObjectsByType<PlayerAppearanceMenu>(FindObjectsSortMode.None);
        foreach(var menu in appearanceMenus)
        {
            if(menu.transform.parent.name != "Offline Menu UI")
                continue;
            Selection.activeObject = menu;
            break;
        }
    }

    public void SelectObjectInHeirarchyByName(string name)
    {
        var obj = GameObject.Find(name);
        if(obj != null)
            Selection.activeObject = obj;
        else
            Debug.LogError($"Object with name {name} not found in scene.");
    }

    public void OpenPrefabView(GameObject prefab)
    {
        AssetDatabase.OpenAsset(prefab);
        SceneView.FrameLastActiveSceneView();
    }

    public void ExitPrefabView()
    {
        StageUtility.GoToMainStage();
    }

    public bool IsConnectedToUGS()
    {
        return CloudProjectSettings.projectBound;
    }

    public void ShowServicesSettings()
    {
        SettingsService.OpenProjectSettings("Project/Services");
    }

    [ContextMenu("Show Vivox Settings")]
    public void ShowVivoxSettings()
    {
        SettingsService.OpenProjectSettings("Project/Services/Vivox");
    }
}
#endif
