using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// menu Tools > Steering > Build Demo Scene
// bikin arena demo di scene yg lagi kebuka: ground, tembok, obstacle, player, npc, kamera
// biar ga usah drag drop manual satu satu
public static class SteeringSceneBuilder
{
    const string ObstacleLayerName = "Obstacle";
    const string MaterialFolder = "Assets/Materials";

    [MenuItem("Tools/Steering/Build Demo Scene")]
    public static void BuildScene()
    {
        int obstacleLayer = EnsureLayer(ObstacleLayerName);
        if (obstacleLayer < 0)
        {
            Debug.LogError("gagal bikin layer Obstacle, bikin manual aja di Project Settings > Tags and Layers");
            return;
        }

        // kalo udah pernah dibuild, hapus dulu yg lama
        var old = GameObject.Find("SteeringDemo");
        if (old != null) Undo.DestroyObjectImmediate(old);

        var root = new GameObject("SteeringDemo");
        Undo.RegisterCreatedObjectUndo(root, "Build Steering Demo");

        Material groundMat = GetOrCreateMaterial("M_Ground", new Color(0.55f, 0.6f, 0.55f));
        Material obstacleMat = GetOrCreateMaterial("M_Obstacle", new Color(0.35f, 0.35f, 0.4f));
        Material playerMat = GetOrCreateMaterial("M_Player", new Color(0.2f, 0.5f, 1f));
        Material npcMat = GetOrCreateMaterial("M_NPC", new Color(0.2f, 0.8f, 0.3f));

        // ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.localScale = new Vector3(4f, 1f, 4f);   // jadi 40x40
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // tembok keliling arena, masuk layer obstacle biar npc ga keluar
        var walls = new GameObject("Walls"); walls.transform.SetParent(root.transform);
        float half = 20f, wallH = 2f, wallT = 1f;
        MakeBox(walls.transform, "Wall_N", new Vector3(0, wallH / 2, half), new Vector3(2 * half + wallT, wallH, wallT), obstacleMat, obstacleLayer);
        MakeBox(walls.transform, "Wall_S", new Vector3(0, wallH / 2, -half), new Vector3(2 * half + wallT, wallH, wallT), obstacleMat, obstacleLayer);
        MakeBox(walls.transform, "Wall_E", new Vector3(half, wallH / 2, 0), new Vector3(wallT, wallH, 2 * half + wallT), obstacleMat, obstacleLayer);
        MakeBox(walls.transform, "Wall_W", new Vector3(-half, wallH / 2, 0), new Vector3(wallT, wallH, 2 * half + wallT), obstacleMat, obstacleLayer);

        // obstacle di tengah
        var obstacles = new GameObject("Obstacles"); obstacles.transform.SetParent(root.transform);
        MakeBox(obstacles.transform, "Box_1", new Vector3(0, 1, 5), new Vector3(2, 2, 2), obstacleMat, obstacleLayer);
        MakeBox(obstacles.transform, "Box_2", new Vector3(-6, 1, -3), new Vector3(3, 2, 1.5f), obstacleMat, obstacleLayer);
        MakeBox(obstacles.transform, "Box_3", new Vector3(7, 1, -6), new Vector3(1.5f, 2, 4), obstacleMat, obstacleLayer);
        MakeBox(obstacles.transform, "Box_Long", new Vector3(4, 1, 10), new Vector3(8, 2, 1), obstacleMat, obstacleLayer);
        MakeCylinder(obstacles.transform, "Pillar_1", new Vector3(-8, 1, 8), 1f, obstacleMat, obstacleLayer);
        MakeCylinder(obstacles.transform, "Pillar_2", new Vector3(10, 1, 3), 1.5f, obstacleMat, obstacleLayer);
        MakeCylinder(obstacles.transform, "Pillar_3", new Vector3(-3, 1, -10), 1f, obstacleMat, obstacleLayer);

        // player
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.SetParent(root.transform);
        player.transform.position = new Vector3(0, 1, -8);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());  // diganti CharacterController
        var cc = player.AddComponent<CharacterController>();
        cc.center = Vector3.zero; cc.height = 2f; cc.radius = 0.5f;
        player.AddComponent<SimplePlayerController>();
        // kasih hidung biar keliatan ngadep ke mana
        MakeNose(player.transform, playerMat);

        // npc
        var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "NPC_SteeringAgent";
        npc.transform.SetParent(root.transform);
        npc.transform.position = new Vector3(-10, 1, 10);
        npc.GetComponent<Renderer>().sharedMaterial = npcMat;
        var sensor = npc.AddComponent<SteeringSensor>();
        sensor.sensorDistance = 3f;
        sensor.sensorRadius = 0.5f;
        sensor.obstacleMask = 1 << obstacleLayer;
        sensor.castHeight = 0f;
        var agent = npc.AddComponent<SteeringAgent>();
        agent.target = player.transform;
        MakeNose(npc.transform, npcMat);

        // kamera dari atas agak miring
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 28, -22);
            cam.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
        }

        Selection.activeGameObject = npc;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("scene demo udah jadi, tinggal play terus gerakin player pake wasd");
    }

    // helper

    static void MakeBox(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, int layer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = size;
        go.layer = layer;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void MakeCylinder(Transform parent, string name, Vector3 pos, float diameter, Material mat, int layer)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name; go.transform.SetParent(parent);
        go.transform.position = pos; go.transform.localScale = new Vector3(diameter, 1f, diameter);
        go.layer = layer;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void MakeNose(Transform parent, Material mat)
    {
        var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(parent);
        nose.transform.localPosition = new Vector3(0, 0.3f, 0.5f);
        nose.transform.localScale = new Vector3(0.3f, 0.3f, 0.6f);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material GetOrCreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder("Assets", "Materials");

        string path = $"{MaterialFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null && GraphicsSettings.currentRenderPipeline != null)
            shader = GraphicsSettings.currentRenderPipeline.defaultShader;
        if (shader == null) shader = Shader.Find("Standard");

        mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // nambahin layer ke TagManager kalo belum ada, balikin index layernya
    static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)   // 0-7 udah dipake unity
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }
}
