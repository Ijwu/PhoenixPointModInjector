using PhoenixPointModLoader.Manager;
using System;
using System.IO;
using System.Reflection;

namespace PhoenixPointModLoader
{
    class LegacyPhoenixPointMod : IPhoenixPointMod
    {
        private Type _modClass;
        private MethodInfo _initMethod;

        public LegacyPhoenixPointMod(Type modClass, MethodInfo initMethod)
        {
            _modClass = modClass;
            _initMethod = initMethod;
        }

        public Type GetModClass()
        {
            return _modClass;
        }

        public ModLoadPriority Priority => ModLoadPriority.Normal;


        public void Initialize()
        {
            _initMethod.Invoke(null, null);
        }
    }
}
