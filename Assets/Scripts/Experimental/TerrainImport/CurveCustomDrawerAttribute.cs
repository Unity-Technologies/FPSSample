using UnityEngine;


public class CurveCustomDrawerAttribute : PropertyAttribute
{
    public float minX;
    public float minY;
    public float maxX;
    public float maxY;
    public string color;
 
    public CurveCustomDrawerAttribute(float minX, float minY, float maxX, float maxY, string htmlColor = "#00FF00FF")
    {
        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;
        color = htmlColor;
    }
}
