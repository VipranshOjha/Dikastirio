using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class TextConverter : MonoBehaviour
{
    [MenuItem("Tools/Update Legacy Text")]
    static void UpdateLegacyText()
    {
        Debug.Log("Converting Legacy Text to TMP");
        Text[] allText = FindObjectsByType<Text>(FindObjectsSortMode.None);
        Debug.Log($"Found {allText.Length} Legacy Text{(allText.Length > 1 ? "s" : "")}");
        List<TextMigrator> migratorList = new List<TextMigrator>();
        foreach (var t in allText)
        {

            TextMigrator migrator = new TextMigrator()
            {
                transform = t.transform,
                textValue = t.text,
                textSize = t.fontSize,
                autoSize = t.resizeTextForBestFit,
                textAlignment = t.alignment,
            };
            if (migrator.autoSize)
            {
                migrator.minSize = t.resizeTextMinSize;
                migrator.maxSize = t.resizeTextMaxSize;
            }
            migratorList.Add(migrator);
            DestroyImmediate(t);
        }

        StringBuilder sb = new StringBuilder();
        foreach (var migrator in migratorList)
        {
            sb.AppendLine($" -Migrating {migrator.textValue} to TMP");
            TMP_Text tmp = migrator.transform.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = migrator.textValue;
            tmp.fontSize = migrator.textSize;

            TMPAlignment alignment = GetAlignment(migrator.textAlignment);
            tmp.verticalAlignment = alignment.verticalAlignment;
            tmp.horizontalAlignment = alignment.horizontalAlignment;

            if (migrator.autoSize)
            {
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = migrator.minSize;
                tmp.fontSizeMax = migrator.maxSize;
            }
        }
        Debug.Log($"Legacy Text to TMP Conversion Complete\nUpdated Texts:\n{sb}");
    }

    static TMPAlignment GetAlignment(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperCenter:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Center,
                    verticalAlignment = VerticalAlignmentOptions.Top
                };
            case TextAnchor.UpperLeft:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Left,
                    verticalAlignment = VerticalAlignmentOptions.Top
                };
            case TextAnchor.UpperRight:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Right,
                    verticalAlignment = VerticalAlignmentOptions.Top
                };

            case TextAnchor.MiddleCenter:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Center,
                    verticalAlignment = VerticalAlignmentOptions.Middle
                };
            case TextAnchor.MiddleLeft:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Left,
                    verticalAlignment = VerticalAlignmentOptions.Middle
                };
            case TextAnchor.MiddleRight:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Right,
                    verticalAlignment = VerticalAlignmentOptions.Middle
                };

            case TextAnchor.LowerCenter:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Center,
                    verticalAlignment = VerticalAlignmentOptions.Bottom
                };
            case TextAnchor.LowerLeft:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Left,
                    verticalAlignment = VerticalAlignmentOptions.Bottom
                };
            case TextAnchor.LowerRight:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Right,
                    verticalAlignment = VerticalAlignmentOptions.Bottom
                };


            default:
                return new TMPAlignment()
                {
                    horizontalAlignment = HorizontalAlignmentOptions.Left,
                    verticalAlignment = VerticalAlignmentOptions.Top
                };
        }
    }
}

public struct TextMigrator
{
    public Transform transform;
    public string textValue;
    public int textSize;
    public bool autoSize;
    public int minSize;
    public int maxSize;
    public TextAnchor textAlignment;
}

public struct TMPAlignment
{
    public HorizontalAlignmentOptions horizontalAlignment;
    public VerticalAlignmentOptions verticalAlignment;
}
