// Performs fading at the edge of the screen. 
float EdgeOfScreenFade(float2 coordNDC, float fadeRcpLength)
{
    float2 coordCS = coordNDC * 2 - 1;
    float2 t = Remap10(abs(coordCS), fadeRcpLength, fadeRcpLength);
    return Smoothstep01(t.x) * Smoothstep01(t.y);
}
