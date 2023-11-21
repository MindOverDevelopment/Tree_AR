using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Broccoli.Utils
{
    class AmbientOcclusionBaker
    {
        public static void BakeAO (
            MeshCollider collider, 
            ref Color[] colors, 
            Vector3[] verts, 
            Vector3[] normals, 
            int[] branchTri, 
            int[] leafTri, 
            GameObject obj, 
            int samples, 
            float distance, 
            float strength = 1f)
        {
            if (collider == null) return;
                
            if (colors.Length == 0) {
                colors = new Color[verts.Length];
            }

            Mesh obstacle = new Mesh(); //Tree mesh used for collision detection

            int n = branchTri.Length;
            int m = leafTri.Length;
            int[] triangles = new int[n + 2 * m]; //counting twice the leafs in order to make the collider double sided
            int k = triangles.Length;
            for (int i=0; i<k; i += 3)
            {
                if(i < n)
                {
                    triangles[i] = branchTri[i];
                    triangles[i+1] = branchTri[i+1];
                    triangles[i+2] = branchTri[i+2];
                }

                else if (i >= n && i < n+m)
                {
                    triangles[i] = leafTri[i - n];
                    triangles[i + 1] = leafTri[i - n + 1];
                    triangles[i + 2] = leafTri[i - n + 2];
                }

                else //inversing triangles to have normals pointing backawards
                {
                    triangles[i] = leafTri[i - (n + m)];
                    triangles[i + 1] = leafTri[i - (n + m) + 2];
                    triangles[i + 2] = leafTri[i - (n + m) + 1];
                }
            }


            obstacle.vertices = verts;
            obstacle.normals = normals;
            obstacle.triangles = triangles;
            obstacle.RecalculateBounds();
            collider.sharedMesh = obstacle;
    
            Transform transform = obj.transform;


            n = verts.Length;
            for(int i=0; i<n; i++)
            {
                Vector3 pos = transform.TransformPoint(verts[i]);
                float ao = 0;
                Vector3 nrm = transform.TransformVector(normals[i]);
                for (int sample=0; sample<samples; sample++)
                {
                    Vector3 dir = Random.onUnitSphere + nrm;
                    RaycastHit hit;
                    if(Physics.Linecast(pos + nrm*.1f, pos + dir.normalized * distance, out hit))
                    {
                        ao += 1 - Mathf.Pow(hit.distance / distance, 10f);
                    }
                }
                ao = 1 - ao/samples*strength;
                colors[i].r = ao;
                colors[i].g = ao;
                colors[i].b = ao;
                colors[i].a = 1;
            }
        }

        public static void BakeAO (
            MeshCollider collider, 
            ref Color[] colors, 
            Vector3[] verts, 
            Vector3[] normals, 
            int[] tris, 
            int samples,
            float strength = 1f)
        {
            if (collider == null) return;
                
            if (colors.Length == 0) {
                colors = new Color[verts.Length];
            }

            Mesh obstacle = new Mesh(); //Tree mesh used for collision detection

            int m = tris.Length;
            int[] triangles = new int[2 * m]; //counting twice the leafs in order to make the collider double sided
            int k = triangles.Length;
            for (int i=0; i<k; i += 3)
            {
                if (i < m)
                {
                    triangles[i] = tris[i];
                    triangles[i + 1] = tris[i + 1];
                    triangles[i + 2] = tris[i + 2];
                }

                else //inversing triangles to have normals pointing backawards
                {
                    triangles[i] = tris[i - m];
                    triangles[i + 1] = tris[i - m + 2];
                    triangles[i + 2] = tris[i - m + 1];
                }
            }


            obstacle.vertices = verts;
            obstacle.normals = normals;
            obstacle.triangles = triangles;
            obstacle.RecalculateBounds();
            float distance = obstacle.bounds.size.magnitude * 0.25f;
            float maxY = obstacle.bounds.max.y;
            float maxYThr = 0.5f * maxY;
            collider.sharedMesh = obstacle;
    
            //Transform transform = obj.transform;
            int n = verts.Length;
            for(int i=0; i<n; i++)
            {
                Vector3 pos = verts[i];
                float ao = 0;
                
                if (pos.y > -0.001f) {
                    Vector3 nrm = normals[i];
                    nrm = Vector3.up;
                    for (int sample=0; sample<samples; sample++)
                    {
                        Vector3 dir = Random.onUnitSphere + nrm;
                        RaycastHit hit;
                        if(Physics.Linecast(pos + nrm * 0.01f, pos + dir.normalized * distance, out hit)) {
                            ao += 1 - Mathf.Pow(hit.distance / distance, 10f);
                        }
                    }
                    ao = 1 - ao/samples*strength;
                    if (ao > 1f) {
                        Debug.Log ("AO > 1");
                    }
                    ao =  Mathf.Clamp01 (ao);
                    if (pos.y > maxYThr) {
                        ao = 1f - ao;
                        float aoFactor = Mathf.InverseLerp (maxY, maxYThr, pos.y);
                        // easeInCubic
                        //aoFactor = aoFactor * aoFactor * aoFactor;
                        // easeOutCubic
                        //aoFactor = 1f - Mathf.Pow (1f - aoFactor, 3);
                        ao *= aoFactor;
                        ao = 1f - ao;
                    }
                } else {
                    ao = 0f;
                }
                
                colors[i].r = ao;
                colors[i].g = ao;
                colors[i].b = ao;
                colors[i].a = 1;
            }
        }
    }
}