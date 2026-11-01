using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Hearthbound
{
    public static class WorldLook
    {
        static readonly Mesh[] rocks = new Mesh[4];
        static readonly Dictionary<Color32, Material> gels = new Dictionary<Color32, Material>();
        static Mesh flowerMesh;
        static Material metal;

        // Give every face texture coordinates measured in metres, avoiding stretched pillars.
        public static GameObject Surface(GameObject go, string texture, float metresPerTile = 2f)
        {
            go.GetComponent<Renderer>().sharedMaterial = ForestLook.Surface(texture);
            MeshFilter mf = go.GetComponent<MeshFilter>();
            Mesh mesh = Object.Instantiate(mf.sharedMesh);
            mesh.name = "Textured " + go.name;
            Vector3[] vertices = mesh.vertices, normals = mesh.normals;
            Vector2[] uv = new Vector2[vertices.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                Vector3 p = Vector3.Scale(vertices[i], go.transform.localScale);
                Vector3 n = normals[i];
                uv[i] = Mathf.Abs(n.y) > 0.6f ? new Vector2(p.x, p.z) :
                    Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                uv[i] /= metresPerTile;
            }
            mesh.uv = uv; mf.sharedMesh = mesh;
            go.AddComponent<ForestMeshOwner>().OwnedMesh = mesh;
            return go;
        }

        public static GameObject Rock(Transform parent, Vector3 pos, Vector3 scale, int variant)
        {
            variant = (variant & int.MaxValue) % rocks.Length;
            if (rocks[variant] == null) rocks[variant] = MakeRock(variant);
            GameObject go = new GameObject("Pebble"); go.transform.SetParent(parent, false);
            go.transform.localPosition = pos; go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0, variant * 73f, 0);
            go.AddComponent<MeshFilter>().sharedMesh = rocks[variant];
            go.AddComponent<MeshCollider>().sharedMesh = rocks[variant];
            go.AddComponent<MeshRenderer>().sharedMaterial = ForestLook.Surface("Rock");
            return go;
        }

        static Mesh MakeRock(int seed)
        {
            var rng = new System.Random(seed + 702);
            var ring = new Vector3[21];
            for (int r = 0; r < 3; r++) for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 7f + 0.12f * r;
                float radius = (r == 1 ? 0.52f : 0.37f) * (0.82f + (float)rng.NextDouble() * 0.3f);
                ring[r * 7 + i] = new Vector3(Mathf.Cos(a) * radius,
                    (r - 1) * 0.35f + ((float)rng.NextDouble() - 0.5f) * 0.15f, Mathf.Sin(a) * radius);
            }
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int i = 0; i < 7; i++)
            {
                int j = (i + 1) % 7;
                for (int r = 0; r < 2; r++)
                {
                    Triangle(v, uv, t, ring[r*7+i], ring[r*7+j], ring[(r+1)*7+i]);
                    Triangle(v, uv, t, ring[r*7+j], ring[(r+1)*7+j], ring[(r+1)*7+i]);
                }
                Triangle(v, uv, t, ring[14+i], ring[14+j], new Vector3(0.07f,0.43f,-0.05f));
                Triangle(v, uv, t, ring[j], ring[i], new Vector3(0,-0.42f,0));
            }
            Mesh m = new Mesh { name = "Angular rock " + seed };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t,0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        static void Triangle(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b-a,c-a);
            if (Vector3.Dot(normal,(a+b+c)/3f) < 0) { Vector3 swap=b; b=c; c=swap; normal=-normal; }
            int n=v.Count; v.Add(a); v.Add(b); v.Add(c);
            foreach(Vector3 p in new [] {a,b,c})
                uv.Add(Mathf.Abs(normal.y)>Mathf.Max(Mathf.Abs(normal.x),Mathf.Abs(normal.z)) ? new Vector2(p.x,p.z) :
                    Mathf.Abs(normal.x)>Mathf.Abs(normal.z) ? new Vector2(p.z,p.y) : new Vector2(p.x,p.y));
            t.Add(n);t.Add(n+1);t.Add(n+2);
        }

        public static void Flower(Transform parent, Vector3 pos, Color color)
        {
            if (flowerMesh == null) flowerMesh = MakeFlower();
            GameObject go = new GameObject("Flower"); go.transform.SetParent(parent,false); go.transform.localPosition=pos;
            float variation = Mathf.Repeat(pos.x * 17f + pos.z * 31f,1f);
            go.transform.localRotation=Quaternion.Euler(0,variation*360f,0);
            go.transform.localScale=Vector3.one*(0.8f+variation*0.4f);
            go.AddComponent<MeshFilter>().sharedMesh=flowerMesh;
            go.AddComponent<MeshRenderer>().sharedMaterials=new [] {Prim.Mat(color), Prim.Mat(new Color(0.25f,0.48f,0.16f)),Prim.Mat(new Color(1f,0.7f,0.12f))};
        }

        static Mesh MakeFlower()
        {
            var v=new List<Vector3>();var uv=new List<Vector2>();
            var petals=new List<int>();var green=new List<int>();var center=new List<int>();
            // Curved petal fans, visible from above and from the player camera.
            for(int p=0;p<5;p++)
            {
                float a=p*Mathf.PI*2f/5f;
                Vector3 dir=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 side=new Vector3(-dir.z,0,dir.x);
                Vector3 c=new Vector3(0,0.43f,0)+dir*0.13f;
                for(int k=0;k<8;k++)
                {
                    float u=k*Mathf.PI/4f, u2=(k+1)*Mathf.PI/4f;
                    Vector3 e=c+dir*(Mathf.Cos(u)*0.14f)+side*(Mathf.Sin(u)*0.085f)+Vector3.up*(0.025f*Mathf.Cos(u));
                    Vector3 e2=c+dir*(Mathf.Cos(u2)*0.14f)+side*(Mathf.Sin(u2)*0.085f)+Vector3.up*(0.025f*Mathf.Cos(u2));
                    DoubleTriangle(v,uv,petals,c-Vector3.up*0.025f,e,e2);
                }
            }
            for(int k=0;k<8;k++)
            {
                float a=k*Mathf.PI/4f,b=(k+1)*Mathf.PI/4f;
                DoubleTriangle(v,uv,center,new Vector3(0,0.46f,0),new Vector3(Mathf.Cos(a)*0.065f,0.44f,Mathf.Sin(a)*0.065f),new Vector3(Mathf.Cos(b)*0.065f,0.44f,Mathf.Sin(b)*0.065f));
            }
            for(int k=0;k<2;k++)
            {
                Vector3 side=k==0?Vector3.right*0.015f:Vector3.forward*0.015f;
                DoubleTriangle(v,uv,green,-side,side,new Vector3(0,0.44f,0)+side);
                DoubleTriangle(v,uv,green,-side,new Vector3(0,0.44f,0)+side,new Vector3(0,0.44f,0)-side);
            }
            DoubleTriangle(v,uv,green,new Vector3(0,0.18f,0),new Vector3(-0.20f,0.28f,0),new Vector3(-0.09f,0.15f,0.07f));
            DoubleTriangle(v,uv,green,new Vector3(0,0.24f,0),new Vector3(0.2f,0.34f,0.02f),new Vector3(0.1f,0.20f,-0.07f));
            Mesh m=new Mesh {name="Wildflower",subMeshCount=3};m.SetVertices(v);m.SetUVs(0,uv);
            m.SetTriangles(petals,0);m.SetTriangles(green,1);m.SetTriangles(center,2);m.RecalculateNormals();m.RecalculateBounds();return m;
        }
        static void DoubleTriangle(List<Vector3> v,List<Vector2> uv,List<int> t,Vector3 a,Vector3 b,Vector3 c)
        {
            int n=v.Count;v.Add(a);v.Add(b);v.Add(c);uv.Add(Vector2.zero);uv.Add(Vector2.right);uv.Add(Vector2.up);
            t.Add(n);t.Add(n+1);t.Add(n+2);
            v.Add(a);v.Add(c);v.Add(b);uv.Add(Vector2.zero);uv.Add(Vector2.up);uv.Add(Vector2.right);
            t.Add(n+3);t.Add(n+4);t.Add(n+5);
        }

        public static Material Gel(Color color)
        {
            Color32 key=color;
            if(gels.TryGetValue(key,out Material m)&&m!=null)return m;
            color.a=0.76f;
            m=new Material(Prim.Mat(Color.white));m.name="Translucent slime";
            m.color=color;m.SetColor("_BaseColor",color);
            m.SetFloat("_Smoothness",0.92f);m.SetFloat("_Metallic",0f);
            m.SetFloat("_Surface",1f);m.SetFloat("_Blend",1f);
            m.SetFloat("_SrcBlend",(float)BlendMode.One);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);m.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0f);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);
            gels[key]=m;return m;
        }

        static Material Metal()
        {
            if(metal==null)
            {
                metal=new Material(Prim.Mat(new Color(0.16f,0.18f,0.21f)));
                metal.SetFloat("_Metallic",0.65f);metal.SetFloat("_Smoothness",0.5f);
            }
            return metal;
        }
        public static void ChestBody(Transform body)
        {
            Color wood=Color.white;
            Color inside=new Color(0.035f,0.025f,0.018f);
            Surface(Prim.Box(body,new Vector3(0,0.04f,0),new Vector3(1.1f,0.08f,0.7f),wood,false,"ChestBottom"),"ChestWood",1.2f);
            for(int side=-1;side<=1;side+=2)
            {
                Surface(Prim.Box(body,new Vector3(side*0.51f,0.34f,0),new Vector3(0.08f,0.52f,0.7f),wood,false,"ChestSide"),"ChestWood",1.2f);
                Surface(Prim.Box(body,new Vector3(0,0.34f,side*0.31f),new Vector3(0.94f,0.52f,0.08f),wood,false,"ChestEnd"),"ChestWood",1.2f);
                Prim.Box(body,new Vector3(side*0.466f,0.32f,0),new Vector3(0.006f,0.46f,0.54f),inside,false,"InnerSide");
                Prim.Box(body,new Vector3(0,0.32f,side*0.266f),new Vector3(0.93f,0.46f,0.006f),inside,false,"InnerEnd");
            }
            Prim.Box(body,new Vector3(0,0.086f,0),new Vector3(0.94f,0.012f,0.54f),inside,false,"InsideBottom");
            BoxCollider collider=body.gameObject.AddComponent<BoxCollider>();
            collider.center=new Vector3(0,0.3f,0);collider.size=new Vector3(1.1f,0.6f,0.7f);
        }
        public static void ChestBands(Transform body, Transform lid)
        {
            for(int i=-1;i<=1;i+=2)
            {
                // Separate exterior strips leave the chest opening unobstructed.
                for(int side=-1;side<=1;side+=2)
                {
                    var a=Prim.Box(body,new Vector3(i*0.39f,0.30f,side*0.354f),new Vector3(0.09f,0.6f,0.014f),Color.white,false,"ChestBand");
                    a.GetComponent<Renderer>().sharedMaterial=Metal();
                    var b=Prim.Box(lid,new Vector3(i*0.39f,0.12f,0.35f+side*0.375f),new Vector3(0.09f,0.24f,0.014f),Color.white,false,"LidBandEnd");
                    b.GetComponent<Renderer>().sharedMaterial=Metal();
                }
                var top=Prim.Box(lid,new Vector3(i*0.39f,0.245f,0.35f),new Vector3(0.09f,0.014f,0.76f),Color.white,false,"LidBandTop");
                top.GetComponent<Renderer>().sharedMaterial=Metal();
            }
        }
        public static void CrackLight(Transform parent)
        {
            var go=new GameObject("Crack glow");go.transform.SetParent(parent,false);go.transform.localPosition=Vector3.up*0.5f;
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,0.48f,0.12f);
            light.intensity=1.4f;light.range=4.5f;light.shadows=LightShadows.None;
        }
    }
}
