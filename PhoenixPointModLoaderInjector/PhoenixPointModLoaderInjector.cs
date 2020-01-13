﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Options;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static System.Console;


namespace PhoenixPointModloaderInjector
{
	internal static class PhoenixPointModLoaderInjector
    {
        // return codes
        private const int RC_NORMAL = 0;
        private const int RC_UNHANDLED_STATE = 1;
        private const int RC_BAD_OPTIONS = 2;
        private const int RC_MISSING_BACKUP_FILE = 3;
        private const int RC_BACKUP_FILE_INJECTED = 4;
        private const int RC_BAD_MANAGED_DIRECTORY_PROVIDED = 5;
        private const int RC_MISSING_MOD_LOADER_ASSEMBLY = 6;
        private const int RC_REQUIRED_GAME_VERSION_MISMATCH = 7;
       

        private const string MOD_LOADER_DLL_FILE_NAME = "PhoenixPointModLoader.dll";
        private const string GAME_DLL_FILE_NAME = "Assembly-CSharp.dll";
        private const string BACKUP_FILE_EXT = ".orig";

        private const string HOOK_TYPE = "PhoenixPoint.Common.Game.PhoenixGame";
        private const string HOOK_METHOD = "BootCrt";
        private const string INJECT_TYPE = "PhoenixPointModLoader.PhoenixPointModLoader";
        private const string INJECT_METHOD = "Init";

        private const string GAME_VERSION_TYPE = "VersionInfo";
        private const string GAME_VERSION_CONST = "CURRENT_VERSION_NUMBER";
        

        // ReSharper disable once InconsistentNaming
        private static readonly ReceivedOptions OptionsIn = new ReceivedOptions();

        // ReSharper disable once InconsistentNaming
        private static readonly OptionSet Options = new OptionSet
        {
            {
                "d|detect",
                "Detect if the PhoenixPoint game assembly is already injected",
                v => OptionsIn.Detecting = v != null
            },
            {
                "g|gameversion",
                "Print the PhoenixPoint game version number",
                v => OptionsIn.GameVersion = v != null
            },
            {
                "h|?|help",
                "Print this useful help message",
                v => OptionsIn.Helping = v != null
            },
            {
                "i|install",
                "Install the Mod (this is the default behavior)",
                v => OptionsIn.Installing = v != null
            },
            {
                "manageddir=",
                "specify managed dir where PhoenixPoint game's Assembly-CSharp.dll is located",
                v => OptionsIn.ManagedDir = v
            },
            {
                "y|nokeypress",
                "Anwser prompts affirmatively",
                v => OptionsIn.RequireKeyPress = v == null
            },
            {
                "reqmismatchmsg=",
                "Print msg if required version check fails",
                v => OptionsIn.RequiredGameVersionMismatchMessage = v
            },
            {
                "requiredversion=",
                "Don't continue with /install, /update, etc. if the PhoenixPoint game version does not match given argument",
                v => OptionsIn.RequiredGameVersion = v
            },
            {
                "r|restore",
                "Restore pristine backup PhoenixPoint game assembly to folder",
                v => OptionsIn.Restoring = v != null
            },
            {
                "u|update",
                "Update mod loader injection of PhoenixPoint game assembly to current PPML version",
                v => OptionsIn.Updating = v != null
            },
            {
                "v|version",
                "Print the PhoenixPointModInjector version number",
                v => OptionsIn.Versioning = v != null
            }
        };

