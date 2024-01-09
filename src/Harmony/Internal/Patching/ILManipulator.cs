using HarmonyLib.Internal.Util;
using HarmonyLib.Tools;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using MonoMod.Utils.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MethodBody = Mono.Cecil.Cil.MethodBody;
using SRE = System.Reflection.Emit;

namespace HarmonyLib.Internal.Patching;

/// <summary>
///    High-level IL code manipulator for MonoMod that allows to manipulate a method as a stream of CodeInstructions.
/// </summary>
internal sealed class ILManipulator
{
    private static readonly Dictionary<short, SRE.OpCode> SREOpCodes = new();
    private static readonly Dictionary<short, OpCode> CecilOpCodes = new();

    private static readonly Dictionary<SRE.OpCode, SRE.OpCode> ShortToLongMap = new()
    {
        [SRE.OpCodes.Beq_S] = SRE.OpCodes.Beq,
        [SRE.OpCodes.Bge_S] = SRE.OpCodes.Bge,
        [SRE.OpCodes.Bge_Un_S] = SRE.OpCodes.Bge_Un,
        [SRE.OpCodes.Bgt_S] = SRE.OpCodes.Bgt,
        [SRE.OpCodes.Bgt_Un_S] = SRE.OpCodes.Bgt_Un,
        [SRE.OpCodes.Ble_S] = SRE.OpCodes.Ble,
        [SRE.OpCodes.Ble_Un_S] = SRE.OpCodes.Ble_Un,
        [SRE.OpCodes.Blt_S] = SRE.OpCodes.Blt,
        [SRE.OpCodes.Blt_Un_S] = SRE.OpCodes.Blt_Un,
        [SRE.OpCodes.Bne_Un_S] = SRE.OpCodes.Bne_Un,
        [SRE.OpCodes.Brfalse_S] = SRE.OpCodes.Brfalse,
        [SRE.OpCodes.Brtrue_S] = SRE.OpCodes.Brtrue,
        [SRE.OpCodes.Br_S] = SRE.OpCodes.Br,
        [SRE.OpCodes.Leave_S] = SRE.OpCodes.Leave
    };

    private readonly IEnumerable<RawInstruction> codeInstructions;
    private readonly bool debug;
    private readonly Dictionary<VariableDefinition, SRE.LocalBuilder> localsCache = new();
    private readonly List<MethodInfo> transpilers = new();

    static ILManipulator()
    {
        foreach (FieldInfo field in typeof(SRE.OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            SRE.OpCode sreOpCode = (SRE.OpCode)field.GetValue(null);
            SREOpCodes[sreOpCode.Value] = sreOpCode;
        }

        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            OpCode cecilOpCode = (OpCode)field.GetValue(null);
            CecilOpCodes[cecilOpCode.Value] = cecilOpCode;
        }
    }

    /// <summary>
    ///    Initialize IL transpiler
    /// </summary>
    /// <param name="body">Body of the method to transpile</param>
    /// <param name="debug">Whether to always log everything for this instance</param>
    public ILManipulator(MethodBody body, bool debug)
    {
        this.Body = body;
        this.debug = debug;
        this.codeInstructions = this.ReadBody(this.Body);
    }

    public MethodBody Body { get; }

    private int GetTarget(MethodBody body, object insOp)
    {
        if (insOp is ILLabel lab)
            return body.Instructions.IndexOf(lab.Target);
        if (insOp is Instruction ins)
            return body.Instructions.IndexOf(ins);
        return -1;
    }

    private int[] GetTargets(MethodBody body, object instructionOps)
    {
        if (instructionOps is ILLabel[] labels)
        {
            return labels
                .Select(label => label.Target is null ?
                    -1 :
                    body.Instructions.IndexOf(label.Target))
                .ToArray();
        }

        if (instructionOps is Instruction[] instructions)
        {
            return instructions
                .Select(body.Instructions.IndexOf)
                .ToArray();
        }

        return Array.Empty<int>();
    }

