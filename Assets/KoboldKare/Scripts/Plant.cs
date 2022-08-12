﻿using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
using Photon.Pun;
using KoboldKare;
using System.IO;

[RequireComponent(typeof(GenericReagentContainer))]
public class Plant : GeneHolder, IPunInstantiateMagicCallback, ISavable {
    public ScriptablePlant plant;
    [SerializeField]
    private GenericReagentContainer container;

    [SerializeField]
    public Color darkenedColor;

    [SerializeField]
    private VisualEffect effect, wateredEffect;
    [SerializeField]
    private GameObject display;

    [SerializeField]
    public AudioSource audioSource;
    public delegate void SwitchAction();
    public event SwitchAction switched;
    
    private static readonly int BrightnessContrastSaturation = Shader.PropertyToID("_HueBrightnessContrastSaturation");

    void Start() {
        container.OnFilled.AddListener(OnFilled);
    }

    void OnDestroy() {
        container.OnFilled.RemoveListener(OnFilled);
    }

    IEnumerator GrowRoutine() {
        yield return new WaitForSeconds(30f);
        if (!photonView.IsMine) {
            yield break;
        }
        if (plant.possibleNextGenerations == null || plant.possibleNextGenerations.Length == 0f) {
            PhotonNetwork.Destroy(gameObject);
            yield break;
        }
        photonView.RPC(nameof(GenericReagentContainer.Spill), RpcTarget.All, container.volume);
        photonView.RPC(nameof(SwitchToRPC), RpcTarget.AllBufferedViaServer,
            PlantDatabase.GetID(plant.possibleNextGenerations[Random.Range(0, plant.possibleNextGenerations.Length)]));
    }

    void OnFilled(ReagentContents contents, GenericReagentContainer.InjectType injectType) {
        if (plant.possibleNextGenerations == null || plant.possibleNextGenerations.Length == 0) {
            return;
        }
        foreach(Renderer renderer in display.GetComponentsInChildren<Renderer>()) {
            renderer.material.SetFloat("_BounceAmount", 1f);
            StartCoroutine(DarkenMaterial(renderer.material));
        }
        wateredEffect.SendEvent("Play");
        audioSource.Play();
        effect.gameObject.SetActive(false);
        effect.gameObject.SetActive(true);
        StopCoroutine(nameof(GrowRoutine));
        StartCoroutine(nameof(GrowRoutine));
    }

    [PunRPC]
    void SwitchToRPC(short newPlantID) {
        ScriptablePlant checkPlant = PlantDatabase.GetPlant(newPlantID);
        if (checkPlant == plant) {
            return;
        }
        SwitchTo(checkPlant);
    }

    public override void SetGenes(KoboldGenes newGenes) {
        if (display != null) {
            Vector4 hbcs = new Vector4(newGenes.hue / 255f, newGenes.brightness / 255f, 0.5f, newGenes.saturation / 255f);
            foreach (var r in display.GetComponentsInChildren<Renderer>()) {
                foreach (var material in r.materials) {
                    material.SetColor(BrightnessContrastSaturation, hbcs);
                }
            }
        }
        base.SetGenes(newGenes);
    }

    void SwitchTo(ScriptablePlant newPlant) {
        if (plant == newPlant) {
            return;
        }
        UndarkenMaterials();
        wateredEffect.Stop();
         // Plant == newPlant should always return true for deserialization, skip that step and assert
        if(display != null){
            Destroy(display);
        }
        if(newPlant.display != null){
            display = GameObject.Instantiate(newPlant.display,transform);
            // TODO: This is a hack to make sure future iterations have recieved the genes.
            SetGenes(GetGenes());
        }

        if (photonView.IsMine) {
            foreach (var produce in newPlant.produces) {
                int spawnCount = Random.Range(produce.minProduce, produce.maxProduce);
                for(int i=0;i<spawnCount;i++) {
                    PhotonNetwork.InstantiateRoomObject(produce.prefab.photonName,
                         transform.position + Vector3.up + Random.insideUnitSphere * 0.5f, Quaternion.identity, 0,
                         new object[] { GetGenes(), false });
                }
            }
        }

        switched?.Invoke();
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData[0] is short) {
            SwitchTo(PlantDatabase.GetPlant((short)info.photonView.InstantiationData[0]));
        }

        if (info.photonView.InstantiationData != null && info.photonView.InstantiationData[1] is KoboldGenes) {
            SetGenes((KoboldGenes)info.photonView.InstantiationData[1]);
        } else {
            SetGenes(new KoboldGenes().Randomize());
        }

        PlantSpawnEventHandler.TriggerPlantSpawnEvent(photonView.gameObject, plant);
    }

    void UndarkenMaterials(){
        if (display == null) {
            return;
        }
        foreach(Renderer renderer in display.GetComponentsInChildren<Renderer>()) {
            if (renderer.material.HasProperty("_Color")) {
                renderer.material.SetColor("_Color", Color.white);
            }

            if (renderer.material.HasProperty("_BaseColor")) {
                renderer.material.SetColor("_BaseColor", Color.white);
            }
        }
    }

    IEnumerator DarkenMaterial(Material tgtMat) {
        float startTime = Time.time;
        float duration = 1f;
        while(Time.time < startTime + duration) {
            float t = (Time.time - startTime) / duration;
            if (tgtMat.HasProperty("_Color")) {
                tgtMat.SetColor("_Color", Color.Lerp(tgtMat.GetColor("_Color"), darkenedColor, t));
            }
            if (tgtMat.HasProperty("_BaseColor")) {
                tgtMat.SetColor("_BaseColor", Color.Lerp(tgtMat.GetColor("_BaseColor"), darkenedColor, t));
            }
            yield return null;
        }
    }
    
    public void Save(BinaryWriter writer, string version) {
        writer.Write(PlantDatabase.GetID(plant));
        writer.Write(transform.position.x);
        writer.Write(transform.position.y);
        writer.Write(transform.position.z);
        GetGenes().Serialize(writer);
    }

    public void Load(BinaryReader reader, string version) {
        SwitchTo(PlantDatabase.GetPlant(reader.ReadInt16()));
        float x = reader.ReadSingle();
        float y = reader.ReadSingle();
        float z = reader.ReadSingle();
        transform.position = new Vector3(x,y,z);
        SetGenes(GetGenes().Deserialize(reader));
    }
}
