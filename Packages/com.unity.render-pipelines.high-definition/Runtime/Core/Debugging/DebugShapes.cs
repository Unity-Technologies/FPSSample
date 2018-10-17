namespace UnityEngine.Experimental.Rendering
{
    public partial class DebugShapes
    {
        // Singleton
        static DebugShapes s_Instance = null;

        static public DebugShapes instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new DebugShapes();
                }

                return s_Instance;
            }
        }
        
        Mesh m_sphereMesh = null;
        Mesh m_boxMesh = null;
        Mesh m_coneMesh = null;
        Mesh m_pyramidMesh = null;

        // This code has been grabbed from http://wiki.unity3d.com/index.php/ProceduralPrimitives
        void BuildSphere(ref Mesh outputMesh, float radius, uint longSubdiv, uint latSubdiv)
        {
            // Make sure it is empty before pushing anything to it
            outputMesh.Clear();

            // Build the vertices array
            Vector3[] vertices = new Vector3[(longSubdiv + 1) * latSubdiv + 2];
            float _pi = Mathf.PI;
            float _2pi = _pi * 2f;

            vertices[0] = Vector3.up * radius;
            for (int lat = 0; lat < latSubdiv; lat++)
            {
                float a1 = _pi * (float)(lat + 1) / (latSubdiv + 1);
                float sin1 = Mathf.Sin(a1);
                float cos1 = Mathf.Cos(a1);

                for (int lon = 0; lon <= longSubdiv; lon++)
                {
                    float a2 = _2pi * (float)(lon == longSubdiv ? 0 : lon) / longSubdiv;
                    float sin2 = Mathf.Sin(a2);
                    float cos2 = Mathf.Cos(a2);

                    vertices[lon + lat * (longSubdiv + 1) + 1] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
                }
            }
            vertices[vertices.Length - 1] = Vector3.up * -radius;

            // Build the normals array
            Vector3[] normals = new Vector3[vertices.Length];
            for (int n = 0; n < vertices.Length; n++)
            {
                normals[n] = vertices[n].normalized;
            }

            // Build the UV array
            Vector2[] uvs = new Vector2[vertices.Length];
            uvs[0] = Vector2.up;
            uvs[uvs.Length - 1] = Vector2.zero;
            for (int lat = 0; lat < latSubdiv; lat++)
            {
                for (int lon = 0; lon <= longSubdiv; lon++)
                {
                    uvs[lon + lat * (longSubdiv + 1) + 1] = new Vector2((float)lon / longSubdiv, 1f - (float)(lat + 1) / (latSubdiv + 1));
                }
            }

            // Build the index array
            int nbFaces = vertices.Length;
            int nbTriangles = nbFaces * 2;
            int nbIndexes = nbTriangles * 3;
            int[] triangles = new int[nbIndexes];

            // Top Cap
            int i = 0;
            for (int lon = 0; lon < longSubdiv; lon++)
            {
                triangles[i++] = lon + 2;
                triangles[i++] = lon + 1;
                triangles[i++] = 0;
            }

            //Middle
            for (uint lat = 0; lat < latSubdiv - 1; lat++)
            {
                for (uint lon = 0; lon < longSubdiv; lon++)
                {
                    uint current = lon + lat * (longSubdiv + 1) + 1;
                    uint next = current + longSubdiv + 1;

                    triangles[i++] = (int)current;
                    triangles[i++] = (int)current + 1;
                    triangles[i++] = (int)next + 1;

                    triangles[i++] = (int)current;
                    triangles[i++] = (int)next + 1;
                    triangles[i++] = (int)next;
                }
            }

            // Bottom Cap
            for (int lon = 0; lon < longSubdiv; lon++)
            {
                triangles[i++] = vertices.Length - 1;
                triangles[i++] = vertices.Length - (lon + 2) - 1;
                triangles[i++] = vertices.Length - (lon + 1) - 1;
            }

            // Assign them to 
            outputMesh.vertices = vertices;
            outputMesh.normals = normals;
            outputMesh.uv = uvs;
            outputMesh.triangles = triangles;

            outputMesh.RecalculateBounds();
        }

        void BuildBox(ref Mesh outputMesh, float length, float width, float height)
        {
            outputMesh.Clear();

            Vector3 p0 = new Vector3(-length * .5f, -width * .5f, height * .5f);
            Vector3 p1 = new Vector3(length * .5f, -width * .5f, height * .5f);
            Vector3 p2 = new Vector3(length * .5f, -width * .5f, -height * .5f);
            Vector3 p3 = new Vector3(-length * .5f, -width * .5f, -height * .5f);

            Vector3 p4 = new Vector3(-length * .5f, width * .5f, height * .5f);
            Vector3 p5 = new Vector3(length * .5f, width * .5f, height * .5f);
            Vector3 p6 = new Vector3(length * .5f, width * .5f, -height * .5f);
            Vector3 p7 = new Vector3(-length * .5f, width * .5f, -height * .5f);

            Vector3[] vertices = new Vector3[]
            {
	            // Bottom
	            p0, p1, p2, p3,
	            // Left
	            p7, p4, p0, p3,
	            // Front
	            p4, p5, p1, p0,
	            // Back
	            p6, p7, p3, p2,
	            // Right
	            p5, p6, p2, p1,
	            // Top
	            p7, p6, p5, p4
            };

            Vector3 up = Vector3.up;
            Vector3 down = Vector3.down;
            Vector3 front = Vector3.forward;
            Vector3 back = Vector3.back;
            Vector3 left = Vector3.left;
            Vector3 right = Vector3.right;

            Vector3[] normales = new Vector3[]
            {
	            // Bottom
	            down, down, down, down,
	            // Left
	            left, left, left, left,
	            // Front
	            front, front, front, front,
	            // Back
	            back, back, back, back,
	            // Right
	            right, right, right, right,
	            // Top
	            up, up, up, up
            };

            Vector2 _00 = new Vector2(0f, 0f);
            Vector2 _10 = new Vector2(1f, 0f);
            Vector2 _01 = new Vector2(0f, 1f);
            Vector2 _11 = new Vector2(1f, 1f);

            Vector2[] uvs = new Vector2[]
            {
	            // Bottom
	            _11, _01, _00, _10,
	            // Left
	            _11, _01, _00, _10,
	            // Front
	            _11, _01, _00, _10,
	            // Back
	            _11, _01, _00, _10,
	            // Right
	            _11, _01, _00, _10,
	            // Top
	            _11, _01, _00, _10,
            };

            int[] triangles = new int[]
            {
	            // Bottom
	            3, 1, 0,
                3, 2, 1,			
	            // Left
	            3 + 4 * 1, 1 + 4 * 1, 0 + 4 * 1,
                3 + 4 * 1, 2 + 4 * 1, 1 + 4 * 1,
	            // Front
	            3 + 4 * 2, 1 + 4 * 2, 0 + 4 * 2,
                3 + 4 * 2, 2 + 4 * 2, 1 + 4 * 2,
	            // Back
	            3 + 4 * 3, 1 + 4 * 3, 0 + 4 * 3,
                3 + 4 * 3, 2 + 4 * 3, 1 + 4 * 3,
	            // Right
	            3 + 4 * 4, 1 + 4 * 4, 0 + 4 * 4,
                3 + 4 * 4, 2 + 4 * 4, 1 + 4 * 4,
	            // Top
	            3 + 4 * 5, 1 + 4 * 5, 0 + 4 * 5,
                3 + 4 * 5, 2 + 4 * 5, 1 + 4 * 5,
            };

            outputMesh.vertices = vertices;
            outputMesh.normals = normales;
            outputMesh.uv = uvs;
            outputMesh.triangles = triangles;

            outputMesh.RecalculateBounds();
        }

        void BuildCone(ref Mesh outputMesh, float height, float topRadius, float bottomRadius, int nbSides)
        {
            outputMesh.Clear();

            int nbVerticesCap = nbSides + 1;

            // bottom + top + sides
            Vector3[] vertices = new Vector3[nbVerticesCap + nbVerticesCap + nbSides * 2 + 2];
            int vert = 0;
            float _2pi = Mathf.PI * 2f;

            // Bottom cap
            vertices[vert++] = new Vector3(0f, 0f, 0f);
            while (vert <= nbSides)
            {
                float rad = (float)vert / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Sin(rad) * bottomRadius, Mathf.Cos(rad) * bottomRadius, 0f);
                vert++;
            }

            // Top cap
            vertices[vert++] = new Vector3(0f, 0f , height);
            while (vert <= nbSides * 2 + 1)
            {
                float rad = (float)(vert - nbSides - 1) / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Sin(rad) * topRadius, Mathf.Cos(rad) * topRadius, height);
                vert++;
            }

            // Sides
            int v = 0;
            while (vert <= vertices.Length - 4)
            {
                float rad = (float)v / nbSides * _2pi;
                vertices[vert] = new Vector3(Mathf.Sin(rad) * topRadius, Mathf.Cos(rad) * topRadius, height);
                vertices[vert + 1] = new Vector3(Mathf.Sin(rad) * bottomRadius, Mathf.Cos(rad) * bottomRadius, 0);
                vert += 2;
                v++;
            }
            vertices[vert] = vertices[nbSides * 2 + 2];
            vertices[vert + 1] = vertices[nbSides * 2 + 3];

            // bottom + top + sides
            Vector3[] normales = new Vector3[vertices.Length];
            vert = 0;

            // Bottom cap
            while (vert <= nbSides)
            {
                normales[vert++] = new Vector3(0, 0, -1);
            }

            // Top cap
            while (vert <= nbSides * 2 + 1)
            {
                normales[vert++] = new Vector3(0, 0, 1);
            }

            // Sides
            v = 0;
            while (vert <= vertices.Length - 4)
            {
                float rad = (float)v / nbSides * _2pi;
                float cos = Mathf.Cos(rad);
                float sin = Mathf.Sin(rad);

                normales[vert] = new Vector3(sin, cos, 0f);
                normales[vert + 1] = normales[vert];

                vert += 2;
                v++;
            }
            normales[vert] = normales[nbSides * 2 + 2];
            normales[vert + 1] = normales[nbSides * 2 + 3];

            Vector2[] uvs = new Vector2[vertices.Length];

            // Bottom cap
            int u = 0;
            uvs[u++] = new Vector2(0.5f, 0.5f);
            while (u <= nbSides)
            {
                float rad = (float)u / nbSides * _2pi;
                uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
                u++;
            }

            // Top cap
            uvs[u++] = new Vector2(0.5f, 0.5f);
            while (u <= nbSides * 2 + 1)
            {
                float rad = (float)u / nbSides * _2pi;
                uvs[u] = new Vector2(Mathf.Cos(rad) * .5f + .5f, Mathf.Sin(rad) * .5f + .5f);
                u++;
            }

            // Sides
            int u_sides = 0;
            while (u <= uvs.Length - 4)
            {
                float t = (float)u_sides / nbSides;
                uvs[u] = new Vector3(t, 1f);
                uvs[u + 1] = new Vector3(t, 0f);
                u += 2;
                u_sides++;
            }
            uvs[u] = new Vector2(1f, 1f);
            uvs[u + 1] = new Vector2(1f, 0f);

            int nbTriangles = nbSides + nbSides + nbSides * 2;
            int[] triangles = new int[nbTriangles * 3 + 3];

            // Bottom cap
            int tri = 0;
            int i = 0;
            while (tri < nbSides - 1)
            {
                triangles[i] = 0;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = tri + 2;
                tri++;
                i += 3;
            }
            triangles[i] = 0;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = 1;
            tri++;
            i += 3;

            // Top cap
            //tri++;
            while (tri < nbSides * 2)
            {
                triangles[i] = tri + 2;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = nbVerticesCap;
                tri++;
                i += 3;
            }

            triangles[i] = nbVerticesCap + 1;
            triangles[i + 1] = tri + 1;
            triangles[i + 2] = nbVerticesCap;
            tri++;
            i += 3;
            tri++;

            // Sides
            while (tri <= nbTriangles)
            {
                triangles[i] = tri + 2;
                triangles[i + 1] = tri + 1;
                triangles[i + 2] = tri + 0;
                tri++;
                i += 3;

                triangles[i] = tri + 1;
                triangles[i + 1] = tri + 2;
                triangles[i + 2] = tri + 0;
                tri++;
                i += 3;
            }

            outputMesh.vertices = vertices;
            outputMesh.normals = normales;
            outputMesh.uv = uvs;
            outputMesh.triangles = triangles;

            outputMesh.RecalculateBounds();
        }

        void BuildPyramid(ref Mesh outputMesh, float width, float height, float depth)
        {
            outputMesh.Clear();

            // Allocate the buffer
            Vector3[] vertices = new Vector3[16];

            // Top Face
            vertices[0] = new Vector3(0f, 0f, 0f);
            vertices[1] = new Vector3(-width/2.0f, height / 2.0f, depth);
            vertices[2] = new Vector3( width / 2.0f, height / 2.0f, depth);

            // Left Face
            vertices[3] = new Vector3(0f, 0f, 0f);
            vertices[4] = new Vector3(width / 2.0f, height / 2.0f, depth);
            vertices[5] = new Vector3(width / 2.0f, -height / 2.0f, depth);

            // Bottom Face
            vertices[6] = new Vector3(0f, 0f, 0f);
            vertices[7] = new Vector3(width / 2.0f, -height / 2.0f, depth);
            vertices[8] = new Vector3(-width / 2.0f, -height / 2.0f, depth);

            // Right Face
            vertices[9] = new Vector3(0f, 0f, 0f);
            vertices[10] = new Vector3(-width / 2.0f, -height / 2.0f, depth);
            vertices[11] = new Vector3(-width / 2.0f, height / 2.0f, depth);

            // Cap
            vertices[12] = new Vector3(-width / 2.0f, height / 2.0f, depth);
            vertices[13] = new Vector3(-width / 2.0f, -height / 2.0f, depth);
            vertices[14] = new Vector3(width / 2.0f, -height / 2.0f, depth);
            vertices[15] = new Vector3(width / 2.0f, height / 2.0f, depth);

            // TODO: support the uv/normals
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];

            // The indexes for the side part is simple
            int[] triangles = new int[18];
            for(int idx = 0; idx < 12; ++idx)
            {
                triangles[idx] = idx;
            }

            // Cap indexes
            triangles[12] = 12;
            triangles[13] = 13;
            triangles[14] = 14;
            triangles[15] = 12;
            triangles[16] = 14;
            triangles[17] = 15;

            outputMesh.vertices = vertices;
            outputMesh.normals = normals;
            outputMesh.uv = uvs;
            outputMesh.triangles = triangles;

            outputMesh.RecalculateBounds();
        }

        void BuildShapes()
        {
            m_sphereMesh = new Mesh();
            BuildSphere(ref m_sphereMesh, 1.0f, 24, 16);

            m_boxMesh = new Mesh();
            BuildBox(ref m_boxMesh, 1.0f, 1.0f, 1.0f);

            m_coneMesh = new Mesh();
            BuildCone(ref m_coneMesh, 1.0f, 1.0f, 0.0f, 16);

            m_pyramidMesh = new Mesh();
            BuildPyramid(ref m_pyramidMesh, 1.0f, 1.0f, 1.0f);
        }

        public void CheckResources()
        {
            if (m_sphereMesh == null || m_boxMesh == null || m_coneMesh == null || m_pyramidMesh == null)
            {
                BuildShapes();
            }
        }

        public Mesh RequestSphereMesh()
        {
            CheckResources();
            return m_sphereMesh;
        }

        public Mesh RequestBoxMesh()
        {
            CheckResources();
            return m_boxMesh;
        }

        public Mesh RequestConeMesh()
        {
            CheckResources();
            return m_coneMesh;
        }

        public Mesh RequestPyramidMesh()
        {
            CheckResources();
            return m_pyramidMesh;
        }
    }
}
