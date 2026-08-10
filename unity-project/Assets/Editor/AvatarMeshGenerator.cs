using UnityEditor;
using UnityEngine;

public static class AvatarMeshGenerator
{
    [MenuItem("V-STIPA/Generate Stylized Meta-Style Avatars")]
    public static void GenerateAvatars()
    {
        CreateMetaStyleAvatar("WarmAvatar", new Color(0.45f, 0.48f, 0.52f), new Color(0.96f, 0.96f, 0.96f), new Color(0.85f, 0.68f, 0.58f), new Color(0.18f, 0.18f, 0.20f));
        CreateMetaStyleAvatar("SternAvatar", new Color(0.25f, 0.28f, 0.32f), new Color(0.90f, 0.90f, 0.92f), new Color(0.80f, 0.64f, 0.54f), new Color(0.12f, 0.12f, 0.14f));
        CreateMetaStyleAvatar("NeutralAvatar", new Color(0.35f, 0.40f, 0.48f), new Color(0.94f, 0.94f, 0.94f), new Color(0.88f, 0.70f, 0.60f), new Color(0.15f, 0.15f, 0.18f));
        Debug.Log("[AvatarMeshGenerator] Stylized Meta-Style Avatars (Beanie, Grey Blazer, White Shirt, Trousers) generated successfully.");
    }

