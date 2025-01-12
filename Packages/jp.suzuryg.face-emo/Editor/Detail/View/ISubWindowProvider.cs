﻿using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Suzuryg.FaceEmo.Detail.View
{
    public interface ISubWindowProvider
    {
        T Provide<T>() where T : EditorWindow, ISubWindow;
        T ProvideIfOpenedAlready<T>() where T : EditorWindow, ISubWindow;
    }
}
