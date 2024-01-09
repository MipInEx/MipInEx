using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Mono.Cecil.Cil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using OpCode = System.Reflection.Emit.OpCode;
using OpCodes = System.Reflection.Emit.OpCodes;
using CecilOpCode = Mono.Cecil.Cil.OpCode;
using CecilOpCodes = Mono.Cecil.Cil.OpCodes;

namespace HarmonyLib.Internal.Util;

internal static class EmitterExtensions
{
    private static DynamicMethodDefinition emitDMD;
    private static MethodInfo emitDMDMethod;
    private static Action<CecilILGenerator, OpCode, object> emitCodeDelegate;
    private readonly static Dictionary<Type, CecilOpCode> storeOpCodes = new()
    {
        [typeof(sbyte)] = CecilOpCodes.Stind_I1,
        [typeof(byte)] = CecilOpCodes.Stind_I1,
        [typeof(short)] = CecilOpCodes.Stind_I2,
        [typeof(ushort)] = CecilOpCodes.Stind_I2,
        [typeof(int)] = CecilOpCodes.Stind_I4,
        [typeof(uint)] = CecilOpCodes.Stind_I4,
        [typeof(long)] = CecilOpCodes.Stind_I8,
        [typeof(ulong)] = CecilOpCodes.Stind_I8,
        [typeof(float)] = CecilOpCodes.Stind_R4,
        [typeof(double)] = CecilOpCodes.Stind_R8,
    };

    private static readonly Dictionary<Type, CecilOpCode> loadOpCodes = new()
    {
            [typeof(sbyte)] = CecilOpCodes.Ldind_I1,
            [typeof(byte)] = CecilOpCodes.Ldind_I1,
            [typeof(short)] = CecilOpCodes.Ldind_I2,
            [typeof(ushort)] = CecilOpCodes.Ldind_I2,
            [typeof(int)] = CecilOpCodes.Ldind_I4,
            [typeof(uint)] = CecilOpCodes.Ldind_I4,
            [typeof(long)] = CecilOpCodes.Ldind_I8,
            [typeof(ulong)] = CecilOpCodes.Ldind_I8,
            [typeof(float)] = CecilOpCodes.Ldind_R4,
            [typeof(double)] = CecilOpCodes.Ldind_R8,
    };

    [MethodImpl(MethodImplOptions.Synchronized)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    static EmitterExtensions()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        if (EmitterExtensions.emitDMD != null)
            return;
        EmitterExtensions.InitEmitterHelperDMD();
    }

    public static CecilOpCode GetCecilStoreOpCode(this Type t)
    {
        if (t.IsEnum)
            return CecilOpCodes.Stind_I4;
        return EmitterExtensions.storeOpCodes.TryGetValue(t, out CecilOpCode opCode) ? opCode : CecilOpCodes.Stind_Ref;
    }

      public static CecilOpCode GetCecilLoadOpCode(this Type t)
      {
        if (t.IsEnum)
            return CecilOpCodes.Ldind_I4;
        return loadOpCodes.TryGetValue(t, out CecilOpCode opCode) ? opCode : CecilOpCodes.Ldind_Ref;
      }

    public static Type OpenRefType(this Type t)
    {
        if (t.IsByRef)
            return t.GetElementType();
        return t;
    }

    private static void InitEmitterHelperDMD()
    {
        EmitterExtensions.emitDMD = new DynamicMethodDefinition(
            "EmitOpcodeWithOperand",
            typeof(void),
            new Type[] { typeof(CecilILGenerator), typeof(OpCode), typeof(object) });
        ILGenerator il = EmitterExtensions.emitDMD.GetILGenerator();

        Label current = il.DefineLabel();

        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Brtrue, current);

