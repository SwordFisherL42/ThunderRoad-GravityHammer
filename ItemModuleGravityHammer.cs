using BS;

namespace FisherGravityHammer
{
    public class ItemModuleGravityHammer : ItemModule
    {
        public float readyHumVolume = 0.5f;
        public float minimumVelocityToActivate = 0.3f;
        public bool onlyActivateOnRagdolls = false;
        public bool userIsEffected = true;
        public float userEffectMultiplier = 0.1f;
        public float effectRadius = 5f;
        public float liftModifier = 10f;
        public float chargeUpTime = 1.5f;
        public float timeToReturn = 0.75f;
        public float baseForceModifier = 3.0f;
        public float impulseForceModifier = 3.0f;
        public float materialEmission = 0.0001f;
        public float powerupEmissionOffset = 0.0f;
        public float glowDefaultIntensity = 0.00f;
        public float glowDefaultRange = 0.00f;
        public float glowActivationIntensity = 0.01f;
        public float glowActivationRange = 10.0f;
        public int[] enabledRGB = { 0, 100, 200 };
        public int[] disabledRGB = {75, 0, 0};
        public float disabledEmissionValue = 0.00f;
        public float emissionDelay = 0.005f;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<ItemGravityHammer>();
        }
    }
}
