namespace BepInEx.GUI.Loader;

#if !RELEASE
[HarmonyPatch(typeof(LogEventArgs))]
public static class EventArgsPatchTest
{
    public static Stopwatch Timer = Stopwatch.StartNew();

    internal static string FormatTimestamp()
    {
        return Timer?.Elapsed.ToString("HH:mm:ss.fffffff");
    }

    public static string ToString(LogEventArgs __instance)
    {
        return $"[DO I DIE? {Timer?.Elapsed.ToString("MM/dd/yyyy HH:mm:ss.fffffff")}] [{__instance.Level,-7}:{__instance.Source.SourceName,10}] {__instance.Data}";
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(LogEventArgs.ToString))]
    public static bool ToString(this LogEventArgs __instance, ref string __result)
    {
        __result = $"[DO I DIE? {FormatTimestamp()}] [{__instance.Level,-7}:{__instance.Source.SourceName,10}] {__instance.Data}";
        return false;
    }
}

///    //OG IL Code
///    //// Token: 0x0600014C RID: 332 RVA: 0x00004AEA File Offset: 0x00002CEA
///    //	.method public hidebysig virtual
///    //        instance string ToString() cil managed
///    //{
///    //		// Header Size: 1 byte
///    //		// Code Size: 39 (0x27) bytes
///    //		.maxstack 8
///    //
///    //		/* 0x00002CEB 72DB140070   */ IL_0000: ldstr     "[{0,-7}:{1,10}] {2}"
///    //		/* 0x00002CF0 02           */ IL_0005: ldarg.0
///    //		/* 0x00002CF1 2847010006   */ IL_0006: call instance valuetype BepInEx.Logging.LogLevel BepInEx.Logging.LogEventArgs::get_Level()
///    //		/* 0x00002CF6 8C1F000002   */ IL_000B: box BepInEx.Logging.LogLevel
///    //        /* 0x00002CFB 02           */ IL_0010: ldarg.0
///    //		/* 0x00002CFC 2849010006   */ IL_0011: call instance class BepInEx.Logging.ILogSource BepInEx.Logging.LogEventArgs::get_Source()
///    //        /* 0x00002D01 6F5E010006   */
///    //                                      IL_0016: callvirt instance string BepInEx.Logging.ILogSource::get_SourceName()
///    //        /* 0x00002D06 02           */
///    //                                      IL_001B: ldarg.0
///    //		/* 0x00002D07 2845010006   */ IL_001C: call instance object BepInEx.Logging.LogEventArgs::get_Data()
///    //        /* 0x00002D0C 283B01000A   */
///    //                                      IL_0021: call string[mscorlib] System.String::Format(string, object, object, object)
///    //        /* 0x00002D11 2A           */
///    //                                      IL_0026: ret
///    //	} // end of method LogEventArgs::ToString
///    //My IL Code
///    // Token: 0x06000020 RID: 32 RVA: 0x000026F8 File Offset: 0x000008F8
///    //.method public hidebysig static
///    //    string ToString(
///    //
///    //        class [BepInEx]
//BepInEx.Logging.LogEventArgs __instance
//	) cil managed
//{
//	// Header Size: 12 bytes
//	// Code Size: 100 (0x64) bytes
//	// LocalVarSig Token: 0x1100000B RID: 11
//	.maxstack 6
//	.locals init (
//        [0] valuetype[netstandard]System.Nullable`1 < valuetype[netstandard]System.DateTime >,
//		[1] valuetype[netstandard]System.DateTime,
//		[2] string
//	)
//
//    /* (36,5)-(36,6) ./EventArgsPatchTest.cs */
///* 0x00000904 00           */
//                                  IL_0000:
//    nop
//    /* (37,9)-(37,160) ./EventArgsPatchTest.cs */
//    /* 0x00000905 7262070070   */ IL_0001: ldstr     "[DO I DIE? {0}] [{1,-7}:{2,10}] {3}"
//    /* 0x0000090A 1A           */
//                                  IL_0006:
//    ldc.i4.4
//    /* 0x0000090B 8D14000001   */
//                                  IL_0007:
//    newarr[netstandard]System.Object
//    /* 0x00000910 25           */ IL_000C: dup
//    /* 0x00000911 16           */ IL_000D: ldc.i4.0
//    /* 0x00000912 281C000006   */
//                                  IL_000E:
//    call valuetype[netstandard]System.Nullable`1 < valuetype[netstandard]System.DateTime > BepInEx.GUI.Loader.EventArgsPatchTest::get_Timestamp()
//    /* 0x00000917 0A           */
//                                  IL_0013:
//    stloc.0
//    /* 0x00000918 1200         */
//                                  IL_0014:
//    ldloca.s V_0
//    /* 0x0000091A 25           */
//                                  IL_0016:
//    dup
//    /* 0x0000091B 284D00000A   */ IL_0017: call instance bool valuetype[netstandard]System.Nullable`1 < valuetype[netstandard]System.DateTime >::get_HasValue()
//    /* 0x00000920 2D04         */
//                                  IL_001C:
//    brtrue.s IL_0022
//
//    /* 0x00000922 26           */
//                                  IL_001E:
//    pop
//    /* 0x00000923 14           */ IL_001F: ldnull
//    /* 0x00000924 2B12         */ IL_0020: br.s IL_0034
//
//    /* 0x00000926 285000000A   */
//                                  IL_0022:
//    call instance !0 valuetype[netstandard]System.Nullable`1 < valuetype[netstandard]System.DateTime >::GetValueOrDefault()
//    /* 0x0000092B 0B           */
//                                  IL_0027:
//    stloc.1
//    /* 0x0000092C 1201         */
//                                  IL_0028:
//    ldloca.s V_1
//    /* 0x0000092E 722A070070   */
//                                  IL_002A:
//    ldstr     "MM/dd/yyyy HH:mm:ss.fffffff"
//    /* 0x00000933 285100000A   */
//                                  IL_002F:
//    call instance string[netstandard] System.DateTime::ToString(string)
//
//    /* 0x00000938 A2           */
//                                  IL_0034:
//    stelem.ref
//    /* 0x00000939 25           */ IL_0035: dup
//    /* 0x0000093A 17           */ IL_0036: ldc.i4.1
//    /* 0x0000093B 02           */
//                                  IL_0037:
//    ldarg.0
//    /* 0x0000093C 6F5200000A   */
//                                  IL_0038:
//    callvirt instance valuetype[BepInEx]BepInEx.Logging.LogLevel[BepInEx]BepInEx.Logging.LogEventArgs::get_Level()
//    /* 0x00000941 8C32000001   */
//                                  IL_003D:
//    box[BepInEx]BepInEx.Logging.LogLevel
//    /* 0x00000946 A2           */ IL_0042: stelem.ref
//    /* 0x00000947 25           */ IL_0043: dup
//    /* 0x00000948 18           */ IL_0044: ldc.i4.2
//    /* 0x00000949 02           */
//                                  IL_0045:
//    ldarg.0
//    /* 0x0000094A 6F5300000A   */
//                                  IL_0046:
//    callvirt instance class [BepInEx]
//BepInEx.Logging.ILogSource[BepInEx] BepInEx.Logging.LogEventArgs::get_Source()
//    /* 0x0000094F 6F5400000A   */
//                                  IL_004B: callvirt instance string[BepInEx] BepInEx.Logging.ILogSource::get_SourceName()
//    /* 0x00000954 A2           */
//                                  IL_0050: stelem.ref
//    /* 0x00000955 25           */ IL_0051: dup
//    /* 0x00000956 19           */ IL_0052: ldc.i4.3
//	/* 0x00000957 02           */ IL_0053: ldarg.0
//	/* 0x00000958 6F5500000A   */ IL_0054: callvirt instance object[BepInEx] BepInEx.Logging.LogEventArgs::get_Data()
//    /* 0x0000095D A2           */
//                                  IL_0059: stelem.ref
//    /* 0x0000095E 281C00000A   */ IL_005A: call string[netstandard] System.String::Format(string, object[])
//    /* 0x00000963 0C           */
//                                  IL_005F: stloc.2
//	/* 0x00000964 2B00         */ IL_0060: br.s IL_0062
//
//    /* (38,5)-(38,6) ./EventArgsPatchTest.cs */
///* 0x00000966 08           */
//                                  IL_0062: ldloc.2
//	/* 0x00000967 2A           */ IL_0063: ret
//} // end of method EventArgsPatchTest::ToString

#endif