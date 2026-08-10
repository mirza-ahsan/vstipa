using UnityEditor;
using UnityEngine;

public static class AvatarMeshGenerator
{
    [MenuItem("V-STIPA/Generate 3D Avatars")]
    public static void GenerateAvatars()
    {
        CreateAvatar("WarmAvatar", new Color(0.18f, 0.52f, 0.65f), new Color(0.92f, 0.78f, 0.68f));
        CreateAvatar("SternAvatar", new Color(0.20f, 0.22f, 0.28f), new Color(0.88f, 0.74f, 0.64f));
        CreateAvatar("NeutralAvatar", new Color(0.15f, 0.28f, 0.45f), new Color(0.90f, 0.76f, 0.66f));
        Debug.Log("[AvatarMeshGenerator] 3D Humanoid Avatars with Lip-Sync blendshapes generated successfully.");
    }

    public static GameObject CreateAvatar(string avatarName, Color suitColor, Color skinColor)
    {
        GameObject rootGo = new GameObject(avatarName);

        // 1. Create Rig Bones Hierarchy
        GameObject hips = new GameObject("Hips");
        hips.transform.SetParent(rootGo.transform, false);
        hips.transform.localPosition = new Vector3(0, 0.9f, 0);

        GameObject spine = new GameObject("Spine");
        spine.transform.SetParent(hips.transform, false);
        spine.transform.localPosition = new Vector3(0, 0.3f, 0);

        GameObject chest = new GameObject("Chest");
        chest.transform.SetParent(spine.transform, false);
        chest.transform.localPosition = new Vector3(0, 0.25f, 0);

        GameObject neck = new GameObject("Neck");
        neck.transform.SetParent(chest.transform, false);
        neck.transform.localPosition = new Vector3(0, 0.15f, 0);

        GameObject head = new GameObject("Head");
        head.transform.SetParent(neck.transform, false);
        head.transform.localPosition = new Vector3(0, 0.12f, 0);

        GameObject leftArm = new GameObject("LeftArm");
        leftArm.transform.SetParent(chest.transform, false);
        leftArm.transform.localPosition = new Vector3(-0.25f, 0.1f, 0);

        GameObject rightArm = new GameObject("RightArm");
        rightArm.transform.SetParent(chest.transform, false);
        rightArm.transform.localPosition = new Vector3(0.25f, 0.1f, 0);

        // 2. Create Torso Mesh
        GameObject torsoGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        torsoGo.name = "TorsoMesh";
        torsoGo.transform.SetParent(chest.transform, false);
        torsoGo.transform.localPosition = new Vector3(0, 0, 0);
        torsoGo.transform.localScale = new Vector3(0.38f, 0.28f, 0.25f);
        Material suitMat = new Material(Shader.Find("Standard"));
        suitMat.color = suitColor;
        torsoGo.GetComponent<Renderer>().sharedMaterial = suitMat;

        // 3. Create Skinned Head Mesh with Lip-Sync Blendshapes
        GameObject headMeshGo = new GameObject("HeadMesh");
        headMeshGo.transform.SetParent(head.transform, false);
        SkinnedMeshRenderer headRenderer = headMeshGo.AddComponent<SkinnedMeshRenderer>();

        Material skinMat = new Material(Shader.Find("Standard"));
        skinMat.color = skinColor;
        headRenderer.sharedMaterial = skinMat;

        Mesh headMesh = BuildHeadMeshWithBlendshapes();
        headRenderer.sharedMesh = headMesh;

        // Bind Bones
        headRenderer.bones = new Transform[] { head.transform, neck.transform };
        headRenderer.rootBone = head.transform;

        // 4. Attach Gesture & LipSync Controllers
        AvatarGestureController gestureCtrl = rootGo.AddComponent<AvatarGestureController>();
        gestureCtrl.headBone = head.transform;
        gestureCtrl.spineBone = spine.transform;
        gestureCtrl.leftArmBone = leftArm.transform;
        gestureCtrl.rightArmBone = rightArm.transform;
        gestureCtrl.CacheOriginalRotations();

        AvatarLipSync lipSync = rootGo.AddComponent<AvatarLipSync>();
        lipSync.headMeshRenderer = headRenderer;

        return rootGo;
    }

