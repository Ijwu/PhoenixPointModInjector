using System;
using NUnit.Framework;
using PhoenixPointModLoader.Manager;
using PhoenixPointModLoader.Tests.Helpers;

namespace PhoenixPointModLoader.Tests
{
	[TestFixture]
	public class ModMetadataTests
	{
		private ModMetadataBuilder _metadataBuilder = ModMetadataBuilder.Instance;

		[Test]
		public void ResolveDependencies_AllDependenciesLoaded_ReturnsTrue()
		{
			ModMetadata firstDep = _metadataBuilder.WithName("Dep One").Build();
			ModMetadata secondDep = _metadataBuilder.WithName("Dep Two").Build();

			ModMetadata rootMod = _metadataBuilder
									.WithName("Root Mod")
									.WithVersion("1.0")
									.WithDependency(firstDep)
									.WithDependency(secondDep)
									.Build();

			Assert.IsTrue(rootMod.TryResolveDependencies(new[] { firstDep, secondDep, rootMod }));
		}

		[Test]
		public void ResolveDependencies_NotAllDependenciesLoaded_ReturnsFalse()
		{
			ModMetadata firstDep = _metadataBuilder.WithName("Dep One").Build();
			ModMetadata secondDep = _metadataBuilder.WithName("Dep Two").Build();

			ModMetadata rootMod = _metadataBuilder
									.WithName("Root Mod")
									.WithVersion("1.0")
									.WithDependency(firstDep)
									.WithDependency(secondDep)
									.Build();

			Assert.IsFalse(rootMod.TryResolveDependencies(new[] { firstDep}));
		}

		[Test]
		public void ResolveDependencies_CircularDependency_ThrowsException()
		{
			ModMetadata firstDep = _metadataBuilder.WithName("Dep One").Build();
			ModMetadata secondDep = _metadataBuilder.WithName("Dep Two").Build();
			ModMetadata thirdDep = _metadataBuilder.WithName("Dep Three").Build();

			firstDep.Dependencies.Add(secondDep);
			secondDep.Dependencies.Add(thirdDep);
			thirdDep.Dependencies.Add(firstDep);

			Assert.Throws<ModCircularDependencyException>(() => firstDep.TryResolveDependencies(new[] { firstDep, secondDep, thirdDep }));
		}
	}
}