        private static int Main(string[] args)
        {
            try
            {
                try
                {
                    Options.Parse(args);
                }
                catch (OptionException e)
                {
                    SayOptionException(e);
                    return RC_BAD_OPTIONS;
                }

                if (OptionsIn.Helping)
                {
                    SayHelp(Options);
                    return RC_NORMAL;
                }

                if (OptionsIn.Versioning)
                {
                    SayVersion();
                    return RC_NORMAL;
                }

                var managedDirectory = Directory.GetCurrentDirectory();
                if (!string.IsNullOrEmpty(OptionsIn.ManagedDir))
                {
                    if (!Directory.Exists(OptionsIn.ManagedDir))
                    {
                        SayManagedDirMissingError(OptionsIn.ManagedDir);
                        return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                    }

                    managedDirectory = Path.GetFullPath(OptionsIn.ManagedDir);
                }

                var gameDllPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME);
                var gameDllBackupPath = Path.Combine(managedDirectory, GAME_DLL_FILE_NAME + BACKUP_FILE_EXT);
                var modLoaderDllPath = Path.Combine(managedDirectory, MOD_LOADER_DLL_FILE_NAME);

                if (!File.Exists(gameDllPath))
                {
                    SayGameAssemblyMissingError(OptionsIn.ManagedDir);
                    return RC_BAD_MANAGED_DIRECTORY_PROVIDED;
                }

                if (!File.Exists(modLoaderDllPath))
                {
                    SayModLoaderAssemblyMissingError(modLoaderDllPath);
                    return RC_MISSING_MOD_LOADER_ASSEMBLY;
                }
                

                var injected = IsInjected(gameDllPath, out var isCurrentInjection, out var gameVersion);

                if (OptionsIn.GameVersion)
                {
                    SayGameVersion(gameVersion);
                    return RC_NORMAL;
                }

                if (!string.IsNullOrEmpty(OptionsIn.RequiredGameVersion)
                    && OptionsIn.RequiredGameVersion != gameVersion)
                {
                    SayRequiredGameVersion(gameVersion, OptionsIn.RequiredGameVersion);
                    SayRequiredGameVersionMismatchMessage(OptionsIn.RequiredGameVersionMismatchMessage);
                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_REQUIRED_GAME_VERSION_MISMATCH;
                }

                if (OptionsIn.Detecting)
                {
                    SayInjectedStatus(injected);
                    return RC_NORMAL;
                }

                SayHeader();

                if (OptionsIn.Restoring)
                {
                    if (injected)
                        Restore(gameDllPath, gameDllBackupPath);
                    else
                        SayAlreadyRestored();

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }

                if (OptionsIn.Updating)
                {
                    if (injected)
                    {
                        if (PromptForUpdateYesNo(OptionsIn.RequireKeyPress))
                        {
                            Restore(gameDllPath, gameDllBackupPath);
                            Inject(gameDllPath, modLoaderDllPath);
                        }
                        else
                        {
                            SayUpdateCanceled();
                        }
                    }

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }

                if (OptionsIn.Installing)
                {
                    if (!injected)
                    {
                        Backup(gameDllPath, gameDllBackupPath);
                        Inject(gameDllPath, modLoaderDllPath);
                    }
                    else
                    {
                        SayAlreadyInjected(isCurrentInjection);
                    }

                    PromptForKey(OptionsIn.RequireKeyPress);
                    return RC_NORMAL;
                }
            }
            catch (BackupFileNotFound e)
            {
                SayException(e);
                SayHowToRecoverMissingBackup(e.BackupFileName);
                return RC_MISSING_BACKUP_FILE;
            }
            catch (BackupFileInjected e)
            {
                SayException(e);
                SayHowToRecoverInjectedBackup(e.BackupFileName);
                return RC_BACKUP_FILE_INJECTED;
            }
            catch (Exception e)
            {
                SayException(e);
            }

            return RC_UNHANDLED_STATE;
        }

        private static void SayInjectedStatus(bool injected)
        {
            WriteLine(injected.ToString().ToLower());
        }

        private static void Backup(string filePath, string backupFilePath)
        {
            File.Copy(filePath, backupFilePath, true);
            WriteLine($"{Path.GetFileName(filePath)} backed up to {Path.GetFileName(backupFilePath)}");
        }

        private static void Restore(string filePath, string backupFilePath)
        {
            if (!File.Exists(backupFilePath))
                throw new BackupFileNotFound();

            if (IsInjected(backupFilePath))
                throw new BackupFileInjected();

            File.Copy(backupFilePath, filePath, true);
            WriteLine($"{Path.GetFileName(backupFilePath)} restored to {Path.GetFileName(filePath)}");
        }

        private static void Inject(string hookFilePath, string injectFilePath)
        {
            WriteLine($"Injecting {Path.GetFileName(hookFilePath)} with {INJECT_TYPE}.{INJECT_METHOD} at {HOOK_TYPE}.{HOOK_METHOD}");

            using (var game = ModuleDefinition.ReadModule(hookFilePath, new ReaderParameters { ReadWrite = true }))
            using (var injecting = ModuleDefinition.ReadModule(injectFilePath))
            {
                var success = InjectModHookPoint(game, injecting);
                
                success &= WriteNewAssembly(hookFilePath, game);

                if (!success)
                    WriteLine("Failed to inject the game assembly.");
            }
        }

        private static bool WriteNewAssembly(string hookFilePath, ModuleDefinition game)
        {
            // save the modified assembly
            WriteLine($"Writing back to {Path.GetFileName(hookFilePath)}...");
            game.Write();
            WriteLine("Injection complete!");
            return true;
        }

