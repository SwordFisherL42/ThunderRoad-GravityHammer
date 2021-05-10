using UnityEngine;
using System.Collections;
using ThunderRoad;
using System;

namespace FisherGravityHammer
{
    public class ItemGravityHammer : MonoBehaviour
    {
        //Unity Vars
        protected Item item;
        protected ItemModuleGravityHammer module;
        protected AudioSource AudioChargeUp;
        protected AudioSource AudioChargeDown;
        protected AudioSource AudioWeaponReady;
        protected AudioSource AudioChargeRelease;
        protected Material GravityMatPBR;
        protected Light PL1;
        protected Color enabledColor;
        protected Color disabledColor;

        //Module Paramaters
        protected float baseForce;
        protected float impulseModifer;
        protected float liftModifier;
        protected float effectRadius;
        protected float dismemberRadius;
        protected float maxDamage;
        protected float chargeUpTime;
        protected float minimumVelocityToActivate;
        protected bool userEffected;
        protected float userModifier;
        protected bool onlyActivateOnRagdolls;
        protected float PBR_EMISSION;
        protected float PBR_EMISSION_OFFSET;
        protected float PL_DEFAULT_RANGE;
        protected float PL_DEFAULT_INTENSITY;
        protected float PL_FIRING_RANGE;
        protected float PL_FIRING_INTENSITY;
        protected float TIME_TO_RETURN;
        protected float disabledEmissionValue;
        protected float emissionDelay;
        protected float readyHumVolume;
        protected float[] thrustFlicker;

        //Internal Logic Vars
        protected bool isPoweringUp;
        protected bool isReadyToUse;
        protected bool isDisabled;
        protected bool hammerJustHit;
        protected float timeSinceActivation;
        protected float emissionChargeRate;
        protected float intensityReturnRate;
        protected float rangeReturnRate;
        protected bool itemInLeftHand;
        protected bool itemInRightHand;
        protected bool itemAlreadyHeld;
        protected float enabledEmission;
        protected float disabledEmission;
        protected float currentEmission;
        protected bool chargeDownExecuting;
        protected float thrusterIntensity;

        //External Object Vars
        protected Rigidbody nearbyRigidBody;

        protected void Awake()
        {
            item = this.GetComponent<Item>();
            module = item.data.GetModule<ItemModuleGravityHammer>();
            //Get parameters from module
            baseForce = module.baseForceModifier;// * 1000.0f;
            impulseModifer = module.impulseForceModifier;
            liftModifier = module.liftModifier;
            effectRadius = module.effectRadius;
            dismemberRadius = module.dismemberRadius;
            maxDamage = module.maxGravityDamage;
            chargeUpTime = module.chargeUpTime;
            userEffected = module.userIsEffected;
            userModifier = module.userEffectMultiplier;
            onlyActivateOnRagdolls = module.onlyActivateOnRagdolls;
            minimumVelocityToActivate = module.minimumVelocityToActivate;
            PL_DEFAULT_INTENSITY = module.glowDefaultIntensity;
            PL_DEFAULT_RANGE = module.glowDefaultRange;
            PL_FIRING_INTENSITY = module.glowActivationIntensity;
            PL_FIRING_RANGE = module.glowActivationRange;
            TIME_TO_RETURN = module.timeToRecharge;
            PBR_EMISSION = module.materialEmission;
            PBR_EMISSION_OFFSET = module.powerupEmissionOffset;
            disabledEmissionValue = module.disabledEmissionValue;
            emissionDelay = module.emissionDelay;
            readyHumVolume = module.readyHumVolume;
            try { enabledColor = new Color(module.enabledRGB[0], module.enabledRGB[1], module.enabledRGB[2]); }
            catch { enabledColor = GravityMatPBR.GetColor(module.customShaderColorRef); }
            try { disabledColor = new Color(module.disabledRGB[0], module.disabledRGB[1], module.disabledRGB[2]); }
            catch { disabledColor = new Color(90, 0, 5); }

            //Get Custom References from item prefab
            //TO-DO: Looking into using a Shepard-Tone here, which would allow a constant chargeup/chargedown.
            AudioChargeUp = item.GetCustomReference(module.AudioChargeUpRef).GetComponent<AudioSource>();
            AudioChargeDown = item.GetCustomReference(module.AudioChargeDownRef).GetComponent<AudioSource>();
            AudioWeaponReady = item.GetCustomReference(module.AudioWeaponReadyRef).GetComponent<AudioSource>();
            AudioChargeRelease = item.GetCustomReference(module.AudioChargeReleaseRef).GetComponent<AudioSource>();
            GravityMatPBR = item.GetCustomReference(module.GravityMatPBRRef).GetComponent<Renderer>().material;
            PL1 = item.GetCustomReference(module.PL1Ref).GetComponent<Light>();

            //Set Default values
            //Sounds
            AudioWeaponReady.volume = readyHumVolume;

            //Point Light
            PL1.intensity = PL_DEFAULT_INTENSITY;
            PL1.range = PL_DEFAULT_RANGE;
            PL1.color = enabledColor;
            intensityReturnRate = Mathf.Abs((PL_FIRING_INTENSITY - PL_DEFAULT_INTENSITY) / (TIME_TO_RETURN));
            rangeReturnRate = Mathf.Abs((PL_FIRING_RANGE - PL_DEFAULT_RANGE) / (TIME_TO_RETURN));

            //PBR Emission Properties
            enabledEmission = PBR_EMISSION;
            disabledEmission = disabledEmissionValue;
            GravityMatPBR.SetColor(module.customShaderColorRef, enabledColor);
            GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, PBR_EMISSION);
            emissionChargeRate = (PBR_EMISSION_OFFSET) / (chargeUpTime);

