﻿using Suzuryg.FaceEmo.Detail.AV3.Importers;
using Suzuryg.FaceEmo.Domain;
using Suzuryg.FaceEmo.UseCase;
using Suzuryg.FaceEmo.Components;
using Suzuryg.FaceEmo.Components.Settings;
using Suzuryg.FaceEmo.Detail;
using Suzuryg.FaceEmo.Detail.AV3;
using Suzuryg.FaceEmo.Detail.Drawing;
using Suzuryg.FaceEmo.Detail.View;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Suzuryg.FaceEmo.Detail.Localization;
using UniRx;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.Contact.Components;
using System.Linq;

namespace Suzuryg.FaceEmo.AppMain
{
    [CustomEditor(typeof(FaceEmoLauncherComponent))]
    public class FaceEmoLauncher : ScriptlessEditor
    {
        GameObject _rootObject;
        InspectorView _inspectorView;
        CompositeDisposable _disposables = new CompositeDisposable();

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            Initialize();
            _inspectorView?.OnGUI();
        }

        private void Initialize()
        {
            if (_rootObject == null)
            {
                _disposables?.Dispose();
                _disposables = new CompositeDisposable();

                _rootObject = (target as FaceEmoLauncherComponent).gameObject;
                var installer = new FaceEmoInstaller(_rootObject);

                // Inspector view
                _inspectorView = installer.Container.Resolve<InspectorView>().AddTo(_disposables);
                _inspectorView.OnLaunchButtonClicked.Synchronize().Subscribe(_ => Launch(target as FaceEmoLauncherComponent)).AddTo(_disposables);
                _inspectorView.OnLocaleChanged.Synchronize().Subscribe(ChangeLocale).AddTo(_disposables);
                _inspectorView.OnMenuUpdated.Synchronize().Subscribe(x => UpdateMenu(x.menu, x.isModified)).AddTo(_disposables);

                // Disposables
                installer.Container.Resolve<InspectorThumbnailDrawer>().AddTo(_disposables);

                // Initialize
                var menuRepository = installer.Container.Resolve<IMenuRepository>();
                var updateMenuSubject = installer.Container.Resolve<UpdateMenuSubject>();
                updateMenuSubject.OnNext(menuRepository.Load(null), isModified: false);
            }
        }

        private static bool CanLaunch()
        {
            var canLaunch = true;
            var loc = LocalizationSetting.GetTable(LocalizationSetting.GetLocale());

#if !USE_MODULAR_AVATAR
            EditorUtility.DisplayDialog(DomainConstants.SystemName, LocalizationSetting.InsertLineBreak(loc.Launcher_Message_CheckModularAvatar), "OK");
            canLaunch = false;
#endif

#if !VALID_VRCSDK3_AVATARS
            EditorUtility.DisplayDialog(DomainConstants.SystemName, LocalizationSetting.InsertLineBreak(loc.Launcher_Message_CheckVrcSdk3Avatars), "OK");
            canLaunch = false;
#endif

            return canLaunch;
        }

        public static void Launch(FaceEmoLauncherComponent launcher)
        {
            var loc = LocalizationSetting.GetTable(LocalizationSetting.GetLocale());
            try
            {
                if (!CanLaunch()) { return; }

                var rootObject = launcher.gameObject;

                MainWindow mainWindow;
                var existingWindows = Resources.FindObjectsOfTypeAll<MainWindow>();
                if (existingWindows.Any())
                {
                    mainWindow = existingWindows.First();
                }
                else
                {
                    mainWindow = CreateInstance<MainWindow>();
                }

                mainWindow.Initialize(rootObject.GetFullPath());
                mainWindow.Show();
                mainWindow.Focus();
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog(DomainConstants.SystemName, loc.Common_Message_FailedToLaunch + "\n" + loc.Common_Message_SeeConsole, "OK");
                Debug.LogError(loc.Common_Message_FailedToLaunch + "\n" + ex?.ToString());
            }
        }

        private void UpdateMenu(IMenu menu, bool isModified)
        {
            var existingWindows = Resources.FindObjectsOfTypeAll<MainWindow>();
            if (existingWindows.Any())
            {
                existingWindows.First().UpdateMenu(menu, isModified);
            }
        }

        private void ChangeLocale(Locale locale)
        {
            // Change all instance's locale
            foreach (var window in Resources.FindObjectsOfTypeAll<MainWindow>())
            {
                window.ChangeLocale(locale);
            }
        }

        [MenuItem("FaceEmo/New Menu", false, 0)]
        public static GameObject Create(MenuCommand menuCommand)
        {
            var gameObject = GetLauncherObject(menuCommand);

            // Create GameObject which has unique name in acitive scene.
            var baseName = DomainConstants.SystemName;
            var objectName = baseName;
            var cnt = 1;
            while (GameObject.Find(objectName))
            {
                objectName = $"{baseName}_{cnt}";
                cnt++;
            }
            gameObject.name = objectName;

            return gameObject;
        }

