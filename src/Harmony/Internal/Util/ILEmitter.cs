using System;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Utils;

namespace HarmonyLib.Internal.Util;

/// <summary>
/// Helper wrapper around ILProcessor to allow emitting code at certain positions
/// </summary>
internal sealed class ILEmitter
{
    public readonly ILProcessor IL;

    private readonly List<LabelledExceptionHandler> pendingExceptionHandlers;
    private readonly List<Label> pendingLabels;
    public Instruction? emitBefore;

    public ILEmitter(ILProcessor il)
    {
        this.IL = il;
        this.pendingExceptionHandlers = new();
        this.pendingLabels = new();
    }

    private Instruction Target => this.emitBefore ?? this.IL.Body.Instructions[this.IL.Body.Instructions.Count - 1];

    public ExceptionBlock BeginExceptionBlock(Label start)
    {
        return new ExceptionBlock()
        {
            start = start
        };
    }

    public void EndExceptionBlock(ExceptionBlock block)
    {
        this.EndHandler(block, block.cur);
    }

    public void BeginHandler(ExceptionBlock block, ExceptionHandlerType handlerType, Type? exceptionType = null)
    {
        LabelledExceptionHandler prev = block.prev = block.cur;
        if (prev != null)
            this.EndHandler(block, prev);

        block.skip = this.DeclareLabel();

        this.Emit(OpCodes.Leave, block.skip);

        Label handlerLabel = this.DeclareLabel();
        this.MarkLabel(handlerLabel);
        block.cur = new LabelledExceptionHandler()
        {
            tryStart = block.start,
            tryEnd = handlerLabel,
            handlerType = handlerType,
            handlerEnd = block.skip,
            exceptionType = exceptionType != null ? IL.Import(exceptionType) : null
        };
        if (handlerType == ExceptionHandlerType.Filter)
            block.cur.filterStart = handlerLabel;
        else
            block.cur.handlerStart = handlerLabel;
    }

    public void EndHandler(ExceptionBlock block, LabelledExceptionHandler handler)
    {
        switch (handler.handlerType)
        {
            case ExceptionHandlerType.Filter:
                Emit(OpCodes.Endfilter);
                break;
            case ExceptionHandlerType.Finally:
                Emit(OpCodes.Endfinally);
                break;
            default:
                Emit(OpCodes.Leave, block.skip);
                break;
        }

        MarkLabel(block.skip);
        pendingExceptionHandlers.Add(block.cur);
    }

    public VariableDefinition DeclareVariable(Type type)
    {
        var varDef = new VariableDefinition(IL.Import(type));
        IL.Body.Variables.Add(varDef);
        return varDef;
    }

    public Label DeclareLabel()
    {
        return new Label();
    }

    public Label DeclareLabelFor(Instruction ins)
    {
        return new Label
        {
            emitted = true,
            instruction = ins
        };
    }

    public void MarkLabel(Label label)
    {
        if (label.emitted)
            return;
        this.pendingLabels.Add(label);
    }

    public Instruction SetOpenLabelsTo(Instruction ins)
    {
        if (this.pendingLabels.Count != 0)
        {
            foreach (Label pendingLabel in this.pendingLabels)
            {
                foreach (Instruction targetIns in pendingLabel.targets)
                    if (targetIns.Operand is Instruction)
                        targetIns.Operand = ins;
                    else if (targetIns.Operand is Instruction[] targets)
                        for (var i = 0; i < targets.Length; i++)
                            if (targets[i] == pendingLabel.instruction)
                            {
                                targets[i] = ins;
                                break;
                            }

                pendingLabel.instruction = ins;
                pendingLabel.emitted = true;
            }

            this.pendingLabels.Clear();
        }

        if (this.pendingExceptionHandlers.Count != 0)
        {
            foreach (LabelledExceptionHandler exHandler in this.pendingExceptionHandlers)
                IL.Body.ExceptionHandlers.Add(new ExceptionHandler(exHandler.handlerType)
                {
                    TryStart = exHandler.tryStart?.instruction,
                    TryEnd = exHandler.tryEnd?.instruction,
                    FilterStart = exHandler.filterStart?.instruction,
                    HandlerStart = exHandler.handlerStart?.instruction,
                    HandlerEnd = exHandler.handlerEnd?.instruction,
                    CatchType = exHandler.exceptionType
                });

            this.pendingExceptionHandlers.Clear();
        }

        return ins;
    }

    public void Emit(OpCode opcode)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode)));
    }

    public void Emit(OpCode opcode, Label label)
    {
        Instruction ins = this.SetOpenLabelsTo(this.IL.Create(opcode, label.instruction));
        label.targets.Add(ins);
        this.IL.InsertBefore(this.Target, ins);
    }

    public void Emit(OpCode opcode, ConstructorInfo cInfo)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, this.IL.Import(cInfo))));
    }

    public void Emit(OpCode opcode, MethodInfo mInfo)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, this.IL.Import(mInfo))));
    }

    public void Emit(OpCode opcode, Type cls)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, this.IL.Import(cls))));
    }

    public void EmitUnsafe(OpCode opcode, object arg)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, arg)));
    }

    public void Emit(OpCode opcode, int arg)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, arg)));
    }

    public void Emit(OpCode opcode, string arg)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, arg)));
    }

    public void Emit(OpCode opcode, FieldInfo fInfo)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, this.IL.Import(fInfo))));
    }

    public void Emit(OpCode opcode, VariableDefinition varDef)
    {
        this.IL.InsertBefore(this.Target, SetOpenLabelsTo(this.IL.Create(opcode, varDef)));
    }

    public sealed class Label
    {
        public bool emitted;
        public Instruction instruction = Instruction.Create(OpCodes.Nop);
        public List<Instruction> targets = new();
    }

    public sealed class ExceptionBlock
    {
        public LabelledExceptionHandler prev = null!, cur = null!;
        public Label start = null!, skip = null!;
    }

    public sealed class LabelledExceptionHandler
    {
        public TypeReference? exceptionType;
        public ExceptionHandlerType handlerType;
        public Label tryStart = null!, tryEnd = null!, filterStart = null!, handlerStart = null!, handlerEnd = null!;
    }
}