    private IEnumerable<RawInstruction> ReadBody(MethodBody body)
    {
        List<RawInstruction> instructions = new(body.Instructions.Count);

        RawInstruction ReadInstruction(Instruction instruction)
        {
            return new RawInstruction(
                instruction: new CodeInstruction(SREOpCodes[instruction.OpCode.Value]),
                operand: instruction.OpCode.OperandType switch
                {
                    OperandType.InlineField    => ((MemberReference)instruction.Operand).ResolveReflection(),
                    OperandType.InlineMethod   => ((MemberReference)instruction.Operand).ResolveReflection(),
                    OperandType.InlineType     => ((MemberReference)instruction.Operand).ResolveReflection(),
                    OperandType.InlineTok      => ((MemberReference)instruction.Operand).ResolveReflection(),
                    OperandType.InlineVar      => (VariableDefinition)instruction.Operand,
                    OperandType.ShortInlineVar => (VariableDefinition)instruction.Operand,
                    // Handle Harmony's speciality of using smaller types for indices in ld/starg
                    OperandType.InlineArg           => (short)((ParameterDefinition)instruction.Operand).Index,
                    OperandType.ShortInlineArg      => (byte)((ParameterDefinition)instruction.Operand).Index,
                    OperandType.InlineBrTarget      => GetTarget(body, instruction.Operand),
                    OperandType.ShortInlineBrTarget => GetTarget(body, instruction.Operand),
                    OperandType.InlineSwitch        => GetTargets(body, instruction.Operand),
                    _                               => instruction.Operand
                },
                cilInstruction: instruction);
        }

        // Pass 1: Convert IL to base abstract CodeInstructions
        instructions.AddRange(body.Instructions.Select(ReadInstruction));

        //Pass 2: Resolve CodeInstructions for branch parameters
        foreach (RawInstruction unresolvedInstruction in instructions)
        {
            unresolvedInstruction.Operand = unresolvedInstruction.Instruction.opcode.OperandType switch
            {
                SRE.OperandType.ShortInlineBrTarget => instructions[(int)unresolvedInstruction.Operand].Instruction,
                SRE.OperandType.InlineBrTarget => instructions[(int)unresolvedInstruction.Operand].Instruction,
                SRE.OperandType.InlineSwitch => ((int[])unresolvedInstruction.Operand)
                    .Select(i => instructions[i].Instruction)
                    .ToArray(),
                _ => unresolvedInstruction.Operand
            };
        }

        // Pass 3: Attach exception blocks to each code instruction
        foreach (ExceptionHandler exception in body.ExceptionHandlers)
        {
            CodeInstruction tryStart = instructions[body.Instructions.IndexOf(exception.TryStart)].Instruction;
            CodeInstruction tryEnd = instructions[body.Instructions.IndexOf(exception.TryEnd)].Instruction;
            CodeInstruction handlerStart = instructions[body.Instructions.IndexOf(exception.HandlerStart)].Instruction;

            int handlerEndPos = exception.HandlerEnd == null ?
                instructions.Count - 1 :
                body.Instructions.IndexOf(exception.HandlerEnd.Previous);
            CodeInstruction handlerEnd = instructions[handlerEndPos].Instruction;

            tryStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            handlerEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));

