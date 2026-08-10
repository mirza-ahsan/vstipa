using UnityEditor;
using UnityEngine;

public static class AvatarMeshGenerator
{
    [MenuItem("V-STIPA/Generate 3D Avatars")]
    public static void GenerateAvatars()
    {
        CreateAvatar("WarmAvatar", new Color(0.18f, 0.52f, 0.65f), new Color(0.92f, 0.78f, 0.68f), new Color(0.15f, 0.12f, 0.10f));
        CreateAvatar("SternAvatar", new Color(0.20f, 0.22f, 0.28f), new Color(0.88f, 0.74f, 0.64f), new Color(0.25f, 0.20f, 0.15f));
        CreateAvatar("NeutralAvatar", new Color(0.15f, 0.28f, 0.45f), new Color(0.90f, 0.76f, 0.66f), new Color(0.30f, 0.22f, 0.18f));
        Debug.Log("[AvatarMeshGenerator] Detailed 3D Humanoid Avatars generated successfully.");
    }

    public static GameObject CreateAvatar(string avatarName, Color suitColor, Color skinColor, Color hairColor)
    {
        GameObject rootGo = new GameObject(avatarName);

        // Materials
        Material suitMat = new Material(Shader.Find("Standard"));
        suitMat.color = suitColor;

        Material shirtMat = new Material(Shader.Find("Standard"));
        shirtMat.color = Color.white;

        Material skinMat = new Material(Shader.Find("Standard"));
        skinMat.color = skinColor;

        Material hairMat = new Material(Shader.Find("Standard"));
        hairMat.color = hairColor;

        Material eyeMat = new Material(Shader.Find("Standard"));
        eyeMat.color = new Color(0.1f, 0.1f, 0.1f);

        Material shoeMat = new Material(Shader.Find("Standard"));
        shoeMat.color = new Color(0.08f, 0.08f, 0.08f);

        // 1. Bones Hierarchy (Humanoid 1.75m Skeleton)
        GameObject hips = new GameObject("Hips");
        hips.transform.SetParent(rootGo.transform, false);
        hips.transform.localPosition = new Vector3(0, 0.90f, 0);

        GameObject spine = new GameObject("Spine");
        spine.transform.SetParent(hips.transform, false);
        spine.transform.localPosition = new Vector3(0, 0.30f, 0);

        GameObject chest = new GameObject("Chest");
        chest.transform.SetParent(spine.transform, false);
        chest.transform.localPosition = new Vector3(0, 0.25f, 0);

        GameObject neck = new GameObject("Neck");
        neck.transform.SetParent(chest.transform, false);
        neck.transform.localPosition = new Vector3(0, 0.18f, 0);

        GameObject head = new GameObject("Head");
        head.transform.SetParent(neck.transform, false);
        head.transform.localPosition = new Vector3(0, 0.14f, 0);

        // Left Arm Hierarchy
        GameObject leftShoulder = new GameObject("LeftShoulder");
        leftShoulder.transform.SetParent(chest.transform, false);
        leftShoulder.transform.localPosition = new Vector3(-0.20f, 0.12f, 0);

        GameObject leftArm = new GameObject("LeftArm");
        leftArm.transform.SetParent(leftShoulder.transform, false);
        leftArm.transform.localPosition = new Vector3(-0.12f, 0, 0);

        GameObject leftForeArm = new GameObject("LeftForeArm");
        leftForeArm.transform.SetParent(leftArm.transform, false);
        leftForeArm.transform.localPosition = new Vector3(0, -0.28f, 0);

        // Right Arm Hierarchy
        GameObject rightShoulder = new GameObject("RightShoulder");
        rightShoulder.transform.SetParent(chest.transform, false);
        rightShoulder.transform.localPosition = new Vector3(0.20f, 0.12f, 0);

        GameObject rightArm = new GameObject("RightArm");
        rightArm.transform.SetParent(rightShoulder.transform, false);
        rightArm.transform.localPosition = new Vector3(0.12f, 0, 0);

        GameObject rightForeArm = new GameObject("RightForeArm");
        rightForeArm.transform.SetParent(rightArm.transform, false);
        rightForeArm.transform.localPosition = new Vector3(0, -0.28f, 0);

        // Legs Hierarchy
        GameObject leftUpLeg = new GameObject("LeftUpLeg");
        leftUpLeg.transform.SetParent(hips.transform, false);
        leftUpLeg.transform.localPosition = new Vector3(-0.12f, 0, 0);

        GameObject leftLeg = new GameObject("LeftLeg");
        leftLeg.transform.SetParent(leftUpLeg.transform, false);
        leftLeg.transform.localPosition = new Vector3(0, -0.42f, 0);

        GameObject rightUpLeg = new GameObject("RightUpLeg");
        rightUpLeg.transform.SetParent(hips.transform, false);
        rightUpLeg.transform.localPosition = new Vector3(0.12f, 0, 0);

        GameObject rightLeg = new GameObject("RightLeg");
        rightLeg.transform.SetParent(rightUpLeg.transform, false);
        rightLeg.transform.localPosition = new Vector3(0, -0.42f, 0);

        // 2. Torso Geometry (Suit Jacket & Shirt)
        GameObject jacketGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jacketGo.name = "Jacket";
        jacketGo.transform.SetParent(chest.transform, false);
        jacketGo.transform.localPosition = new Vector3(0, -0.05f, 0);
        jacketGo.transform.localScale = new Vector3(0.44f, 0.38f, 0.26f);
        jacketGo.GetComponent<Renderer>().sharedMaterial = suitMat;
        Object.DestroyImmediate(jacketGo.GetComponent<Collider>());

        GameObject shirtGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shirtGo.name = "ShirtCollar";
        shirtGo.transform.SetParent(neck.transform, false);
        shirtGo.transform.localPosition = new Vector3(0, -0.08f, 0.04f);
        shirtGo.transform.localScale = new Vector3(0.18f, 0.12f, 0.16f);
        shirtGo.GetComponent<Renderer>().sharedMaterial = shirtMat;
        Object.DestroyImmediate(shirtGo.GetComponent<Collider>());

        // 3. Neck & Head Mesh
        GameObject neckMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neckMesh.name = "NeckMesh";
        neckMesh.transform.SetParent(neck.transform, false);
        neckMesh.transform.localPosition = new Vector3(0, -0.02f, 0);
        neckMesh.transform.localScale = new Vector3(0.14f, 0.08f, 0.14f);
        neckMesh.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(neckMesh.GetComponent<Collider>());

        // Head Skinned Mesh Renderer with Facial Blendshapes
        GameObject headMeshGo = new GameObject("HeadMesh");
        headMeshGo.transform.SetParent(head.transform, false);
        SkinnedMeshRenderer headRenderer = headMeshGo.AddComponent<SkinnedMeshRenderer>();
        headRenderer.sharedMaterial = skinMat;

        Mesh headMesh = BuildDetailedHeadMesh();
        headRenderer.sharedMesh = headMesh;
        headRenderer.bones = new Transform[] { head.transform, neck.transform };
        headRenderer.rootBone = head.transform;

        // Eyes & Hair Features
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "LeftEye";
        leftEye.transform.SetParent(head.transform, false);
        leftEye.transform.localPosition = new Vector3(-0.045f, 0.03f, 0.125f);
        leftEye.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        leftEye.GetComponent<Renderer>().sharedMaterial = eyeMat;
        Object.DestroyImmediate(leftEye.GetComponent<Collider>());

        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "RightEye";
        rightEye.transform.SetParent(head.transform, false);
        rightEye.transform.localPosition = new Vector3(0.045f, 0.03f, 0.125f);
        rightEye.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
        rightEye.GetComponent<Renderer>().sharedMaterial = eyeMat;
        Object.DestroyImmediate(rightEye.GetComponent<Collider>());

        GameObject hairGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hairGo.name = "Hair";
        hairGo.transform.SetParent(head.transform, false);
        hairGo.transform.localPosition = new Vector3(0, 0.07f, -0.02f);
        hairGo.transform.localScale = new Vector3(0.28f, 0.16f, 0.30f);
        hairGo.GetComponent<Renderer>().sharedMaterial = hairMat;
        Object.DestroyImmediate(hairGo.GetComponent<Collider>());

        // 4. Arms Geometry
        CreateLimb(leftArm.transform, "LeftUpperArmMesh", new Vector3(0, -0.14f, 0), new Vector3(0.12f, 0.28f, 0.12f), suitMat);
        CreateLimb(leftForeArm.transform, "LeftLowerArmMesh", new Vector3(0, -0.14f, 0), new Vector3(0.10f, 0.26f, 0.10f), suitMat);
        CreateLimb(leftForeArm.transform, "LeftHandMesh", new Vector3(0, -0.30f, 0), new Vector3(0.09f, 0.12f, 0.04f), skinMat);

        CreateLimb(rightArm.transform, "RightUpperArmMesh", new Vector3(0, -0.14f, 0), new Vector3(0.12f, 0.28f, 0.12f), suitMat);
        CreateLimb(rightForeArm.transform, "RightLowerArmMesh", new Vector3(0, -0.14f, 0), new Vector3(0.10f, 0.26f, 0.10f), suitMat);
        CreateLimb(rightForeArm.transform, "RightHandMesh", new Vector3(0, -0.30f, 0), new Vector3(0.09f, 0.12f, 0.04f), skinMat);

        // 5. Legs Geometry
        CreateLimb(leftUpLeg.transform, "LeftUpperLegMesh", new Vector3(0, -0.21f, 0), new Vector3(0.16f, 0.40f, 0.16f), suitMat);
        CreateLimb(leftLeg.transform, "LeftLowerLegMesh", new Vector3(0, -0.21f, 0), new Vector3(0.14f, 0.40f, 0.14f), suitMat);
        CreateLimb(leftLeg.transform, "LeftShoeMesh", new Vector3(0, -0.43f, 0.06f), new Vector3(0.12f, 0.08f, 0.24f), shoeMat);

        CreateLimb(rightUpLeg.transform, "RightUpperLegMesh", new Vector3(0, -0.21f, 0), new Vector3(0.16f, 0.40f, 0.16f), suitMat);
        CreateLimb(rightLeg.transform, "RightLowerLegMesh", new Vector3(0, -0.21f, 0), new Vector3(0.14f, 0.40f, 0.14f), suitMat);
        CreateLimb(rightLeg.transform, "RightShoeMesh", new Vector3(0, -0.43f, 0.06f), new Vector3(0.12f, 0.08f, 0.24f), shoeMat);

        // 6. Controllers Attachment
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

    private static void CreateLimb(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(parent, false);
        limb.transform.localPosition = localPos;
        limb.transform.localScale = scale;
        limb.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(limb.GetComponent<Collider>());
    }

    private static Mesh BuildDetailedHeadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "HumanoidHeadMesh";

        int lon = 18;
        int lat = 18;
        float radius = 0.14f;

        Vector3[] baseVertices = new Vector3[(lon + 1) * lat + 2];
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
                Vector3 v = new Vector3(sin1 * cos2, cos1, sin1 * sin2) * radius;

                // Subtle facial sculpting (Chin & Nose protrusion)
                if (v.z > 0.05f && v.y < 0.01f)
                {
                    v.z += 0.02f; // Nose & jaw slope
                }
                baseVertices[index] = v;
            }
        }
        baseVertices[baseVertices.Length - 1] = Vector3.down * radius;

        mesh.vertices = baseVertices;

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

        // 5 Facial Blendshapes
        Vector3[] deltaAa = new Vector3[baseVertices.Length];
        Vector3[] deltaO = new Vector3[baseVertices.Length];
        Vector3[] deltaE = new Vector3[baseVertices.Length];
        Vector3[] deltaU = new Vector3[baseVertices.Length];
        Vector3[] deltaSmile = new Vector3[baseVertices.Length];

        for (int i = 0; i < baseVertices.Length; i++)
        {
            if (baseVertices[i].y < -0.02f && baseVertices[i].z > 0.02f)
            {
                deltaAa[i] = new Vector3(0, -0.05f, 0.02f);
                deltaO[i] = new Vector3(0, -0.03f, 0.04f);
                deltaE[i] = new Vector3(baseVertices[i].x * 0.4f, -0.02f, 0.01f);
                deltaU[i] = new Vector3(-baseVertices[i].x * 0.3f, -0.02f, 0.05f);
                deltaSmile[i] = new Vector3(0, 0.025f, 0.01f);
            }
        }

        mesh.AddBlendShapeFrame("viseme_aa", 100.0f, deltaAa, null, null);
        mesh.AddBlendShapeFrame("viseme_O", 100.0f, deltaO, null, null);
        mesh.AddBlendShapeFrame("viseme_E", 100.0f, deltaE, null, null);
        mesh.AddBlendShapeFrame("viseme_U", 100.0f, deltaU, null, null);
        mesh.AddBlendShapeFrame("mouthSmile", 100.0f, deltaSmile, null, null);

        return mesh;
    }
}
