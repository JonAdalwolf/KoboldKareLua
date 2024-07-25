using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using NetStack.Serialization;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class GenericEdible : GenericUsable {
    [SerializeField]
    private Sprite eatSymbol;
    [SerializeField]
    private GenericReagentContainer container;

    [SerializeField] private bool destroyOnEat = true;
    [SerializeField] private AudioPack eatSoundPack;
    LuaManager manager;
    public override Sprite GetSprite(Kobold k) {
        return eatSymbol;
    }

    public override bool CanUse(Kobold k) {
        return container.volume > 0.01f && k.bellyContainer.volume < k.bellyContainer.maxVolume;
    }

    public override void LocalUse(Kobold k) {
        base.LocalUse(k);
        // Only successfully eat if we own both the edible, and the kobold. Otherwise, wait for ownership to successfully transfer
        float spillAmount = Mathf.Min(10f, k.bellyContainer.maxVolume - k.bellyContainer.volume);
        ReagentContents spill = container.Spill(spillAmount);
        photonView.RPC(nameof(GenericReagentContainer.Spill), RpcTarget.Others, spillAmount);
        BitBuffer buffer = new BitBuffer(4);
        buffer.AddReagentContents(spill);
        k.bellyContainer.photonView.RPC(nameof(GenericReagentContainer.AddMixRPC), RpcTarget.All, buffer, photonView.ViewID, (byte)GenericReagentContainer.InjectType.Spray);
    }

    public override void Use()
    {
        if (!manager)
            manager = GameManager.instance.luaManager;
        foreach (ScriptableLuaScript luaScript in manager.scripts)
        {
            Script lua = luaScript.lua;
            if (lua.Globals["OnObjectUse"] == null)
                continue;
            Table proxy = LuaManager.FindExistingGO(gameObject, lua);
            if (proxy == null)
                proxy = LuaManager.CreateNewGOProxy(gameObject, lua);
            lua.Call(lua.Globals["OnObjectUse"], proxy);
        }
        if (destroyOnEat) {
            PhotonNetwork.Destroy(photonView.gameObject);
        }
        GameManager.instance.SpawnAudioClipInWorld(eatSoundPack, transform.position);
    }
}
