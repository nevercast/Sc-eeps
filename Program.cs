global using ScreepsDotNet.API;
global using ScreepsDotNet.API.Bot;
global using ScreepsDotNet.API.World;
global using ScreepsDotNet.Native.World;

global using static Bot.ServiceCollection;

using System;
using System.Diagnostics.CodeAnalysis;
using Bot;

namespace ScreepsDotNet
{
    public static partial class Program
    {
        private static Looper? looper = null!;

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Program))]
        public static void Main() { }

        [System.Runtime.Versioning.SupportedOSPlatform("wasi")]
        public static void Init()
        {
            try
            {
                Register<IGame, NativeGame>();
                Register<ILogger, Logger>();
                Register<IBot, NeverBot>();
                looper = new Looper();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("wasi")]
        public static void Loop()
        {
            try
            {
                looper!.Tick();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}