            //Weapon Flags/Parameters
            ResetWeapon();
            itemInLeftHand = false;
            itemInRightHand = false;
            isDisabled = false;

            //Setup Item events
            item.OnGrabEvent += OnGrabEvent;
            item.OnUngrabEvent += OnUngrabEvent;
            item.OnHeldActionEvent += OnHeldAction;
        }

        protected void LateUpdate()
        {
            if (!PL1.enabled) PL1.enabled = true;

            if ((isDisabled) || (!itemInRightHand && !itemInLeftHand) || (isReadyToUse)) return;

            if (!isReadyToUse && !isPoweringUp && !hammerJustHit) PowerUp();

            if (hammerJustHit)
            {
                //Slowly Fade Activation-Light Intensity/Range back to defaults
                if (PL1.intensity > PL_DEFAULT_INTENSITY) PL1.intensity -= intensityReturnRate * Time.deltaTime;
                if (PL1.range > PL_DEFAULT_RANGE) PL1.range -= rangeReturnRate * Time.deltaTime;
                //If we are really close to the defaults, or we overshot them, clamp back to defaults.
                if (PL1.intensity <= PL_DEFAULT_INTENSITY) PL1.intensity = PL_DEFAULT_INTENSITY;
                if (PL1.range <= PL_DEFAULT_RANGE) PL1.range = PL_DEFAULT_RANGE;
                //When we get back to defaults, allow hammer to power up again.
                if ((PL1.intensity == PL_DEFAULT_INTENSITY) && (PL1.range == PL_DEFAULT_RANGE))
                {
                    hammerJustHit = false;
                    return;
                }
                return;
            }

            if (isPoweringUp)
            {
                if (GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) < (PBR_EMISSION + PBR_EMISSION_OFFSET))
                {
                    GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) + (emissionChargeRate * Time.deltaTime));
                }
                if (timeSinceActivation < chargeUpTime) timeSinceActivation += Time.deltaTime;
            }

            if (isPoweringUp && !isReadyToUse && (timeSinceActivation >= chargeUpTime) && (GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) >= (PBR_EMISSION + PBR_EMISSION_OFFSET)))
            {
                isReadyToUse = true;
                isPoweringUp = false;
                AudioWeaponReady.Play();
            }
        }

        public void ResetWeapon()
        {
            //Debug.Log("[DEBUG][Gravity Hammer] Weapon flags reset");
            isReadyToUse = false;
            isPoweringUp = false;
            timeSinceActivation = 0.0f;
        }

        public void ToggleWeapon()
        {
            isDisabled = !isDisabled;
            //Debug.Log("[DEBUG][Gravity Hammer] Weapon Toggled, isDisabled: " + isDisabled);
            //Toggled effects/sounds
            if (isDisabled)
            {
                GravityMatPBR.SetColor(module.customShaderColorRef, disabledColor);
                PL1.intensity = 0.0f;
                GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, disabledEmissionValue);
                PowerDown();
            }

            else
            {
                GravityMatPBR.SetColor(module.customShaderColorRef, enabledColor);
                PL1.intensity = PL_DEFAULT_INTENSITY;
                GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, PBR_EMISSION);
                PowerUp();
            }
        }

        public void PowerUp()
        {
            if (isDisabled || isPoweringUp) return;
            GravityMatPBR.SetColor(module.customShaderColorRef, enabledColor);
            AudioChargeUp.Play();
            ResetWeapon();
            isPoweringUp = true;
            //Debug.Log("[DEBUG][Gravity Hammer] Powering Up...");
        }

        public void PowerDown()
        {
            //Debug.Log("[DEBUG][Gravity Hammer] Powering Down...");
            AudioChargeDown.Play();
            AudioWeaponReady.Stop();
            ResetWeapon();
            //StartCoroutine(ChargeDownDelay(emissionDelay));

        }

        public void GravityBurst(float force, float blastRadius, float dismemberRadius, float liftMult)
        {
            hammerJustHit = true;
            GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, PBR_EMISSION - PBR_EMISSION_OFFSET);
            PL1.intensity = PL_FIRING_INTENSITY;
            PL1.range = PL_FIRING_RANGE;

            // TO-DO: Play Particle effect //
            AudioWeaponReady.Stop();
            AudioChargeRelease.Play();
            //Debug.Log("[DEBUG][Gravity Hammer] GravityBurst Activated !");
            //Debug.Log("[DEBUG][Gravity Hammer] userEffected by Gravity Burst: " + userEffected);
            if (userEffected)
            {
                Player.local.locomotion.rb.AddExplosionForce(impulseModifer * force * userModifier * Player.local.locomotion.rb.mass, item.transform.position, blastRadius, liftMult * Player.local.locomotion.rb.mass);
                Player.local.locomotion.rb.AddForce(Vector3.up * force * userModifier * Player.local.locomotion.rb.mass);
                Player.local.locomotion.rb.AddForce(-1.0f * Player.local.locomotion.rb.transform.forward * force * userModifier * Player.local.locomotion.rb.mass);
            }

            try
            {
                Vector3 origin = item.transform.position;
                foreach (Item i in Item.list)
                {
                    if (Math.Abs(Vector3.Distance(i.transform.position, origin)) <= blastRadius)
                    {
                        if (item.name.Contains(i.name)) continue;
                        //Debug.Log("[DEBUG][Gravity Hammer] Hit Item: " + item.name);
                        i.rb.AddExplosionForce(force * i.rb.mass, origin, blastRadius, liftMult, ForceMode.Impulse);
                        i.rb.AddForce(Vector3.up * liftMult * i.rb.mass, ForceMode.Impulse);
                    }
                }

                foreach (Creature creature in Creature.list)
                {
                    if (creature == Player.currentCreature) continue;
                    float creatureDistance = Math.Abs(Vector3.Distance(creature.transform.position, origin));
                    if (creatureDistance <= blastRadius)
                    {
                        // Damage Creatures in Range
                        //Debug.Log("[DEBUG][Gravity Hammer] Hit Creature: " + creature.name);
                        if (!creature.isKilled)
                        {
                            // Linearly decrease AOE damage over distance
                            // float gravityDamage = maxDamage - ((maxDamage / blastRadius) * creatureDistance);
                            creature.Damage(new CollisionInstance(new DamageStruct(DamageType.Blunt, maxDamage - ((maxDamage / blastRadius) * creatureDistance)), (MaterialData)null, (MaterialData)null));
                            // Debug.Log("[DEBUG][Gravity Hammer] Damaging Creature: " + gravityDamage);
                        }
                        // Apply Forces to Creature Main Body
                        creature.locomotion.rb.AddExplosionForce(force * creature.locomotion.rb.mass, origin, blastRadius, liftMult, ForceMode.Impulse);
                        creature.locomotion.rb.AddForce(Vector3.up * liftMult * creature.locomotion.rb.mass, ForceMode.Impulse);

                        //// Dismember Creature Parts
                        if (module.enableDismemberment && (creatureDistance <= dismemberRadius)) { 
                            creature.ragdoll.headPart.Slice();
                            creature.ragdoll.GetPart(RagdollPart.Type.LeftLeg).Slice();
                            creature.ragdoll.GetPart(RagdollPart.Type.RightLeg).Slice();
                            creature.ragdoll.GetPart(RagdollPart.Type.RightArm).Slice();
                            creature.ragdoll.GetPart(RagdollPart.Type.LeftArm).Slice();
                        }

                        // Apply Forces to Creature Parts
                        foreach (RagdollPart part in creature.ragdoll.parts)
                        {
                            //Debug.Log("[DEBUG][Gravity Hammer] Appyling Force to RD-part " + part.name);
                            part.rb.AddExplosionForce(force * part.rb.mass, origin, blastRadius, liftMult, ForceMode.Impulse);
                            part.rb.AddForce(Vector3.up * liftMult * part.rb.mass, ForceMode.Impulse);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[DEBUG][Gravity Hammer][EXCEPTION] " + e.Message + " \n " + e.StackTrace);
            }

            PowerUp();
        }

        public void OnGrabEvent(Handle handle, RagdollHand interactor)
        {
            itemAlreadyHeld = (itemInRightHand || itemInLeftHand);
            if (interactor.playerHand == Player.local.handRight) itemInRightHand = true;
            if (interactor.playerHand == Player.local.handLeft) itemInLeftHand = true;
            if (!itemAlreadyHeld && (itemInRightHand || itemInLeftHand)) PowerUp();
        }

        public void OnUngrabEvent(Handle handle, RagdollHand interactor, bool throwing)
        {
            if (interactor.playerHand == Player.local.handRight) itemInRightHand = false;
            if (interactor.playerHand == Player.local.handLeft) itemInLeftHand = false;
            if (!itemInRightHand && !itemInLeftHand)
            {
                if (!isDisabled) PowerDown();
            }
        }

        public void OnHeldAction(RagdollHand interactor, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart) ToggleWeapon();
            if (action == Interactable.Action.Grab)
            {
                if (interactor.playerHand == Player.local.handRight) itemInRightHand = true;
                if (interactor.playerHand == Player.local.handLeft) itemInLeftHand = true;
            }

            if (action == Interactable.Action.Ungrab)
            {
                if (interactor.playerHand == Player.local.handRight) itemInRightHand = false;
                if (interactor.playerHand == Player.local.handLeft) itemInLeftHand = false;
            }
        }

        protected void OnTriggerEnter(Collider hit)
        {
            if (isDisabled || (!itemInRightHand && !itemInLeftHand)) { return; }

            var gravityTriggerVelocity = item.rb.velocity.magnitude;
            //Debug.Log("[DEBUG][Gravity Hammer] Velocity magnitude: " + gravityTriggerVelocity);
            try
            {
                if (gravityTriggerVelocity < minimumVelocityToActivate)
                {
                    //Debug.Log("[DEBUG][Gravity Hammer]Gravity Burst not activated: Did not meet minimum Trigger Velocity-->" + minimumVelocityToActivate);
                    return;
                }
                if (!isReadyToUse)
                {
                    //Debug.Log("[DEBUG][Gravity Hammer] Gravity Burst not activated: Weapon is not ready to use yet.");
                    return;
                }

                if (hit.gameObject.tag.Contains("Player")
                    || hit.gameObject.name.Contains("Hand")
                    || hit.gameObject.name.Contains("HipsLeft")
                    || hit.gameObject.name.Contains("HipsRight")
                    || hit.gameObject.name.Contains("BackLeft")
                    || hit.gameObject.name.Contains("BackRight")
                    || hit.gameObject.name.Contains("Default")
                    || hit.gameObject.name.Contains("Nav")
                    || hit.gameObject.name.Contains("Zone")
                    || hit.gameObject.name.Contains("Touch"))
                {
                    //Debug.Log("[DEBUG][Gravity Hammer] Gravity Trigger aborted via rejection criteria.");
                    return;
                }

                // Debug.Log("[DEBUG][Gravity Hammer] GravityTrigger from " + item.name + " activated on " + hit.name + " with tag " + hit.gameObject.tag);
                var hitPart = hit.gameObject.GetComponent<RagdollPart>();
                // Ragdoll Gravity Hammer Activation
                if (hitPart != null)
                {
                    Creature triggerCreature = hitPart.ragdoll.creature;
                    if (triggerCreature == Player.currentCreature) return;
                    //Debug.Log("[DEBUG][Gravity Hammer] Gravity Burst Ragdoll Activate: " + hitPart.name);
                    GravityBurst(baseForce, effectRadius, dismemberRadius, liftModifier);
                    return;
                }
                // World object Gravity Hammer Activation, escape if onlyActivateOnRagdolls is true
                else
                {
                    if (onlyActivateOnRagdolls) return;
                    //Debug.Log("[DEBUG][Gravity Hammer] Gravity Burst OTHER Activate: " + hit.name + " " + hit.gameObject.name);
                    Creature hitRootCreature = hit.gameObject.transform.root.GetComponentInChildren<Creature>();
                    if (hitRootCreature != null) {
                        //Debug.Log("[DEBUG][Gravity Hammer] Gravity Burst OTHER Activate contains creature... ");
                        if (hitRootCreature == Player.currentCreature) return;
                    }
                    GravityBurst(baseForce, effectRadius, dismemberRadius, liftModifier);
                }
            }
            catch { Debug.Log("[ERROR][Gravity Hammer] Bad or Unhandled GravityTrigger event."); }
            
        }

        IEnumerator ChargeDownDelay(float dTime)
        {
            //Debug.Log("Started ChargeDownDelay at timestamp : " + Time.time);
            chargeDownExecuting = true;

            while (GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) > (PBR_EMISSION - PBR_EMISSION_OFFSET))
            {
                GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, (GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) - (emissionChargeRate * Time.deltaTime)));
                //Debug.Log("[DEBUG][Gravity Hammer] Time: " + Time.time + " Charging Down, emission: " + GravityMatPBR.GetFloat(module.pbrEmissionIntensity));
                yield return new WaitForSeconds(dTime);
            }
            chargeDownExecuting = false;
            //Debug.Log("Finished ChargeDownDelay at timestamp : " + Time.time);
        }

        IEnumerator DisabledEmissionDelay(float dTime)
        {
            //Debug.Log("Entered DisabledEmissionDelay at timestamp : " + Time.time);
            while (chargeDownExecuting)
            {
                yield return null;
            }
            //Debug.Log("Started DisabledEmissionDelay at timestamp : " + Time.time);
            while (GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) > disabledEmission)
            {
                GravityMatPBR.SetFloat(module.customShaderEmissionIntensityRef, GravityMatPBR.GetFloat(module.customShaderEmissionIntensityRef) - (emissionChargeRate * Time.deltaTime));
                //Debug.Log("[DEBUG][Gravity Hammer] Time: " + Time.time + " Disabling, emission: " + GravityMatPBR.GetFloat(module.pbrEmissionIntensity));
                yield return new WaitForSeconds(dTime);
            }
            PL1.intensity = 0.0f;
            PL1.range = 0.0f;
            GravityMatPBR.SetColor(module.customShaderColorRef, disabledColor);
            //Debug.Log("Finished DisabledEmissionDelay at timestamp : " + Time.time);
        }

    }
}
