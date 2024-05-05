// See https://aka.ms/new-console-template for more information

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe
{
    var path = "/Users/hadashi/tmp/log";
    fixed (char* pathPtr = path)
    {
        var result = CsBindgen.NativeMethods.unio_file_read_to_end((ushort*)pathPtr, path.Length);
        var decoded = System.Text.Encoding.UTF8.GetString(result.bytes.AsSpan());
        Console.WriteLine(decoded);
    }
}

Console.WriteLine("Hello, World!");

namespace CsBindgen
{
    partial struct ByteBuffer
    {
        public unsafe Span<byte> AsSpan()
        {
            return new Span<byte>(ptr, length);
        }

        public unsafe Span<T> AsSpan<T>()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(ptr), length / Unsafe.SizeOf<T>());
        }
    }

    internal static unsafe partial class NativeMethods
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/native-interop/cross-platform
        // Library path will search
        // win => __DllName, __DllName.dll
        // linux, osx => __DllName.so, __DllName.dylib

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, DllImportResolver);
        }

        static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == __DllName)
            {
                var path = "/Users/hadashi/dev/Unio/unio_native/target/debug/libunio.dylib";
                // var extension = "";
                //
                // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                // {
                //     path += "win-";
                //     extension = ".dll";
                // }
                // else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                // {
                //     path += "osx-";
                //     extension = ".dylib";
                // }
                // else
                // {
                //     path += "linux-";
                //     extension = ".so";
                // }
                //
                // if (RuntimeInformation.ProcessArchitecture == Architecture.X86)
                // {
                //     path += "x86";
                // }
                // else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
                // {
                //     path += "x64";
                // }
                // else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                // {
                //     path += "arm64";
                // }
                //
                // path += "/native/" + __DllName + extension;

                return NativeLibrary.Load(path, assembly, searchPath);
            }

            return IntPtr.Zero;
        }
    }
}