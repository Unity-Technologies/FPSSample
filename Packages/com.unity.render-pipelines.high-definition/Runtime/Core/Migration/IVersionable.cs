using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    /// <summary>Implement this interface to use with <see cref="MigrationDescription{TVersion, TTarget}"/></summary>
    /// <typeparam name="TVersion">An enum to use to describe the version.</typeparam>
    public interface IVersionable<TVersion>
        where TVersion : struct, IConvertible
    {
        /// <summary>Accessors to the current version of the instance.</summary>
        TVersion version { get; set; }
    }
}
