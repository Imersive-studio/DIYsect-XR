using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class LearningAssemblyTests
    {
        [Test]
        public void TheLearningAssemblyIsBuiltUnderItsOwnName()
        {
            Assert.AreEqual(LearningAssembly.Name, typeof(LearningAssembly).Assembly.GetName().Name);
            Assert.AreEqual("DIYsect.Learning", typeof(LearningAssembly).Namespace);
        }
    }
}
