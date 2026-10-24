using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Pulse.Spike.Platform;

[StructLayout(LayoutKind.Sequential)]
internal struct CGPoint
{
    public double X;
    public double Y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGSize
{
    public double Width;
    public double Height;
}

[StructLayout(LayoutKind.Sequential)]
internal struct CGRect
{
    public CGPoint Origin;
    public CGSize Size;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NSEdgeInsets
{
    public double Top;
    public double Left;
    public double Bottom;
    public double Right;
}

/// <summary>
/// Minimal Objective-C runtime interop. Each objc_msgSend overload must match the exact signature
/// of the method it calls (required on arm64, where objc_msgSend is not variadic-compatible).
/// </summary>
[SupportedOSPlatform("macos")]
internal static unsafe partial class ObjC
{
    private const string Lib = "/usr/lib/libobjc.A.dylib";

    // Block ABI flags (clang "Block Implementation Specification").
    private const int BlockIsGlobal = 1 << 28;
    private const int BlockHasSignature = 1 << 30;

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_getClass(string name);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint sel_registerName(string name);

    [LibraryImport(Lib)]
    public static partial nint object_getClass(nint obj);

    [LibraryImport(Lib)]
    public static partial nint object_setClass(nint obj, nint cls);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    public static partial nint objc_allocateClassPair(nint superclass, string name, nint extraBytes);

    [LibraryImport(Lib)]
    public static partial void objc_registerClassPair(nint cls);

    [LibraryImport(Lib, StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool class_addMethod(nint cls, nint selector, nint implementation, string types);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Msg(nint receiver, nint selector);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Msg(nint receiver, nint selector, nint arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial nint Msg(nint receiver, nint selector, ulong arg1, nint arg2);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial void MsgVoid(nint receiver, nint selector);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial void MsgVoid(nint receiver, nint selector, nint arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial void MsgVoid(nint receiver, nint selector, byte arg);

    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial byte MsgByte(nint receiver, nint selector, nint arg);

    /// <summary>Two doubles come back in registers on both arm64 and x86_64.</summary>
    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial CGPoint MsgPoint(nint receiver, nint selector);

    /// <summary>arm64 only: x86_64 returns structs this size through objc_msgSend_stret.</summary>
    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial CGRect MsgRect(nint receiver, nint selector);

    /// <summary>arm64 only: x86_64 returns structs this size through objc_msgSend_stret.</summary>
    [LibraryImport(Lib, EntryPoint = "objc_msgSend")]
    public static partial NSEdgeInsets MsgInsets(nint receiver, nint selector);

    public static nint Class(string name) => objc_getClass(name);

    public static nint Sel(string name) => sel_registerName(name);

    public static bool RespondsTo(nint obj, string selector) =>
        obj != 0 && MsgByte(obj, Sel("respondsToSelector:"), Sel(selector)) != 0;

    /// <summary>
    /// Wraps an unmanaged function in a global Objective-C block. Global blocks are never copied or freed,
    /// so the memory is allocated once and kept for the life of the process.
    /// </summary>
    /// <param name="invoke">Function whose first parameter is the block itself.</param>
    /// <param name="signature">Objective-C type encoding of the block, e.g. "v16@?0@8".</param>
    public static nint CreateGlobalBlock(nint invoke, string signature)
    {
        var descriptor = (BlockDescriptor*)NativeMemory.Alloc((nuint)sizeof(BlockDescriptor));
        descriptor->Reserved = 0;
        descriptor->Size = (nuint)sizeof(BlockLiteral);
        descriptor->Signature = Marshal.StringToCoTaskMemUTF8(signature);

        var block = (BlockLiteral*)NativeMemory.Alloc((nuint)sizeof(BlockLiteral));
        block->Isa = ConcreteGlobalBlock();
        block->Flags = BlockIsGlobal | BlockHasSignature;
        block->Reserved = 0;
        block->Invoke = invoke;
        block->Descriptor = descriptor;
        return (nint)block;
    }

    private static nint ConcreteGlobalBlock()
    {
        foreach (var path in new[] { "/usr/lib/libSystem.B.dylib", "/usr/lib/system/libsystem_blocks.dylib" })
        {
            if (NativeLibrary.TryLoad(path, out var library) &&
                NativeLibrary.TryGetExport(library, "_NSConcreteGlobalBlock", out var isa))
            {
                return isa;
            }
        }

        throw new EntryPointNotFoundException("_NSConcreteGlobalBlock");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlockLiteral
    {
        public nint Isa;
        public int Flags;
        public int Reserved;
        public nint Invoke;
        public BlockDescriptor* Descriptor;
    }

    /// <summary>Layout without copy/dispose helpers: the signature follows the size directly.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct BlockDescriptor
    {
        public nuint Reserved;
        public nuint Size;
        public nint Signature;
    }
}
