using System;
using System.IO;
using System.Reflection;

namespace PhoenixPointModLoader
{
    class LegacyPhoenixPointMod : IPhoenixPointMod
    {
        private Type modClass;
        private MethodInfo initMethod;

        public LegacyPhoenixPointMod(Type modClass, MethodInfo initMethod)
        {
            this.modClass = modClass;
            this.initMethod = initMethod;
        }

        public Type getModClass()
        {
            return this.modClass;
        }

        public ModLoadPriority Priority => ModLoadPriority.Normal;

        public void Initialize()
        {
            this.initMethod.Invoke(null, null);
        }
    }
}
