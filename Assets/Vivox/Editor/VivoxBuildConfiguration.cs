/*
Copyright (c) 2014-2018 by Mercer Road Corp

Permission to use, copy, modify or distribute this software in binary or source form
for any purpose is allowed only under explicit prior consent in writing from Mercer Road Corp

THE SOFTWARE IS PROVIDED "AS IS" AND MERCER ROAD CORP DISCLAIMS
ALL WARRANTIES WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL MERCER ROAD CORP
BE LIABLE FOR ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL
DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR
PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS
ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS
SOFTWARE.
*/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[System.Serializable]
public class VivoxBuildTargetPreferences
{
    [Header("Subdirectory name inside of builds folder")]
    public string BuildPath;
    public bool IncludedInBuildAll;
    public readonly BuildTargetGroup BuildTargetGroup;
    public readonly BuildTarget BuildTarget;
    [Tooltip("If build will run deploy and run")]
    public bool shouldRunAfterBuild = true;
    [Header("Used to group Build All so like platforms build in order")]
    [Tooltip("Helpful to group like device hardware based on graphic needs")]
    public int OverrideGroup;

    public VivoxBuildTargetPreferences(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget, int overrideGroup, bool includeInBuildAll)
    {
        BuildPath = $"/{buildTarget}";
        BuildTarget = buildTarget;
        BuildTargetGroup = buildTargetGroup;
        OverrideGroup = overrideGroup;
        IncludedInBuildAll = includeInBuildAll;
    }
}

[System.Serializable]
public class VivoxBuildConfiguration : ScriptableObject
{
    #region Public Properties

    public string BasePath = "/Builds";
    public VivoxBuildTargetPreferences Win64 = new VivoxBuildTargetPreferences(BuildTargetGroup.Standalone,
        BuildTarget.StandaloneWindows64, 1, true);

    public VivoxBuildTargetPreferences Win = new VivoxBuildTargetPreferences(BuildTargetGroup.Standalone,
        BuildTarget.StandaloneWindows, 1, true);

    public VivoxBuildTargetPreferences MacOS = new VivoxBuildTargetPreferences(BuildTargetGroup.Standalone,
        BuildTarget.StandaloneOSX, 1, true);

    public VivoxBuildTargetPreferences IOS = new VivoxBuildTargetPreferences(BuildTargetGroup.iOS,
        BuildTarget.iOS, 2, true);

    public VivoxBuildTargetPreferences Android =new VivoxBuildTargetPreferences(BuildTargetGroup.Android,
        BuildTarget.Android, 2, true);

    public VivoxBuildTargetPreferences XboxOne = new VivoxBuildTargetPreferences(BuildTargetGroup.XboxOne,
        BuildTarget.XboxOne, 3, false);

    public VivoxBuildTargetPreferences PS4 = new VivoxBuildTargetPreferences(BuildTargetGroup.PS4,
        BuildTarget.PS4, 3, false);

    public VivoxBuildTargetPreferences Switch = new VivoxBuildTargetPreferences(BuildTargetGroup.Switch,
        BuildTarget.Switch, 3, false);

    public List<VivoxBuildTargetPreferences> BuildList
    {
        get
        {
            return new List<VivoxBuildTargetPreferences>()
        {
            Win64,
            Win,
            MacOS,
            IOS,
            Android,
            XboxOne,
            PS4,
            Switch
        };
        }
    }
    public EditorBuildSettingsScene[] Levels { get => EditorBuildSettings.scenes; }

    #endregion
}