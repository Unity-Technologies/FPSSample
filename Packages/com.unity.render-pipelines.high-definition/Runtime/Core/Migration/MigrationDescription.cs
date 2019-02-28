using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Helpers to manipulate <see cref="MigrationDescription{TVersion, TTarget}"/></summary>
    public static class MigrationDescription
    {
        /// <summary>Create a new migration description.</summary>
        /// <typeparam name="TVersion">An enum identifying the version.</typeparam>
        /// <typeparam name="TTarget">The type to migrate.</typeparam>
        /// <param name="steps">The steps of the migration.</param>
        /// <returns>The migration description.</returns>
        public static MigrationDescription<TVersion, TTarget> New<TVersion, TTarget>(
            params MigrationStep<TVersion, TTarget>[] steps
        )
            where TVersion : struct, IConvertible
            where TTarget : class, IVersionable<TVersion>
        {
            return new MigrationDescription<TVersion, TTarget>(steps);
        }
    }

    /// <summary>Describe migration steps to perform when upgrading from one version of an object to another.</summary>
    /// <typeparam name="TVersion">An enum identifying the version.</typeparam>
    /// <typeparam name="TTarget">The type to migrate.</typeparam>
    /// <example>
    /// <code>
    ///
    /// class MyComponent : MonoBehaviour, IVersionable<MyComponent.Version>
    /// {
    ///     enum Version
    ///     {
    ///         First,
    ///         Second
    ///     }
    ///
    ///     static readonly MigrationDescription<Version, MyComponent> k_MigrationDescription
    ///         = MigrationDescription.New(
    ///             MigrationStep.New(Version.First, (MyComponent target) =>
    ///             {
    ///                 // Migration code for first version
    ///             }),
    ///             MigrationStep.New(Version.Second, (MyComponent target) =>
    ///             {
    ///                 // Migration code for second version
    ///             })
    ///         );
    ///
    ///     [SerializeField]
    ///     Version m_Version;
    ///     Version IVersionable<Version>.Version { get { return m_Version; } set { m_Version = value; } }
    ///
    ///     void Awake()
    ///     {
    ///         k_MigrationDescription.Migrate(this);
    ///     }
    /// }
    /// </code>
    /// </example>
    public struct MigrationDescription<TVersion, TTarget>
        where TVersion : struct, IConvertible
        where TTarget : class, IVersionable<TVersion>
    {
        /// <summary>Steps of the migration. They will be in ascending order of <typeparamref name="TVersion" />.</summary>
        readonly MigrationStep<TVersion, TTarget>[] Steps;

        /// <summary>Build a migration description.</summary>
        /// <param name="steps">The step to follow between each version migration.</param>
        public MigrationDescription(params MigrationStep<TVersion, TTarget>[] steps)
        {
            // Sort by version
            Array.Sort(steps, (l, r) => (int)(object)l.Version - (int)(object)r.Version);
            Steps = steps;
        }

        /// <summary>
        /// Execute the migration on the provided instance.
        ///
        /// All steps with a version greater than the instance version will be executed in ascending order.
        /// Eg: for instance with version 2 and step version 1, 3, 5, and 6.
        /// It will execute steps 3 then 5 then 6.
        /// </summary>
        /// <param name="target">The instance to migrate.</param>
        public void Migrate(TTarget target)
        {
            if ((int)(object)target.version == (int)(object)Steps[Steps.Length - 1].Version)
                return;

            for (int i = 0; i < Steps.Length; ++i)
            {
                if ((int)(object)target.version < (int)(object)Steps[i].Version)
                {
                    Steps[i].Migrate(target);
                    target.version = Steps[i].Version;
                }
            }
        }
    }
}
