# Animation Pipeline

### Iteration
We assume an iterative workflow where animations are put into game in their earliest/mockup inceptions and continuously updated as the projects progresses, bug surface and standards change late in production.

### Authoring
Animation is done in Maya. A Maya scene typically contains a grouping of related animations, for instance all animations for the Stand State.

![](images/MayaAnimation.png)

From here a seperate clip is exported for each animation, with the help of a custom exporter. Splitting each animation into multiple exports allow for quick export and import times. E.G:

> //sample_game/prototype01/main/ProjectFolder/Assets/Animation/Characters/Terraformer/Clips/Locomotion--Run_N.anim.fbx
> //sample_game/prototype01/main/ProjectFolder/Assets/Animation/Characters/Terraformer/Clips/Locomotion--Run_S.anim.fbx

### Humanoid and Generic
We use both Humanoid and Generic for our characters, which let’s us exercise and show both pipelines:

* Humanoid for Third Person - where you might want to share animation between characters.
* Generic for First Person - which is relative to camera and likely very specific to the skeleton you are using
* Generic for Menu and Cinematic sequences

The use of translation driven squash and stretch (TranslateScale Component) allow for non- uniform scale to be used with Generic as well as with Humanoid/Re-targeting.

### Avatars
We aim to define the Avatar for a Skeleton once (well once for Humanoid and once for Generic) and let any subsequent model and animation FBX’s copy this avatar:

![](images/AvatarReference.png)

Currently all Avatars copies must be updated if the source Avatar changes. Here is a small script that will accomplish that:

![](images/UpdateAvatarReferences.png)

### Avatar Masks
We use Avatar Masks for all our animations for a few reasons:

* Only generate animation curves for relevant bones.
* Generate curves for Generic bones when using Humanoid (these are masked out by default when no mask is used).


![](images/DefaultMask.png)

As with Avatars, we assume a single default mask will be used for all Clips, unless a special case mask is needed. So we use the copy from option:

![](images/ClipMask.png)

If the source mask changes it must be updated in all dependent clips, so there's a button for that:

![](images/UpdateAnimationMasks.png)

### Import AssetPostProcessor
An AssetPostProcessor is used during animation import, which does just a few things:

The idea is to give the importer more keyframes to work with and rely on it disregarding those it does not need during compression.
* We have a lot of fast movement in our game, so we over sample Humanoid animations by a factor 2. This will allow the resulting Muscle clips to better represent the original motion and we in turn rely on compression to throw away the keys that are not needed.
* We force the length of any Clip that is present in the FBX (matched by name) to the frame range in the file. 

> //sample_game/prototype01/main/ProjectFolder/Assets/Scripts/EditorTools/Editor/AnimationImport.cs

### Humanoid Configuration
Humanoid has the option of Leg and Arm stretch (Anti pop/Soft IK), which will slight lengthen the bones as they reach full extension, which can be especially useful during retargeting to avoid IK pops. This can however have the side effect, that arm and legs may never fully straighten the way they do in the source animation.

With our choice of animation style and having almost purely character specific animations, turning this feature off made the most sense. 

![](images/HumanoidSettings.png)