    private static Mesh BuildHeadMeshWithBlendshapes()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HumanoidHeadMesh";

        // Head Sphere Geometry (Low-poly 14x14 sphere)
        int lon = 14;
        int lat = 14;
        float radius = 0.14f;

        Vector3[] baseVertices = new Vector3[(lon + 1) * lat + 2];
        Vector2[] uvs = new Vector2[baseVertices.Length];

        baseVertices[0] = Vector3.up * radius;
        for (int i = 0; i < lat; i++)
        {
            float a1 = Mathf.PI * (float)(i + 1) / (lat + 1);
            float sin1 = Mathf.Sin(a1);
            float cos1 = Mathf.Cos(a1);

            for (int j = 0; j <= lon; j++)
            {
                float a2 = 2.0f * Mathf.PI * (float)(j == lon ? 0 : j) / lon;
                float sin2 = Mathf.Sin(a2);
                float cos2 = Mathf.Cos(a2);

                int index = i * (lon + 1) + j + 1;
                baseVertices[index] = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;
            }
        }
        baseVertices[baseVertices.Length - 1] = Vector3.down * radius;

        mesh.vertices = baseVertices;

        // Build Triangles
        int numFaces = baseVertices.Length;
        int[] triangles = new int[numFaces * 6];
        int t = 0;
        for (int i = 0; i < lon; i++)
        {
            triangles[t++] = 0;
            triangles[t++] = i + 2;
            triangles[t++] = i + 1;
        }
        for (int i = 0; i < lat - 1; i++)
        {
            for (int j = 0; j < lon; j++)
            {
                int current = i * (lon + 1) + j + 1;
                int next = current + lon + 1;

                triangles[t++] = current;
                triangles[t++] = current + 1;
                triangles[t++] = next + 1;

                triangles[t++] = current;
                triangles[t++] = next + 1;
                triangles[t++] = next;
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // 5. Build Blendshapes for Lip Sync & Gestures
        // Shape 1: viseme_aa (Open Mouth)
        Vector3[] deltaAa = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.02f && baseVertices[i].z > 0.02f)
            {
                deltaAa[i] = new Vector3(0, -0.05f, 0.02f);
            }
        }
        mesh.AddBlendShapeFrame("viseme_aa", 100.0f, deltaAa, null, null);

        // Shape 2: viseme_O (Round Mouth)
        Vector3[] deltaO = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.02f && baseVertices[i].z > 0.02f)
            {
                deltaO[i] = new Vector3(0, -0.03f, 0.04f);
            }
        }
        mesh.AddBlendShapeFrame("viseme_O", 100.0f, deltaO, null, null);

        // Shape 3: viseme_E (Wide Mouth / Teeth)
        Vector3[] deltaE = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.02f && baseVertices[i].z > 0.02f)
            {
                deltaE[i] = new Vector3(baseVertices[i].x * 0.4f, -0.02f, 0.01f);
            }
        }
        mesh.AddBlendShapeFrame("viseme_E", 100.0f, deltaE, null, null);

        // Shape 4: viseme_U (Pucker Mouth)
        Vector3[] deltaU = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.02f && baseVertices[i].z > 0.02f)
            {
                deltaU[i] = new Vector3(-baseVertices[i].x * 0.3f, -0.02f, 0.05f);
            }
        }
        mesh.AddBlendShapeFrame("viseme_U", 100.0f, deltaU, null, null);

        // Shape 5: mouthSmile (Friendly Smile)
        Vector3[] deltaSmile = new Vector3[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.01f && baseVertices[i].z > 0.03f)
            {
                deltaSmile[i] = new Vector3(0, 0.02f, 0);
            }
        }
        mesh.AddBlendShapeFrame("mouthSmile", 100.0f, deltaSmile, null, null);

        return mesh;
    }
}
