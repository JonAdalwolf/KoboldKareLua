using MoonSharp.Interpreter;
using UnityEngine;

[CreateAssetMenu(fileName = "New lua script", menuName = "Scripting/Lua script", order = 1)]
public class ScriptableLuaScript : ScriptableObject
{

    [HideInInspector] public bool executing;
    [HideInInspector] public Script lua;
    
    [SerializeField] private new string name;
    public string GetName() => name;
    
    [Multiline(20)] public string luaScript;
    
}