    public static GameObject CreateMetaStyleAvatar(string avatarName, Color blazerColor, Color shirtColor, Color skinColor, Color beanieColor)
    {
        GameObject rootGo = new GameObject(avatarName);

        // Materials with Smooth Standard Shaders
        Material blazerMat = CreateSmoothMaterial(blazerColor, 0.2f);
        Material shirtMat = CreateSmoothMaterial(shirtColor, 0.1f);
        Material trouserMat = CreateSmoothMaterial(new Color(0.12f, 0.12f, 0.14f), 0.1f);
        Material skinMat = CreateSmoothMaterial(skinColor, 0.3f);
        Material beanieMat = CreateSmoothMaterial(beanieColor, 0.05f);
        Material eyeMat = CreateSmoothMaterial(new Color(0.08f, 0.08f, 0.10f), 0.8f);
        Material eyeWhiteMat = CreateSmoothMaterial(Color.white, 0.6f);
        Material eyebrowMat = CreateSmoothMaterial(new Color(0.12f, 0.10f, 0.08f), 0.1f);
        Material shoeMat = CreateSmoothMaterial(new Color(0.08f, 0.08f, 0.08f), 0.4f);

        // 1. Humanoid Skeleton (1.75m Scale)
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

        // Arms & Shoulders
        GameObject leftShoulder = new GameObject("LeftShoulder");
        leftShoulder.transform.SetParent(chest.transform, false);
        leftShoulder.transform.localPosition = new Vector3(-0.20f, 0.12f, 0);

        GameObject leftArm = new GameObject("LeftArm");
        leftArm.transform.SetParent(leftShoulder.transform, false);
        leftArm.transform.localPosition = new Vector3(-0.10f, 0, 0);

        GameObject leftForeArm = new GameObject("LeftForeArm");
        leftForeArm.transform.SetParent(leftArm.transform, false);
        leftForeArm.transform.localPosition = new Vector3(0, -0.28f, 0);

        GameObject rightShoulder = new GameObject("RightShoulder");
        rightShoulder.transform.SetParent(chest.transform, false);
        rightShoulder.transform.localPosition = new Vector3(0.20f, 0.12f, 0);

        GameObject rightArm = new GameObject("RightArm");
        rightArm.transform.SetParent(rightShoulder.transform, false);
        rightArm.transform.localPosition = new Vector3(0.10f, 0, 0);

        GameObject rightForeArm = new GameObject("RightForeArm");
        rightForeArm.transform.SetParent(rightArm.transform, false);
        rightForeArm.transform.localPosition = new Vector3(0, -0.28f, 0);

        // Legs
        GameObject leftUpLeg = new GameObject("LeftUpLeg");
        leftUpLeg.transform.SetParent(hips.transform, false);
        leftUpLeg.transform.localPosition = new Vector3(-0.11f, 0, 0);

        GameObject leftLeg = new GameObject("LeftLeg");
        leftLeg.transform.SetParent(leftUpLeg.transform, false);
        leftLeg.transform.localPosition = new Vector3(0, -0.42f, 0);

        GameObject rightUpLeg = new GameObject("RightUpLeg");
        rightUpLeg.transform.SetParent(hips.transform, false);
        rightUpLeg.transform.localPosition = new Vector3(0.11f, 0, 0);

        GameObject rightLeg = new GameObject("RightLeg");
        rightLeg.transform.SetParent(rightUpLeg.transform, false);
        rightLeg.transform.localPosition = new Vector3(0, -0.42f, 0);

        // 2. Clothing (Grey Blazer + White Shirt V-Neck)
        GameObject blazerTorso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blazerTorso.name = "GreyBlazerTorso";
        blazerTorso.transform.SetParent(chest.transform, false);
        blazerTorso.transform.localPosition = new Vector3(0, -0.04f, 0);
        blazerTorso.transform.localScale = new Vector3(0.42f, 0.36f, 0.24f);
        blazerTorso.GetComponent<Renderer>().sharedMaterial = blazerMat;
        Object.DestroyImmediate(blazerTorso.GetComponent<Collider>());

        // Blazer Lapels
        GameObject leftLapel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftLapel.name = "LeftLapel";
        leftLapel.transform.SetParent(chest.transform, false);
        leftLapel.transform.localPosition = new Vector3(-0.09f, 0.04f, 0.125f);
        leftLapel.transform.localScale = new Vector3(0.08f, 0.20f, 0.02f);
        leftLapel.transform.localRotation = Quaternion.Euler(0, 0, -12f);
        leftLapel.GetComponent<Renderer>().sharedMaterial = blazerMat;
        Object.DestroyImmediate(leftLapel.GetComponent<Collider>());

        GameObject rightLapel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightLapel.name = "RightLapel";
        rightLapel.transform.SetParent(chest.transform, false);
        rightLapel.transform.localPosition = new Vector3(0.09f, 0.04f, 0.125f);
        rightLapel.transform.localScale = new Vector3(0.08f, 0.20f, 0.02f);
        rightLapel.transform.localRotation = Quaternion.Euler(0, 0, 12f);
        rightLapel.GetComponent<Renderer>().sharedMaterial = blazerMat;
        Object.DestroyImmediate(rightLapel.GetComponent<Collider>());

        // White V-Neck Shirt
        GameObject shirtV = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shirtV.name = "WhiteShirtVNeck";
        shirtV.transform.SetParent(chest.transform, false);
        shirtV.transform.localPosition = new Vector3(0, 0.02f, 0.115f);
        shirtV.transform.localScale = new Vector3(0.16f, 0.22f, 0.02f);
        shirtV.GetComponent<Renderer>().sharedMaterial = shirtMat;
        Object.DestroyImmediate(shirtV.GetComponent<Collider>());

        // Neck Mesh
        GameObject neckMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neckMesh.name = "NeckMesh";
        neckMesh.transform.SetParent(neck.transform, false);
        neckMesh.transform.localPosition = new Vector3(0, -0.02f, 0);
        neckMesh.transform.localScale = new Vector3(0.13f, 0.08f, 0.13f);
        neckMesh.GetComponent<Renderer>().sharedMaterial = skinMat;
        Object.DestroyImmediate(neckMesh.GetComponent<Collider>());

        // 3. Head & Facial Features (Skinned Mesh + Beanie)
        GameObject headMeshGo = new GameObject("HeadMesh");
        headMeshGo.transform.SetParent(head.transform, false);
        SkinnedMeshRenderer headRenderer = headMeshGo.AddComponent<SkinnedMeshRenderer>();
        headRenderer.sharedMaterial = skinMat;

        Mesh headMesh = BuildMetaFacialMesh();
        headRenderer.sharedMesh = headMesh;
        headRenderer.bones = new Transform[] { head.transform, neck.transform };
        headRenderer.rootBone = head.transform;

        // Beanie Cap (Matching Reference Image)
        GameObject beanieGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beanieGo.name = "BeanieCap";
        beanieGo.transform.SetParent(head.transform, false);
        beanieGo.transform.localPosition = new Vector3(0, 0.075f, -0.01f);
        beanieGo.transform.localScale = new Vector3(0.285f, 0.18f, 0.295f);
        beanieGo.GetComponent<Renderer>().sharedMaterial = beanieMat;
        Object.DestroyImmediate(beanieGo.GetComponent<Collider>());

        // Eyes (Whites & Pupils)
        CreateEye(head.transform, "LeftEye", new Vector3(-0.048f, 0.025f, 0.125f), eyeWhiteMat, eyeMat);
        CreateEye(head.transform, "RightEye", new Vector3(0.048f, 0.025f, 0.125f), eyeWhiteMat, eyeMat);

        // Eyebrows
        GameObject leftBrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftBrow.name = "LeftEyebrow";
        leftBrow.transform.SetParent(head.transform, false);
        leftBrow.transform.localPosition = new Vector3(-0.048f, 0.052f, 0.132f);
        leftBrow.transform.localScale = new Vector3(0.038f, 0.008f, 0.012f);
        leftBrow.transform.localRotation = Quaternion.Euler(0, 0, -4f);
        leftBrow.GetComponent<Renderer>().sharedMaterial = eyebrowMat;
        Object.DestroyImmediate(leftBrow.GetComponent<Collider>());

        GameObject rightBrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightBrow.name = "RightEyebrow";
        rightBrow.transform.SetParent(head.transform, false);
        rightBrow.transform.localPosition = new Vector3(0.048f, 0.052f, 0.132f);
        rightBrow.transform.localScale = new Vector3(0.038f, 0.008f, 0.012f);
        rightBrow.transform.localRotation = Quaternion.Euler(0, 0, 4f);
        rightBrow.GetComponent<Renderer>().sharedMaterial = eyebrowMat;
        Object.DestroyImmediate(rightBrow.GetComponent<Collider>());

        // 4. Arms (Blazer Sleeves + Hands)
        CreatePart(leftArm.transform, "LeftBlazerSleeve", new Vector3(0, -0.14f, 0), new Vector3(0.12f, 0.28f, 0.12f), blazerMat);
        CreatePart(leftForeArm.transform, "LeftBlazerLowerSleeve", new Vector3(0, -0.12f, 0), new Vector3(0.10f, 0.22f, 0.10f), blazerMat);
        CreatePart(leftForeArm.transform, "LeftHand", new Vector3(0, -0.28f, 0), new Vector3(0.085f, 0.11f, 0.04f), skinMat);

        CreatePart(rightArm.transform, "RightBlazerSleeve", new Vector3(0, -0.14f, 0), new Vector3(0.12f, 0.28f, 0.12f), blazerMat);
        CreatePart(rightForeArm.transform, "RightBlazerLowerSleeve", new Vector3(0, -0.12f, 0), new Vector3(0.10f, 0.22f, 0.10f), blazerMat);
        CreatePart(rightForeArm.transform, "RightHand", new Vector3(0, -0.28f, 0), new Vector3(0.085f, 0.11f, 0.04f), skinMat);

        // 5. Trousers & Shoes
        CreatePart(leftUpLeg.transform, "LeftTrouserUpper", new Vector3(0, -0.21f, 0), new Vector3(0.15f, 0.40f, 0.15f), trouserMat);
        CreatePart(leftLeg.transform, "LeftTrouserLower", new Vector3(0, -0.21f, 0), new Vector3(0.13f, 0.40f, 0.13f), trouserMat);
        CreatePart(leftLeg.transform, "LeftShoe", new Vector3(0, -0.43f, 0.05f), new Vector3(0.11f, 0.08f, 0.24f), shoeMat);

        CreatePart(rightUpLeg.transform, "RightTrouserUpper", new Vector3(0, -0.21f, 0), new Vector3(0.15f, 0.40f, 0.15f), trouserMat);
        CreatePart(rightLeg.transform, "RightTrouserLower", new Vector3(0, -0.21f, 0), new Vector3(0.13f, 0.40f, 0.13f), trouserMat);
        CreatePart(rightLeg.transform, "RightShoe", new Vector3(0, -0.43f, 0.05f), new Vector3(0.11f, 0.08f, 0.24f), shoeMat);

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

    private static Material CreateSmoothMaterial(Color col, float smoothness)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = col;
        mat.SetFloat("_Glossiness", smoothness);
        return mat;
    }

