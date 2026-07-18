using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization;
using eQuantic.Linq.Expressions.Runtime;

namespace eQuantic.Linq.Expressions.Resolution;

/// <summary>
/// Emits runtime types that stand in for compiler-generated anonymous types, so projections such as
/// <c>x =&gt; new { x.Id, x.Name }</c> can be rebuilt in a process where the original anonymous type does not exist.
/// Emitted types replicate anonymous-type semantics: ordered read-only properties, a positional constructor,
/// structural <see cref="object.Equals(object)"/>/<see cref="object.GetHashCode"/> and anonymous-style <see cref="object.ToString"/>.
/// </summary>
internal sealed class AnonymousTypeFactory
{
    internal const string AssemblyName = "eQuantic.Linq.Expressions.Anonymous";

    /// <summary>Process-wide factory so identical shapes map to the same emitted type.</summary>
    public static AnonymousTypeFactory Shared { get; } = new();

    private readonly ConcurrentDictionary<string, Lazy<Type>> _cache = new();
    private readonly Lazy<ModuleBuilder> _module;
    private int _typeCount;

    public AnonymousTypeFactory()
    {
        _module = new Lazy<ModuleBuilder>(static () =>
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(AssemblyName), AssemblyBuilderAccess.Run);
            return assembly.DefineDynamicModule(AssemblyName);
        });
    }

    /// <summary>Whether the given type was emitted by this factory.</summary>
    public static bool IsGenerated(Type type) => type.Assembly.GetName().Name == AssemblyName;

    /// <summary>Detects compiler-generated anonymous types (C# and VB).</summary>
    public static bool IsAnonymous(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        && type.Name.Contains("AnonymousType")
        && (type.Name.StartsWith("<>", StringComparison.Ordinal) || type.Name.StartsWith("VB$", StringComparison.Ordinal));

    /// <summary>Number of distinct shapes emitted so far.</summary>
    public int EmittedCount => _typeCount;

    /// <summary>Gets or emits a type matching the given ordered property shape.</summary>
    /// <param name="properties">Ordered property shape.</param>
    /// <param name="maxEmittedTypes">Optional cap on distinct emitted shapes (cache hits are always allowed).</param>
    public Type GetOrCreate(IReadOnlyList<KeyValuePair<string, Type>> properties, int? maxEmittedTypes = null)
    {
        var key = BuildKey(properties);

        if (maxEmittedTypes is { } cap && !_cache.ContainsKey(key) && _typeCount >= cap)
        {
            throw new TypeResolutionException(
                $"The anonymous-type emission cap ({cap}) was reached; refusing to materialize a new shape. " +
                "Raise TypeResolutionOptions.MaxAnonymousTypes if this workload legitimately needs more shapes.");
        }

        var lazy = _cache.GetOrAdd(key, _ => new Lazy<Type>(() => Emit(properties)));
        return lazy.Value;
    }

    private static string BuildKey(IReadOnlyList<KeyValuePair<string, Type>> properties)
    {
        var builder = new StringBuilder();
        foreach (var property in properties)
        {
            builder.Append(property.Key)
                .Append('|')
                .Append(property.Value.AssemblyQualifiedName)
                .Append(';');
        }

        return builder.ToString();
    }

    private Type Emit(IReadOnlyList<KeyValuePair<string, Type>> properties)
    {
        var index = Interlocked.Increment(ref _typeCount);
        var typeBuilder = _module.Value.DefineType(
            $"{AssemblyName}.AnonymousType{index}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.BeforeFieldInit,
            typeof(object));

        var count = properties.Count;
        var fields = new FieldBuilder[count];
        var parameterTypes = new Type[count];

        for (var i = 0; i < count; i++)
        {
            parameterTypes[i] = properties[i].Value;
            fields[i] = typeBuilder.DefineField(
                $"_{CamelCase(properties[i].Key)}",
                properties[i].Value,
                FieldAttributes.Private | FieldAttributes.InitOnly);
        }

        EmitConstructor(typeBuilder, properties, fields, parameterTypes);

        for (var i = 0; i < count; i++)
        {
            EmitProperty(typeBuilder, properties[i].Key, properties[i].Value, fields[i]);
        }

        EmitRuntimeDelegation(typeBuilder, nameof(object.Equals), typeof(bool), [typeof(object)], nameof(AnonymousTypeRuntime.ObjectsEqual));
        EmitRuntimeDelegation(typeBuilder, nameof(object.GetHashCode), typeof(int), Type.EmptyTypes, nameof(AnonymousTypeRuntime.HashOf));
        EmitRuntimeDelegation(typeBuilder, nameof(object.ToString), typeof(string), Type.EmptyTypes, nameof(AnonymousTypeRuntime.Render));

        return typeBuilder.CreateTypeInfo()!.AsType();
    }

    private static void EmitConstructor(
        TypeBuilder typeBuilder,
        IReadOnlyList<KeyValuePair<string, Type>> properties,
        FieldBuilder[] fields,
        Type[] parameterTypes)
    {
        var constructor = typeBuilder.DefineConstructor(
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            parameterTypes);

        // Parameter names must match property names so System.Text.Json can bind the constructor.
        for (var i = 0; i < properties.Count; i++)
        {
            constructor.DefineParameter(i + 1, ParameterAttributes.None, CamelCase(properties[i].Key));
        }

        var jsonConstructor = typeof(JsonConstructorAttribute).GetConstructor(Type.EmptyTypes)!;
        constructor.SetCustomAttribute(new CustomAttributeBuilder(jsonConstructor, []));

        var il = constructor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);

        for (var i = 0; i < fields.Length; i++)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg, i + 1);
            il.Emit(OpCodes.Stfld, fields[i]);
        }

        il.Emit(OpCodes.Ret);
    }

    private static void EmitProperty(TypeBuilder typeBuilder, string name, Type type, FieldBuilder field)
    {
        var property = typeBuilder.DefineProperty(name, PropertyAttributes.None, type, Type.EmptyTypes);
        var getter = typeBuilder.DefineMethod(
            $"get_{name}",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
            type,
            Type.EmptyTypes);

        var il = getter.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);

        property.SetGetMethod(getter);
    }

    private static void EmitRuntimeDelegation(TypeBuilder typeBuilder, string methodName, Type returnType, Type[] parameterTypes, string runtimeMethod)
    {
        var method = typeBuilder.DefineMethod(
            methodName,
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            returnType,
            parameterTypes);

        var target = typeof(AnonymousTypeRuntime).GetMethod(runtimeMethod, BindingFlags.Public | BindingFlags.Static)!;

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        if (parameterTypes.Length == 1)
        {
            il.Emit(OpCodes.Ldarg_1);
        }

        il.Emit(OpCodes.Call, target);
        il.Emit(OpCodes.Ret);
    }

    private static string CamelCase(string name)
    {
        if (name.Length == 0 || char.IsLower(name[0]))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
