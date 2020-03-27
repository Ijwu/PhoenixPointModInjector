using PhoenixPointModLoader.Manager;
using System;
using System.IO;
using System.Reflection;

namespace PhoenixPointModLoader
{
    class LegacyPhoenixPointMod : IPhoenixPointMod
    {
        private MethodInfo _initMethod;

        public LegacyPhoenixPointMod(Type modClass)
        {
            _initMethod = modClass.GetMethod("Init");
        }

        public ModLoadPriority Priority => ModLoadPriority.Normal;


        public void Initialize()
        {
            _initMethod.Invoke(null, null);
        }
    }
}
