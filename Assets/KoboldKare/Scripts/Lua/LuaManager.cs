using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using Photon.Pun;
using UnityEngine.InputSystem;
using LuaProxies;
using Random = UnityEngine.Random;

public class LuaManager : MonoBehaviour
{
    [HideInInspector]
    public List<ScriptableLuaScript> scripts;
    [HideInInspector]
    public bool exec;
    List<Kobold> koboldRemovalList = new List<Kobold>();
    List<string> koboldRemovalStrings = new List<string>();

    PlayerInput controls;
    InputAction movement;
    InputAction interaction;
    InputAction mouse1;
    InputAction mouse2;
    InputAction mouseWheel;
    InputAction extra1;
    InputAction extra2;
    Vector2 moveInput;
    
    bool findingControls;
    int deltaFrames;
    HashSet<Kobold> kobolds;
    GameObject playerCanvas;
    PlayerPossession playerPoss;
    //Script lua = new Script(CoreModules.Preset_HardSandbox);

    void Awake()
    {
        LuaCustomConverters.RegisterAll();
        UserData.RegisterProxyType<KoboldProxy, Kobold>(k => new KoboldProxy(k));
        UserData.RegisterProxyType<GObjectProxy, GameObject>(g => new GObjectProxy(g));
        UserData.RegisterProxyType<AnimatorProxy, Animator>(a => new AnimatorProxy(a));
        UserData.RegisterProxyType<AudioProxy, AudioSource>(a => new AudioProxy(a));
        UserData.RegisterProxyType<RBProxy, Rigidbody>(r => new RBProxy(r));
        UserData.RegisterProxyType<RectProxy, RectTransform>(r => new RectProxy(r));
        
        kobolds = new HashSet<Kobold>(FindObjectsOfType<Kobold>());
        Kobold.spawned += OnKoboldSpawn;
    }

    public void OnLoad()
    {
        
        foreach (var script in scripts)
        {
            script.lua = new Script(CoreModules.Preset_HardSandbox)
            { Options = { DebugPrint = s => Debug.Log("LUA: "+s) } };
            script.lua.LoadString(script.luaScript);

            script.lua.Globals["InstantiateAtKobold"] = (Func<Script, Table, string, float, Table>)InstantiateAtKobold;
            script.lua.Globals["InstantiateAtPosition"] = (Func<Script, float, float, float, string, float, Table>)InstantiateAtPosition;
            script.lua.Globals["InstantiateAtObject"] = (Func<Script, GObjectProxy, string, bool, float, Table>)InstantiateAtObject;
            script.lua.Globals["DestroyObject"] = (Action<GObjectProxy, float>)DestroyObject;
            script.lua.Globals["AddCanvasElement"] = (Func<Script, string, float, Table>)AddCanvasElement;
            script.lua.Globals["AddCanvasElementAtPosition"] = (Func<Script, string, float, float, float, Table>)AddCanvasElementAtPosition;
            script.lua.Globals["RemoveNewCanvasElement"] = (Action<Script, GObjectProxy, float>)RemoveNewCanvasElement;
            script.lua.Globals["GetObjectInView"] = (Func<Script, float, Table>)GetObjectInView;
            script.lua.Globals["CreateCustomKoboldAtPlayer"] = (Action<Table, string, float, float, float, float, float, float, float, float, float, float, float, float, string, int>)LuaKoboldCreation.CreateCustomKobold;
            script.lua.Globals["CloneKobold"] = (Action<Table>)LuaKoboldCreation.CloneKobold;
            script.lua.Globals["GetMoney"] = (Func<float>)GetMoney;
            script.lua.Globals["AddMoney"] = (Action<float>)AddMoney;
            script.lua.Globals["GetStars"] = (Func<int>)GetStars;
            script.lua.Globals["AddStars"] = (Action<int>)AddStars;

            script.lua.Globals["kobolds"] = DynValue.NewTable(script.lua);
            script.lua.Globals["input"] = DynValue.NewTable(script.lua);
            script.lua.Globals["handledObjects"] = DynValue.NewTable(script.lua);
            script.lua.Globals["self"] = null;

            if(script.lua.Globals["Start"] != null)
                script.lua.Call(script.lua.Globals["Start"]);
            
            script.executing = true;
        }
        exec = true;
    }

    public void Unload()
    {
        print("Unloading and restarting lua scripts...");
        foreach (var script in scripts)
        {
            script.lua.Globals.Clear();
            script.executing = false;
        }
        deltaFrames = 0;
        exec = false;
    }