            switch (exception.HandlerType)
            {
                case ExceptionHandlerType.Catch:
                    handlerStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock,
                        exception.CatchType.ResolveReflection()));
                    break;
                case ExceptionHandlerType.Filter:
                    CodeInstruction filterStart = instructions[body.Instructions.IndexOf(exception.FilterStart)].Instruction;
                    filterStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptFilterBlock));
                    break;
                case ExceptionHandlerType.Finally:
                    handlerStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
                    break;
                case ExceptionHandlerType.Fault:
                    handlerStart.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFaultBlock));
                    break;
            }
        }

        return instructions;
    }

    /// <summary>
    ///    Adds a transpiler method that edits the IL of the given method
    /// </summary>
    /// <param name="transpiler">Transpiler method</param>
    /// <exception cref="NotImplementedException">Currently not implemented</exception>
    public void AddTranspiler(MethodInfo transpiler)
    {
        this.transpilers.Add(transpiler);
    }

    private object[] GetTranspilerArguments(SRE.ILGenerator il,
                                            MethodInfo transpiler,
                                            IEnumerable<CodeInstruction> instructions,
                                            MethodBase? orignal = null)
    {
        List<object> result = new();

        foreach (ParameterInfo parameter in transpiler.GetParameters())
        {
            Type parameterType = parameter.ParameterType;

            if (parameterType.IsAssignableFrom(typeof(SRE.ILGenerator)))
                result.Add(il);
            else if (parameterType.IsAssignableFrom(typeof(MethodBase)) && orignal != null)
                result.Add(orignal);
            else if (parameterType.IsAssignableFrom(typeof(IEnumerable<CodeInstruction>)))
                result.Add(instructions);
        }

        return result.ToArray();
    }

    public IEnumerable<KeyValuePair<SRE.OpCode, object>> GetRawInstructions()
    {
        return this.codeInstructions
            .Select(i => new KeyValuePair<SRE.OpCode, object>(i.Instruction.opcode, i.Operand));
    }

    public List<CodeInstruction> GetInstructions(SRE.ILGenerator il, MethodBase? original = null)
    {
        return ILManipulator.NormalizeInstructions(
            this.ApplyTranspilers(
                il,
                original,
                vDef =>
                il.DeclareLocal(vDef.VariableType.ResolveReflection()),
                il.DefineLabel))
            .ToList();
    }

    private IEnumerable<CodeInstruction> ApplyTranspilers(SRE.ILGenerator il,
                                                          MethodBase? original,
                                                          Func<VariableDefinition, SRE.LocalBuilder> getLocal,
                                                          Func<SRE.Label> defineLabel)
    {
        // Step 1: Prepare labels for instructions. Use ToList to force
        List<CodeInstruction> instructions = this.Prepare(getLocal, defineLabel)
            .Select(i => i.Instruction)
            .ToList();

        if (this.transpilers.Count == 0)
            return instructions;

        // Step 2: Run the code instructions through transpilers
        IEnumerable<CodeInstruction> tempInstructions = ILManipulator.NormalizeInstructions(instructions);

        foreach (MethodInfo transpiler in this.transpilers)
        {
            object[] args = this.GetTranspilerArguments(il, transpiler, tempInstructions, original);

            Logger.Log(Logger.LogChannel.Info, () => $"Running transpiler {transpiler.FullDescription()}", debug);
            IEnumerable<CodeInstruction> newInstructions = (IEnumerable<CodeInstruction>)transpiler.Invoke(null, args);
            tempInstructions = ILManipulator.NormalizeInstructions(newInstructions).ToList();
        }

        return tempInstructions;
    }

    public Dictionary<int, CodeInstruction> GetIndexedInstructions(SRE.ILGenerator il)
    {
        static int Grow(ref int i, int s)
        {
            int result = i;
            i += s;
            return result;
        }

        int size = 0;
        return this.Prepare(
                vDef => il.DeclareLocal(vDef.VariableType.ResolveReflection()),
                il.DefineLabel)
            .ToDictionary(
                i => Grow(ref size, i.CILInstruction.GetSize()),
                i => i.Instruction);
    }

    private IEnumerable<RawInstruction> Prepare(Func<VariableDefinition, SRE.LocalBuilder> getLocal,
                                                Func<SRE.Label> defineLabel)
    {
        // First resolve all variables properly so that they are all defined in case of (st|ld)loc_N is used
        // This is especially useful
        this.localsCache.Clear();
        foreach (VariableDefinition variableDefinition in this.Body.Variables)
        {
            this.localsCache[variableDefinition] = getLocal(variableDefinition);
        }

        foreach (RawInstruction unresolvedInstruction in codeInstructions)
        {
            // Set operand to the same as the IL operand (in most cases they are the same)
            unresolvedInstruction.Instruction.operand = unresolvedInstruction.Operand;

            switch (unresolvedInstruction.Instruction.opcode.OperandType)
            {
                case SRE.OperandType.InlineVar:
                case SRE.OperandType.ShortInlineVar:
                    if (unresolvedInstruction.Operand is VariableDefinition varDef)
                    {
                        unresolvedInstruction.Instruction.operand = this.localsCache[varDef];
                    }

                    break;
                case SRE.OperandType.InlineSwitch when unresolvedInstruction.Operand is CodeInstruction[] targets:
                    {
                        List<SRE.Label> labels = new();
                        foreach (CodeInstruction target in targets)
                        {
                            SRE.Label label = defineLabel();
                            target.labels.Add(label);
                            labels.Add(label);
                        }

                        unresolvedInstruction.Instruction.operand = labels.ToArray();
                    }
                    break;
                case SRE.OperandType.ShortInlineBrTarget:
                case SRE.OperandType.InlineBrTarget:
                    {
                        if (unresolvedInstruction.Instruction.operand is CodeInstruction target)
                        {
                            SRE.Label label = defineLabel();
                            target.labels.Add(label);
                            unresolvedInstruction.Instruction.operand = label;
                        }
                    }
                    break;
            }
        }

        return codeInstructions;
    }

    /// <summary>
    ///    Processes and writes IL to the provided method body.
    ///    Note that this cleans the existing method body (removes insturctions and exception handlers).
    /// </summary>
    /// <param name="body">Method body to write to.</param>
    /// <param name="original">Original method that transpiler can optionally call into</param>
    /// <exception cref="NotSupportedException">
    ///    One of IL opcodes contains a CallSide (e.g. calli), which is currently not
    ///    fully supported.
    /// </exception>
    /// <exception cref="ArgumentNullException">One of IL opcodes with an operand contains a null operand.</exception>
    public void WriteTo(MethodBody body, MethodBase? original = null)
    {
        // Clean up the body of the target method
        body.Instructions.Clear();
        body.ExceptionHandlers.Clear();

        CecilILGenerator il = new(body.GetILProcessor());
        SRE.ILGenerator cil = il.GetProxy();

        // Define an "empty" label
        // In Harmony, the first label can point to the end of the method
        // Apparently, some transpilers naively call new Label() to define a label and thus end up
        // using the first label without knowing it
        // By defining the first label we'll ensure label count is correct
        il.DefineLabel();

        // Step 1: Apply transpilers
        // We don't remove trailing `ret`s because we need to do so only if prefixes/postfixes are present
        IEnumerable<CodeInstruction> newInstructions = this.ApplyTranspilers(cil, original, il.GetLocal, il.DefineLabel);

        // Step 2: Emit code
        foreach ((CodeInstruction cur, CodeInstruction next) in newInstructions.Pairwise())
        {
            cur.labels.ForEach(il.MarkLabel);
            cur.blocks.ForEach(il.MarkBlockBefore);

            // We need to handle exception handler opcodes specially because ILProcessor emits them automatically
            // Case 1: leave + start or end of exception block => ILProcessor generates leave automatically
            if ((cur.opcode == SRE.OpCodes.Leave || cur.opcode == SRE.OpCodes.Leave_S) &&
                (cur.blocks.Count > 0 || next?.blocks.Count > 0))
                goto mark_block;
            // Case 2: endfilter/endfinally and end of exception marker => ILProcessor will generate the correct end
            if ((cur.opcode == SRE.OpCodes.Endfilter || cur.opcode == SRE.OpCodes.Endfinally) && cur.blocks.Count > 0)
                goto mark_block;
            // Other cases are either intentional leave or invalid IL => let them be processed and let JIT generate correct exception

            // We don't replace `ret`s yet because we might not need to
            // We do that only if we add prefixes/postfixes
            // We also don't need to care for long/short forms thanks to Cecil/MonoMod

            // Temporary fix: CecilILGenerator doesn't properly handle ldarg
            switch (cur.opcode.OperandType)
            {
                case SRE.OperandType.InlineNone:
                    il.Emit(cur.opcode);
                    break;
                case SRE.OperandType.InlineSig:
                    throw new NotSupportedException(
                        "Emitting opcodes with CallSites is currently not fully implemented");
                default:
                    if (cur.operand == null)
                        throw new ArgumentNullException(nameof(cur.operand), $"Invalid argument for {cur}");

                    il.Emit(cur.opcode, cur.operand);
                    break;
            }

            mark_block:
            cur.blocks.ForEach(il.MarkBlockAfter);
        }

        // Special Harmony interop case: if no instructions exist, at least emit a quick return to attempt to get a valid method
        // Vanilla Harmony (almost) always emits a `ret` which allows for skipping original method by writing an empty transpiler
        if (body.Instructions.Count == 0)
            il.Emit(SRE.OpCodes.Ret);

        // Note: We lose all unassigned labels here along with any way to log them
        // On the contrary, we gain better logging anyway down the line by using Cecil
    }

    /// <summary>
    ///    Normalizes instructions into a consistent format for passing to transpilers.
    ///    Converts short branches to long, ensures that certain fields are properly initialized.
    /// </summary>
    /// <param name="instrs">Enumerable of instructions</param>
    /// <returns>Enumerable of normalized instructions</returns>
    private static IEnumerable<CodeInstruction> NormalizeInstructions(IEnumerable<CodeInstruction> instrs)
    {
        // Yes, we mutate original objects to save speed
        foreach (CodeInstruction ins in instrs)
        {
            // Ensure labels and blocks are initialized since some transpilers set them to null
            ins.labels ??= new List<SRE.Label>();
            ins.blocks ??= new List<ExceptionBlock>();
            // Do short -> long conversion for Harmony 2 compat
            if (ILManipulator.ShortToLongMap.TryGetValue(ins.opcode, out SRE.OpCode longOpCode))
                ins.opcode = longOpCode;
            yield return ins;
        }
    }

    public static Dictionary<int, CodeInstruction>? GetInstructions(MethodBody? body)
    {
        if (body == null)
            return null;

        try
        {
            return new ILManipulator(body, false)
                .GetIndexedInstructions(PatchProcessor.CreateILGenerator());
        }
        catch (Exception e)
        {
            Logger.Log(Logger.LogChannel.Warn,
                () => $"Could not read instructions of {body.Method.GetID()}: {e.Message}");
            return null;
        }
    }

    private sealed class RawInstruction
    {
        public CodeInstruction Instruction { get; set; }
        public object Operand { get; set; }
        public Instruction CILInstruction { get; set; }

        public RawInstruction(CodeInstruction instruction, object operand, Instruction cilInstruction)
        {
            this.Instruction = instruction;
            this.Operand = operand;
            this.CILInstruction = cilInstruction;
        }
    }
}
