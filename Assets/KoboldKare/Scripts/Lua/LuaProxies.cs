using MoonSharp.Interpreter;
using UnityEngine;

namespace LuaProxies
{
    class KoboldProxy
    {
        public readonly Kobold target;
        [MoonSharpHidden]
        public KoboldProxy(Kobold k) { target = k; }
        
        public float GetGene(string gene)
        {
            KoboldGenes g = target.GetGenes();
            switch (gene)
            {
                case "baseSize": return g.baseSize;
                case "energy": return g.maxEnergy;
                case "fat": return g.fatSize;
                case "ballSize": return g.ballSize;
                case "dickSize": return g.dickSize;
                case "breastSize": return g.breastSize;
                case "bellySize": return g.bellySize;
                case "metabolism": return g.metabolizeCapacitySize;
                case "dickThickness": return g.dickThickness;
                case "hue": return g.hue / 255f;
                case "saturation": return g.saturation / 255f;
                case "brightness": return g.brightness / 255f;
                default: return -1;
            }
        }
        public void SetGene(string gene, float value)
        {
            KoboldGenes g = target.GetGenes();
            switch (gene)
            {
                case "baseSize": g.baseSize = value; break;
                case "energy": g.maxEnergy = value; break;
                case "fat": g.fatSize = value; break;
                case "ballSize": g.ballSize = value; break;
                case "dickSize": g.dickSize = value; break;
                case "breastSize": g.breastSize = value; break;
                case "bellySize": g.bellySize = value; break;
                case "metabolism": g.metabolizeCapacitySize = value; break;
                case "dickThickness": g.dickThickness = value; break;
                case "hue": g.hue = (byte)Mathf.RoundToInt(Mathf.Clamp(value, 0, 255)); break;
                case "saturation": g.saturation = (byte)Mathf.RoundToInt(Mathf.Clamp(value, 0, 255)); break;
                case "brightness": g.brightness = (byte)Mathf.RoundToInt(Mathf.Clamp(value, 0, 255)); break;
            }
            target.SetGenes(g);
        }
        public float GetStat(string stat)
        {
            KoboldCharacterController k = target.controller;
            if (!k)
                return -1;
            switch (stat)
            {
                case "jumpStrength": return k.jumpStrength;
                case "speed": return k.speed;
                case "crouchSpeed": return k.crouchSpeed;
                case "walkSpeedMultiplier": return k.walkSpeedMultiplier;
                case "accel": return k.accel;
                case "crouchAccel": return k.crouchAccel;
                case "airAccel": return k.airAccel;
                case "friction": return k.friction;
                case "crouchFriction": return k.crouchFriction;
                default: return -1;
            }
        }
        public void SetStat(string stat, float value)
        {
            KoboldCharacterController k = target.controller;
            if (!k)
                return;
            switch (stat)
            {
                case "jumpStrength": k.jumpStrength = value; break;
                case "speed": k.speed = value; break;
                case "crouchSpeed": k.crouchSpeed = value; break;
                case "walkSpeedMultiplier": k.walkSpeedMultiplier = value; break;
                case "accel": k.accel = value; break;
                case "crouchAccel": k.crouchAccel = value; break;
                case "airAccel": k.airAccel = value; break;
                case "friction": k.friction = value; break;
                case "crouchFriction": k.crouchFriction = value; break;
            }
        }
        public Vector3 GetGravityMod()
        {
            if (target.controller)
                return target.controller.gravityMod;
            return -Vector3.one;
        }
        public void SetGravityMod(float x = float.NaN, float y = float.NaN, float z = float.NaN)
        {
            KoboldCharacterController k = target.controller;
            if (!k)
                return;
            Vector3 newGrav = new Vector3();
            newGrav.x = float.IsNaN(x) ? k.gravityMod.x : x;
            newGrav.y = float.IsNaN(y) ? k.gravityMod.y : y;
            newGrav.z = float.IsNaN(z) ? k.gravityMod.z : z;
            k.gravityMod = newGrav;
        }

        public void SetGravityModVector(Vector3 direction)
        {
            KoboldCharacterController k = target.controller;
            if (!k)
                return;
            k.gravityMod = direction;
        }
        
