// #define AUTO_INCREMENT_BUILD

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if AUTO_INCREMENT_BUILD
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#endif

public class BuildIdIncrementer : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

#if AUTO_INCREMENT_BUILD
    /// <summary>
    /// The version code to increment. Determines the index of the version number to increment.
    /// </summary>
    int m_VersionCode = 2;
    bool m_UpdatingBuildId = false;
#endif

    public void OnPostprocessBuild(BuildReport report)
    {
#if AUTO_INCREMENT_BUILD
        if(m_UpdatingBuildId)
        {
            m_UpdatingBuildId = false;
            XRMultiplayer.Utils.Log($"Build Auto Updated: {PlayerSettings.bundleVersion}");
        }
#endif
    }

    public void OnPreprocessBuild(BuildReport report)
    {
#if AUTO_INCREMENT_BUILD
        m_UpdatingBuildId = true;
        string[] currentVersion = Application.version.Split('.');
        if (currentVersion.Length == m_VersionCode + 1)
        {
            if (int.TryParse(currentVersion[m_VersionCode], out int result))
            {
                result++;
            }

            PlayerSettings.bundleVersion = $"{currentVersion[0]}.{currentVersion[1]}.{result}";

#if UNITY_ANDROID
            PlayerSettings.Android.bundleVersionCode = result;
#endif
        }

        XRMultiplayer.Utils.Log($"Updating Build ID: {PlayerSettings.bundleVersion}");
#endif
    }
}
