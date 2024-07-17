using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

public class LuaManager : MonoBehaviour
{
    [HideInInspector] public List<ScriptableLuaScript> scripts;
    Script lua = new Script(CoreModules.Preset_HardSandbox);
    bool exec;

    public void OnModsLoaded()
    {
        lua.Options.DebugPrint = s => Debug.Log("LUA: "+s);
        foreach (var script in scripts)
        {
            lua.DoString(script.luaScript);
            if(lua.Globals["Start"] != null)
                lua.Call(lua.Globals["Start"]);
            script.executing = true;
        }
        exec = true;
    }

    void Update()
    {
        if (!exec) return;
        foreach (var script in scripts)
        {
            if(!script.executing) continue;
            
            lua.DoString(script.luaScript);
        }
    }

    void FixedUpdate()
    {
        if (!exec) return;
        foreach (var script in scripts)
        {
            if(!script.executing) continue;
            
            if(lua.Globals["FixedUpdate"] != null)
                lua.Call(lua.Globals["FixedUpdate"]);
        }
    }
}
