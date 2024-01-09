using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace MipInEx.Bootstrap;

internal sealed class TypeDefinitionReference : ICacheable
{
    private TypeDefinitionReference? declaringType;
    private string name;
    private string? @namespace;
    private int arity;

    public TypeDefinitionReference() : this(null, null, string.Empty, 0)
    { }

    public TypeDefinitionReference(string name) : this(null, null, name, 0)
    { }

    public TypeDefinitionReference(string name, int arity) : this(null, null, name, arity)
    { }

    public TypeDefinitionReference(string? @namespace, string name) : this(null, @namespace, name, 0)
    { }

    public TypeDefinitionReference(string? @namespace, string name, int arity) : this(null, @namespace, name, arity)
    { }

    public TypeDefinitionReference(TypeDefinitionReference declaringType, string name) : this(declaringType, declaringType.@namespace, name, 0)
    { }

    public TypeDefinitionReference(TypeDefinitionReference declaringType, string name, int arity) : this(declaringType, declaringType.@namespace, name, arity)
    { }

    private TypeDefinitionReference(
        TypeDefinitionReference? declaringType, 
        string? @namespace, 
        string name, 
        int arity)
    {
        this.declaringType = declaringType;
        this.name = name;
        this.@namespace = @namespace;
        this.arity = arity;
    }

    public TypeDefinitionReference? DeclaringType => this.declaringType;
    public string Name => this.name;
    public string? Namespace => this.@namespace;
    public int Arity => this.arity;

    public TypeDefinition? FetchTypeDefinition(AssemblyDefinition assemblyDefinition)
    {
        if (this.declaringType is not null)
        {
            TypeDefinition? declaringTypeDefinition = this.declaringType.FetchTypeDefinition(assemblyDefinition);
            if (declaringTypeDefinition is null)
            {
                return null;
            }

            foreach (TypeDefinition nestedTypeDefinition in declaringTypeDefinition.NestedTypes)
            {
                if (nestedTypeDefinition.Name == this.Name &&
                    nestedTypeDefinition.GenericParameters.Count == this.arity)
                {
                    return nestedTypeDefinition;
                }
            }

            return null;
        }

        foreach (TypeDefinition typeDefinition in assemblyDefinition.MainModule.Types)
        {
            if (typeDefinition.Name == this.name &&
                typeDefinition.GenericParameters.Count == this.arity)
            {
                return typeDefinition;
            }
        }

        return null;
    }

    public TypeDefinition? FetchTypeDefinition(IEnumerable<AssemblyDefinition> assemblyDefinitions)
    {
        if (this.declaringType is not null)
        {
            TypeDefinition? declaringTypeDefinition = this.declaringType.FetchTypeDefinition(assemblyDefinitions);
            if (declaringTypeDefinition is null)
            {
                return null;
            }

            foreach (TypeDefinition nestedTypeDefinition in declaringTypeDefinition.NestedTypes)
            {
                if (nestedTypeDefinition.Name == this.Name &&
                    nestedTypeDefinition.GenericParameters.Count == this.arity)
                {
                    return nestedTypeDefinition;
                }
            }

            return null;
        }

        foreach (AssemblyDefinition assemblyDefinition in assemblyDefinitions)
        {
            foreach (TypeDefinition typeDefinition in assemblyDefinition.MainModule.Types)
            {
                if (typeDefinition.Name == this.name &&
                    typeDefinition.GenericParameters.Count == this.arity)
                {
                    return typeDefinition;
                }
            }

            return null;
        }

        return null;
    }

