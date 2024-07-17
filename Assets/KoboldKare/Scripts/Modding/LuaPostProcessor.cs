using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;

public class LuaPostProcessor : ModPostProcessor
{
    [SerializeField] LuaManager manager;
    bool managerError;
    private List<ScriptableLuaScript> addedScripts;

    public override void Awake() {
        base.Awake();
        addedScripts = new List<ScriptableLuaScript>();
    }

    public override async Task LoadAllAssets(IList<IResourceLocation> locations) {
        addedScripts.Clear();
        var opHandle = Addressables.LoadAssetsAsync<ScriptableLuaScript>(locations, LoadScript);
        await opHandle.Task;
    }

    private void LoadScript(ScriptableLuaScript script) {
        if (script == null) {
            return;
        }
        LuaScriptDatabase.AddScript(script);
        addedScripts.Add(script);
        if (manager)
            manager.scripts.Add(script);
        else if (!managerError)
        {
            Debug.LogError("A lua manager was not specified in the lua post processor. Please specify the game object responsible for running lua files.");
            managerError = true;
        }
    }

    public override void UnloadAllAssets() {
        foreach (var script in addedScripts) {
            LuaScriptDatabase.RemoveScript(script);
        }
        addedScripts.Clear();
    }
}