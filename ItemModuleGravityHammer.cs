using ThunderRoad;

namespace FisherGravityHammer
{
    public class ItemModuleGravityHammer : ItemModule
    {
        // Physics/Behaviour settings
        public float maxGravityDamage = 100f;
        public float baseForceModifier = 20f;
        public float effectRadius = 15f;
        public float liftModifier = 2f;
        public float impulseForceModifier = 4f;
        public bool enableDismemberment = true;
        public float dismemberRadius = 3f;
        public bool userIsEffected = true;
        public float userEffectMultiplier = 10f;
        public bool onlyActivateOnRagdolls = false;
        public float minimumVelocityToActivate = 0.3f;
        public float chargeUpTime = 1.5f;
        public float timeToRecharge = 0.75f;
        public float readyHumVolume = 0.5f;

        // Shader control settings
        public float materialEmission = 0.0001f;
        public float powerupEmissionOffset = 0f;
        public float glowDefaultIntensity = 0f;
        public float glowDefaultRange = 0f;
        public float glowActivationIntensity = 0.01f;
        public float glowActivationRange = 10f;
        public int[] enabledRGB = { 0, 100, 200 };
        public int[] disabledRGB = {75, 0, 0};
        public float disabledEmissionValue = 0f;
        public float emissionDelay = 0.005f;

        // Custom Shader references
        public string customShaderEmissionIntensityRef = "Vector1_27BFB28D";
        public string customShaderColorRef = "Color_13D2CF13";

        // Item definition references
        public string AudioChargeUpRef = "ChargeUp";
        public string AudioChargeDownRef = "ChargeDown";
        public string AudioWeaponReadyRef = "ChargeReady";
        public string AudioChargeReleaseRef = "ChargeRelease";
        public string GravityMatPBRRef = "GravityPBR";
        public string PL1Ref = "PL1";

        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemGravityHammer>();
        }
    }
}