        private static bool InjectModHookPoint(ModuleDefinition game, ModuleDefinition injecting)
        {
            // get the methods that we're hooking and injecting
            var injectedMethod = injecting.GetType(INJECT_TYPE).Methods.Single(x => x.Name == INJECT_METHOD);
            var hookedMethod = game.GetType(HOOK_TYPE).Methods.First(x => x.Name == HOOK_METHOD);
            
            // If the return type is an iterator -- need to go searching for its MoveNext method which contains the actual code you'll want to inject
            if (hookedMethod.ReturnType.Name.Contains("IEnumerator"))
            {
                var nestedIterator = game.GetType(HOOK_TYPE).NestedTypes.First(x => x.Name.Contains(HOOK_METHOD));
                hookedMethod = nestedIterator.Methods.First(x => x.Name.Equals("MoveNext"));
            }

            // As of Battletech  v1.1 the Start() iterator method of Battletech.Main has this at the end
            //
            //  ...
            //
            //      Serializer.PrepareSerializer();
            //      this.activate.enabled = true;
            //      yield break;
            //
            //  }
            //

            // We want to inject after the PrepareSerializer call -- so search for that call in the CIL

            // REALITYMACHINA NOTE - equivaalent in PhoenixPoint.Common.Game.PhoenixGame.BootCrt, at least on launch
  
            var targetInstruction = -1;
            WriteLine("This is a debugging line for our count of instructions");

            WriteLine(hookedMethod.Body.Instructions.Count);
            for (var i = 0; i < hookedMethod.Body.Instructions.Count; i++)
            {
                var instruction = hookedMethod.Body.Instructions[i];
                
                if (instruction.OpCode.Code.Equals(Code.Call) && instruction.OpCode.OperandType.Equals(OperandType.InlineMethod))
                {
                    var methodReference = (MethodReference)instruction.Operand;
                    WriteLine(methodReference.Name);
                    if (methodReference.Name.Contains("MenuCrt"))
                        targetInstruction = i + 1; // hack - we want to run after that instruction has been fully processed, not in the middle of it.
                }

            }
            
            if (targetInstruction == -1)
            {
                WriteLine("This is a debugging line and our target line was not found.");
                return false;
            }
            hookedMethod.Body.GetILProcessor().InsertAfter(hookedMethod.Body.Instructions[targetInstruction],
                Instruction.Create(OpCodes.Call, game.ImportReference(injectedMethod)));


            WriteLine("This is another debugging line. If we've gotten here, we should be fine?");

            return true;
        }

        private static bool IsInjected(string dllPath)
        {
            return IsInjected(dllPath, out _, out _);
        }

        private static bool IsInjected(string dllPath, out bool isCurrentInjection, out string gameVersion)
        {
            isCurrentInjection = false;
            gameVersion = "";
            var detectedInject = false;
            using (var dll = ModuleDefinition.ReadModule(dllPath))
            {
                foreach (var type in dll.Types)
                {
                    // Standard methods
                    foreach (var methodDefinition in type.Methods)
                    {
                        if (IsHookInstalled(methodDefinition, out isCurrentInjection))
                            detectedInject = true;
                    }

                    // Also have to check in places like IEnumerator generated methods (Nested)
                    foreach (var nestedType in type.NestedTypes)
                        foreach (var methodDefinition in nestedType.Methods)
                        {
                            if (IsHookInstalled(methodDefinition, out isCurrentInjection))
                                detectedInject = true;
                        }

                    if (type.FullName == GAME_VERSION_TYPE)
                    {
                        var fieldInfo = type.Fields.First(x => x.IsLiteral && !x.IsInitOnly && x.Name == GAME_VERSION_CONST);

                        if (null != fieldInfo)
                            gameVersion = fieldInfo.Constant.ToString();
                    }

                    if (detectedInject && !string.IsNullOrEmpty(gameVersion))
                        return true;
                }
            }

            return detectedInject;
        }