    private void Update()
    {
        if (!exec) return;
        if (GameManager.instance.isPaused && !NetworkManager.instance.online) return;
        
        if (!controls)
        {
            if(!findingControls)
                StartCoroutine(FindHostControls());
            return;
        }

        foreach (ScriptableLuaScript luaScript in scripts)
        {
            Script lua = luaScript.lua;
            
            KoboldFrameUpdate(lua);
            InputDetection(lua);
            HandledObjUpdate(lua);
        
            Table players = (Table)lua.Globals["kobolds"];
            Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
            if (k && players[k.LuaID] != null)
                lua.Globals["self"] = players[k.LuaID];
            else
                lua.Globals["self"] = null;

            foreach (var script in scripts)
            {
                if(!script.executing) continue;
            
                lua.DoString(script.luaScript);
            }
        }

    }

    private void FixedUpdate()
    {
        if (!exec) return;
        if (GameManager.instance.isPaused && !NetworkManager.instance.online) return;

        foreach (var script in scripts)
        {
            if(!script.executing) continue;
            script.lua.Globals["deltaFrames"] = deltaFrames;
            
            if(script.lua.Globals["FixedUpdate"] != null)
                script.lua.Call(script.lua.Globals["FixedUpdate"]);
        }

        deltaFrames++;
    }
    
    private void OnKoboldSpawn(Kobold kobold) {
        kobolds.Add(kobold);
    }

    private void KoboldFrameUpdate(Script lua)
    {
        koboldRemovalList.Clear();
        Table players = (Table)lua.Globals["kobolds"];

        foreach (var kobold in kobolds)
        {
            if (!kobold)
            {
                koboldRemovalList.Add(kobold);
                continue;
            }

            if (String.Equals(kobold.LuaID, "-1"))
            {
                kobold.LuaID = Random.Range(100000000, 999999999).ToString();
                koboldRemovalStrings.Add(kobold.LuaID);
            }

            Table player = players.Get(kobold.LuaID).Table;
            if (player == null)
            {
                // Kobold component goes to "body"
                DynValue koboldID = DynValue.NewTable(lua);
                koboldID.Table["body"] = new KoboldProxy(kobold);
                
                // GameObject goes to "object"
                GObjectProxy proxy = new GObjectProxy(kobold.gameObject) { script = lua };
                koboldID.Table["object"] = proxy;
                
                // Animator component goes to "animator"
                if (kobold.mainAnimator)
                    koboldID.Table["animator"] = kobold.mainAnimator;
                else
                    koboldID.Table["animator"] = new AnimatorProxy(kobold.transform.GetChild(0).GetComponent<Animator>());
                
                // Rigidbody component goes to "physics"
                koboldID.Table["physics"] = new RBProxy(kobold.body);
                
                // All of that becomes the kobold table
                players.Set(kobold.LuaID, koboldID);
            }


        }

        foreach (var kobold in koboldRemovalList)
        {
            int stringIndex = koboldRemovalList.IndexOf(kobold);
            players.Remove(koboldRemovalStrings[stringIndex]);
            koboldRemovalStrings.RemoveAt(stringIndex);
            kobolds.Remove(kobold);
        }
            
    }

    private void InputDetection(Script lua)
    {
        Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
        if (!k) return;
        KoboldCharacterController controller = k.controller;
        if (!controller || !controls) return;

        moveInput = movement.ReadValue<Vector2>();

        Table pInput = (Table)lua.Globals["input"];
        pInput["jumping"] = controller.inputJump;
        pInput["interacting"] = interaction.ReadValue<float>() > 0.9f;
        pInput["mouse1"] = mouse1.ReadValue<float>() > 0.9f;
        pInput["mouse2"] = mouse2.ReadValue<float>() > 0.9f;
        pInput["extra1"] = extra1.ReadValue<float>() > 0.9f;
        pInput["extra2"] = extra2.ReadValue<float>() > 0.9f;
        pInput["mouseWheel"] = mouseWheel.ReadValue<float>();
        pInput["forwardMove"] = moveInput.x;
        pInput["sideMove"] = moveInput.y;
        pInput["paused"] = GameManager.instance.isPaused;
        pInput["equipMenuOpen"] = playerPoss.equipmentUI.activeInHierarchy;
        pInput["SetCursorLock"] = (Action<bool>)SetCursorLock;
    }

    
    void HandledObjUpdate(Script lua)
    {
        Table o = (Table)lua.Globals["handledObjects"];
        for (int i = 1; i <= o.Length; i++)
        {
            Table table = (Table)o[i];
            GObjectProxy value = (GObjectProxy)table["object"];
            if (!value.target)
                o.Remove(i);
        }
    }

