#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

namespace XRMultiplayer
{
#if UNITY_EDITOR
    [CustomEditor(typeof(NetworkObjectDispenser))]
    public class NetworkObjectDispenserEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return DrawBaseContainer();
        }

        VisualElement DrawBaseContainer()
        {
            var root = new VisualElement();

            Box backgroundBox = new();
            Foldout foldout = new Foldout
            {
                name = "Dispenser Panels Foldout",
                text = "Dispenser Spawn Groups",
            };

            // Creating a second nested foldout to help with the layout
            Foldout persistentFoldout = new Foldout
            {
                name = "Persistent Panel Foldout",
                text = "Persistent Spawn Group",
            };

            persistentFoldout.style.paddingLeft = 11.0f;


            persistentFoldout.Add(new PropertyField(serializedObject.FindProperty("m_PersistentPanel")));
            foldout.Add(persistentFoldout);

            var togglePanels = new PropertyField(serializedObject.FindProperty("m_Panels"), "Toggleable Spawn Groups");
            foldout.Add(togglePanels);

            backgroundBox.Add(foldout);
            root.Add(backgroundBox);

            var capacityContainer = GetCapacityContainer();
            root.Add(capacityContainer);
            capacityContainer.PlaceBehind(backgroundBox);

            return root;
        }

        VisualElement GetCapacityContainer()
        {
            var container = new VisualElement();
            container.Add(new PropertyField(serializedObject.FindProperty("m_ClearButton")));
            container.Add(new PropertyField(serializedObject.FindProperty("m_Capacity")));
            container.Add(new PropertyField(serializedObject.FindProperty("m_CountText")));
            container.Add(new PropertyField(serializedObject.FindProperty("m_DistanceCheckTimeInterval")));
            return container;

        }
    }

    [CustomPropertyDrawer(typeof(DispenserPanel))]
    public class DispenserPanelDrawer : PropertyDrawer
    {
        NetworkObjectDispenser m_target = null;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if(Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<NetworkObjectDispenser>() != null)
            {
                m_target = (NetworkObjectDispenser)Selection.activeGameObject.GetComponent(typeof(NetworkObjectDispenser));
            }
            var container = new VisualElement();

            int panelId = property.FindPropertyRelative("panelId").intValue;

            Box backgroundBox = new();
            backgroundBox.style.backgroundColor = new UnityEngine.Color(0.1f, 0.1f, 0.1f, .5f);
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("panelName"), "Group Name"));
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("panel"), "Group GameObject"));


            if(m_target == null)
            {
                Label errorLabel = new Label("Please select a NetworkObjectDispenser GameObject to Spawn Previews.");
                backgroundBox.Add(errorLabel);
                container.Add(backgroundBox);
                return container;
            }

            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;
            TextElement textElement = new()
            {
                style =
                {
                    fontSize = 12,
                    marginTop = 5.0f,
                    marginLeft = 5.0f,
                    marginBottom = 5.0f
                },
                text = "Group Preview"
            };
            buttonContainer.Add(textElement);
            var showButton = ObjectDispenserEditorButton.CreateDefaultButton("Show");
            showButton.style.marginLeft = 50.0f;
            showButton.style.alignSelf = Align.FlexEnd;
            var hideButton = ObjectDispenserEditorButton.CreateDefaultButton("Hide");
            hideButton.style.alignSelf = Align.FlexEnd;

            PanelToggleGroup panelToggle = new(showButton, hideButton, panelId, -1);

            showButton.clicked += () => SpawnPanel(panelToggle);
            hideButton.clicked += () => ClearPanel(panelToggle);
            m_target.OnProxiesUpdated += () => UpdateButtonsState(panelToggle);

            UpdateButtonsState(panelToggle);

            buttonContainer.Add(showButton);
            buttonContainer.Add(hideButton);

            backgroundBox.Add(buttonContainer);

            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("dispenserSlots"), "Spawn Slots"));
            container.Add(backgroundBox);

            return container;
        }

        void SpawnPanel(PanelToggleGroup panelToggle)
        {
            if(m_target != null)
                m_target.SpawnProxyPanel(panelToggle.panelId);
            UpdateButtonsState(panelToggle);
        }

        void ClearPanel(PanelToggleGroup panelToggle)
        {
            if(m_target != null)
                m_target.ClearProxyPanel(panelToggle.panelId);
            UpdateButtonsState(panelToggle);
        }

        void UpdateButtonsState(PanelToggleGroup panelToggle)
        {
            bool isShowing = m_target.IsProxyPanelShowing(panelToggle.panelId);
            bool IsFull = m_target.IsProxyPanelFull(panelToggle.panelId);
            panelToggle.showButton.SetEnabled(!IsFull);
            panelToggle.hideButton.SetEnabled(isShowing);
        }
    }

    [CustomPropertyDrawer(typeof(DispenserSlot))]
    public class DispenserSlotDrawer : PropertyDrawer
    {
        NetworkObjectDispenser m_target = null;
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if(Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<NetworkObjectDispenser>() != null)
            {
                m_target = (NetworkObjectDispenser)Selection.activeGameObject.GetComponent(typeof(NetworkObjectDispenser));
            }

            var container = new VisualElement();

            int slotId = property.FindPropertyRelative("slotId").intValue;
            int panelId = property.FindPropertyRelative("panelId").intValue;
            bool hasSpawnedProxy = property.FindPropertyRelative("hasSpawnedProxy").boolValue;

            Box backgroundBox = new();
            backgroundBox.style.backgroundColor = new UnityEngine.Color(0.1f, 0.1f, 0.1f, .5f);
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("freezeOnSpawn"), "Freeze On Spawn"));
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("distanceToSpawnNew"), "Distance To Spawn New"));
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("spawnCooldown"), "Spawn Cooldown"));
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("dispenserSlotTransform"), "Spawn Transform"));
            backgroundBox.Add(new PropertyField(property.FindPropertyRelative("spawnableInteractablePrefab"), "Spawn Prefab"));

            if(m_target == null)
            {
                Label errorLabel = new Label("Please select a NetworkObjectDispenser GameObject to Spawn Previews.");
                backgroundBox.Add(errorLabel);
                container.Add(backgroundBox);
                return container;
            }


            var buttonContainer = new VisualElement();
            buttonContainer.style.flexDirection = FlexDirection.Row;


            TextElement textElement = new()
            {
                style =
                {
                    fontSize = 12,
                    marginTop = 5.0f,
                    marginLeft = 5.0f,
                    marginBottom = 5.0f
                },
                text = "Object Preview"
            };
            buttonContainer.Add(textElement);
            var showButton = ObjectDispenserEditorButton.CreateDefaultButton("Show");
            showButton.style.marginLeft = 50.0f;
            var hideButton = ObjectDispenserEditorButton.CreateDefaultButton("Hide");

            PanelToggleGroup panelToggle = new(showButton, hideButton, panelId, slotId);

            showButton.clicked += () => SpawnButton(panelToggle);
            hideButton.clicked += () => ClearButton(panelToggle);
            m_target.OnProxiesUpdated += () => UpdateButtonsState(panelToggle);

            UpdateButtonsState(panelToggle);

            buttonContainer.Add(showButton);
            buttonContainer.Add(hideButton);

            backgroundBox.Add(buttonContainer);

            container.Add(backgroundBox);
            return container;
        }

        void SpawnButton(PanelToggleGroup panelToggle)
        {
            if(m_target != null)
            {
                m_target.SpawnProxy(panelToggle.panelId, panelToggle.slotId);
            }
            UpdateButtonsState(panelToggle);
        }

        void ClearButton(PanelToggleGroup panelToggle)
        {
            if(m_target != null)
            {
                m_target.ClearProxy(panelToggle.panelId, panelToggle.slotId);
            }
            UpdateButtonsState(panelToggle);
        }

        void UpdateButtonsState(PanelToggleGroup panelToggle)
        {
            bool isProxyShowing = m_target.IsProxySlotShowing(panelToggle.panelId, panelToggle.slotId);
            panelToggle.showButton.SetEnabled(!isProxyShowing);
            panelToggle.hideButton.SetEnabled(isProxyShowing);
        }
    }

    public static class ObjectDispenserEditorButton
    {
        public static Button CreateDefaultButton(string buttonText)
        {
            var button = new Button
            {
                text = buttonText,
                style =
                {
                    height = 20.0f,
                    width = 75.0f,
                }
            };

            return button;
        }
    }

    public struct PanelToggleGroup
    {
        public Button showButton;
        public Button hideButton;
        public int panelId;
        public int slotId;

        public PanelToggleGroup(Button showButton, Button hideButton, int panelId, int slotId)
        {
            this.showButton = showButton;
            this.hideButton = hideButton;
            this.panelId = panelId;
            this.slotId = slotId;
        }
    }
#endif
}