        [MenuItem("FaceEmo/Restore Menu", false, 1)]
        public static void Restore(MenuCommand menuCommand)
        {
            var selectedPath = EditorUtility.OpenFilePanelWithFilters(title: null, directory: FaceEmoBackupper.BackupDir, filters: new[] { "FaceEmoProject" , "asset" });
            if (string.IsNullOrEmpty(selectedPath)) { return; }

            // OpenFilePanel path is in OS format, so convert it to Unity format
            var unityPath = PathConverter.ToUnityPath(selectedPath);

            var gameObject = GetLauncherObject(menuCommand);
            var name = System.IO.Path.GetFileName(selectedPath).Replace(".asset", string.Empty);
            gameObject.name = name;

            var installer = new FaceEmoInstaller(gameObject);
            var backupper = installer.Container.Resolve<IBackupper>();
            try
            {
                backupper.Import(unityPath);
            }
            catch (Exception ex)
            {
                var loc = LocalizationSetting.GetTable(LocalizationSetting.GetLocale());
                EditorUtility.DisplayDialog(DomainConstants.SystemName, loc.Common_Message_FailedToOpenProject + "\n" + loc.Common_Message_SeeConsole, "OK");
                Debug.LogError(loc.Common_Message_FailedToOpenProject + "\n" + ex?.ToString());
                if (gameObject != null) { DestroyImmediate(gameObject); }
            }
        }

        private static GameObject GetLauncherObject(MenuCommand menuCommand)
        {
            var gameObject = new GameObject();
            gameObject.AddComponent<FaceEmoLauncherComponent>();
            GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {DomainConstants.SystemName} Object");
            Selection.activeObject = gameObject;
            UnityEditorInternal.InternalEditorUtility.SetIsInspectorExpanded(gameObject.GetComponent<FaceEmoLauncherComponent>(), true);
            return gameObject;
        }

        [InitializeOnLoadMethod]
        private static void AddHierarchyItemOnGUI()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            if (EditorPrefs.GetBool(DetailConstants.KeyHideHierarchyIcon, DetailConstants.DefaultHideHierarchyIcon))
            {
                return;
            }

            var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (gameObject == null)
            {
                return;
            }

            var avatarDescriptor = gameObject.GetComponent<VRCAvatarDescriptor>();
            if (avatarDescriptor == null)
            {
                return;
            }

            var loc = LocalizationSetting.GetTable(LocalizationSetting.GetLocale());