        public void Cum() { target.Cum(); }
        public bool Ragdolling() { return target.GetRagdoller().ragdolled; }
        public void SetShapeKey(string shape, int value)
        {
            CharacterDescriptor desc = target.GetComponent<CharacterDescriptor>();
            foreach (SkinnedMeshRenderer render in desc.bodyRenderers)
            {
                Mesh m = render.sharedMesh;
                int index = m.GetBlendShapeIndex(shape);
                if(index == -1) continue;
                render.SetBlendShapeWeight(index, value);
            }
        }
    }

    class GObjectProxy
    {
        public readonly GameObject target;
        public Script script;
        [MoonSharpHidden] public GObjectProxy(GameObject g) 
        { target = g; }

        public void LookAt(Table viewTarget)
        {
            GObjectProxy g = (GObjectProxy)viewTarget["object"];
            target.transform.LookAt(g.target.gameObject.transform);
        }

        public Vector3 GetDirection(string dir)
        {
            Transform t = target.transform;
            switch (dir.ToLower())
            {
                case "forward": return t.forward;
                case "backward": return -t.forward;
                case "left": return -t.right;
                case "right": return t.right;
                case "up": return t.up;
                case "down": return -t.up;
                default: return Vector3.zero;
            }
        }

        public Table GetScale()
        {
            Vector3 s = target.transform.localScale;
            Table scale = DynValue.NewTable(script).Table;
            scale["x"] = s.x;
            scale["y"] = s.y;
            scale["z"] = s.z;
            return scale;
        }
        public Vector3 GetScaleAsVector() { return target.transform.localScale; }
        
        public Table GetRotation()
        {
            Vector3 s = target.transform.rotation.eulerAngles;
            Table scale = DynValue.NewTable(script).Table;
            scale["x"] = s.x;
            scale["y"] = s.y;
            scale["z"] = s.z;
            return scale;
        }
        public Vector3 GetRotationAsVector() { return target.transform.rotation.eulerAngles; }

        public Table GetPosition()
        {
            Vector3 s = target.transform.position;
            Table scale = DynValue.NewTable(script).Table;
            scale["x"] = s.x;
            scale["y"] = s.y;
            scale["z"] = s.z;
            return scale;
        }
        public Vector3 GetPositionAsVector() { return target.transform.position; }

        public Table FindChildByName(string name)
        {
            GameObject child = target.transform.Find(name)?.gameObject;
            Table existing = LuaManager.FindExistingGO(child, script);
            if(existing == null)
                return LuaManager.CreateNewGOProxy(target, script);
            return existing;
        }

        public void SetPosition(float x, float y, float z) { target.transform.position = new Vector3(x, y, z); }
        public void SetRotation(float x, float y, float z) { target.transform.rotation = Quaternion.Euler(x, y, z); }
        public void SetScale(float x, float y, float z) { target.transform.localScale = new Vector3(x, y, z); }
    }

    class AnimatorProxy
    {
        readonly Animator target;
        [MoonSharpHidden] public AnimatorProxy(Animator a)
        { target = a; }
        
        public void Play(string name){ target.Play(name); }
        public void SetBool(string name, bool value) { target.SetBool(name, value); }
        public void SetInt(string name, int value) { target.SetInteger(name, value); }
        public void SetFloat(string name, float value) { target.SetFloat(name, value); }
    }

    class AudioProxy
    {
        readonly AudioSource target;
        [MoonSharpHidden] public AudioProxy(AudioSource a)
        { target = a; }
        
        public void Play() { target.Play(); }
        public void Stop() { target.Stop(); }
        public void Pause() { target.Pause(); }
        public void SetVolume(float volume) { target.volume = volume; }
        public float GetTime() { return target.time; }
        public int GetSamples() { return target.timeSamples; }
    }

    class RBProxy
    {
        readonly Rigidbody target;
        public Script script;
        [MoonSharpHidden] public RBProxy(Rigidbody a)
        { target = a; }

        public void Thrust(float x, float y, float z, string mode = "VelocityChange") { Vector3 force = new Vector3(x, y, z); ThrustAsVector(force, mode); }
        public void ThrustAsVector(Vector3 direction, string mode = "VelocityChange")
        {
            switch (mode.ToLower())
            {
                case "acceleration":
                    target.AddForce(direction, ForceMode.Acceleration);
                    break;
                case "force":
                    target.AddForce(direction, ForceMode.Force);
                    break;
                case "impulse":
                    target.AddForce(direction, ForceMode.Impulse);
                    break;
                default:
                    target.AddForce(direction, ForceMode.VelocityChange);
                    break;
            }
        }

