using System;
using UnityEditor;

namespace GameBuilder
{
    public static class EditorGuiUtilities
    {
        public readonly struct LabelWidth : IDisposable
        {
            private readonly float w;
            public LabelWidth(float width)
            {
                w = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = width;
            }

            public readonly void Dispose()
            {
                EditorGUIUtility.labelWidth = w;
            }
        }
    }
}