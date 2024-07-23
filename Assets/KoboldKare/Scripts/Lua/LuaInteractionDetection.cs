using MoonSharp.Interpreter;
using UnityEngine;

public class LuaInteractionDetection : MonoBehaviour
{
    LuaManager manager;
    void Start() { manager = GameManager.instance.luaManager; }

    void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject) return;
        foreach (ScriptableLuaScript luaScript in manager.scripts)
        {
            Script lua = luaScript.lua;
            if (lua.Globals["OnCollisionEnter"] == null)
                continue;
            Table proxy = LuaManager.FindExistingGO(gameObject, lua);
            if (proxy == null)
                proxy = LuaManager.CreateNewGOProxy(gameObject, lua);
            lua.Call(lua.Globals["OnCollisionEnter"], proxy, other.gameObject.tag, other.gameObject.layer);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject) return;
        foreach (ScriptableLuaScript luaScript in manager.scripts)
        {
            Script lua = luaScript.lua;
            if (lua.Globals["OnCollisionEnter"] == null)
                continue;
            Table proxy = LuaManager.FindExistingGO(gameObject, lua);
            if (proxy == null)
                proxy = LuaManager.CreateNewGOProxy(gameObject, lua);
            lua.Call(lua.Globals["OnCollisionEnter"], proxy, other.gameObject.tag, other.gameObject.layer);
        }
    }
}
