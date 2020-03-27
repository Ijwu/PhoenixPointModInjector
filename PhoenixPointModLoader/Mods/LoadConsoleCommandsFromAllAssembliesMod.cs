using Base.Utils.GameConsole;
using Harmony;
using PhoenixPointModLoader.Manager;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PhoenixPointModLoader.Mods
{
	public class LoadConsoleCommandsFromAllAssembliesMod : IPhoenixPointMod
	{
		public ModLoadPriority Priority => ModLoadPriority.Low;

		public void Initialize()
		{
			var harmonyInstance = HarmonyInstance.Create("io.github.ijwu.ppml");
			var target = typeof(ConsoleCommandAttribute).GetMethod("LoadCommands");
			var postfix = new HarmonyMethod(this.GetType().GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic));
			harmonyInstance.Patch(target, postfix: postfix);

			ConsoleCommandAttribute.LoadCommands();
		}

		[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Used by Harmony.")]
		private static void Postfix(SortedList<string, ConsoleCommandAttribute> ___CommandToInfo)
		{
			var allConsoleCommandsEverywhere = from assembly in AppDomain.CurrentDomain.GetAssemblies()
											   from type in assembly.GetTypes()
											   from method in type.GetMethods()
											   let attr = method.GetCustomAttribute(typeof(ConsoleCommandAttribute))
															as ConsoleCommandAttribute
											   where attr != null
											   select new
											   {
												   Name = attr.Command ?? method.Name,
												   Attribute = attr,
												   MethodInfo = method
											   };

			foreach (var item in allConsoleCommandsEverywhere)
			{
				if (item.Attribute == null)
				{
					continue;
				}

				if (___CommandToInfo.ContainsKey(item.Name))
				{
					continue;
				}

				Debug.Log($"Successfully loaded command `{item.Name}` from assembly `{item.MethodInfo.DeclaringType.Assembly.FullName}`.");

				SetMethodInfo(item.Attribute, item.MethodInfo);
				___CommandToInfo[item.Name] = item.Attribute;
			}
		}

		/// <summary>
		/// Necessary to do this since LoadCommands() does this and I'm Postfix'ing it then overwriting its work.
		/// This is MEGA sloppy as I'm basically undoing all the checks that LoadCommands does.
		/// I'm only doing this for commands not previously registered, though, so hopefully the fallout isn't too bad.
		/// </summary>
		private static void SetMethodInfo(ConsoleCommandAttribute attr, MethodInfo minfo)
		{
			var minfoField = AccessTools.Field(attr.GetType(), "_methodInfo");
			minfoField.SetValue(attr, minfo);
		}

	}

}