    public static Table CreateNewGOProxy(GameObject GO, Script script)
    {
        DynValue obj = DynValue.NewTable(script);
        GObjectProxy proxy = new GObjectProxy(GO) { script = script };
        obj.Table["object"] = proxy;
        string name = GO.name;
        if (name.Contains("(Clone)"))
            name = name.Substring(0,name.Length - 7);
        obj.Table["name"] = name;
        obj.Table["layer"] = GO.layer;
        obj.Table["tag"] = GO.tag;
        Animator anim = GO.GetComponent<Animator>();
        if (anim)
            obj.Table["animator"] = new AnimatorProxy(anim);
        
        Rigidbody rb = GO.GetComponent<Rigidbody>();
        if (rb)
            obj.Table["physics"] = new RBProxy(rb);

        RectTransform rect = GO.GetComponent<RectTransform>();
        if (rect)
        {
            RectProxy prox = new RectProxy(rect) { script = script };
            obj.Table["rectTransform"] = prox;
        }
            
        
        AudioSource[] audios = GO.GetComponentsInChildren<AudioSource>();
        if(audios.Length > 0)
        {
            Table aud = DynValue.NewTable(script).Table;
            for (int i = 1; i <= audios.Length; i++)
            {
                aud[i] = new AudioProxy(audios[i-1]);
            }
            obj.Table["audios"] = aud;
        }
        
        Table globals = (Table)script.Globals["handledObjects"];
        globals[globals.Length+1] = obj;
        return obj.Table;
    }

    public static Table FindExistingGO(GameObject GO, Script script)
    {
        Table t = (Table)script.Globals["handledObjects"];
        if (t == null)
            return null;
        for (int i = 1; i <= t.Length; i++)
        {
            Table obj = t.Get(i).Table;
            
            GObjectProxy ExistingObj = (GObjectProxy)obj["object"];
            if (ExistingObj.target == GO)
                return obj;
        }
        return null;
    }
    
    IEnumerator FindHostControls()
    {
        findingControls = true;
        while(!controls && !playerCanvas)
        {
            print("finding host's controls...");
            Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
            if (k)
            {
                Transform t = k.transform.Find("PlayerController(Clone)");
                if (t)
                {
                    if(!controls)
                        controls = t.GetComponent<PlayerInput>();
                    if (!playerCanvas)
                    {
                        playerCanvas = t.GetComponentInChildren<RectTransform>().gameObject;
                        playerPoss = t.GetComponentInChildren<PlayerPossession>();
                    }
                        
                }
                    
                if (controls)
                {
                    movement = controls.actions["Move"];
                    interaction = controls.actions["Use"];
                    mouse1 = controls.actions["Grab"];
                    mouse2 = controls.actions["ActivateGrab"];
                    extra1 = controls.actions["LuaExtra1"];
                    extra2 = controls.actions["LuaExtra2"];
                    mouseWheel = controls.actions["Grab Push and Pull"];
                }
            }
            yield return new WaitForSeconds(1);
        }
        findingControls = false;
    }
    
    Table InstantiateAtKobold(Script lua, Table k, string gameObj, float lifetime = -1)
    {
        KoboldProxy ko = (KoboldProxy)k["body"];
        if (ko == null)
        { Debug.Log("LUA: Specified kobold is not valid"); return null; }
        
        Kobold kobold = ko.target;
        if (!kobold)
        { Debug.Log("LUA: Specified kobold is not valid"); return null; }
        
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        var koboldTransform = kobold.hip.transform;
        if (pool != null && pool.ResourceCache.ContainsKey(gameObj) && !pool.ResourceCache[gameObj].CompareTag("Player")) {
            GameObject GO = PhotonNetwork.InstantiateRoomObject(gameObj, koboldTransform.position + koboldTransform.forward, Quaternion.identity);
            if(lifetime > 0)
                Destroy(GO, lifetime);
            Debug.Log("LUA: Spawned " + gameObj);
            
            Table existing = FindExistingGO(GO, lua);
            return existing ?? CreateNewGOProxy(GO, lua);
        }
        Debug.LogError("LUA: There is no registered game object with the supplied name, or the registered object is a Kobold");
        return null;
    }
    