        public bool GetGravity() { return target.useGravity; }
        public void SetGravity(bool grav) { target.useGravity = grav; }
        public Table GetVelocity()
        {
            Vector3 velVector = target.velocity;
            Table vel = DynValue.NewTable(script).Table;
            vel["x"] = velVector.x;
            vel["y"] = velVector.y;
            vel["z"] = velVector.z;
            return vel;
        }
        public Vector3 GetVelocityAsVector() { return target.velocity; }
        public float GetVelocityMagnitude() { return target.velocity.magnitude; }
        public float Drag(float setDrag = 0.01f)
        {
            if (setDrag < 0.1f)
                return target.drag;
            target.drag = setDrag;
            return target.drag;
        }
        public float AngularDrag(float setDrag = 0)
        {
            if (setDrag < 0.01f)
                return target.angularDrag;
            target.angularDrag = setDrag;
            return target.angularDrag;
        }
        public float Mass(float setMass = 0)
        {
            if (setMass < 0.01f)
                return target.mass;
            target.mass = setMass;
            return target.mass;
        }

        public void AddTorque(float x, float y, float z, string mode = "VelocityChange") { Vector3 torque = new Vector3(x, y, z); AddTorqueAsVector(torque, mode); }
        public void AddTorqueAsVector(Vector3 direction, string mode = "VelocityChange")
        {
            switch (mode.ToLower())
            {
                case "acceleration":
                    target.AddTorque(direction, ForceMode.Acceleration);
                    break;
                case "force":
                    target.AddTorque(direction, ForceMode.Force);
                    break;
                case "impulse":
                    target.AddTorque(direction, ForceMode.Impulse);
                    break;
                default:
                    target.AddTorque(direction, ForceMode.VelocityChange);
                    break;
            }
        }
    }

    class RectProxy
    {
        readonly RectTransform target;
        public Script script;
        [MoonSharpHidden] public RectProxy(RectTransform r)
        { target = r; }

        public Table GetPosition()
        {
            Vector2 rect = target.anchoredPosition;
            Table pos = DynValue.NewTable(script).Table;
            pos["x"] = rect.x;
            pos["y"] = rect.y;
            return pos;
        }
        public Vector2 GetPositionAsVector() { return target.anchoredPosition; }
        
        public void SetPosition(float x = float.NaN, float y = float.NaN)
        {
            Vector2 rect = target.anchoredPosition;
            if(!float.IsNaN(x))
                rect.x = x;
            if(!float.IsNaN(y))
                rect.y = y;
            target.anchoredPosition = rect;
        }
        public void SetPositionAsVector(Vector2 pos) { target.anchoredPosition = pos; }
        
        public Table GetRotation()
        {
            Vector3 rect = target.localRotation.eulerAngles;
            Table rot = DynValue.NewTable(script).Table;
            rot["x"] = rect.x;
            rot["y"] = rect.y;
            rot["z"] = rect.z;
            return rot;
        }
        public Vector3 GetRotationAsVector() { return target.localRotation.eulerAngles; }
        
        public void SetRotation(float x = float.NaN, float y = float.NaN, float z = float.NaN)
        {
            Vector3 rot = target.localRotation.eulerAngles;
            if(!float.IsNaN(x))
                rot.x = x;
            if(!float.IsNaN(y))
                rot.y = y;
            if(!float.IsNaN(z))
                rot.z = z;
            target.localRotation = Quaternion.Euler(rot);
        }
        public void SetRotationAsVector(Vector3 rot) { target.localRotation = Quaternion.Euler(rot); }

        public Table GetScale()
        {
            Vector3 rect = target.localScale;
            Table scale = DynValue.NewTable(script).Table;
            scale["x"] = rect.x;
            scale["y"] = rect.y;
            scale["z"] = rect.z;
            return scale;
        }
        public Vector3 GetScaleAsVector() { return target.localScale; }
        
        public void SetScale(float x = float.NaN, float y = float.NaN, float z = float.NaN)
        {
            Vector3 scale = target.localScale;
            if(!float.IsNaN(x))
                scale.x = x;
            if(!float.IsNaN(y))
                scale.y = y;
            if(!float.IsNaN(z))
                scale.z = z;
            target.localScale = scale;
        }
        public void SetScaleAsVector(Vector3 rot) { target.localScale = rot; }
    }
}
