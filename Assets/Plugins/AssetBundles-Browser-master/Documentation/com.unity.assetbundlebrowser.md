# Unity Asset Bundle Browser tool

This tool enables the user to view and edit the configuration of asset bundles for their Unity project. It will block editing that would create invalid bundles, and inform you of any issues with existing bundles. It also provides basic build functionality.

Use this tool as an alternative to selecting assets and setting their asset bundle manually in the inspector. It can be dropped into any Unity project with a version of 5.6 or greater. It will create a new menu item in __Window__ > __AssetBundle Browser__. The bundle configuration, build functionality, and build-bundle inspection are split into three tabs within the new window.

![BrowserHeader](images/browser_header.png)

### Requires Unity 5.6+

# Usage - Configure

This window provides an explorer like interface to managing and modifying asset bundles in your project. When first opened, the tool will parse all bundle data in the background, slowly marking warnings or errors it detects. It does what it can to stay in sync with the project, but cannot always be aware of activity outside the tool. To force a quick pass at error detection, or to update the tool with changes made externally, hit the Refresh button in the upper left.

The window is broken into four sections: Bundle List, Bundle Details, Asset List, and Asset Details. 
![BroserConfigure](images/browser_configure2.png)

### Bundle List

Left hand pane showing a list of all bundles in the project. Available functionality:

* Select a bundle or set of bundles to see a list of the assets that will be in the bundle in the Asset List pane.

* Bundles with variants are a darker grey and can be expanded to show the list of variants.

* Right-click or slow-double-click to rename bundle or bundle folder.

* If a bundle has any error, warning, or info message, an icon will appear on the right side. Mouse over the icon for more information.

* If a bundle has at least one scene in it (making it a scene bundle) and non-scene assets explicitly included, it will be marked as having an error. This bundle will not build until fixed.

* Bundles with duplicated assets will be marked with a warning (more information on duplication in Asset List section below)

* Empty bundles will be marked with an info message. For a number of reasons, empty bundles are not very stable and can dissapear from this list at times.

* Folders of bundles will be marked with the highest message from the contained bundles.

* To fix the duplicated inclusion of assets in bundles, you can:

    * Right click on a single bundle to move all assets determined to be duplicates into a new bundle.

    * Right click on multiple bundles to either move the assets from all selected bundles that are duplicates into a new bundle, or only those that are shared within the selection.

    * You can also drag duplicate assets out of the Asset List pane into the Bundle List to explicitly include them in a bundle. More info on this in the Asset List feature set below.

* Right click or hit DEL to delete bundles.

* Drag bundles around to move them into and out of folders, or merge them.

* Drag assets from the Project Explorer onto bundels to add them.

* Drag assets onto empty space to create a new bundle.

* Right click to create new bundles or bundle folders.

* Right click to "Convert to Variant"

    * This will add a variant (initially called "newvariant") to the selected bundle.

    * All assets currently in selected bundle will be moved into the new variant

    * ComingSoon: Mismatch detection between variants.
    
Icons indicate if the bundle is a standard or a scene bundle. 

![Icon for standard bundle](images/ABundleBrowserIconY1756Basic.png)

![Icon for scene bundle](images/ABundleBrowserIconY1756Scene.png)

### Bundle Details

Lower left hand pane showing details of the bundles(s) selected in the Bundle List pane. This pane will show the following information if it is available:

* Total bundle size. This is a sum of the on-disk size of all assets.

* Bundles that the current bundle depends on

* Any messages (error/warning/info) associated with the current bundle.

### Asset List

Upper right hand pane providing a list of assets contained in whichever bundles are selected in the Bundle List.  The search field above this list will match with assets in any bundle. The Asset List will only display matching assets, and the Bundle List will only display bundles that contain matching assets.  Available functionality:

* View all assets anticipated to be included in bundle. Sort asset list by any column header.

* View assets explicitly included in bundle. These are assets that have been assigned a bundle explicitly. The inspector will reflect the bundle inclusion, and in this view they will say the bundle name next to the asset name.