    Table InstantiateAtPosition(Script lua, float x, float y, float z, string gameObj, float lifetime = -1)
    {
        Vector3 pos = new Vector3(x, y, z);
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        if (pool != null && pool.ResourceCache.ContainsKey(gameObj) && !pool.ResourceCache[gameObj].CompareTag("Player")) {
            GameObject GO = PhotonNetwork.InstantiateRoomObject(gameObj, pos, Quaternion.identity);
            if(lifetime > 0)
                Destroy(GO, lifetime);
            Debug.Log("LUA: Spawned " + gameObj);
            
            Table existing = FindExistingGO(GO, lua);
            return existing ?? CreateNewGOProxy(GO, lua);
        }
        Debug.LogError("LUA: There is no registered game object with the supplied name");
        return null;
    }

    Table InstantiateAtObject(Script lua, GObjectProxy GO, string gameObj, bool parent = false, float lifetime = -1)
    {
        Transform t = GO.target.transform;
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        if (pool != null && pool.ResourceCache.ContainsKey(gameObj) && !pool.ResourceCache[gameObj].CompareTag("Player")) {
            GameObject newObj = PhotonNetwork.InstantiateRoomObject(gameObj, t.position, parent ? t.localRotation : Quaternion.identity);
            if(lifetime > 0)
                Destroy(newObj, lifetime);
            if (parent)
                newObj.transform.parent = GO.target.transform;
            Debug.Log("LUA: Spawned " + gameObj);
            
            Table existing = FindExistingGO(newObj, lua);
            return existing ?? CreateNewGOProxy(newObj, lua);
        }
        Debug.LogError("LUA: There is no registered game object with the supplied name");
        return null;
    }

    Table GetObjectInView(Script lua, float distance = 5f)
    {
        Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
        Vector3 aimPosition = k.GetComponentInChildren<Animator>().GetBoneTransform(HumanBodyBones.Head).position;
        Vector3 aimDir = k.GetComponentInChildren<CharacterControllerAnimator>(true).eyeDir;
        // ReSharper disable once Unity.PreferNonAllocApi
        foreach (RaycastHit hit in Physics.RaycastAll(aimPosition, aimDir, distance))
        {
            GameObject GO = hit.transform.gameObject;
            if (!GO || GO == k.gameObject || GO.layer == 10) continue;
            if (GO.GetComponentInParent<Kobold>() == k) continue;

            Table existing = FindExistingGO(GO, lua);
            return existing ?? CreateNewGOProxy(GO, lua);
        }
        return null;
    }

    Table AddCanvasElement(Script lua, string newObj, float lifetime = 0)
    {
        if (!playerCanvas)
        {
            Debug.LogError("LUA: Could not find player canvas element");
            return null;
        }
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        if (pool != null && pool.ResourceCache[newObj])
        {
            GameObject GO = Instantiate(pool.ResourceCache[newObj], playerCanvas.transform);
            if(lifetime > 0)
                Destroy(GO, lifetime);
            
            return CreateNewGOProxy(GO, lua);
        }
        Debug.LogError("LUA: Specified object does not exist");
        return null;
    }
    
    Table AddCanvasElementAtPosition(Script lua, string newObj, float x, float y, float lifetime = 0)
    {
        if (!playerCanvas)
        {
            Debug.LogError("LUA: Could not find player canvas element");
            return null;
        }
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        if (pool != null && pool.ResourceCache[newObj])
        {
            GameObject GO = Instantiate(pool.ResourceCache[newObj], playerCanvas.transform);
            if(lifetime > 0)
                Destroy(GO, lifetime);
            GO.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            return CreateNewGOProxy(GO, lua);
        }
        Debug.LogError("LUA: Specified object does not exist");
        return null;
    }

    void RemoveNewCanvasElement(Script lua, GObjectProxy obj, float delay = 0)
    {
        lua.Globals[obj] = null;
        if (delay == 0)
            Destroy(obj.target);
        else Destroy(obj.target, delay);
    }

    void DestroyObject(GObjectProxy obj, float delay = 0)
    {
        if(delay > 0)
            Destroy(obj.target, delay);
        else
            Destroy(obj.target);
    }

    void SetCursorLock(bool status)
    {
        if (status)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    float GetMoney()
    {
        Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
        if (!k) return -1;
        return k.GetComponent<MoneyHolder>().GetMoney();
    }
    void AddMoney(float value)
    {
        Kobold k = (Kobold)PhotonNetwork.LocalPlayer.TagObject;
        if (!k) return;
        k.photonView.RPC(nameof(MoneyHolder.AddMoney), RpcTarget.All, value);
    }
    
    int GetStars() { return ObjectiveManager.GetStars(); }
    void AddStars(int value) { ObjectiveManager.GiveStars(value); }
}
