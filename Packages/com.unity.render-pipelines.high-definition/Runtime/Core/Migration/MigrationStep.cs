using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Define helpers to manipulate <see cref="MigrationStep{TVersion, TTarget}"/>.</summary>
    public static class MigrationStep
    {
        /// <summary>Create a new <see cref="MigrationStep{TVersion, TTarget}"/>.</summary>
        /// <typeparam name="TVersion">An enum identifying the version.</typeparam>
        /// <typeparam name="TTarget">The type to migrate.</typeparam>
        /// <param name="version">The version of the step.</param>
        /// <param name="action">The migration action to perform.</param>
        /// <returns>The migration step.</returns>
        public static MigrationStep<TVersion, TTarget> New<TVersion, TTarget>(TVersion version, Action<TTarget> action)
            where TVersion : struct, IConvertible
            where TTarget : class, IVersionable<TVersion>
        {
            return new MigrationStep<TVersion, TTarget>(version, action);
        }
    }

    /// <summary>Define a migration step.</summary>
    /// <typeparam name="TVersion">An enum identifying the version.</typeparam>
    /// <typeparam name="TTarget">The type to migrate.</typeparam>
    public struct MigrationStep<TVersion, TTarget> : IEquatable<MigrationStep<TVersion, TTarget>>
        where TVersion : struct, IConvertible
        where TTarget : class, IVersionable<TVersion>
    {
        readonly Action<TTarget> m_MigrationAction;

        /// <summary>The version of the step.</summary>
        public readonly TVersion Version;

        /// <summary>Create a new migration step.</summary>
        /// <param name="version">The version of the step.</param>
        /// <param name="action">The migration action to perform.</param>
        public MigrationStep(TVersion version, Action<TTarget> action)
        {
            Version = version;
            m_MigrationAction = action;
        }

        /// <summary>
        /// Migrate the instance for this step and set the version of the instance to this version.
        ///
        /// If the instance has a version greater or equal to the step one, nothing will be applied.
        /// </summary>
        /// <param name="target">The instance to migrate.</param>
        public void Migrate(TTarget target)
        {
            if ((int)(object)target.version >= (int)(object)Version)
                return;

            m_MigrationAction(target);
            target.version = Version;
        }

        /// <summary>Evaluate equality between migration steps.</summary>
        /// <param name="other">Other step to evaluate.</param>
        /// <returns>True when the steps are equals.</returns>
        public bool Equals(MigrationStep<TVersion, TTarget> other)
        {
            return Version.Equals(other.Version);
        }
    }
}