        il.Emit(OpCodes.Ldstr, "Provided operand is null!");
        il.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] {typeof(string)}));
        il.Emit(OpCodes.Throw);

        foreach (MethodInfo method in typeof(CecilILGenerator).GetMethods().Where(m => m.Name == "Emit"))
        {
            ParameterInfo[] paramInfos = method.GetParameters();
            if (paramInfos.Length != 2)
                continue;

            Type[] types = paramInfos.Select(p => p.ParameterType).ToArray();
            if (types[0] != typeof(OpCode))
                continue;

            Type paramType = types[1];

            il.MarkLabel(current);
            current = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Isinst, paramType);
            il.Emit(OpCodes.Brfalse, current);

            il.Emit(OpCodes.Ldarg_2);

            if (paramType.IsValueType)
                il.Emit(OpCodes.Unbox_Any, paramType);

            LocalBuilder loc = il.DeclareLocal(paramType);
            il.Emit(OpCodes.Stloc, loc);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc, loc);
            il.Emit(OpCodes.Callvirt, method);
            il.Emit(OpCodes.Ret);
        }

        il.MarkLabel(current);
        il.Emit(OpCodes.Ldstr, "The operand is none of the supported types!");
        il.Emit(OpCodes.Newobj, typeof(Exception).GetConstructor(new[] {typeof(string)}));
        il.Emit(OpCodes.Throw);
        il.Emit(OpCodes.Ret);

        EmitterExtensions.emitDMDMethod = EmitterExtensions.emitDMD.Generate();
        EmitterExtensions.emitCodeDelegate = EmitterExtensions.emitDMDMethod
                .CreateDelegate<Action<CecilILGenerator, OpCode, object>>();
    }

    public static void Emit(this CecilILGenerator il, OpCode opcode, object operand)
    {
        EmitterExtensions.emitCodeDelegate(il, opcode, operand);
    }

    public static void MarkBlockBefore(this CecilILGenerator il, ExceptionBlock block)
    {
        switch (block.blockType)
        {
            case ExceptionBlockType.BeginExceptionBlock:
                il.BeginExceptionBlock();
                return;
            case ExceptionBlockType.BeginCatchBlock:
                il.BeginCatchBlock(block.catchType);
                return;
            case ExceptionBlockType.BeginExceptFilterBlock:
                il.BeginExceptFilterBlock();
                return;
            case ExceptionBlockType.BeginFaultBlock:
                il.BeginFaultBlock();
                return;
            case ExceptionBlockType.BeginFinallyBlock:
                il.BeginFinallyBlock();
                return;
            case ExceptionBlockType.EndExceptionBlock:
                return;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void MarkBlockAfter(this CecilILGenerator il, ExceptionBlock block)
    {
        if (block.blockType == ExceptionBlockType.EndExceptionBlock)
            il.EndExceptionBlock();
    }

    public static LocalBuilder GetLocal(this CecilILGenerator il, VariableDefinition varDef)
    {
        Dictionary<LocalBuilder, VariableDefinition> vars = (Dictionary<LocalBuilder, VariableDefinition>)AccessTools
            .Field(typeof(CecilILGenerator), "_Variables")
            .GetValue(il);

        LocalBuilder? loc = vars.FirstOrDefault(kv => kv.Value == varDef).Key;
        if (loc != null)
            return loc;
        // TODO: Remove once MonoMod allows to specify this manually
        Type type = varDef.VariableType.ResolveReflection();
        bool pinned = varDef.VariableType.IsPinned;
        int index = varDef.Index;
        loc = (LocalBuilder) (
            EmitterExtensions.c_LocalBuilder_params == 4 ? 
                EmitterExtensions.c_LocalBuilder.Invoke(new object?[] { index, type, null, pinned }) :
            EmitterExtensions.c_LocalBuilder_params == 3 ? 
                EmitterExtensions.c_LocalBuilder.Invoke(new object?[] { index, type, null }) :
            EmitterExtensions.c_LocalBuilder_params == 2 ?
                EmitterExtensions.c_LocalBuilder.Invoke(new object?[] { type, null }) :
            EmitterExtensions.c_LocalBuilder_params == 0 ?
                EmitterExtensions.c_LocalBuilder.Invoke(Array.Empty<object?>()) :
                throw new NotSupportedException()
        );

        EmitterExtensions.f_LocalBuilder_position?.SetValue(loc, (ushort)index);
        EmitterExtensions.f_LocalBuilder_is_pinned?.SetValue(loc, pinned);
        vars[loc] = varDef;
        return loc;
    }

    private static readonly ConstructorInfo c_LocalBuilder =
        typeof(LocalBuilder).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                            .OrderByDescending(c => c.GetParameters().Length).First();
    private static readonly FieldInfo f_LocalBuilder_position =
        typeof(LocalBuilder).GetField("position", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo f_LocalBuilder_is_pinned =
        typeof(LocalBuilder).GetField("is_pinned", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly int c_LocalBuilder_params = c_LocalBuilder.GetParameters().Length;
}
