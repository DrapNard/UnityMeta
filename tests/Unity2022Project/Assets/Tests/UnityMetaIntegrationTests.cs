using NUnit.Framework;
using UnityMeta;

namespace UnityMeta.IntegrationTests
{
    public sealed class TestClampAttribute : FieldSetAspectAttribute
    {
        public TestClampAttribute(int min, int max)
        {
        }

        [SetTemplate]
        public static int Apply(
            [Value] int value,
            [AspectArgument(0)] int min,
            [AspectArgument(1)] int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }

    public sealed class TestReadAttribute : FieldGetAspectAttribute
    {
        public TestReadAttribute(int offset)
        {
        }

        [GetTemplate]
        public static int Read(
            [Value] int value,
            [AspectArgument(0)] int offset)
        {
            return value + offset;
        }
    }

    public sealed class TestChangeAttribute : FieldChangeAspectAttribute
    {
        [ChangeTemplate]
        public static void Changed(
            [TargetInstance] Probe target,
            [OldValue] int oldValue,
            [NewValue] int newValue)
        {
            target.ChangeCount++;
            target.LastOld = oldValue;
            target.LastNew = newValue;
        }
    }

    public sealed class TestMethodAttribute : MethodAspectAttribute
    {
        [AfterTemplate]
        public static void After(
            [TargetInstance] Probe target,
            [ReturnValue] int result)
        {
            target.LastReturn = result;
        }
    }

    public sealed class Probe
    {
        [TestClamp(0, 100)]
        [TestChange]
        public int Value;

        [TestRead(5)]
        public int Displayed;

        public int ChangeCount;
        public int LastOld;
        public int LastNew;
        public int LastReturn;

        public void SetValue(int value)
        {
            Value = value;
        }

        public void SetDisplayed(int value)
        {
            Displayed = value;
        }

        public int ReadDisplayed()
        {
            return Displayed;
        }

        [TestMethod]
        public int Double(int value)
        {
            return value * 2;
        }
    }

    public sealed class UnityMetaIntegrationTests
    {
        [Test]
        public void Field_transform_and_change_hook_are_woven_by_Unity()
        {
            var probe = new Probe();

            probe.SetValue(500);
            Assert.That(probe.Value, Is.EqualTo(100));
            Assert.That(probe.ChangeCount, Is.EqualTo(1));
            Assert.That(probe.LastOld, Is.EqualTo(0));
            Assert.That(probe.LastNew, Is.EqualTo(100));

            probe.SetValue(500);
            Assert.That(probe.ChangeCount, Is.EqualTo(1));
        }

        [Test]
        public void Field_get_template_transforms_reads_without_mutating_storage()
        {
            var probe = new Probe();
            probe.SetDisplayed(10);

            Assert.That(probe.ReadDisplayed(), Is.EqualTo(15));
        }

        [Test]
        public void After_template_can_observe_return_value()
        {
            var probe = new Probe();
            Assert.That(probe.Double(21), Is.EqualTo(42));
            Assert.That(probe.LastReturn, Is.EqualTo(42));
        }
    }
}