    private static void CreateEye(Transform parent, string name, Vector3 pos, Material whiteMat, Material pupilMat)
    {
        GameObject eyeBase = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyeBase.name = name;
        eyeBase.transform.SetParent(parent, false);
        eyeBase.transform.localPosition = pos;
        eyeBase.transform.localScale = new Vector3(0.028f, 0.024f, 0.020f);
        eyeBase.GetComponent<Renderer>().sharedMaterial = whiteMat;
        Object.DestroyImmediate(eyeBase.GetComponent<Collider>());

        GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pupil.name = "Pupil";
        pupil.transform.SetParent(eyeBase.transform, false);
        pupil.transform.localPosition = new Vector3(0, 0, 0.35f);
        pupil.transform.localScale = new Vector3(0.55f, 0.55f, 0.40f);
        pupil.GetComponent<Renderer>().sharedMaterial = pupilMat;
        Object.DestroyImmediate(pupil.GetComponent<Collider>());
    }

    private static void CreatePart(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = pos;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(part.GetComponent<Collider>());
    }

    private static Mesh BuildMetaFacialMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "MetaFacialHeadMesh";

        int lon = 20;
        int lat = 20;
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

                // Sculpt Chin & Cheekbones matching Meta Horizon Avatar proportions
                if (v.z > 0.04f)
                {
                    v.z += 0.015f; // Face slope
                    if (v.y < -0.03f) v.y *= 1.08f; // Chin lengthen
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

        // 5 Facial Viseme Blendshapes
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
