﻿using Suzuryg.FacialExpressionSwitcher.Domain;
using Suzuryg.FacialExpressionSwitcher.Detail.Data;
using Suzuryg.FacialExpressionSwitcher.Detail.Localization;
using Suzuryg.FacialExpressionSwitcher.Detail.Subject;
using Suzuryg.FacialExpressionSwitcher.Detail.View;
using UnityEngine;
using UnityEditor;

namespace Suzuryg.FacialExpressionSwitcher.AppMain
{
    public class MainWindow : EditorWindow
    {
        [SerializeField] private string _launcherObjectPath;
        private MainView _mainView;
        private Undo.UndoRedoCallback _undoRedoCallback;

        public MainWindow()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        ~MainWindow()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        public void Initialize(string launcherObjectPath)
        {
            _launcherObjectPath = launcherObjectPath;
            Build();
        }

        private void Build()
        {
            Clean();
            if (_launcherObjectPath is string)
            {
                if (!_launcherObjectPath.StartsWith("/"))
                {
                    EditorUtility.DisplayDialog(DomainConstants.SystemName, $"{_launcherObjectPath} is not a full path.", "OK");
                    return;
                }

                var launcherObject = GameObject.Find(_launcherObjectPath);
                if (launcherObject is null)
                {
                    EditorUtility.DisplayDialog(DomainConstants.SystemName, $"{_launcherObjectPath} was not found. Please activate the GameObject.", "OK");
                    return;
                }

                // Unity's bug: If the object is nested more than 1 level, the object is found even if it is deactivated. This code does not deal with the bug.
                launcherObject.SetActive(false);
                var anotherObject = GameObject.Find(_launcherObjectPath);
                launcherObject.SetActive(true);
                if (anotherObject is GameObject)
                {
                    EditorUtility.DisplayDialog(DomainConstants.SystemName, $"{_launcherObjectPath} has duplicate path. Please change GameObject's name.", "OK");
                    return;
                }

                var installer = new FESInstaller(launcherObject);
                _mainView = installer.Container.Resolve<MainView>();
                _mainView.Initialize(rootVisualElement);

                var menuRepository = installer.Container.Resolve<IMenuRepository>();
                var updateMenuSubject = installer.Container.Resolve<UpdateMenuSubject>();
                updateMenuSubject.OnNext(menuRepository.Load(null));

                _undoRedoCallback = () =>
                {
                    if (menuRepository is MenuRepository && updateMenuSubject is UpdateMenuSubject)
                    {
                        updateMenuSubject.OnNext(menuRepository.Load(null));
                    }
                };
                Undo.undoRedoPerformed += _undoRedoCallback;
            }
        }

        private void Clean()
        {
            _mainView?.Dispose();
            _mainView = null;

            if (_undoRedoCallback is Undo.UndoRedoCallback)
            {
                Undo.undoRedoPerformed -= _undoRedoCallback;
            }
        }

        private void OnEnable()
        {
            Build();
        }

        private void OnDisable()
        {
            Clean();
        }

        private void OnPlayModeChanged(PlayModeStateChange playModeStateChange)
        {
            // UI is enabled only in EditMode.
            switch (playModeStateChange)
            {
                case PlayModeStateChange.EnteredEditMode:
                    rootVisualElement.Clear();
                    Build();
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    rootVisualElement.Clear();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    rootVisualElement.Clear();
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    rootVisualElement.Clear();
                    break;
            }
        }
    }
}