        private static bool IsHookInstalled(MethodDefinition methodDefinition, out bool isCurrentInjection)
        {
            isCurrentInjection = false;

            if (methodDefinition.Body == null)
                return false;

            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode.Equals(OpCodes.Call) &&
                    instruction.Operand.ToString().Equals($"System.Void {INJECT_TYPE}::{INJECT_METHOD}()"))
                {
                    isCurrentInjection =
                        methodDefinition.FullName.Contains(HOOK_TYPE) &&
                        methodDefinition.FullName.Contains(HOOK_METHOD);
                    return true;
                }
            }

            return false;
        }

        private static void SayHelp(OptionSet p)
        {
            SayHeader();
            WriteLine("Usage: PhoenixPointModLoaderInjector.exe [OPTIONS]+");
            WriteLine("Inject the PhoenixPoint game assembly with an entry point for mod enablement.");
            WriteLine("If no options are specified, the program assumes you want to /install.");
            WriteLine();
            WriteLine("Options:");
            p.WriteOptionDescriptions(Out);
        }

        private static void SayGameVersion(string version)
        {
            WriteLine(version);
        }

        private static void SayRequiredGameVersion(string version, string expectedVersion)
        {
            WriteLine($"Expected PhoenixPoint game v{expectedVersion}");
            WriteLine($"Actual PhoenixPoint game v{version}");
        }

        private static void SayRequiredGameVersionMismatchMessage(string msg)
        {
            if (!string.IsNullOrEmpty(msg))
                WriteLine(msg);
        }

        private static void SayVersion()
        {
            WriteLine(GetProductVersion());
        }

        private static string GetProductVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private static void SayOptionException(OptionException e)
        {
            SayHeader();
            Write("PhoenixPointModLoaderInjector.exe: ");
            WriteLine(e.Message);
            WriteLine("Try `PhoenixPointModLoaderInjector.exe --help' for more information.");
        }

        private static void SayManagedDirMissingError(string givenManagedDir)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the directory '{givenManagedDir}'. Are you sure it exists?");
        }

        private static void SayGameAssemblyMissingError(string givenManagedDir)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the PhoenixPoint game assembly {GAME_DLL_FILE_NAME} in directory '{givenManagedDir}'.\n" +
                "Are you sure that is the correct directory?");
        }

        private static void SayModLoaderAssemblyMissingError(string expectedModLoaderAssemblyPath)
        {
            SayHeader();
            WriteLine($"ERROR: We could not find the PhoenixPoint game assembly {MOD_LOADER_DLL_FILE_NAME} at '{expectedModLoaderAssemblyPath}'.\n" +
                $"Is {MOD_LOADER_DLL_FILE_NAME} in the correct place? It should be in the same directory as this injector executable.");
        }
        
        private static void SayHeader()
        {
            WriteLine("PhoenixPointModLoader Injector");
            WriteLine("----------------------------");
        }

        private static void SayHowToRecoverMissingBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"The backup game assembly file must be in the directory with the injector for /restore to work. The backup file should be named \"{backupFileName}\".");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayHowToRecoverInjectedBackup(string backupFileName)
        {
            WriteLine("----------------------------");
            WriteLine($"The backup game assembly file named \"{backupFileName}\" was already PPML injected. Something has gone wrong.");
            WriteLine("You may need to reinstall or use Steam/GOG's file verification function if you have no other backup.");
        }

        private static void SayAlreadyInjected(bool isCurrentInjection)
        {
            WriteLine(isCurrentInjection
                ? $"ERROR: {GAME_DLL_FILE_NAME} already injected at {INJECT_TYPE}.{INJECT_METHOD}."
                : $"ERROR: {GAME_DLL_FILE_NAME} already injected with an older PhoenixPointModLoader Injector.  Please revert the file and re-run injector!");
        }

        private static void SayAlreadyRestored()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} already restored.");
        }

        private static void SayUpdateCanceled()
        {
            WriteLine($"{GAME_DLL_FILE_NAME} update cancelled.");
        }

        private static void SayException(Exception e)
        {
            WriteLine($"ERROR: An exception occured: {e}");
        }

        private static bool PromptForUpdateYesNo(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return true;

            WriteLine("Would you like to update your assembly now? (y/n)");
            return ReadKey().Key == ConsoleKey.Y;
        }

        private static void PromptForKey(bool requireKeyPress)
        {
            if (!requireKeyPress)
                return;

            WriteLine("Press any key to continue.");
            ReadKey();
        }

        
    }

    public class BackupFileInjected : Exception
    {
        public BackupFileInjected(string backupFileName = "Assembly-CSharp.dll.orig") : base(FormulateMessage(backupFileName))
        {
            BackupFileName = backupFileName;
        }

        public string BackupFileName { get; }

        private static string FormulateMessage(string backupFileName)
        {
            return $"The backup file \"{backupFileName}\" was PPML-injected.";
        }
    }

    public class BackupFileNotFound : FileNotFoundException
    {
        public BackupFileNotFound(string backupFileName = "Assembly-CSharp.dll.orig") : base(FormulateMessage(backupFileName))
        {
            BackupFileName = backupFileName;
        }

        public string BackupFileName { get; }

        private static string FormulateMessage(string backupFileName)
        {
            return $"The backup file \"{backupFileName}\" could not be found.";
        }
    }
}