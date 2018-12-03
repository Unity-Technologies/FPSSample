using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR

namespace PocketHammer
{
	public class TCGraphicsHelper {

		public static RenderTexture CreateRenderTarget(int size) {
			RenderTexture rtt = new RenderTexture(size, size, 0,RenderTextureFormat.ARGBFloat);
			return rtt;
		}

		public static Texture2D CreateTexture(int size) {
			return new Texture2D(size,size,TextureFormat.RGBAFloat, false);
		}

//		public static void LoadTextureValue(float value, ref Texture2D texture) {
//			int size = texture.width;
//			for(int y=0;y<size;y++) {
//				for(int x=0;x<size;x++) {
//					Color color = new Color(value,0.0f,0.0f,1.0f);
//					texture.SetPixel(x,y,color);
//				}
//			}
//			texture.Apply();
//		}

		public static void LoadTextureData(float[,] data, ref Texture2D texture) {
			int size = data.GetUpperBound(0) + 1;

			Color[] colors = new Color[size*size];

			int i = 0;
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					float value = data[x,y];
					colors[i].r = value;
					colors[i].a = 1.0f;
					i++;
				}
			}

			texture.SetPixels(colors);
			texture.Apply();
		}

		public static void LoadTextureData(float[,,] data, int lastIndex, ref Texture2D texture) {
			int size = data.GetUpperBound(0) + 1;

			Color[] colors = new Color[size*size];

			int i = 0;
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					float value = data[x,y,lastIndex];
					colors[i].r = value;
					colors[i].a = 1.0f;
					i++;
				}
			}

			texture.SetPixels(colors);
			texture.Apply();
		}

		public static void ReadDataFromTexture(Texture2D texture, ref float[,] data) 
		{

			
			int size = texture.width;

			// TODO: use Texture2D.GetRawTextureData ??

			Color[] colors = texture.GetPixels();

			int i=0;
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					//Color color = texture.GetPixel(x,y);
					float value = colors[i++].r;
					data[x,y] = value;
				}
			}
		}

		public static void ReadDataFromTexture(Texture2D texture, ref float[,,] data, int lastIndex) {

			int size = texture.width;

			// TODO: use Texture2D.GetRawTextureData ??

			Color[] colors = texture.GetPixels();

			int i=0;
			for(int y=0;y<size;y++) {
				for(int x=0;x<size;x++) {
					//Color color = texture.GetPixel(x,y);
					float value = colors[i++].r;
					data[x,y, lastIndex] = value;
				}
			}
		}

		public static void DrawTexture(Texture2D texture, Material material, Vector2 position,float rotation, Vector2 scale) {

			// Set filter mode
			texture.filterMode = FilterMode.Trilinear;
			texture.Apply(true);       

			GL.PushMatrix();

			// Setup scale and material 
			GL.LoadPixelMatrix(0,1,1,0);

            // Y axis correction
            position.y = 1 - position.y;
            rotation = -rotation;

            Quaternion rot = Quaternion.Euler(new Vector3(0,0,rotation));
			Matrix4x4 mat = Matrix4x4.identity;

            mat = mat * Matrix4x4.Translate(position);
            mat = mat * Matrix4x4.Rotate(rot);
            mat = mat * Matrix4x4.Scale(scale);
            mat = mat * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0));
            //            mat.SetTRS(position,rot,scale);

            GL.MultMatrix (mat);

			// Draw
			Graphics.DrawTexture(new Rect(0,0,1,1),texture, material);    

			GL.PopMatrix();
		}

		public static void ReadRenderTarget(int size, ref Texture2D texture) {
			Rect texR = new Rect(0,0,size,size);
			texture.ReadPixels(texR,0,0,false);
			texture.Apply(true); 
		}
	}
}

#endif