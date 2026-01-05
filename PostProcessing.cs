using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameBuilderEditor
{
    /// <summary>
    /// Inherit this class to create build post processors that execute after a build is finished, using the
    /// <see cref="GameBuilderWindow"/>
    /// </summary>
    public interface IBuildPostProcessor
    {
        /// <summary>
        /// This will be called after the game is successfully built
        /// </summary>
        void Execute(BuildReport report);

        /// <summary>
        /// copes this instance
        /// </summary>
        IBuildPostProcessor Copy();
    }

    /// <summary>
    /// Handles scheduling <see cref="BuildPostProcessor"/>s to execute after a successful build
    /// </summary>
    internal class BuildPostProcessorInitializer : IPostprocessBuildWithReport
    {
        /// <summary>
        /// If set, they'll be executed after a successful build and then be reset to null
        /// </summary>
        public static IBuildPostProcessor[] postProcessors;

        int IOrderedCallback.callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (postProcessors == null)
                return;
            foreach (var postProcessor in postProcessors)
            {
                try
                {
                    postProcessor.Execute(report);
                }
                catch
                {
                    Debug.LogErrorFormat(
                        "build post processor {0} failed. the rest will not execute. the exception will be printed below",
                        postProcessor.GetType().Name);
                    throw;
                }
            }
            postProcessors = null;
        }
    }
}