* View assets implicitly included in bundle. These assets will say *auto* as the name of the bundle next to the asset name. If looking at these assets in the inspector they will say *None* as the assigned bundle.

    * These assets have been added to the selected bundle(s) due to a dependency on another included asset. Only assets that are not explicitly assigned to a bundle will be implicitly included in any bundles.

    * Note that this list of implicit includes can be incomplete. There are known issues with materials and textures not always showing up correctly.

    * As multiple assets can share dependencies, it is common for a given asset to be implicitly included in multiple bundles. If the tool detects this case, it will mark both the bundle and the asset in question with a warning icon.

    * To fix the duplicate-inclusion warnings, you can manually move assets into a new bundle or right click the bundle and selecting one of the "Move duplicate" options.

* Drag assets from the Project Explorer into this view to add them to the selected bundle. This is only valid if only one bundle is selected, and the asset type is compatable (scenes onto scene bundles, etc.) 

* Drag assets (explicit or implicit) from the Asset List into the Bundle List (to add them to different bundles, or a newly created bundle).

* Right click or hit DEL to remove assets from bundles (does not remove assets from project).

* Select or double-click assets to reveal them in the Project Explorer.

A note on including folders in bundles. It is possible to assign an asset folder (from the Project Explorer) to a bundle. When viewing this in the browser, the folder itself will be listed as explicit and the contents implicit. This reflects the priority system used to assign assets to bundles. For example, say your game had five prefabs in Assets/Prefabs, and you marked the folder "Prefabs" as being in one bundle, and one of the actual prefabs ("PrefabA") as being in another. Once built, "PrefabA" would be in one bundle, and the other four prefabs would be in the other.

### Asset Details

Lower right hand pane showing details of the asset(s) selected in the Asset List pane. This pane cannot be interacted with, but will show the following information if it is available:

* Full path of asset

* Reason for implicit inclusion in bundles if it is implicit.

* Reason for warning if any.

* Reason for error if any.

### Troubleshooting

* *Can't rename or delet a specific bundle.* This is occasionally caused when first adding this tool to an existing project. Please force a reimport of your assets through the Unity menu system to refresh the data.

### External Tool Integration
Other tools that generate asset bundle data can choose to integrate with the browser. Currently the primary example is the [Asset Bundle Graph Tool](https://bitbucket.org/Unity-Technologies/assetbundlegraphtool).  If integrations are detected, then a selection bar will appear near the top of the browser.  It will allow you to select the Default data source (Unity's AssetDatabase) or an integrated tool. If none are detected, the selector is not present, though you can add it by right-clicking on the tab header and selecting "Custom Sources".  

# Usage - Build

The Build tab provides basic build functionality to get you started using asset bundles. In most profressional scenarios, users will end up needing a more advanced build setup. All are welcome to use the build code in this tool as a starting point for writing their own once this no longer meets their needs. For the most part, the options here are directly tied to the options the engine expects in [BuildAssetBundleOptions](https://docs.unity3d.com/ScriptReference/BuildAssetBundleOptions.html). Interface:

* *Build Target* - Platform the bundles will be built for

* *Output Path* - Path for saving built bundles. By default this is AssetBundles/. You can edit the path manually, or by selecting "Browse". To return to the default naming convention, hit "Reset".

* *Clear Folders* - This will delete all data from the build path folder prior to building.

* *Copy to StreamingAssets* - After the build is complete, this will copy the results to Assets/StreamingAssets. This can be useful for testing, but would not be used in production.

* *Advanced Settings*

    * *Compression* - Choose between no compression, standard LZMA, or chunk-based LZ4 compression.

    * *Exclude Type Information* - Do not include type information within the asset bundle

    * *Force Rebuild* - Rebuild bundles needing to be built. This is different than "Clear Folders" as this option will not delete bundles that no longer exist.

    * *Ignore Type Tree Changes* - Ignore the type tree changes when doing the incremental build check.

    * *Append Hash* - Append the hash to the asset bundle name.

    * *Strict Mode* - Do not allow the build to succeed if any errors are reporting during it.

    * *Dry Run Build* - Do a dry run build.

* *Build* - Executes build.

# Usage - Inspect
This tab enables you to inspect the contents of bundles that have already been built. 
### Usage
* If you use the Browser to build, then the path you built to will automatically be added here.
* Click "Add File" or "Add Folder" to add bundles to inspect.  
* Click the "-" next to each row to remove that file or folder. Note, you cannot remove individual files that were added by adding a folder.  
* Select any bundle listed to see details:
  * Name
  * Size on disk
  * Source Asset Paths - the assets explicitly added to this bundle. Note this list is incomplete for scene bundles.
  * Advanced Data - includes information on the Preload Table, Container (explicit assets) and Dependencies.
