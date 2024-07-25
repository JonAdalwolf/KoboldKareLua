using System.Collections;
using System.Collections.Generic;
using MoonSharp.Interpreter;
using Photon.Pun;
using UnityEngine;
using LuaProxies;

public static class LuaKoboldCreation
{
    static byte GetPlayerIndex(string name) {
        var playerDatabase = GameManager.GetPlayerDatabase();
        var players = playerDatabase.GetValidPrefabReferenceInfos();
        foreach (var info in players) {
            if (name.Contains(info.GetKey())) {
                return (byte)players.IndexOf(info);
            }
        }
        return 0;
    }
    
    static byte GetDickIndex(string name){
        var penisDatabase = GameManager.GetPenisDatabase();
        var dicks = penisDatabase.GetValidPrefabReferenceInfos();
        foreach (var info in dicks) {
            if (name.Contains(info.GetKey())) {
                return (byte)dicks.IndexOf(info);
            }
        }

        return byte.MaxValue;
    }
    
    public static void CreateCustomKobold(Table t, string species="Kobold", float maxEnergy=5, float baseSize=20, float fatSize=0, float ballSize=5, float dickSize=5, float breastSize=0, float bellySize=20,
                                   float metabolizeCapacitySize=20, float dickThickness=0.5f, float hue=255, float brightness=127.5f, float saturation=127.5f, string dickEquip="None",
                                   int grabCount=1)
    {
        KoboldProxy kp = (KoboldProxy)t["body"];
        Kobold k = kp.target;
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        var koboldTransform = k.hip.transform;
        GameObject GO = null;
        if (pool != null && pool.ResourceCache.ContainsKey(species))
            GO = PhotonNetwork.InstantiateRoomObject(species, koboldTransform.position + koboldTransform.forward, Quaternion.identity);
        if (!GO)
            return;
        Kobold newKobold = GO.GetComponent<Kobold>();
        
        KoboldGenes genes = new KoboldGenes();
        genes.maxEnergy = Mathf.Max(5f, maxEnergy);
        genes.baseSize = Mathf.Max(1,baseSize);
        genes.fatSize = Mathf.Max(1,fatSize);
        genes.ballSize = Mathf.Max(1,ballSize);
        genes.dickSize = Mathf.Max(1,dickSize);
        genes.breastSize = Mathf.Max(1,breastSize);
        genes.bellySize = Mathf.Max(20,bellySize);
        genes.metabolizeCapacitySize = Mathf.Max(20,metabolizeCapacitySize);
        genes.hue = (byte)Mathf.Clamp(hue, 0, 255);
        genes.brightness = (byte)Mathf.Clamp(brightness, 0, 255);
        genes.saturation = (byte)Mathf.Clamp(saturation, 0, 255);
        var dickDatabase = GameManager.GetPenisDatabase().GetValidPrefabReferenceInfos();
        genes.dickEquip = GetDickIndex(dickEquip);
        genes.dickThickness = Mathf.Max(1,dickThickness);
        genes.grabCount = (byte)Mathf.Max(1,grabCount);
        genes.species = GetPlayerIndex(species);
        
        newKobold.SetGenes(genes);
    }

    public static void CloneKobold(Table t)
    {
        KoboldProxy kp = (KoboldProxy)t["body"];
        Kobold k = kp.target;
        DefaultPool pool = PhotonNetwork.PrefabPool as DefaultPool;
        var koboldTransform = k.hip.transform;
        GameObject GO = PhotonNetwork.InstantiateRoomObject(k.name.Substring(0,k.name.Length-7), koboldTransform.position + koboldTransform.forward, Quaternion.identity);
        Kobold newKobold = GO.GetComponent<Kobold>();
        newKobold.SetGenes(k.GetGenes());
    }
}
