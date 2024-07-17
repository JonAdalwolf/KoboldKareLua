using System;
using System.Collections.Generic;
using UnityEngine;

public class LuaScriptDatabase : MonoBehaviour {
    private static LuaScriptDatabase instance;
    private Dictionary<string,ScriptableLuaScript> luaDictionary;
    private ScriptableLuaScript dummyLua;
    private class LuaSorter : IComparer<ScriptableLuaScript> {
        public int Compare(ScriptableLuaScript x, ScriptableLuaScript y) {
            return String.Compare(x.name, y.name, StringComparison.InvariantCulture);
        }
    }
    private LuaSorter luaSorter;
    public void Awake() {
        if (instance != null) {
            Destroy(this);
        } else {
            instance = this;
        }

        dummyLua = ScriptableObject.CreateInstance<ScriptableLuaScript>();
        
        luaSorter = new LuaSorter();
        luaDictionary = new Dictionary<string, ScriptableLuaScript>();
        foreach(var lua in scripts) {
            luaDictionary.Add(lua.name, lua);
        }

        if (luaDictionary.Count > 255) {
            throw new UnityException("Too many lua scripts, only support up to 255 unique scripts...");
        }
    }
    public static ScriptableLuaScript GetScript(string name) {
        if (!ModManager.GetReady()) {
            return instance.dummyLua;
        }
        if (instance.luaDictionary.ContainsKey(name)) {
            return instance.luaDictionary[name];
        }

        return null;
    }
    public static ScriptableLuaScript GetReagent(byte id) {
        if (!ModManager.GetReady()) {
            return instance.dummyLua;
        }
        
        return instance.scripts[id];
    }
    public static byte GetID(ScriptableLuaScript reagent) {
        return (byte)instance.scripts.IndexOf(reagent);
    }

    public static void AddScript(ScriptableLuaScript newReagent) {
        for (int i = 0; i < instance.scripts.Count; i++) {
            var reagent = instance.scripts[i];
            // Replace strategy
            if (reagent.name == newReagent.name) {
                instance.scripts[i] = newReagent;
                instance.luaDictionary[newReagent.name] = newReagent;
                instance.scripts.Sort(instance.luaSorter);
                return;
            }
        }

        instance.scripts.Add(newReagent);
        instance.luaDictionary.Add(newReagent.name, newReagent);
        instance.scripts.Sort(instance.luaSorter);
    }
    
    public static void RemoveScript(ScriptableLuaScript reagent) {
        if (instance.scripts.Contains(reagent)) {
            instance.scripts.Remove(reagent);
            instance.luaDictionary.Remove(reagent.name);
        }
    }

    public static List<ScriptableLuaScript> GetReagents() => instance.scripts;
    public List<ScriptableLuaScript> scripts;

}