            const float buttonWidth = 30;
            float xOffsetForOtherTools = EditorPrefs.GetFloat(DetailConstants.KeyHierarchyIconOffset, DetailConstants.DefaultHierarchyIconOffset);
            selectionRect.xMin += selectionRect.width - buttonWidth - xOffsetForOtherTools;
            selectionRect.xMax -= xOffsetForOtherTools;
            selectionRect.yMin += 1;
            selectionRect.yMax -= 1;
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("b8710db71cc992745987c4e92d53fcd1")); // logo
            if (GUI.Button(selectionRect, new GUIContent(string.Empty, loc?.Common_Tooltip_LaunchFromHierarchy)))
            {
                if (!CanLaunch()) { return; }

                var exists = false;
                foreach (var launcher in FindObjectsOfType<FaceEmoLauncherComponent>()?.OrderBy(x => x.name))
                {
                    if (ReferenceEquals(launcher?.AV3Setting?.TargetAvatar, avatarDescriptor))
                    {
                        Launch(launcher);
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    var launcherObject = Create(new MenuCommand(null, 0));
                    var installer = new FaceEmoInstaller(launcherObject);

                    var launcher = launcherObject.GetComponent<FaceEmoLauncherComponent>();
                    launcher.AV3Setting.TargetAvatar = avatarDescriptor;

                    try
                    {
                        ImportPatternsAndOptions(launcherObject, installer, avatarDescriptor);
                    }
                    catch (Exception ex)
                    {
                        EditorUtility.DisplayDialog(DomainConstants.SystemName, loc.Launcher_Message_ImportError + "\n\n" + ex?.Message, "OK");
                        Debug.LogError(loc.Launcher_Message_ImportError + ex?.ToString());

                        UnityEngine.Object.DestroyImmediate(launcher);

                        installer = new FaceEmoInstaller(launcherObject);

                        launcher = launcherObject.GetComponent<FaceEmoLauncherComponent>();
                        launcher.AV3Setting.TargetAvatar = avatarDescriptor;
                    }

                    Launch(launcher);
                }
            }
            GUI.DrawTexture(selectionRect, icon, ScaleMode.ScaleToFit, alphaBlend: true);
        }

        private static void ImportPatternsAndOptions(GameObject rootObject, FaceEmoInstaller installer, VRCAvatarDescriptor avatarDescriptor)
        {
            using (var disposables = new CompositeDisposable())
            {
                // resolve
                var av3Setting = installer.Container.Resolve<AV3Setting>();
                var menuRepository = installer.Container.Resolve<IMenuRepository>();
                var selectionSynchronizer = installer.Container.Resolve<SelectionSynchronizer>().AddTo(disposables);
                var localizationSetting = installer.Container.Resolve<IReadOnlyLocalizationSetting>();
                var localizationTable = localizationSetting.Table;

                // initialize
                var menu = menuRepository.Load(string.Empty);
                var expressionImporter = new ExpressionImporter(menu, av3Setting, ImportUtility.GetNewAssetDir(), localizationSetting);
                var contactImporter = new ContactSettingImporter(av3Setting);

                // import
                var importedPatterns = expressionImporter.ImportExpressionPatterns(avatarDescriptor);
                var importedClips = expressionImporter.ImportOptionalClips(avatarDescriptor);
                var importedContacts = contactImporter.Import(avatarDescriptor);

                // change selection
                if (menu.Registered.Order.Any())
                {
                    selectionSynchronizer.ChangeMenuItemListViewSelection(menu.Registered.Order.First());
                }

                // save
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                menuRepository.Save(string.Empty, menu, "Import Expression Patterns");

                // pattern results
                var patternResults = new List<(MessageType type, string message)>();
                if (importedPatterns.Any())
                {
                    patternResults.Add((MessageType.None, localizationTable.ExpressionImporter_Message_PatternsImported));
                    patternResults.Add((MessageType.None, string.Empty));

                    foreach (var pattern in importedPatterns)
                    {
                        patternResults.Add((MessageType.None, $"{pattern.DisplayName}{localizationTable.Common_Colon}{pattern.Branches.Count}{localizationTable.ExpressionImporter_Expressions}"));
                    }

                    patternResults.Add((MessageType.None, string.Empty));
                    patternResults.Add((MessageType.Info, LocalizationSetting.InsertLineBreak(localizationTable.ExpressionImporter_Info_BehaviorDifference)));
                }
                else
                {
                    patternResults.Add((MessageType.None, localizationTable.ExpressionImporter_Message_NoPatterns));
                }

                // option results
                var optionResults = new List<(MessageType type, string message)>();
                if (importedClips.blink != null)
                {
                    optionResults.Add((MessageType.None, string.Empty));
                    optionResults.Add((MessageType.None, $"{localizationTable.ExpressionImporter_Blink}{localizationTable.Common_Colon}{importedClips.blink.name}"));
                }
                if (importedClips.mouthMorphCancel != null)
                {
                    optionResults.Add((MessageType.None, string.Empty));
                    optionResults.Add((MessageType.None, $"{localizationTable.ExpressionImporter_MouthMorphCanceler}{localizationTable.Common_Colon}{importedClips.mouthMorphCancel.name}"));
                }
                if (importedContacts.Any())
                {
                    optionResults.Add((MessageType.None, string.Empty));
                    optionResults.Add((MessageType.None, $"{localizationTable.ExpressionImporter_Contacts}{localizationTable.Common_Colon}"));
                    foreach (var contact in importedContacts)
                    {
                        optionResults.Add((MessageType.None, contact.name));
                    }
                }

                if (optionResults.Any())
                {
                    optionResults.Insert(0, (MessageType.None, localizationTable.ExpressionImporter_Message_OptionalSettingsImported));

                    if (rootObject != null)
                    {
                        var path = rootObject.GetFullPath();
                        if (path.StartsWith("/"))
                        {
                            path = path.Substring(1);
                        }
                        optionResults.Add((MessageType.None, string.Empty));
                        optionResults.Add((MessageType.Info, localizationTable.ExpressionImporter_Info_ChangeOptionalSettings + "\n\n" + path));
                    }
                }
                else
                {
                    optionResults.Add((MessageType.None, localizationTable.ExpressionImporter_Message_NoOptionalSettings));
                }

                // show messages
                var messages = new List<(MessageType type, string message)>(patternResults);
                messages.Add((MessageType.None, string.Empty));
                foreach (var item in optionResults)
                {
                    messages.Add(item);
                }
                OptoutableDialog.Show(DomainConstants.SystemName, string.Empty,
                    "OK", isRiskyAction: false,
                    additionalMessages: messages, windowHeight: OptoutableDialog.GetHeightWithoutMessage());
            }
        }

        private const string HideHierarchyIconPath = "FaceEmo/Hide Hierarchy Icon";
        private const int HideHierarchyIconOrder = 100;

        [MenuItem(HideHierarchyIconPath, false, HideHierarchyIconOrder)]
        private static void HideHierarchyIcon()
        {
            EditorPrefs.SetBool(DetailConstants.KeyHideHierarchyIcon, !EditorPrefs.GetBool(DetailConstants.KeyHideHierarchyIcon, DetailConstants.DefaultHideHierarchyIcon));
        }

        [MenuItem(HideHierarchyIconPath, true, HideHierarchyIconOrder)]
        private static bool HideHierarchyIconValidate()
        {
            UnityEditor.Menu.SetChecked(HideHierarchyIconPath, EditorPrefs.GetBool(DetailConstants.KeyHideHierarchyIcon, DetailConstants.DefaultHideHierarchyIcon));
            return true;
        }
    }
}