    public Type? FetchType(Assembly assembly)
    {
        if (this.declaringType is not null)
        {
            Type? declaringType = this.declaringType.FetchType(assembly);
            if (declaringType is null)
            {
                return null;
            }

            foreach (Type nestedType in declaringType.GetNestedTypes())
            {
                if (nestedType.Name == this.Name &&
                    nestedType.GenericTypeArguments.Length == this.arity)
                {
                    return nestedType;
                }
            }

            return null;
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        foreach (Type type in types)
        {
            if (type.Name == this.name &&
                type.GenericTypeArguments.Length == this.arity)
            {
                return type;
            }
        }

        return null;
    }

    public Type? FetchType(IEnumerable<Assembly> assemblies)
    {
        if (this.declaringType is not null)
        {
            Type? declaringType = this.declaringType.FetchType(assemblies);
            if (declaringType is null)
            {
                return null;
            }

            foreach (Type nestedType in declaringType.GetNestedTypes())
            {
                if (nestedType.Name == this.Name &&
                    nestedType.GenericTypeArguments.Length == this.arity)
                {
                    return nestedType;
                }
            }

            return null;
        }

        foreach (Assembly assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types;
            }

            foreach (Type type in types)
            {
                if (type.Name == this.name &&
                    type.GenericTypeArguments.Length == this.arity)
                {
                    return type;
                }
            }

            return null;
        }

        return null;
    }

    public bool Matches(TypeDefinition typeDefinition)
    {
        TypeDefinition? declaringType = typeDefinition.DeclaringType;
        if (declaringType is not null)
        {
            if (this.declaringType is null ||
                !this.declaringType.Matches(declaringType))
            {
                return false;
            }
        }
        else
        {
            string? @namespace = typeDefinition.Namespace;
            if (string.IsNullOrEmpty(@namespace)) @namespace = null;

            if (this.@namespace != @namespace) return false;
        }


        return this.name == typeDefinition.Name &&
            this.arity == typeDefinition.GenericParameters.Count;
    }

    public bool Matches(Type type)
    {
        Type? declaringType = type.DeclaringType;
        if (declaringType != null)
        {
            if (this.declaringType is null || 
                !this.declaringType.Matches(declaringType))
            {
                return false;
            }
        }
        else if (this.@namespace != type.Namespace)
        {
            return false;
        }

        return this.name == type.Name &&
            this.arity == type.GenericTypeArguments.Length;
    }

    public void Load(BinaryReader binaryReader)
    {
        byte type = binaryReader.ReadByte();

        switch (type)
        {
            // global
            case 0:
                this.declaringType = null;
                this.@namespace = null;
                break;
            // namespaced
            case 1:
                this.declaringType = null;
                this.@namespace = binaryReader.ReadString();
                break;
            // nested
            case 2:
                this.declaringType ??= new();
                this.declaringType.Load(binaryReader);
                this.@namespace = this.declaringType.@namespace;
                break;
        }

        this.name = binaryReader.ReadString();
        this.arity = binaryReader.ReadInt32();
    }

    public void Save(BinaryWriter binaryWriter)
    {
        if (this.declaringType is not null)
        {
            // nested type
            binaryWriter.Write((byte)2);
            this.declaringType.Save(binaryWriter);
        }
        else if (this.@namespace is not null)
        {
            // namespaced type
            binaryWriter.Write((byte)1);
            binaryWriter.Write(this.@namespace);
        }
        else
        {
            // global type
            binaryWriter.Write((byte)0);
        }

        binaryWriter.Write(this.name);
        binaryWriter.Write(this.arity);
    }

    public static TypeDefinitionReference Create(Type type)
    {
        Type? declaringType = type.DeclaringType;
        TypeDefinitionReference? declaringTypeReference = declaringType == null ?
            null :
            TypeDefinitionReference.Create(declaringType);

        return new TypeDefinitionReference(declaringTypeReference, type.Namespace, type.Name, type.GenericTypeArguments.Length);
    }

    public static TypeDefinitionReference Create(TypeDefinition typeDefinition)
    {
        TypeDefinition? declaringType = typeDefinition.DeclaringType;
        TypeDefinitionReference? declaringTypeReference = declaringType is null ?
            null :
            TypeDefinitionReference.Create(declaringType);

        string? @namespace = typeDefinition.Namespace;
        if (string.IsNullOrEmpty(@namespace)) @namespace = null;

        return new TypeDefinitionReference(declaringTypeReference, @namespace, typeDefinition.Name, typeDefinition.GenericParameters.Count);
    }
}
