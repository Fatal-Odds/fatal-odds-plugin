using NUnit.Framework;
using UnityEngine;
using FatalOdds.Runtime;
using System.Collections;
using UnityEngine.TestTools;
using FatalOdds.Editor;

namespace FatalOdds.Tests.Runtime
{
    
    /// Runtime tests for the modifier system
    
    public class ModifierSystemTests

    {
        private GameObject testObject;
        private ModifierManager modifierManager;
        private TestStatsComponent testStats;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestObject");
            modifierManager = testObject.AddComponent<ModifierManager>();
            testStats = testObject.AddComponent<TestStatsComponent>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }
        }

        [Test]
        public void ModifierManager_StartsWithZeroModifiers()
        {
            Assert.AreEqual(0, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ModifierManager_CanAddModifier()
        {
            var modifier = StatModifier.Create("test-guid", ModifierType.Flat, 5f)
                .WithSource("Test")
                .Build();

            modifierManager.AddModifier(modifier);

            Assert.AreEqual(1, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ModifierManager_CanRemoveModifier()
        {
            var modifier = StatModifier.Create("test-guid", ModifierType.Flat, 5f)
                .WithSource("Test")
                .Build();

            modifierManager.AddModifier(modifier);
            Assert.AreEqual(1, modifierManager.ActiveModifierCount);

            modifierManager.RemoveModifier(modifier);
            Assert.AreEqual(0, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ModifierManager_CanRemoveModifiersBySource()
        {
            var modifier1 = StatModifier.Create("test-guid-1", ModifierType.Flat, 5f)
                .WithSource("TestSource")
                .Build();

            var modifier2 = StatModifier.Create("test-guid-2", ModifierType.Flat, 3f)
                .WithSource("TestSource")
                .Build();

            var modifier3 = StatModifier.Create("test-guid-3", ModifierType.Flat, 2f)
                .WithSource("OtherSource")
                .Build();

            modifierManager.AddModifier(modifier1);
            modifierManager.AddModifier(modifier2);
            modifierManager.AddModifier(modifier3);

            Assert.AreEqual(3, modifierManager.ActiveModifierCount);

            modifierManager.RemoveModifiersFromSource("TestSource");

            Assert.AreEqual(1, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ModifierCalculator_CalculatesFlatModifiersCorrectly()
        {
            float baseValue = 10f;
            var modifiers = new[]
            {
                StatModifier.Create("test", ModifierType.Flat, 5f).Build(),
                StatModifier.Create("test", ModifierType.Flat, 3f).Build()
            };

            float result = ModifierCalculator.CalculateFinalValue(baseValue, modifiers);

            Assert.AreEqual(18f, result); // 10 + 5 + 3
        }

        [Test]
        public void ModifierCalculator_CalculatesPercentageModifiersCorrectly()
        {
            float baseValue = 10f;
            var modifiers = new[]
            {
                StatModifier.Create("test", ModifierType.Percentage, 1.5f).Build() // +50%
            };

            float result = ModifierCalculator.CalculateFinalValue(baseValue, modifiers);

            Assert.AreEqual(15f, result); // 10 * 1.5
        }

        [Test]
        public void ModifierCalculator_CalculatesAdditivePercentageCorrectly()
        {
            float baseValue = 10f;
            var modifiers = new[]
            {
                StatModifier.Create("test", ModifierType.PercentageAdditive, 0.2f).Build(), // +20%
                StatModifier.Create("test", ModifierType.PercentageAdditive, 0.3f).Build()  // +30%
            };

            float result = ModifierCalculator.CalculateFinalValue(baseValue, modifiers);

            Assert.AreEqual(15f, result); // 10 * (1 + 0.2 + 0.3) = 10 * 1.5
        }

        [UnityTest]
        public IEnumerator ModifierManager_HandlesTemporaryModifiersCorrectly()
        {
            var temporaryModifier = StatModifier.Create("test-guid", ModifierType.Flat, 5f)
                .WithSource("TemporaryTest")
                .WithDuration(0.1f) // 0.1 second duration
                .Build();

            modifierManager.AddModifier(temporaryModifier);
            Assert.AreEqual(1, modifierManager.ActiveModifierCount);

            // Wait for modifier to expire
            yield return new WaitForSeconds(0.2f);

            Assert.AreEqual(0, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ItemDefinition_AppliesModifiersCorrectly()
        {
            var itemDef = ScriptableObject.CreateInstance<ItemDefinition>();
            itemDef.itemName = "Test Item";
            itemDef.modifiers = new System.Collections.Generic.List<StatModifierData>
            {
                new StatModifierData("test-stat", ModifierType.Flat, 5f, "Test Stat")
            };

            itemDef.ApplyToTarget(testObject, 1);

            Assert.AreEqual(1, modifierManager.ActiveModifierCount);
        }

        [Test]
        public void ItemStackingBehavior_LinearStackingWorks()
        {
            var itemData = new ItemCreationData
            {
                itemName = "Stacking Item",
                stacksAdditively = true,
                modifiers = new System.Collections.Generic.List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        modifierType = ModifierType.Flat,
                        value = 10f
                    }
                }
            };

            // Test linear stacking calculation
            float singleStack = itemData.GetEffectiveModifierValue(itemData.modifiers[0], 1);
            float tripleStack = itemData.GetEffectiveModifierValue(itemData.modifiers[0], 3);

            Assert.AreEqual(10f, singleStack);
            Assert.AreEqual(30f, tripleStack); // Linear: 10 * 3
        }

        [Test]
        public void ItemStackingBehavior_DiminishingReturnsWorks()
        {
            var itemData = new ItemCreationData
            {
                itemName = "Diminishing Item",
                stacksAdditively = false, // Diminishing returns
                modifiers = new System.Collections.Generic.List<ModifierCreationData>
                {
                    new ModifierCreationData
                    {
                        modifierType = ModifierType.Flat,
                        value = 10f
                    }
                }
            };

            // Test diminishing returns calculation
            float singleStack = itemData.GetEffectiveModifierValue(itemData.modifiers[0], 1);
            float tripleStack = itemData.GetEffectiveModifierValue(itemData.modifiers[0], 3);

            Assert.AreEqual(10f, singleStack);
            Assert.AreEqual(20f, tripleStack); // Diminishing: 10 + (10 * 0.5 * 2)
        }
    }

    
    /// Test component for runtime testing
    
    public class TestStatsComponent : MonoBehaviour
    {
        [StatTag("Test Health", "Combat")]
        public float health = 100f;

        [StatTag("Test Speed", "Movement")]
        public float speed = 5f;

        [StatTag("Test Damage", "Combat")]
        public float damage = 10f;
    }
}