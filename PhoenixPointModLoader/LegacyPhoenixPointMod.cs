using System;
using System.IO;
using System.Reflection;

namespace PhoenixPointModLoader
{
    class LegacyPhoenixPointMod : IPhoenixPointMod
    {
        public Type ModClass { get; }
        private MethodInfo InitMethod;

        public LegacyPhoenixPointMod(Type modClass, MethodInfo initMethod)
        {
            ModClass = modClass;
            InitMethod = initMethod;
        }

        public ModLoadPriority Priority => ModLoadPriority.Normal;

        public void Initialize()
        {
            InitMethod.Invoke(null, null);
        }
    }
}
