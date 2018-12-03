/* 
This is a VERY quick and dirty, but useful script for replacing the terrain splatmap with an imported one from World Machine.

This script modifies a number of other scripts floating around for this purpose; Do what you want with it!

Import the new Splat map as an asset; Then change the type of it to Advanced, Readable/Writeable, Nearest power of two, and Format Override for RGBA32.

Two things of note:

 * Note that you will need to first create an original splatmap before being able to replace it. To do this, save your world and setup your textures, etc first. 
 * Note that World Machine by default considers the texture origin to be the bottom-left, not the top-left.

4/10/13 Original Creation by Stephen Schmitt
2/21/18 Updated for Unity 2017

*/ 

import System.IO;

class ReplaceSplatmap extends ScriptableWizard
{
var Splatmap: Texture2D;
var New : Texture2D;
var FlipVertical : boolean;

 function OnWizardUpdate(){
        helpString = "Replace the existing splatmap of your terrain with a new one.\nDrag the embedded splatmap texture of your terrain to the 'Splatmap box'.\nThen drag the replacement splatmap texture to the 'New' box\nThen hit 'Replace'.";
        isValid = (Splatmap != null) && (New != null);
    }
	
function OnWizardCreate () {
   
    if (New.format != TextureFormat.RGBA32 && New.format != TextureFormat.ARGB32 && New.format != TextureFormat.RGB24) {
		EditorUtility.DisplayDialog("Wrong format", "Splatmap must be set to the RGBA 32 bit format in the Texture Inspector.\nMake sure the type is Advanced and set the format!", "Cancel"); 
		return;
	}
	
	var w = New.width;
	if (Mathf.ClosestPowerOfTwo(w) != w) {
		EditorUtility.DisplayDialog("Wrong size", "Splatmap width and height must be a power of two!", "Cancel"); 
		return;	
	}  

    try {
    	var pixels = New.GetPixels();	
		if (FlipVertical) {
			var h = w; // always square in unity
			for (var y = 0; y < h/2; y++) {
				var otherY = h - y - 1;	
				for (var x  = 0; x < w; x++) {
					var swapval = pixels[y*w + x];					
					pixels[y*w + x] = pixels[otherY*w + x];
					pixels[otherY*w + x] = swapval;
				}		
			}
		}
		Splatmap.Resize (New.width, New.height, New.format, true);
		Splatmap.SetPixels (pixels);
		Splatmap.Apply();
    }
    catch (err) {
		EditorUtility.DisplayDialog("Not readable", "The 'New' splatmap must be readable. Make sure the type is Advanced and enable read/write and try again!", "Cancel"); 
		return;
	}			
}

@MenuItem("Terrain/Replace Splatmap...")
static function Replace (){
    ScriptableWizard.DisplayWizard(
        "ReplaceSplatmap", ReplaceSplatmap, "Replace");
}
}